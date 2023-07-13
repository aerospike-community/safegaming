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

namespace PlayerGeneration
{
    public partial class Program
    {
        static readonly string[] Games = new string[] { "Slots", "Roulette", "Roulette", "Slots", "Slots", "Roulette", "Roulette", "Slots", };

        private readonly static DateTime RunDateTime = DateTime.Now;
        private static readonly string CommandLineArgsString = null;
        private static bool DebugMode = false;
        private static bool SyncMode = false;

        public async static Task Main(string[] args)
        {
            #region Initialization            
#if DEBUG
            Logger.Instance.SetDebugLevel();
#endif

            Logger.Instance.InfoFormat("PlayerGeneration Main Start");

            Logger.Instance.InfoFormat("Starting {0} ({1}) Version: {2} ",
                                            Common.Functions.Instance.ApplicationName,
                                            Common.Functions.Instance.AssemblyFullName,
                                            Common.Functions.Instance.ApplicationVersion);
            Logger.Instance.InfoFormat("\t\tOS: {0} Framework: {1}",
                                           GetOSInfo(),
                                           GetFrameWorkInfo());
            Logger.Instance.InfoFormat("\t\tHost: {0} IPAdress: {1} RunAs: {2} Domain: {3}",
                                            Common.Functions.Instance.Host,
                                            Common.Functions.Instance.IP,
                                            Common.Functions.Instance.CurrentUserName,
                                            Common.Functions.Instance.DomainName);
            Logger.Instance.InfoFormat("\t\tIPAdresses: {0}",
                                        String.Join(", ", Common.Functions.Instance.IPAddresses?.Select(n => n.ToString())));
            Logger.Instance.InfoFormat("\t\tRunTime Dir: {0}",
                                            Common.Functions.Instance.ApplicationRunTimeDir);


            {
                var (dbName, driverName, driverVersion) = DBConnection.GetInfo();
                Logger.Instance.InfoFormat("\t\t{0}: {1} Version: {2}",
                                                dbName,
                                                driverName,
                                                driverVersion);
            }

            #region Arguments
            {
                var consoleArgs = new ConsoleArguments(Settings.Instance);

                try
                {
                    if (!consoleArgs.ParseSetArguments(args))
                    {
                        return;
                    }

                    if (consoleArgs.Sync)
                    {
                        SyncMode = true;
                    }

                    if (consoleArgs.Debug)
                    {
                        Logger.Instance.Warn("Debug Mode");
                        Logger.Instance.SetDebugLevel();
                        Settings.Instance.IgnoreFaults = false;
                        Logger.Instance.Warn("Ignore Faults disabled (exceptions will be thrown)");
                        ConsoleDisplay.Console.WriteLine("Application Log Level set to Debug");
                        Common.ConsoleHelper.PrintToConsole("Attach Remote Debugger before Menu Selection", ConsoleColor.Gray, ConsoleColor.DarkRed);
                        ConsoleDisplay.Console.WriteLine();

                        var consoleMenu = new Common.ConsoleMenu<DebugMenuItems>()
                        {
                            Header = "Input Debug Option Number and Press <Enter>"
                        };

                        switch (consoleMenu.ShowMenu())
                        {
                            case DebugMenuItems.DebugMode:
                                DebugMode = true;
                                ConsoleDisplay.EnabeConsole = false;
                                System.Diagnostics.Trace.Listeners.Add(new System.Diagnostics.TextWriterTraceListener(Console.Out));
                                System.Diagnostics.Debug.AutoFlush = true;
                                System.Diagnostics.Debug.Indent();
                                break;
                            case DebugMenuItems.DebugModeConsole:
                                DebugMode = true;
                                break;
                            case DebugMenuItems.ExitProgram:
                                Logger.Instance.Debug("Debug Mode Application Exit");
                                return;
                            case DebugMenuItems.NormalMode:
                            default:
                                break;
                        }

                        if (DebugMode) Logger.Instance.Debug("Debug Mode Enabled");

                    }
                }
                catch (CommandLineParser.Exceptions.CommandLineException e)
                {
                    //consoleArgs.ShowUsage();
                    //ConsoleDisplay.Console.WriteLine();
                    ConsoleDisplay.Console.WriteLine(e.Message);
                    ConsoleDisplay.Console.WriteLine("CommandLine: '{0}'", CommandLineArgsString);
                    Common.ConsoleHelper.Prompt("Press Return to Exit", ConsoleColor.Gray, ConsoleColor.DarkRed);
                    return;
                }
                catch (System.Exception e)
                {
                    //consoleArgs.ShowUsage();
                    //ConsoleDisplay.Console.WriteLine();
                    ConsoleDisplay.Console.WriteLine(e.Message);
                    ConsoleDisplay.Console.WriteLine("CommandLine: '{0}'", CommandLineArgsString);
                    Common.ConsoleHelper.Prompt("Press Return to Exit", ConsoleColor.Gray, ConsoleColor.DarkRed);
                    return;
                }
            }
            #endregion


            Logger.Instance.Dump(Settings.Instance,
                                    Logger.DumpType.Info,
                                    "Json Configuration Settings:",
                                    ignoreFldPropNames: "Instance");

            System.Console.CancelKeyPress += Console_CancelKeyPress;
            Logger.Instance.OnLoggingEvent += Instance_OnLoggingEvent;

#if DEBUG
#else
            Logger.Instance.Info("Register UnhandledException event");
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
#endif

            if (Settings.Instance.WorkerThreads > 0 || Settings.Instance.CompletionPortThreads > 0)
            {
                ThreadPool.GetAvailableThreads(out int currWorker, out int currCompletionPort);
                ThreadPool.GetMinThreads(out int minWorker, out int minCompletionPort);

                ThreadPool.SetMaxThreads(Settings.Instance.WorkerThreads > minWorker
                                                ? Settings.Instance.WorkerThreads
                                                : currWorker,
                                            Settings.Instance.CompletionPortThreads > minCompletionPort
                                                ? Settings.Instance.CompletionPortThreads
                                                : currCompletionPort);
            }

            #endregion

            #region Console Display Setup

            ConsoleDisplay.Console.ClearScreen();
            ConsoleDisplay.Console.AdjustScreenStartBlock();

            ConsoleDisplay.Console.WriteLine(" ");
            if (Settings.Instance.UpdateDB)
            {
#if MONGODB
                ConsoleDisplay.Console.WriteLine($"Updating MGDB on Host \"{Settings.Instance.DBConnectionString}\"");
#else
                ConsoleDisplay.Console.WriteLine($"Updating ADB on Host \"{Settings.Instance.DBHost}\"");
#endif
            }
            else
                ConsoleDisplay.Console.WriteLine("DB will NOT be Updated");

            if (Settings.Instance.TruncateSets)
            {
                ConsoleDisplay.Console.WriteLine($"Will be Truncating...");
            }

            {
                ThreadPool.GetAvailableThreads(out int currWorker, out int currCompletionPort);
                ConsoleDisplay.Console.WriteLine("Working Threads: {0} Completion Port Threads: {1}",
                                                    currWorker, currCompletionPort);
            }

            ConsoleDisplay.Console.WriteLine("MaxDegreeOfParallelism: Generation {0}",
                        Settings.Instance.MaxDegreeOfParallelismGeneration);
            ConsoleDisplay.Console.WriteLine("Generating Players: {0} Player Id Start: {1}",
                        Settings.Instance.NbrPlayers,
                        Settings.Instance.PlayerIdStartRange);
#if MONGODB
            ConsoleDisplay.Console.WriteLine("MGDB Connection Timeout: {0}, Operation Timeout: {1} Compression: {2} Max Latency Warning: {3}",
                        Settings.Instance.ConnectionTimeout,
                        Settings.Instance.DBOperationTimeout,
                        Settings.Instance.EnableDriverCompression,
                        Settings.Instance.WarnMaxMSLatencyDBExceeded);
#else
            ConsoleDisplay.Console.WriteLine("ADB Connection Timeout: {0}, Max: {1} Min: {2} Idle: {3} Operation Timeout: {4} Compression: {5} Max Latency Warning: {6}",
                        Settings.Instance.ConnectionTimeout,
                        Settings.Instance.MaxConnectionPerNode,
                        Settings.Instance.MinConnectionPerNode,
                        Settings.Instance.MaxSocketIdle,
                        Settings.Instance.DBOperationTimeout,
                        Settings.Instance.EnableDriverCompression,
                        Settings.Instance.WarnMaxMSLatencyDBExceeded);
#endif
            ConsoleDisplay.Console.WriteLine("Ignore Faults: {0}", Settings.Instance.IgnoreFaults);

            #region Timing and Histogram Events

            PrefStats.EnableEvents = false;
            PrefStats.CaptureType = PrefStats.CaptureTypes.Disabled;

            if (Settings.Instance.TimeEvents
                    && (!string.IsNullOrEmpty(Settings.Instance.TimingJsonFile)
                        || !string.IsNullOrEmpty(Settings.Instance.TimingCSVFile)))
            {
                ConsoleDisplay.Console.WriteLine("Event Timings Json DetailFile: {0} CSV DetailFile: {1}",
                                                    Settings.Instance.TimingJsonFile ?? "N/A",
                                                    Settings.Instance.TimingCSVFile ?? "N/A");
                PrefStats.EnableEvents = true;
                PrefStats.CaptureType |= PrefStats.CaptureTypes.Detail;
                if(!string.IsNullOrEmpty(Settings.Instance.TimingJsonFile))
                {
                    PrefStats.CaptureType |= PrefStats.CaptureTypes.JSON;
                }
                if (!string.IsNullOrEmpty(Settings.Instance.TimingCSVFile))
                {
                    PrefStats.CaptureType |= PrefStats.CaptureTypes.CSV;
                }
            }
            else
            {
                ConsoleDisplay.Console.WriteLine("Event Timing Disabled");                
            }

            if (Settings.Instance.EnableHistogram)
            {
                ConsoleDisplay.Console.WriteLine("Histogram Enabled, reporting to file: {0}",
                                                    Settings.Instance.HGRMFile ?? "N/A");

                PrefStats.EnableEvents = true;
                PrefStats.CaptureType |= PrefStats.CaptureTypes.Histogram;

                if (!string.IsNullOrEmpty(Settings.Instance.HGRMFile))
                {
                    PrefStats.CaptureType |= PrefStats.CaptureTypes.HGRM;
                }

                PrefStats.CreateHistogram(Settings.Instance.HGPrecision,
                                            Settings.Instance.HGLowestTickValue,
                                            Settings.Instance.HGHighestTickValue);
            }
            else
            {
                ConsoleDisplay.Console.WriteLine("Histogram Disabled");
            }

            #endregion

            {
                var currentTimeUTC = DateTimeOffset.UtcNow;
                //Logger.Instance.InfoFormat("Start Time Local: {0} UTC: {1}",
                //                                DateTimeOffset.Now,
                //                                currentTimeUTC);
                ConsoleDisplay.Console.WriteLine("Local: {0} UTC: {1}",
                                                    currentTimeUTC.LocalDateTime,
                                                    currentTimeUTC);
            }
            if (Settings.Instance.SleepBetweenTransMS > 0)
            {
                Logger.Instance.WarnFormat("Sleeping between Transactions for {0} ms", Settings.Instance.SleepBetweenTransMS);
                var consoleColor1 = System.Console.ForegroundColor;
                try
                {
                    System.Console.ForegroundColor = ConsoleColor.Red;
                    ConsoleDisplay.Console.WriteLine("Warning: Sleeping between Transactions for {0} ms", Settings.Instance.SleepBetweenTransMS);
                }
                finally
                {
                    System.Console.ForegroundColor = consoleColor1;
                }                
            }
            if (Settings.Instance.EnableRealtime.HasValue && Settings.Instance.EnableRealtime.Value)
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
            if (Settings.Instance.ContinuousSessions)
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

#if MONGODB
            IDBConnection dbConnection = Settings.Instance.UpdateDB
                                            ? new DBConnection(Settings.Instance.DBConnectionString,
                                                                Settings.Instance.DBName,
                                                                Settings.Instance.ConnectionTimeout,
                                                                Settings.Instance.DBOperationTimeout,
                                                                Settings.Instance.HistoricTimeEndMonth,
                                                                Settings.Instance.EnableDriverCompression,
                                                                displayProgression: ConsolePuttingDB,
                                                                playerProgression: ConsolePuttingPlayer,
                                                                historyProgression: ConsolePuttingHistory)
                                            : null;
#else
             IDBConnection dbConnection = Settings.Instance.UpdateDB
                                            ? new DBConnection(Settings.Instance.DBHost,
                                                                Settings.Instance.DBPort,
                                                                Settings.Instance.ConnectionTimeout,
                                                                Settings.Instance.DBOperationTimeout,
                                                                Settings.Instance.DBUseExternalIPAddresses,                                                        
                                                                displayProgression: ConsolePuttingDB,
                                                                playerProgression: ConsolePuttingPlayer,
                                                                historyProgression: ConsolePuttingHistory)
                                            : null;
#endif

            State[] stateDB;

            {
                Logger.Instance.Debug("Main Reading State/County JSON DetailFile");

                using var readingJsonProg = new Progression(ConsoleGenerating, "State", "Reading State Json DetailFile");

                var stateDBPath = BaseFile.Make(Settings.Instance.StateJsonFile);

                stateDB = Common.JSONExtensions.FromJSON<State[]>(stateDBPath.ReadAllText());

                Logger.Instance.DebugFormat("Main Read State/County JSON DetailFile {0}", stateDB.Length);
            }


            using var countyProg = new Progression(ConsoleGenerating, "County", "Determining Player Counties");
            
            Logger.Instance.Debug("Main Processing State/County into only gaming Counties by State");

            var onlyGamingCountiesByState = (from state in stateDB
                                             where state.FIPSCode > 0 && state.Counties.Any(c => c.OnlineGaming)
                                                        && (Settings.Instance.OnlyTheseGamingStates == null
                                                                || Settings.Instance.OnlyTheseGamingStates.Count == 0
                                                                || Settings.Instance.OnlyTheseGamingStates.Contains(state.Name))
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

            if (dbConnection != null && Settings.Instance.TruncateSets)
            {
                dbConnection.Truncate();
            }

            await InterventionThresholds.Initialize(dbConnection, cancellationTokenSource.Token);
            DateTimeSimulation.Initialize();

#endregion

            var enableDBUpdate = dbConnection != null;

            var startPlayerProcessingTime = Stopwatch.StartNew();
            Logger.Instance.InfoFormat("Main Starting Player Processing {0} Generation",
                                            Settings.Instance.NbrPlayers);

            var maxDegreeOfParallelism = Settings.Instance.MaxDegreeOfParallelismGeneration;

            if (SyncMode)
                maxDegreeOfParallelism = 1;
            else if(Settings.Instance.ContinuousSessions)
            {
                maxDegreeOfParallelism = Settings.Instance.NbrPlayers;
            }

            var parallelOptions = new ParallelOptions()
            {
                CancellationToken = cancellationTokenSource.Token,
                MaxDegreeOfParallelism = maxDegreeOfParallelism
            };

            var idxs = new int[Settings.Instance.NbrPlayers];
            Player.CurrentPlayerId = Settings.Instance.PlayerIdStartRange - 1;
            
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

                    if (Settings.Instance.GenerateUniqueEmails)
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
                    } while (Settings.Instance.ContinuousSessions) ;

                    Interlocked.Increment(ref actualPlayersProcessed);
                    ConsoleGenerating.Decrement("Player " + email);
                });

            startPlayerProcessingTime.Stop();
            ConsolePuttingDB.TaskEndAll();
            dbConnection.Dispose();

            #region Write Timing/Histogram Events

            string histogramOutput = null;

            if (PrefStats.CaptureType != PrefStats.CaptureTypes.Disabled)
            {
                if (PrefStats.CaptureType.HasFlag(PrefStats.CaptureTypes.JSON))
                {
                    ConsoleFileWriting.Increment(Settings.Instance.TimingJsonFile);
                    try
                    {
                        PrefStats.ToJson(Settings.Instance.TimingJsonFile);
                        Logger.Instance.Info($"Written JSON Timing Events: \"{Settings.Instance.TimingJsonFile}\"");
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.Error($"Written JSON Timing Events Failed: \"{Settings.Instance.TimingJsonFile}\"", ex);
                    }
                    ConsoleFileWriting.Decrement(Settings.Instance.TimingJsonFile);
                }

                if (PrefStats.CaptureType.HasFlag(PrefStats.CaptureTypes.Detail)
                        && PrefStats.CaptureType.HasFlag(PrefStats.CaptureTypes.CSV))
                {
                    ConsoleFileWriting.Increment(Settings.Instance.TimingCSVFile);
                    try
                    {
                        PrefStats.ToCSV(Settings.Instance.TimingCSVFile);
                        Logger.Instance.Info($"Writing CSV Timing Events: \"{Settings.Instance.TimingCSVFile}\"");
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.Error($"Writing CSV Timing Events Failed: \"{Settings.Instance.TimingCSVFile}\"", ex);
                    }
                    ConsoleFileWriting.Decrement(Settings.Instance.TimingCSVFile);
                }

                if (PrefStats.CaptureType.HasFlag(PrefStats.CaptureTypes.Histogram))
                {
                    ConsoleFileWriting.Increment(Settings.Instance.HGRMFile ?? "Histogram");
                    
                    try
                    {                        
                        histogramOutput = PrefStats.OutputHistogram(null,
                                                                    Logger.Instance,
                                                                    Settings.Instance.HGRMFile,
                                                                    Settings.Instance.HGReportPercentileTicksPerHalfDistance,
                                                                    Settings.Instance.HGReportUnitRatio);
                        Logger.Instance.Info($"Writing Histogram File to \"{Settings.Instance.HGRMFile ?? "N/A"}\"");
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.Error($"Writing Histogram Failed: \"{Settings.Instance.HGRMFile}\"", ex);
                    }
                    ConsoleFileWriting.Decrement(Settings.Instance.HGRMFile ?? "Histogram");
                }

                ConsoleFileWriting.TaskEndAll();
            }

            #endregion

            #region Terminate

            var playerRate = (decimal)actualPlayersProcessed / (decimal)startPlayerProcessingTime.Elapsed.TotalSeconds;

            Logger.Instance.Info($"PlayerGeneration Main End. Processed {actualPlayersProcessed} Players in {startPlayerProcessingTime.Elapsed} (rate {playerRate:###,##0.000} players/sec).");

            Logger.Instance.Flush(5000);

            ConsoleDisplay.End();
            //GCMonitor.GetInstance().StopGCMonitoring();

            ConsoleDisplay.Console.SetReWriteToWriterPosition();

            ConsoleDisplay.Console.WriteLine();
            ConsoleDisplay.Console.WriteLine("Completed {0} Players in {1} (rate {2:###,##0.000} players/sec)",
                                                   actualPlayersProcessed,
                                                   startPlayerProcessingTime.Elapsed,
                                                   playerRate);
            ConsoleDisplay.Console.SetReWriteToWriterPosition();

            if (!string.IsNullOrEmpty(histogramOutput))
            {
                ConsoleDisplay.Console.WriteLine();
                ConsoleDisplay.Console.WriteLine($"Histogram Output ({Settings.Instance.HGReportTickToUnitRatio}):");
                
                ConsoleDisplay.Console.WriteLine(histogramOutput);
                //ConsoleDisplay.Console.SetReWriteToWriterPosition();                
            }


            var consoleColor = System.Console.ForegroundColor;
            try
            {
                if (ExceptionCount > 0)
                    System.Console.ForegroundColor = ConsoleColor.Red;
                else if (WarningCount > 0)
                    System.Console.ForegroundColor = ConsoleColor.Yellow;
                else
                    System.Console.ForegroundColor = ConsoleColor.Green;

                ConsoleDisplay.Console.WriteLine();
                ConsoleDisplay.Console.WriteLine("Application Logs \"{0}\"", Helpers.MakeRelativePath(logFilePath));
                ConsoleDisplay.Console.WriteLine();
            }
            finally
            {
                System.Console.ForegroundColor = consoleColor;
            }
            
            ConsoleDisplay.Console.SetReWriteToWriterPosition();

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



        private static async Task RanSession(IDBConnection dbConnection,
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

            var totalSession = random.Next(Math.Min(player.Metrics.AvgSessionsPerDay, Settings.Instance.MinPlayerSessions),
                                            Math.Max(player.Metrics.AvgSessionsPerDay, Settings.Instance.MaxPlayerSessions));
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
                                            IDBConnection dbConnection,
                                            CancellationToken token)
        {                        
            var random = new Random(Guid.NewGuid().GetHashCode());

            var maxTrans = random.Next(Settings.Instance.MinTransPerSession, Settings.Instance.MaxTransPerSession);
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
                if (Settings.Instance.GlobalIncremenIntervals > TimeSpan.Zero)
                    await GlobalIncrement.AddUpdate(player,
                                                        resultingWager,
                                                        Settings.Instance.GlobalIncremenIntervals,
                                                        dbConnection,
                                                        token);

                await dbConnection.UpdateLiveWager(player, resultingWager, wager, token);

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


                if (Settings.Instance.SleepBetweenTransMS > 0)
                {
                    ConsoleSleep.Increment("Between Trans", Settings.Instance.SleepBetweenTransMS);

                    Logger.Instance.DebugFormat("Transaction Sleeping for {0} ms", Settings.Instance.SleepBetweenTransMS);

                    Thread.Sleep(Settings.Instance.SleepBetweenTransMS);
                    if(player.UseTime.Ishistoric)
                        player.UseTime.AddMS(Settings.Instance.SleepBetweenTransMS);

                    ConsoleSleep.Decrement("Between Trans");
                }

                ConsoleGeneratingTrans.Decrement("Transactions " + player.UserName);
            }
        }


        public static string GetOSInfo()
        {
            return Common.Functions.IsRunningOnWindows
                                    ? System.Runtime.InteropServices.RuntimeInformation.OSDescription
                                    : (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX)
                                            ? "OSX" : System.Runtime.InteropServices.RuntimeInformation.OSDescription);
        }

        public static string GetFrameWorkInfo()
        {
            return System.Reflection.Assembly.GetEntryAssembly()
                    .GetCustomAttributes(typeof(System.Runtime.Versioning.TargetFrameworkAttribute), true)
                    ?.Cast<System.Runtime.Versioning.TargetFrameworkAttribute>()
                    .FirstOrDefault()
                    ?.FrameworkName
                    ?? System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
        }

    }
}
