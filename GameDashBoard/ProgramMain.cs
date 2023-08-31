using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common;
using Common.Patterns.Tasks;
using System.Threading;
using System.Diagnostics;
using GameDashBoard;

namespace PlayerCommon
{
    public partial class Program
    {
        public static Action InitializationAction = () => { };
        public static Action PreConsoleDisplayAction = delegate { };
        public static Func<SettingsGDB, ConsoleDisplay, IDBConnectionGDB> CreateDBConnection = null;
        public static Action PostConsoleDisplayAction = delegate { };

        public static void Main(string[] args)
        {
            #region Initialization

            var settings = CreateAppSettingsInstance(null);
            var logFile = InitialazationLogs(args);
            InitialazationArguments(args, new ConsoleArgumentsGDB(SettingsGDB.Instance), logFile);
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
            if (SettingsGDB.Instance.Config.ReadDB)
            {
                ConsoleDisplay.Console.WriteLine($"Reading {DBConnection.GetInfo().dbName ?? "<ERROR>"} on Host \"{Settings.Instance.DBConnectionString}\"");
            }
            else
                ConsoleDisplay.Console.WriteLine("DB will NOT be Read");
            

            {
                ThreadPool.GetAvailableThreads(out int currWorker, out int currCompletionPort);
                ConsoleDisplay.Console.WriteLine("Working Threads: {0} Completion Port Threads: {1}",
                                                    currWorker, currCompletionPort);
            }

            ConsoleDisplay.Console.WriteLine("MaxDegreeOfParallelism: Generation {0}",
                        Settings.Instance.MaxDegreeOfParallelism);
            ConsoleDisplay.Console.WriteLine("Generating Sessions: {0} Rate: {1} Start: {2}",
                        SettingsGDB.Instance.Config.NumberOfDashboardSessions,
                        SettingsGDB.Instance.Config.SessionRefreshRateSecs,
                        SettingsGDB.Instance.Config.StartDate);

            PreConsoleDisplayAction?.Invoke();

            ConsoleDisplay.Console.WriteLine("Ignore Faults: {0}", Settings.Instance.IgnoreFaults);


            if (Settings.NotFoundSettingClassProps.Any())
            {
                var consoleColor1 = System.Console.ForegroundColor;
                try
                {
                    System.Console.ForegroundColor = ConsoleColor.Red;
                    ConsoleDisplay.Console.WriteLine("Warning: unmatched appsetting properties found. Check log file...");
                }
                finally
                {
                    System.Console.ForegroundColor = consoleColor1;
                }                
            }

            {
                var currentTimeUTC = DateTimeOffset.UtcNow;                
                ConsoleDisplay.Console.WriteLine("Local: {0} UTC: {1}",
                                                    currentTimeUTC.LocalDateTime,
                                                    currentTimeUTC);
            }
            if (SettingsGDB.Instance.Config.SleepBetweenTransMS > 0)
            {
                Logger.Instance.WarnFormat("Sleeping between Transactions for {0} ms", SettingsGDB.Instance.Config.SleepBetweenTransMS);
                var consoleColor1 = System.Console.ForegroundColor;
                try
                {
                    System.Console.ForegroundColor = ConsoleColor.Red;
                    ConsoleDisplay.Console.WriteLine("Warning: Sleeping between Transactions for {0} ms", SettingsGDB.Instance.Config.SleepBetweenTransMS);
                }
                finally
                {
                    System.Console.ForegroundColor = consoleColor1;
                }                
            }
            
            if (SettingsGDB.Instance.Config.ContinuousSessions)
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
            if (SettingsGDB.Instance.Config.EnableRealtime)
            {
                Logger.Instance.WarnFormat("Real Time Session Enabled");

                var consoleColor1 = System.Console.ForegroundColor;
                try
                {
                    System.Console.ForegroundColor = ConsoleColor.Red;

                    ConsoleDisplay.Console.WriteLine("Warning: Real Time Session Enabled");
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

            ConsoleSession = new ConsoleDisplay("Session Completed: {completed} Working: {working} Task: {tag} {task}", reserveLines: 1, takeStartBlock: false);
            ConsoleGetPlayer = new ConsoleDisplay("Player Completed: {completed} Working: {working} Task: {tag} {task}", reserveLines: 1, takeStartBlock: false);
            //ConsoleGetWager = new ConsoleDisplay("Reading Wager Completed: {completed} Working: {working} Task: {tag} {task}", reserveLines: 1, takeStartBlock: false);
            ConsoleLiveWager = new ConsoleDisplay("Live Eager Completed: {completed} Working: {working} Task: {tag} {task}", reserveLines: 1, takeStartBlock: false);
            ConsoleIntervention = new ConsoleDisplay("Intervention Completed: {completed} Working: {working} Task: {tag} {task}", reserveLines: 1, takeStartBlock: false);
            ConsoleGlobalIncrement = new ConsoleDisplay("Global Increment Completed: {completed} Working: {working} Task: {tag} {task} ", reserveLines: 1, takeStartBlock: false);
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

            using IDBConnectionGDB dbConnection = SettingsGDB.Instance.Config.ReadDB
                                                    ? CreateDBConnection?.Invoke(SettingsGDB.Instance, ConsoleSession)
                                                    : null;
            
            if (dbConnection != null && SettingsGDB.Instance.Config.CreateIdxs)
            {
                dbConnection.CreateIndexes(cancellationTokenSource.Token);
            }

            #endregion

            var maxDegreeOfParallelism = SettingsGDB.Instance.Config.NumberOfDashboardSessions;

            if (SyncMode)
                maxDegreeOfParallelism = 1;
            else 
                maxDegreeOfParallelism = SettingsGDB.Instance.MaxDegreeOfParallelism;
                       
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

            var startProcessingTime = Stopwatch.StartNew();
            Logger.Instance.InfoFormat("Main Starting Sessions {0}",
                                            SettingsGDB.Instance.Config.NumberOfDashboardSessions);
            
            int actualProcessedTrans = 0;
            int nbrSessions = 0;

            Parallel.For(0, SettingsGDB.Instance.Config.NumberOfDashboardSessions,
                            parallelOptions,
                            (sessionId, state) =>
                {
                    var cancellationToken = cancellationTokenSource.Token;

                    if (Logger.Instance.IsDebugEnabled)
                        Logger.Instance.DebugFormat("Main Session {0}", sessionId);

                    ConsoleSession.Increment($"Session {sessionId}");
                    var random = new Random(Guid.NewGuid().GetHashCode());

                    int transactions = int.MaxValue;
                    
                    var startDateTime = SettingsGDB.Instance.Config.StartDate
                                            .Round(DateTimeHelpers.RoundToType.Second);

                    Logger.Instance.DebugFormat("Start Session {0} Transactions {1} StartDate {2}",
                                                sessionId,
                                                transactions,
                                                startDateTime);

                    long refreshMS = SettingsGDB.Instance.Config.SessionRefreshRateSecs * 1000L;
                    var stopWatch = new Stopwatch();
                    Task<int>[] tasks = new Task<int>[3];

                    if (!SettingsGDB.Instance.Config.ContinuousSessions)
                    {
                        transactions = random.Next(SettingsGDB.Instance.Config.MinNbrTransPerSession,
                                                    SettingsGDB.Instance.Config.MaxNbrTransPerSession);
                    }
                    
                    tasks[0] = Task.Factory.StartNew<int>(()
                                              => dbConnection.GetGlobalIncrement(startDateTime,
                                                                                    sessionId,
                                                                                    transactions,
                                                                                    cancellationToken),
                                                        cancellationToken,
                                                        TaskCreationOptions.DenyChildAttach,
                                                        TaskScheduler.Current);
                    if(SyncMode)
                        tasks[0].Wait();

                    if (!SettingsGDB.Instance.Config.ContinuousSessions)
                    {
                        transactions = random.Next(SettingsGDB.Instance.Config.MinNbrTransPerSession,
                                                    SettingsGDB.Instance.Config.MaxNbrTransPerSession);
                    }

                    tasks[1] = Task.Factory.StartNew<int>(() 
                                                => dbConnection.GetLiveWager(startDateTime,
                                                                                sessionId,
                                                                                transactions,
                                                                                cancellationToken),
                                                    cancellationToken,
                                                    TaskCreationOptions.DenyChildAttach,
                                                    TaskScheduler.Current);

                    if (SyncMode)
                        tasks[1].Wait();

                    if (!SettingsGDB.Instance.Config.ContinuousSessions)
                    {
                        transactions = random.Next(SettingsGDB.Instance.Config.MinNbrTransPerSession,
                                                    SettingsGDB.Instance.Config.MaxNbrTransPerSession);
                    }

                    tasks[2] = Task.Factory.StartNew<int>(()
                                                => dbConnection.GetIntervention(startDateTime,
                                                                                sessionId,
                                                                                transactions,
                                                                                cancellationToken),
                                                    cancellationToken,
                                                    TaskCreationOptions.DenyChildAttach,
                                                    TaskScheduler.Current);

                    if (SyncMode)
                        tasks[3].Wait();

                    Logger.Instance.DebugFormat("Waiting for All Transactions Session {0}",
                                                    sessionId);

                    Task.WhenAll(tasks);

                    var totalTrans = tasks.Sum(t => t.Result);                    

                    Interlocked.Add(ref actualProcessedTrans, totalTrans);
                    ConsoleSession.Decrement($"Session {sessionId}");

                    nbrSessions++;

                    Logger.Instance.DebugFormat("End Session {0} Transactions {1} StartDate {2}",
                                                    sessionId,
                                                    totalTrans,
                                                    startDateTime);
                });

            startProcessingTime.Stop();
            
            ConsoleSession.TaskEndAll();
            ConsoleSleep.TaskEndAll();

            dbConnection?.Dispose();

            var histogramOutput = WritePrefFiles();
            
            #region Terminate

            var transRate = (decimal)actualProcessedTrans / (decimal)startProcessingTime.Elapsed.TotalSeconds;

            Logger.Instance.Info($"{Common.Functions.Instance.ApplicationName} Main End. Processed {actualProcessedTrans} Transactions in {startProcessingTime.Elapsed} (rate {transRate:###,##0.000} Trans/sec).");

            Logger.Instance.Flush(5000);

            ConsoleDisplay.End();
            //GCMonitor.GetInstance().StopGCMonitoring();

            ConsoleDisplay.Console.SetReWriteToWriterPosition();

            ConsoleDisplay.Console.WriteLine(" ");
            ConsoleDisplay.Console.WriteLine("Completed {0} Sessions {1}  Transactions {2} (rate {3:###,##0.000} Trans/sec)",
                                                   nbrSessions,
                                                    actualProcessedTrans,
                                                    startProcessingTime.Elapsed,
                                                    transRate);

            PostConsoleDisplayAction?.Invoke();

            Terminate(histogramOutput, logFilePath);

            #endregion            
        }

    }
}
