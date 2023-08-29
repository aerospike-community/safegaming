using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common;
using System.Diagnostics;
using System.Threading;

namespace PlayerCommon
{
    public partial class Program
    {
        public static Func<string, Settings> CreateAppSettingsInstance =
            (appJsonFile) =>
                string.IsNullOrEmpty(appJsonFile)
                    ? new Settings()
                    : new Settings(appJsonFile);

        private readonly static DateTime RunDateTime = DateTime.Now;
        private static readonly string CommandLineArgsString = null;
        private static bool DebugMode = false;

        public static bool SyncMode = false;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        /// <returns>Returns the log file path</returns>
        public static string InitialazationLogs(string[] args)
        {
            var debugFlg = false;
#if DEBUG
            Logger.Instance.SetDebugLevel();
            debugFlg = true;
#endif

            Logger.Instance.InfoFormat($"{Functions.Instance.ExeAssembly.GetName()} Start");

            Logger.Instance.InfoFormat("Starting {0} ({1}) Version: {2} {3}",
                                            Common.Functions.Instance.ApplicationName,
                                            Common.Functions.Instance.AssemblyFullName,
                                            Common.Functions.Instance.ApplicationVersion,
                                            debugFlg ? "Debug Version" : string.Empty);
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
            if (args is null || args.Length == 0)
                Logger.Instance.Info("\t\tArguments: N/A");
            else
                Logger.Instance.InfoFormat("\t\tArguments: {0}",
                                            string.Join(" ", args));

            {
                var (dbName, driverName, driverVersion) = DBConnection.GetInfo();
                Logger.Instance.InfoFormat("\t\t{0}: {1} Version: {2}",
                                                dbName,
                                                driverName,
                                                driverVersion);
            }

            return Logger.GetSetEnvVarLoggerFile();
        }

        public static void InitialazationArguments(string[] args, ConsoleArguments consoleArgs, string logFile)
        {
            try
            {
                if (!consoleArgs.ParseSetArguments(args))
                {
                    Terminate(null, null, "Command Line Arguments");
                    Environment.Exit(-1);
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
                    var currentProcess = Process.GetCurrentProcess();
                    ConsoleDisplay.Console.WriteLine($"Process Id: {currentProcess.Id} Session Id: {currentProcess.SessionId} Working Set: {currentProcess.WorkingSet64}");
                    ConsoleHelper.PrintToConsole("Attach Remote Debugger before Menu Selection", ConsoleColor.Gray, ConsoleColor.DarkRed);
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
                        case DebugMenuItems.LaunchDebugger:
                            DebugMode = true;
                            if (Debugger.IsAttached)
                                Debugger.Break();
                            else
                                Debugger.Launch();
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

        public static void InitialazationConfig()
        {
            if (Settings.NotFoundSettingClassProps.Any())
            {
                Logger.Instance.WarnFormat("AppSetting Properties were not matched to an actual config settings. They are: {0}",
                                                string.Join(',', Settings.NotFoundSettingClassProps));
            }

            Logger.Instance.Dump(Settings.Instance,
                                    Logger.DumpType.Info,
                                    "Json Configuration Settings:",
                                    ignoreFldPropNames: "Instance");

            static void DumpSettingsNested(object instance, IEnumerable<PropertyFieldInfo> settingItems, int tabPos = 1)
            {
                foreach (var item in settingItems.Where(i => i.ItemType.IsClass
                                                                && !i.IsStatic
                                                                && i.IsPublic
                                                                && i.ItemType.Name.Contains("Settings")))
                {
                    var itemValue = item.GetValue(instance);
                    if (itemValue is not null)
                    {
                        Logger.Instance.Dump(item.GetValue(instance),
                                                Logger.DumpType.Info,
                                                item.Name,
                                                ignoreFldPropNames: "Instance",
                                                tabs: tabPos);
                        if (item.ItemType.IsClass
                            && item.ItemType.Name.Contains("Settings"))
                        {
                            DumpSettingsNested(item.GetValue(instance),
                                                new PropertyFieldInfoCollection(item.ItemType),
                                                tabPos + 1);
                        }
                    }
                }
            }

            DumpSettingsNested(Settings.Instance,
                                new PropertyFieldInfoCollection(Settings.Instance.GetType()));
            
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

        }

        public static void InitialazationEventPref()
        {
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
                if (!string.IsNullOrEmpty(Settings.Instance.TimingJsonFile))
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
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>Returns the Histogram Output</returns>
        public static string WritePrefFiles()
        {
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
            }

            return histogramOutput;
        }

        public static void Terminate(string histogramOutput, string logFilePath, string errorTermination = null)
        {
            ConsoleDisplay.Console.SetReWriteToWriterPosition();

            if (!string.IsNullOrEmpty(histogramOutput))
            {
                ConsoleDisplay.Console.WriteLine(" ");
                ConsoleDisplay.Console.WriteLine($"Histogram Output ({Settings.Instance.HGReportTickToUnitRatio}):");

                ConsoleDisplay.Console.WriteLine(histogramOutput);
                //ConsoleDisplay.Console.SetReWriteToWriterPosition();                
            }


            var consoleColor = System.Console.ForegroundColor;
            try
            {
                if (!string.IsNullOrEmpty(errorTermination) || ExceptionCount > 0)
                    System.Console.ForegroundColor = ConsoleColor.Red;
                else if (WarningCount > 0)
                    System.Console.ForegroundColor = ConsoleColor.Yellow;
                else
                    System.Console.ForegroundColor = ConsoleColor.Green;

                ConsoleDisplay.Console.WriteLine(" ");

                if(!string.IsNullOrEmpty(errorTermination))
                {
                    ConsoleDisplay.Console.WriteLine($"Application Terminated due to \"{errorTermination}\"...");
                    ConsoleDisplay.Console.WriteLine(" ");
                }

                if (!string.IsNullOrEmpty(logFilePath))
                {
                    ConsoleDisplay.Console.WriteLine("Application Logs \"{0}\"",
                                                        Helpers.MakeRelativePath(logFilePath));
                }
                else
                {
                    ConsoleDisplay.Console.WriteLine("Check Application Log file for detail results.");
                }

                ConsoleDisplay.Console.WriteLine(" ");
            }
            finally
            {
                System.Console.ForegroundColor = consoleColor;
            }

            ConsoleDisplay.Console.SetReWriteToWriterPosition();
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
