using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common;
using CommandLineParser.Arguments;

namespace PlayerGeneration
{
    internal sealed class ConsoleArguments
    {

        private readonly CommandLineParser.CommandLineParser _cmdLineParser = new();

        public ConsoleArguments(Settings appSettings)
        {
            this.AppSettings = appSettings;

            this._cmdLineParser.ShowUsageOnEmptyCommandline = true;

            this._cmdLineParser.Arguments.Add(new ValueArgument<int>('s', "start-key")
            {
                Optional = true, //Required                
                DefaultValue = AppSettings.PlayerIdStartRange,
                Description = "The value used to start counting the player id generation"
            });

            this._cmdLineParser.Arguments.Add(new ValueArgument<int>('k', "keys")
            {
                Optional = true, //Required                
                DefaultValue = appSettings.NbrPlayers,
                Description = "The number of players generated"
            });

            this._cmdLineParser.Arguments.Add(new SwitchArgument('r', "RealTime", false)
            {
                Optional = true, //Required                
                Description = "Enables Real Time Dates/Times"
            });            

            this._cmdLineParser.Arguments.Add(new ValueArgument<int>('p', "Sleep")
            {
                DefaultValue = appSettings.SleepBetweenTransMS,
                Optional = true, //Required                
                Description = "Sleep between Transactions in MS"
            });
            //ContinuousSessions
            this._cmdLineParser.Arguments.Add(new SwitchArgument('c', "Continuous", false)
            {
                DefaultValue = appSettings.ContinuousSessions,
                Optional = true, //Required                
                Description = "Players will have unlimited session play"
            });

            this._cmdLineParser.Arguments.Add(new SwitchArgument("DisableTimings", false)
            {
                DefaultValue = appSettings.TimeEvents,
                Optional = true, //Required                
                Description = "Disable Event Timings around the API"
            });
                      
            this._cmdLineParser.Arguments.Add(new ValueArgument<string>("TimingJsonFile")
            {
                DefaultValue = appSettings.TimingJsonFile,
                Optional = true, //Required                
                Description = "JSON DetailFile where the timings will be stored"
            });

            this._cmdLineParser.Arguments.Add(new ValueArgument<string>("TimingCSVFile")
            {
                DefaultValue = appSettings.TimingCSVFile,
                Optional = true, //Required                
                Description = "CSV DetailFile where the timings will be stored"
            });

            this._cmdLineParser.Arguments.Add(new ValueArgument<bool>("EnableHistogram")
            {
                DefaultValue = appSettings.EnableHistogram,
                Optional = true, //Required                
                Description = "True/False to enable/disable histogram support"
            });

            this._cmdLineParser.Arguments.Add(new ValueArgument<string>("HGRMFile")
            {
                DefaultValue = appSettings.HGRMFile,
                Optional = true, //Required                
                Description = "Histogram Output File"
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
                Description = "displays application version"
            });

            this._cmdLineParser.Arguments.Add(new ValueArgument<int>('l', "WarnLatency")
            {
                Optional = true, //Required
                DefaultValue = appSettings.WarnMaxMSLatencyDBExceeded,
                Description = "Warn when Max Latency (MS) Exceeded in a DB call"
            });
        }

        public Settings AppSettings { get; }
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

        public bool ParseSetArguments(string[] args)
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

            //if (!this._cmdLineParser.ParsingSucceeded)
            //{
            //    this._cmdLineParser.ShowUsage();
            //    return false;
            //}
            bool explictDisableTiming = false;

            var results = this._cmdLineParser.Arguments
                                                .Where(a => a.Parsed)
                                                .OrderBy(a => a.LongName);

            foreach (var item in results)
            {
                switch (item.LongName)
                {
                    case "start-key":
                        this.AppSettings.PlayerIdStartRange = ((ValueArgument<int>)item).Value;
                        break;
                    case "keys":
                        this.AppSettings.NbrPlayers = ((ValueArgument<int>)item).Value;
                        break;
                    case "WarnLatency":
                        this.AppSettings.WarnMaxMSLatencyDBExceeded = ((ValueArgument<int>)item).Value;
                        break;
                    case "RealTime":
                        this.AppSettings.EnableRealtime = true;
                        break;
                    case "Sleep":
                        this.AppSettings.SleepBetweenTransMS = ((ValueArgument<int>)item).Value;
                        break;
                    case "Continuous":
                        this.AppSettings.ContinuousSessions = true;
                        break;
                    case "DisableTimings":
                        this.AppSettings.TimeEvents = false;
                        explictDisableTiming = true;
                        break;
                    case "TimingJsonFile":
                        this.AppSettings.TimingJsonFile = ((ValueArgument<string>)item).Value;
                        break;
                    case "TimingCSVFile":
                        this.AppSettings.TimingCSVFile = ((ValueArgument<string>)item).Value;
                        break;
                    case "EnableHistogram":
                        this.AppSettings.EnableHistogram = ((ValueArgument<bool>)item).Value;
                        break;
                    case "HGRMFile":
                        this.AppSettings.HGRMFile = ((ValueArgument<string>)item).Value;
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

                        throw new ArgumentException(string.Format("Do not know how to map {0} ({1}) to a setting!",
                                                                    item.LongName,
                                                                    item.GetType().Name),
                                                                    item.LongName);
                }
            }
            
            if(!explictDisableTiming && this.AppSettings.TimeEvents)
            {
                this.AppSettings.TimeEvents = !(string.IsNullOrEmpty(this.AppSettings.TimingJsonFile)
                                                    && string.IsNullOrEmpty(this.AppSettings.TimingCSVFile));
            }

            return true;
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
                                                PlayerGeneration.Program.GetOSInfo(),
                                                PlayerGeneration.Program.GetFrameWorkInfo());
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
