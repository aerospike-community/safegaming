using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common;
using Common.Patterns.Tasks;
using Common.File;
using System.Threading;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Collections.Concurrent;
using GameSimulator;

namespace PlayerCommon
{
    public partial class Program
    {
        public static Action InitializationAction = () => { };
        public static Action PreConsoleDisplayAction = delegate { };
        public static Func<ConsoleDisplay, ConsoleDisplay, ConsoleDisplay, IDBConnectionSim> CreateDBConnection = null;
        public static Action PostConsoleDisplayAction = delegate { };

        static readonly string[] Games = new string[] { "Slots", "Roulette", "Roulette", "Slots", "Slots", "Roulette", "Roulette", "Slots", };
        static readonly ConcurrentBag<Task> LiveFireForgetTasks = new();

        public async static Task Main(string[] args)
        {
            #region Initialization

            var settings = CreateAppSettingsInstance(null);
            var logFile = InitialazationLogs(args);
            InitialazationArguments(args, new ConsoleArgumentsSim(SettingsSim.Instance));
            InitialazationConfig();
            InitialazationEventPref();

            InitializationAction?.Invoke();

            #endregion

            #region Console Display Setup

            ConsoleDisplay.Console.ClearScreen();
            ConsoleDisplay.Console.AdjustScreenStartBlock();

            ConsoleDisplay.Console.WriteLine(" ");
#if DEBUG
            ConsoleDisplay.Console.WriteLine("**Debug Version**");
            ConsoleDisplay.Console.WriteLine(" ");
#endif
            if (SettingsSim.Instance.Config.UpdateDB)
            {
                ConsoleDisplay.Console.WriteLine($"Updating {DBConnection.GetInfo().dbName ?? "<ERROR>"} on Host \"{SettingsSim.Instance.DBConnectionString}\"");
            }
            else
                ConsoleDisplay.Console.WriteLine("DB will NOT be Updated");

            if (SettingsSim.Instance.Config.TruncateSets)
            {
                ConsoleDisplay.Console.WriteLine($"Will be Truncating...");
            }

            {
                ThreadPool.GetAvailableThreads(out int currWorker, out int currCompletionPort);
                ConsoleDisplay.Console.WriteLine("Working Threads: {0} Completion Port Threads: {1}",
                                                    currWorker, currCompletionPort);
            }

            ConsoleDisplay.Console.WriteLine("MaxDegreeOfParallelism: Generation {0}",
                        Settings.Instance.MaxDegreeOfParallelism);
            ConsoleDisplay.Console.WriteLine("Generating Players: {0} Player Id Start: {1}",
                        SettingsSim.Instance.Config.NbrPlayers,
                        SettingsSim.Instance.Config.PlayerIdStartRange);

            PreConsoleDisplayAction?.Invoke();

            ConsoleDisplay.Console.WriteLine("Ignore Faults: {0}", Settings.Instance.IgnoreFaults);

            {
                var currentTimeUTC = DateTimeOffset.UtcNow;
                //Logger.Instance.InfoFormat("Start Time Local: {0} UTC: {1}",
                //                                DateTimeOffset.Now,
                //                                currentTimeUTC);
                ConsoleDisplay.Console.WriteLine("Local: {0} UTC: {1}",
                                                    currentTimeUTC.LocalDateTime,
                                                    currentTimeUTC);
            }
            if (SettingsSim.Instance.Config.SleepBetweenTransMS > 0)
            {
                Logger.Instance.WarnFormat("Sleeping between Transactions for {0} ms", SettingsSim.Instance.Config.SleepBetweenTransMS);
                var consoleColor1 = System.Console.ForegroundColor;
                try
                {
                    System.Console.ForegroundColor = ConsoleColor.Red;
                    ConsoleDisplay.Console.WriteLine("Warning: Sleeping between Transactions for {0} ms", SettingsSim.Instance.Config.SleepBetweenTransMS);
                }
                finally
                {
                    System.Console.ForegroundColor = consoleColor1;
                }                
            }
            if (SettingsSim.Instance.Config.EnableRealtime.HasValue && SettingsSim.Instance.Config.EnableRealtime.Value)
            {
                Logger.Instance.WarnFormat("Real Time Enabled");

                var consoleColor1 = System.Console.ForegroundColor;
                try
                {                    
                    System.Console.ForegroundColor = ConsoleColor.Red;
                    
                    ConsoleDisplay.Console.WriteLine("Warning: Real Time Enabled");
                }
                finally
                {
                    System.Console.ForegroundColor = consoleColor1;
                }                
            }
            if (SettingsSim.Instance.Config.ContinuousSessions)
            {
                Logger.Instance.WarnFormat("Continues Session Enabled");

                var consoleColor1 = System.Console.ForegroundColor;
                try
                {
                    System.Console.ForegroundColor = ConsoleColor.Red;

                    ConsoleDisplay.Console.WriteLine("Warning: Continues Session Enabled");
                }
                finally
                {
                    System.Console.ForegroundColor = consoleColor1;
                }
            }

            if (SyncMode)
            {
                var consoleColor1 = System.Console.ForegroundColor;
                try
                {
                    System.Console.ForegroundColor = ConsoleColor.Red;
                    ConsoleDisplay.Console.WriteLine("Synchronous Mode");
                }
                finally
                {
                    System.Console.ForegroundColor = consoleColor1;
                }
                ConsoleDisplay.Console.WriteLine();
                Logger.Instance.Warn("Synchronous Mode");
            }

            ConsoleDisplay.Console.WriteLine(" ");

            ConsoleDisplay.IncludeRunningTime = true;

            ConsoleGenerating = new ConsoleDisplay("Generating Completed: {completed} Working: {working} Task: {tag} {task}", reserveLines: 1, takeStartBlock: false);
            ConsoleGeneratingTrans = new ConsoleDisplay("Trans Completed: {completed} Working: {working} Task: {tag} {task}", reserveLines: 1, takeStartBlock: false);
            ConsolePuttingDB = new ConsoleDisplay("Updating DB Completed: {completed} Working: {working} Task: {tag} {task}", reserveLines: 1, takeStartBlock: false);
            ConsolePuttingPlayer = new ConsoleDisplay("Updating Player Completed: {completed} Working: {working} Task: {tag} {task}", reserveLines: 1, takeStartBlock: false);
            ConsolePuttingHistory = new ConsoleDisplay("Updating History Completed: {completed} Working: {working} Task: {tag} {task}", reserveLines: 1, takeStartBlock: false);
            ConsoleSleep = new ConsoleDisplay("Sleep: {completed} Working: {working} Task: {tag} {task}", reserveLines: 1, takeStartBlock: false);
            ConsoleFileWriting = new ConsoleDisplay("Writing File: {completed} Working: {working} Task: {tag} {task}", reserveLines: 1, takeStartBlock: false);
            ConsoleWarnings = new ConsoleDisplay("Warnings: {working} Last: {tag}", reserveLines: 1, takeStartBlock: false);
            ConsoleErrors = new ConsoleDisplay("Errors: {working} Last: {tag}", reserveLines: 1, takeStartBlock: false);
            ConsoleExceptions = new ConsoleDisplay("Exceptions: {working} Last: {tag}", reserveLines: 1, takeStartBlock: false);

            //GCMonitor.GetInstance().StartGCMonitoring();

            ConsoleDisplay.Initialze(adjustScreenSize: false,
                                        additionalConsoleInitialize:
                                            (console, tag) =>
                                            {
                                                console.ReserveRwWriteConsoleSpace(tag, 2, -1, autoSizeConsoleWindow: true);
                                                console.ReserveRwWriteConsoleSpace("Prompt", 2, -1, autoSizeConsoleWindow: true);
                                                console.AdjustScreenToFitBasedOnStartBlock();
                                            });
            ConsoleDisplay.Start(false);

#endregion

            #region Creation connection and read required data

            var logFilePath = Logger.GetSetEnvVarLoggerFile();

            using IDBConnectionSim dbConnection = SettingsSim.Instance.Config.UpdateDB
                                                    ? CreateDBConnection?.Invoke(ConsolePuttingDB,
                                                                                    ConsolePuttingPlayer,
                                                                                    ConsolePuttingHistory)
                                                    : null;
            State[] stateDB;

            {
                Logger.Instance.Debug("Main Reading State/County JSON DetailFile");

                using var readingJsonProg = new Progression(ConsoleGenerating, "State", "Reading State Json DetailFile");

                var stateDBPath = BaseFile.Make(SettingsSim.Instance.Config.StateJsonFile);

                stateDB = Helpers.FromJson<State[]>(stateDBPath.ReadAllText());

                Logger.Instance.DebugFormat("Main Read State/County JSON DetailFile {0}", stateDB.Length);
            }


            using var countyProg = new Progression(ConsoleGenerating, "County", "Determining Player Counties");
            
            Logger.Instance.Debug("Main Processing State/County into only gaming Counties by State");

            var onlyGamingCountiesByState = (from state in stateDB
                                             where state.FIPSCode > 0 && state.Counties.Any(c => c.OnlineGaming)
                                                        && (SettingsSim.Instance.Config.OnlyTheseGamingStates == null
                                                                || SettingsSim.Instance.Config.OnlyTheseGamingStates.Count == 0
                                                                || SettingsSim.Instance.Config.OnlyTheseGamingStates.Contains(state.Name))
                                             orderby state.Name
                                             select new
                                             {
                                                 state.Name,
                                                 state.AreaLandSqMeters,
                                                 AreaWaterSqMeters = state.AreaWaterSqMiles,
                                                 state.AreaWaterSqMiles,
                                                 state.AreaLandSqMiles,
                                                 state.HousingCount,
                                                 state.PopulationCount,
                                                 state.FIPSCode,
                                                 MaxHousingCounty = state.Counties.Where(c => c.OnlineGaming).Max(c => c.HousingCount),
                                                 MinHousingCounty = state.Counties.Where(c => c.OnlineGaming).Min(c => c.HousingCount),
                                                 Counties = state.Counties.Where(c => c.OnlineGaming)
                                             }).ToArray();

            if (Logger.Instance.IsDebugEnabled)
                Logger.Instance.Dump(onlyGamingCountiesByState,
                                        dumpType: Logger.DumpType.Debug,
                                        comments: "Main Only using these States/Counties");

            if(onlyGamingCountiesByState.Length == 0)
            {
                Logger.Instance.Error("Main No States/Counties could be processed. Existing the application.");
                Logger.Instance.Flush();
                Environment.Exit(-1);
            }
           
            countyProg.Decrement();

            if (dbConnection != null && SettingsSim.Instance.Config.TruncateSets)
            {
                dbConnection.Truncate();
            }

            await InterventionThresholds.Initialize(dbConnection, cancellationTokenSource.Token);
            DateTimeSimulation.Initialize();

            #endregion

            var maxDegreeOfParallelism = Settings.Instance.MaxDegreeOfParallelism;

            if (SyncMode)
                maxDegreeOfParallelism = 1;
            else if (SettingsSim.Instance.Config.ContinuousSessions)
            {
                maxDegreeOfParallelism = SettingsSim.Instance.Config.NbrPlayers;
            }

            var parallelOptions = new ParallelOptions()
            {
                CancellationToken = cancellationTokenSource.Token,
                MaxDegreeOfParallelism = maxDegreeOfParallelism
            };

            Logger.Instance.Dump(parallelOptions,
                                    Logger.DumpType.Info);
            Logger.Instance.Dump(parallelOptions.TaskScheduler,
                                    Logger.DumpType.Info);

            var enableDBUpdate = dbConnection != null;

            var startPlayerProcessingTime = Stopwatch.StartNew();
            Logger.Instance.InfoFormat("Main Starting Player Processing {0} Generation",
                                            SettingsSim.Instance.Config.NbrPlayers);

            var idxs = new int[SettingsSim.Instance.Config.NbrPlayers];
            Player.CurrentPlayerId = SettingsSim.Instance.Config.PlayerIdStartRange - 1;
            
            for(int i = 0; i < idxs.Length; i++) idxs[i] = i;
            
            int actualPlayersProcessed = 0;

            await Parallel.ForEachAsync(idxs, parallelOptions,
               async (idx, cancellationToken) =>
                {
                    if (Logger.Instance.IsDebugEnabled)
                        Logger.Instance.DebugFormat("Main Player {0}", idx);

                    #region Random Property Init
                    var random = new Random(Guid.NewGuid().GetHashCode());

                    var first = Faker.Name.First();
                    var last = Faker.Name.Last();
                    var domain = Faker.Internet.DomainName();
                    string email;

                    if (SettingsSim.Instance.Config.GenerateUniqueEmails)
                    {
                        if(dbConnection == null || !dbConnection.UsedEmailCntEnabled)
                        {
                            var emailNbr = random.Next(0,99);
                            if(emailNbr > 1)
                                email = $"{last}{first}{emailNbr}@{domain}";
                            else
                                email = $"{first}.{last}@{domain}";
                        }
                        else
                        {
                            email = dbConnection.DeterineEmail(first, last, domain, cancellationTokenSource.Token).Result;
                        }
                    }
                    else
                        email = $"{first}.{last}@{domain}";

                    ConsoleGenerating.Increment("Player " + email);

                    //var state =  Faker.Address.UsStateAbbr();
                    var stateIdx = random.Next(0, onlyGamingCountiesByState.Length - 1);
                    //var stateRec = stateDB.FirstOrDefault(s => s.Name == state);
                    var stateRec = onlyGamingCountiesByState[stateIdx];
                    var state = stateRec.Name;
                    //var housing = random.Next(1000, 10000000);
                    var housing = random.Next((int)stateRec.MinHousingCounty, (int)stateRec.MaxHousingCounty);
                    var countyRecs = stateRec.Counties
                                        .Where(s => housing >= s.HousingCount);
                    var countyIdx = random.Next(0, countyRecs.Count() - 1);
                    var countyRec = countyRecs.ElementAt(countyIdx);
                    var county = countyRec?.Name;
                    var bingeFlag = false;

                    if (string.IsNullOrEmpty(county))
                    {
                        countyIdx = random.Next(0, stateRec.Counties.Count() - 1);

                        countyRec = stateRec.Counties.ElementAt(countyIdx);
                    }

                    Tiers valueTier;

                    switch (countyRec.HouseIncomeTier)
                    {
                        case Tiers.VeryHigh:
                        case Tiers.High:
                            {
                                var probValue = random.Next(0, 1000);
                                if (probValue <= 1)
                                    valueTier = Tiers.VeryHigh;
                                else if (probValue <= 39)
                                    valueTier = Tiers.High;
                                else if (probValue <= 200)
                                    valueTier = Tiers.Medium;
                                else
                                    valueTier = Tiers.Low;
                            }
                            break;
                        case Tiers.Medium:
                            {
                                var probValue = random.Next(0, 10000);
                                if (probValue <= 5)
                                    valueTier = Tiers.VeryHigh;
                                else if (probValue <= 195)
                                    valueTier = Tiers.High;
                                else if (probValue <= 1800)
                                    valueTier = Tiers.Medium;
                                else
                                    valueTier = Tiers.Low;
                            }
                            break;
                        case Tiers.Low:
                            {
                                var probValue = random.Next(0, 100000);
                                if (probValue <= 25)
                                    valueTier = Tiers.VeryHigh;
                                else if (probValue <= 975)
                                    valueTier = Tiers.High;
                                else if (probValue <= 13000)
                                    valueTier = Tiers.Medium;
                                else
                                    valueTier = Tiers.Low;
                            }
                            break;
                        default:
                            valueTier = Tiers.Medium;
                            break;
                    }
                    
                    {
                        var bingeFlagChance = random.Next(1, 100);

                        bingeFlag = bingeFlagChance <= 3;
                    }

                    DateTimeSimulation datetimeHistory;

                    if (DateTimeSimulation.InitialType == DateTimeSimulation.Types.RealTime)                        
                        datetimeHistory = DateTimeRealTime.GenerateDateTime(countyRec.TZOffsets.FirstOrDefault());
                    else
                        datetimeHistory = DateTimeHistory.GenerateDateTime(countyRec.TZOffsets.FirstOrDefault());                    

                    #endregion

                        var player = new Player($"{last}.{first}",
                                                first,
                                                last,
                                                email,
                                                "US",
                                                state,
                                                countyRec,
                                                valueTier,
                                                0,
                                                random.Next(0, 36),
                                                datetimeHistory,
                                                InterventionThresholds.Instance,
                                                bingeFlag);
                    
                    if (Logger.Instance.IsDebugEnabled)
                        Logger.Instance.DebugFormat("Main Generated Player {0} {1}", idx, player.PlayerId);

                    var continuePlay = false;
                    {
                        await RanSession(dbConnection,
                                            enableDBUpdate,
                                            continuePlay,
                                            player,
                                            cancellationToken);
                        continuePlay = true;                        
                    } while (SettingsSim.Instance.Config.ContinuousSessions) ;

                    Interlocked.Increment(ref actualPlayersProcessed);
                    ConsoleGenerating.Decrement("Player " + email);
                });

            Task.WaitAll(LiveFireForgetTasks.ToArray());

            startPlayerProcessingTime.Stop();
            ConsolePuttingDB.TaskEndAll();
            dbConnection?.Dispose();

            var histogramOutput = WritePrefFiles();
            
            #region Terminate

            var playerRate = (decimal)actualPlayersProcessed / (decimal)startPlayerProcessingTime.Elapsed.TotalSeconds;

            Logger.Instance.Info($"{Common.Functions.Instance.ApplicationName} Main End. Processed {actualPlayersProcessed} Players in {startPlayerProcessingTime.Elapsed} (rate {playerRate:###,##0.000} players/sec).");

            Logger.Instance.Flush(5000);

            ConsoleDisplay.End();
            //GCMonitor.GetInstance().StopGCMonitoring();

            ConsoleDisplay.Console.SetReWriteToWriterPosition();

            ConsoleDisplay.Console.WriteLine(" ");
            ConsoleDisplay.Console.WriteLine("Completed {0} Players in {1} (rate {2:###,##0.000} players/sec)",
                                                   actualPlayersProcessed,
                                                   startPlayerProcessingTime.Elapsed,
                                                   playerRate);

            PostConsoleDisplayAction?.Invoke();

            Terminate(histogramOutput, logFilePath);

            #endregion
        }

        private static decimal CreateDespositAmt(Player player, Random random)
        {            
            return player.ValueTier switch
            {
                Tiers.VeryHigh =>
                    random.Next(5000, 25000),
                Tiers.High =>
                    random.Next(1000, 5000),
                Tiers.Medium =>
                    random.Next(100, 1000),
                Tiers.Low =>
                    random.Next(50, 100),
                _ =>
                    random.Next(10, 1000),
            };
        }

        private static async Task RanSession(IDBConnectionSim dbConnection,
                                                bool enableDBUpdate,
                                                bool continuePlay,
                                                Player player,
                                                CancellationToken cancellationToken)
        {
            ConsoleGenerating.Increment("Session " + player.UserName);

            var session = player.CreateSession(true, continuePlay);
            var random = new Random(new Guid().GetHashCode());
            var determineGame = random.Next(0, Games.Length - 1);
            var game = Games[determineGame];

            player.AddFinancialTransaction(new FinTransaction(FinTransaction.Types.Deposit,
                                                                    CreateDespositAmt(player, random),
                                                                    player.UseTime.Current));

            if (player.UseTime.IsRealtime)
            {
                await dbConnection.UpdateCurrentPlayers(player, true, cancellationToken);
            }

            await NewGame(player,
                            session,
                            game,
                            dbConnection,
                            cancellationToken);

            var totalSession = random.Next(Math.Min(player.Metrics.AvgSessionsPerDay, SettingsSim.Instance.Config.MinPlayerSessions),
                                            Math.Max(player.Metrics.AvgSessionsPerDay, SettingsSim.Instance.Config.MaxPlayerSessions));
            var minBet = Math.Max(Roulette.MinimumWager, Slots.MinimumWager);


            for (int sessionIdx = 1; sessionIdx < totalSession;)
            {                
                if (player.Metrics.CurrentBalance < minBet)
                {
                    session = player.CreateSession(false);

                    player.AddFinancialTransaction(new FinTransaction(FinTransaction.Types.Deposit,
                                                                        CreateDespositAmt(player, random),
                                                                        player.UseTime.Current));
                    ++sessionIdx;
                }
                else if (determineGame % 2 == 0)
                {
                    session = player.CreateSession(false);
                    ++sessionIdx;
                }

                determineGame = random.Next(0, Games.Length - 1);
                game = Games[determineGame];

                await NewGame(player,
                                session,
                                game,
                                dbConnection,
                                cancellationToken);
            }

            player.CloseSession(false, true);

            player.Completed();

            if (player.UseTime.IsRealtime)
            {
                await dbConnection.UpdateChangedCurrentPlayer(player, cancellationTokenSource.Token,
                                                                updateSession: true);
            }

            if (Logger.Instance.IsDebugEnabled)
                Logger.Instance.DebugFormat("Main Finished Player Generation of Session/Trans/History {0}", player.PlayerId);

            if (enableDBUpdate && player.UseTime.Ishistoric)
            {
                await dbConnection.UpdateCurrentPlayers(player, true, cancellationToken);
            }

            await InterventionThresholds.RefreshCheck(dbConnection, cancellationToken);

            ConsoleGenerating.Decrement("Session " + player.UserName);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="player"></param>
        /// <param name="session"></param>
        /// <param name="game"></param>
        /// <param name="sessionProgression"></param>
        /// <returns>
        /// Resulting wager from the game (win/loss)
        /// </returns>
        private static async Task NewGame(Player player,
                                            Session session,
                                            string game,
                                            IDBConnectionSim dbConnection,
                                            CancellationToken token)
        {                        
            var random = new Random(Guid.NewGuid().GetHashCode());

            var maxTrans = random.Next(SettingsSim.Instance.Config.MinTransPerSession,
                                        SettingsSim.Instance.Config.MaxTransPerSession);
            var gameInstance = new Game(game);

            player.NewGame(gameInstance);

            if(player.UseTime.IsRealtime)
            {
                await dbConnection.UpdateChangedCurrentPlayer(player, token,
                                                                updateSession: true,
                                                                updateGame: true,
                                                                updateFin: 1);
            }

            for (int i = 0; i < maxTrans; i++)
            {
                ConsoleGeneratingTrans.Increment("Transactions " + player.UserName);

                WagerResultTransaction.Types wagerType = WagerResultTransaction.Types.Wager;
                decimal wagerAmt = player.CalculateWager(random);

                {
                    var (endSession, reDeposit) = player.Session.CheckEndTriggerBalance(wagerAmt, random);

                    if (endSession)
                        break;
                    if (reDeposit)
                    {
                        player.AddFinancialTransaction(new FinTransaction(FinTransaction.Types.Deposit,
                                                                            CreateDespositAmt(player, random),
                                                                            player.UseTime.Current));

                        if (player.UseTime.IsRealtime)
                        {
                            await dbConnection.UpdateChangedCurrentPlayer(player, token,
                                                                           updateFin: 1);
                        }
                    }
                }

                player.UseTime.PlayDelayIncrement();

                var wager = new WagerResultTransaction(game,
                                                        null,
                                                        wagerType,
                                                        wagerAmt,                                                                            
                                                        0,
                                                        0,
                                                        false,
                                                        player.UseTime.Current,
                                                        player.PlayerId);
                wager.UpdateTimeBuckets(player.State, player.County);
                player.NewWagerResultTransaction(wager);
               
                player.UseTime.BetIncrement();

                var play = gameInstance.Play(wagerAmt);

                var resultingWager = new WagerResultTransaction(wager,
                                                                play.Item2
                                                                    ? WagerResultTransaction.Types.Win
                                                                    : WagerResultTransaction.Types.Loss,
                                                                player.UseTime.Current,
                                                                betType: play.Item3,
                                                                wagerAmt: play.Item1);

                resultingWager.UpdateTimeBuckets(player.State, player.County);
                player.NewWagerResultTransaction(resultingWager);

                await Intervention.Determine(player, resultingWager, dbConnection, token);
                if ((dbConnection?.IncrementGlobalEnabled ?? false) && SettingsSim.Instance.Config.GlobalIncremenIntervals > TimeSpan.Zero)
                {
                    await GlobalIncrement.AddUpdate(player,
                                                        resultingWager,
                                                        SettingsSim.Instance.Config.GlobalIncremenIntervals,
                                                        dbConnection,
                                                        SettingsSim.Instance.Config.LiveFireForgetTasks 
                                                            ? LiveFireForgetTasks : null,
                                                        token);
                }

                if (dbConnection?.LiverWagerEnabled ?? false)
                {
                    if (SettingsSim.Instance.Config.LiveFireForgetTasks)
                        LiveFireForgetTasks.Add(dbConnection.UpdateLiveWager(player, resultingWager, wager, token));
                    else
                        await dbConnection.UpdateLiveWager(player, resultingWager, wager, token);
                }

                if (player.UseTime.IsRealtime)
                {
                    await dbConnection.UpdateChangedCurrentPlayer(player, token,
                                                                   updateWagerResult: 2);
                }

                if (player.Session.LossAmounts > player.Session.EndTriggerLosses
                        || player.Session.WagerAmounts > player.Session.EndTriggerWagers
                        || player.Session.CheckEndTriggerBigWin(random)
                        || !player.ActiveSession)
                {
                    ConsoleGeneratingTrans.Decrement("Transactions " + player.UserName);
                    break;
                }


                if (SettingsSim.Instance.Config.SleepBetweenTransMS > 0)
                {
                    ConsoleSleep.Increment("Between Trans", SettingsSim.Instance.Config.SleepBetweenTransMS);

                    Logger.Instance.DebugFormat("Transaction Sleeping for {0} ms", SettingsSim.Instance.Config.SleepBetweenTransMS);

                    Thread.Sleep(SettingsSim.Instance.Config.SleepBetweenTransMS);
                    if(player.UseTime.Ishistoric)
                        player.UseTime.AddMS(SettingsSim.Instance.Config.SleepBetweenTransMS);

                    ConsoleSleep.Decrement("Between Trans");
                }

                ConsoleGeneratingTrans.Decrement("Transactions " + player.UserName);
            }
        }
    }
}
