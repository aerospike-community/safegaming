using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common;
using CommandLineParser.Arguments;
using System.Diagnostics.CodeAnalysis;
using GameSimulator;

namespace PlayerCommon
{
    public class ConsoleArguments
    {

        protected readonly CommandLineParser.CommandLineParser _cmdLineParser = new();

        public ConsoleArguments(Settings appSettings)
        {
            this.AppSettings = appSettings;

            this._cmdLineParser.ShowUsageOnEmptyCommandline = true;
            
            this._cmdLineParser.Arguments.Add(new ValueArgument<bool>("DetailTimings")
            {
                DefaultValue = appSettings.TimeEvents,
                Optional = true, //Required                
                Description = "Enable/Disable Detail Pref Timings"
            });
                      
            this._cmdLineParser.Arguments.Add(new ValueArgument<string>("TimingJsonFile")
            {
                DefaultValue = appSettings.TimingJsonFile,
                Optional = true, //Required                
                Description = "JSON DetailFile where the Pref timings will be stored"
            });

            this._cmdLineParser.Arguments.Add(new ValueArgument<string>("TimingCSVFile")
            {
                DefaultValue = appSettings.TimingCSVFile,
                Optional = true, //Required                
                Description = "CSV DetailFile where the Pref timings will be stored"
            });

            this._cmdLineParser.Arguments.Add(new ValueArgument<bool>("Histogram")
            {
                DefaultValue = appSettings.EnableHistogram,
                Optional = true, //Required                
                Description = "Enable/disable histogram recording"
            });

            this._cmdLineParser.Arguments.Add(new ValueArgument<string>("HGRMFile")
            {
                DefaultValue = appSettings.HGRMFile,
                Optional = true, //Required                
                Description = "Histogram Output File"
            });

            this._cmdLineParser.Arguments.Add(new ValueArgument<int>('d', "MaxDegreeOfParallelism")
            {
                DefaultValue = appSettings.MaxDegreeOfParallelism,
                Optional = true, //Required                
                Description = "Max Degree Of Parallelism"
            });

            this._cmdLineParser.Arguments.Add(new ValueArgument<int>('t', "CompletionPortThreads")
            {
                DefaultValue = appSettings.CompletionPortThreads,
                Optional = true, //Required                
                Description = "Nbr of Completion Ports"
            });

            this._cmdLineParser.Arguments.Add(new ValueArgument<int>("WorkerThreads")
            {
                DefaultValue = appSettings.WorkerThreads,
                Optional = true, //Required                
                Description = ".Net Thread Pool Worker Threads"
            });

            this._cmdLineParser.Arguments.Add(new ValueArgument<bool>("IgnoreFaults")
            {
                DefaultValue = appSettings.IgnoreFaults,
                Optional = true, //Required                
                Description = "True to Ignore any Exceptions during an DB Operation (continue running and log fault))"
            });

            this._cmdLineParser.Arguments.Add(new FileArgument("AppConfigJsonFile")
            {
                DefaultValue = new System.IO.FileInfo(appSettings.AppJsonFile),
                Optional = true, //Required
                FileMustExist = true,
                Description = "The Application Configuration Json File"
            });

            this._cmdLineParser.Arguments.Add(new SwitchArgument("Debug", false)
            {
                Optional = true, //Required                
                Description = "Debug Mode"
            });

            this._cmdLineParser.Arguments.Add(new SwitchArgument("Sync", false)
            {
                Optional = true, //Required                
                Description = "Synchronous Mode"
            });

            this._cmdLineParser.Arguments.Add(new SwitchArgument('v', "Version", false)
            {
                Optional = true, //Required                
                Description = "Displays application version"
            });

            this._cmdLineParser.Arguments.Add(new ValueArgument<int>('l', "WarnLatency")
            {
                Optional = true, //Required
                DefaultValue = appSettings.WarnMaxMSLatencyDBExceeded,
                Description = "Warn when Max Latency (MS) Exceeded in a DB call"
            });
        }

        public Settings AppSettings
        {
            get;
            private set;
        }

        public bool Debug
        {
            get;
            set;
        }

        public bool Sync
        {
            get;
            set;
        }

        protected List<Argument> RemainingArgs { get; } = new List<Argument>();

        public virtual bool ParseSetArguments(string[] args, bool throwIfNotMpaaed = true)
        {
           
            if (args.Any(i => i.Any(c => c == '"')))
            {
                var argList = new List<string>();

                foreach (var arg in args)
                {
                    if (arg.Contains('"'))
                    {
                        var argItems = arg.Split('"', ' ');
                        bool nullFnd = false;

                        foreach (var item in argItems)
                        {
                            if (string.IsNullOrEmpty(item))
                            {
                                if (nullFnd)
                                {
                                    argList.Add(string.Empty);
                                    nullFnd = false;
                                }
                                else
                                    nullFnd = true;
                                continue;
                            }

                            nullFnd = false;
                            argList.Add(item.Trim());
                        }
                    }
                    else
                        argList.Add(arg);
                }
                args = argList.ToArray();
            }

            if (args.Any(a => a == "-?" || a == "--ShowDefaults" || a == "--Help" || a == "--help"))
            {
                this.ShowDefaults();
                return false;
            }

            this._cmdLineParser.ParseCommandLine(args);
        
            var results = this._cmdLineParser.Arguments
                                                .Where(a => a.Parsed)
                                                .OrderBy(a => a.LongName);

            foreach (var item in results)
            {
                switch (item.LongName)
                {                    
                    case "DetailTimings":
                        this.AppSettings.TimeEvents = ((ValueArgument<bool>)item).Value;                        
                        break;
                    case "TimingJsonFile":
                        this.AppSettings.TimingJsonFile = ((ValueArgument<string>)item).Value;
                        break;
                    case "TimingCSVFile":
                        this.AppSettings.TimingCSVFile = ((ValueArgument<string>)item).Value;
                        break;
                    case "Histogram":
                        this.AppSettings.EnableHistogram = ((ValueArgument<bool>)item).Value;
                        break;
                    case "HGRMFile":
                        this.AppSettings.HGRMFile = ((ValueArgument<string>)item).Value;
                        break;
                    case "MaxDegreeOfParallelism":
                        this.AppSettings.MaxDegreeOfParallelism = ((ValueArgument<int>)item).Value;
                        break;
                    case "CompletionPortThreads":
                        this.AppSettings.CompletionPortThreads = ((ValueArgument<int>)item).Value;
                        break;
                    case "WorkerThreads":
                        this.AppSettings.WorkerThreads = ((ValueArgument<int>)item).Value;
                        break;                    
                    case "IgnoreFaults":
                        this.AppSettings.IgnoreFaults = ((ValueArgument<bool>)item).Value;
                        break;
                    case "AppConfigJsonFile":
                        var appConfigFile = ((FileArgument)item).Value;
                        if(appConfigFile is not null
                            && appConfigFile.FullName != this.AppSettings.AppJsonFile)
                        {
                            this.AppSettings = Program.CreateAppSettingsInstance(appConfigFile.FullName);
                        }
                        break;
                    case "Debug":
                        this.Debug = true;
                        break;
                    case "Sync":
                        this.Sync = true;
                        break;
                    case "Version":
                        ShowVersion();
                        return false;                        
                    case "ShowDefaults":
                    case "help":
                        ShowDefaults();
                        return false;                    
                    default:
                        if (throwIfNotMpaaed)
                            ThrowArgumentException(item);

                        RemainingArgs.Add(item);
                        break;
                }
            }
            
            if(this.AppSettings.TimeEvents)
            {
                this.AppSettings.TimeEvents = !(string.IsNullOrEmpty(this.AppSettings.TimingJsonFile)
                                                    && string.IsNullOrEmpty(this.AppSettings.TimingCSVFile));
            }

            return true;
        }

        public static void ThrowArgumentException([NotNull] Argument item)
        {
            throw new ArgumentException(string.Format("Do not know how to map {0} ({1}) to a setting!",
                                                                        item.LongName,
                                                                        item.GetType().Name),
                                                                        item.LongName);
        }

        public void ShowDefaults()
        {
            Console.WriteLine();
            Console.WriteLine("Arguments including Default Values:");
            Console.WriteLine();

            foreach (var cmdArg in this._cmdLineParser.Arguments)
            {
                var defaultValue = (cmdArg as IArgumentWithDefaultValue)?.DefaultValue;
                var example = cmdArg.Example?.Trim();
                var required = cmdArg.Optional ? string.Empty : " (Required)";
                var defaultValueFmt = string.Format(" [Default Value \"{0}\"{1}]",
                                                        defaultValue == null || (defaultValue is string strValue && strValue == string.Empty)
                                                            ? "<none>"
                                                            : defaultValue,
                                                        cmdArg.AllowMultiple ? ", Multiple Allowed" : string.Empty);

                if (cmdArg.ShortName.HasValue)
                {
                    Console.WriteLine("\t-{0}, --{1}{2}{3} {4}",
                                        cmdArg.ShortName.Value,
                                        cmdArg.LongName,
                                        cmdArg is SwitchArgument
                                            ? string.Empty
                                            : defaultValueFmt,
                                        required,
                                        cmdArg.Description);
                }
                else
                {
                    Console.WriteLine("\t--{0}{1}{2} {3}",
                                        cmdArg.LongName,
                                        cmdArg is SwitchArgument
                                            ? string.Empty
                                            : defaultValueFmt,
                                         required,
                                        cmdArg.Description);
                }

                if (!string.IsNullOrEmpty(example))
                {
                    Console.WriteLine("\t\tExample: {0}", example);
                }
            }          
        }        


        public static void ShowVersion()
        {            
            ConsoleDisplay.Console.WriteLine("{0} ({1}) Version: {2}",
                                                Common.Functions.Instance.ApplicationName,
                                                Common.Functions.Instance.AssemblyFullName,
                                                Common.Functions.Instance.ApplicationVersion);
            ConsoleDisplay.Console.WriteLine("\t\tOS: {0} Framework: {1}",
                                                Program.GetOSInfo(),
                                                Program.GetFrameWorkInfo());
            ConsoleDisplay.Console.WriteLine("\t\tHost: {0} IPAdress: {1} RunAs: {2} Domain: {3}",
                                                Common.Functions.Instance.Host,
                                                Common.Functions.Instance.IP,
                                                Common.Functions.Instance.CurrentUserName,
                                                Common.Functions.Instance.DomainName);
            ConsoleDisplay.Console.WriteLine("\t\tRunTime Dir: {0}",
                                                Common.Functions.Instance.ApplicationRunTimeDir);
            
            {
                var (dbName, driverName, driverVersion) = DBConnection.GetInfo();
                ConsoleDisplay.Console.WriteLine("\t\t{0}: {1} Version: {2}",
                                                    dbName,
                                                    driverName,
                                                    driverVersion);
            }
        }
    }
}
