using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLineParser.Arguments;

namespace GameDashBoard
{
    public class ConsoleArgumentsGDB : PlayerCommon.ConsoleArguments
    {
        public new SettingsGDB AppSettings { get; }

        public ConsoleArgumentsGDB(SettingsGDB appSettings)
            : base(appSettings)
        {
            AppSettings = appSettings;

            this._cmdLineParser.Arguments.Add(new ValueArgument<int>('u', "NumberOfDashboardSessions")
            {
                Optional = true, //Required                
                DefaultValue = appSettings.Config.NumberOfDashboardSessions,
                Description = "The number of Dashboards per Session"
            });

            this._cmdLineParser.Arguments.Add(new ValueArgument<int>('f', "SessionRefreshRateSecs")
            {
                Optional = true, //Required                
                DefaultValue = appSettings.Config.SessionRefreshRateSecs,
                Description = "Number of Seconds to poll for updates"
            });

            this._cmdLineParser.Arguments.Add(new ValueArgument<int>('m', "MaxNbrTransPerSession")
            {
                Optional = true, //Required                
                DefaultValue = appSettings.Config.MaxNbrTransPerSession,
                Description = "Maximum number of transaction range per session"
            });

            this._cmdLineParser.Arguments.Add(new ValueArgument<int>('n', "MinNbrTransPerSession")
            {
                Optional = true, //Required                
                DefaultValue = appSettings.Config.MaxNbrTransPerSession,
                Description = "Minimum number of transaction range per session"
            });
            
            this._cmdLineParser.Arguments.Add(new ValueArgument<string>('s', "StartDate")
            {
                Optional = true, //Required                
                DefaultValue = appSettings.Config.StartDate
                                .ToString(appSettings.TimeStampFormatString),
                Description = "Start time used for polling"
            });

            this._cmdLineParser.Arguments.Add(new ValueArgument<int>('p', "Sleep")
            {
                DefaultValue = appSettings.Config.SleepBetweenTransMS,
                Optional = true, //Required                
                Description = "Sleep between Transactions in MS"
            });

            this._cmdLineParser.Arguments.Add(new SwitchArgument('r', "RealTime", false)
            {
                Optional = true, //Required                
                Description = "Enables Real Time Dates/Times"
            });

            //ContinuousSessions
            this._cmdLineParser.Arguments.Add(new SwitchArgument('c', "Continuous", false)
            {
                DefaultValue = appSettings.Config.ContinuousSessions,
                Optional = true, //Required                
                Description = "Players will have unlimited session play"
            });
            
        }

        public override bool ParseSetArguments(string[] args, bool throwIfNotMpaaed = true)
        {
            if (!CheckArgsAndHelp(ref args))
                return false;

            if (!base.ParseSetArguments(args, false))
                return false;

            foreach (var item in this.RemainingArgs)
            {
                switch (item.LongName)
                {
                    case "NumberOfDashboardSessions":
                        this.AppSettings.Config.NumberOfDashboardSessions = ((ValueArgument<int>)item).Value;
                        break;
                    case "SessionRefreshRateSecs":
                        this.AppSettings.Config.SessionRefreshRateSecs = ((ValueArgument<int>)item).Value;
                        break;
                    case "MaxNbrTransPerSession":
                        this.AppSettings.Config.MaxNbrTransPerSession = ((ValueArgument<int>)item).Value;
                        break;
                    case "MinNbrTransPerSession":
                        this.AppSettings.Config.MinNbrTransPerSession = ((ValueArgument<int>)item).Value;
                        break;
                    case "StartDate":
                        this.AppSettings.Config.StartDate = DateTimeOffset.Parse(((ValueArgument<string>)item).Value);
                        break;
                    case "Sleep":
                        this.AppSettings.Config.SleepBetweenTransMS = ((ValueArgument<int>)item).Value;
                        break;
                    case "RealTime":
                        this.AppSettings.Config.EnableRealtime = true;
                        break;
                    case "Continuous":
                        this.AppSettings.Config.ContinuousSessions = true;
                        break;                   
                    default:
                        if (throwIfNotMpaaed)
                            ConsoleArgumentsGDB.ThrowArgumentException(item);
                        break;
                }
            }

            return true;
        }
    }
}
