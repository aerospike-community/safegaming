using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommandLineParser.Arguments;

namespace GameSimulator
{
    public class ConsoleArgumentsSim : PlayerCommon.ConsoleArguments
    {
        public new SettingsSim AppSettings { get; }

        public ConsoleArgumentsSim(SettingsSim appSettings)
            : base(appSettings)
        {
            AppSettings = appSettings;

            this._cmdLineParser.Arguments.Add(new ValueArgument<int>('s', "start-key")
            {
                Optional = true, //Required                
                DefaultValue = appSettings.Config.PlayerIdStartRange,
                Description = "The value used to start counting the player id generation"
            });

            this._cmdLineParser.Arguments.Add(new ValueArgument<int>('k', "keys")
            {
                Optional = true, //Required                
                DefaultValue = appSettings.Config.NbrPlayers,
                Description = "The number of players generated (NbrPlayers)"
            });

            this._cmdLineParser.Arguments.Add(new SwitchArgument('r', "RealTime", false)
            {
                Optional = true, //Required                
                Description = "Enables Real Time Dates/Times"
            });

            this._cmdLineParser.Arguments.Add(new ValueArgument<int>('p', "Sleep")
            {
                DefaultValue = appSettings.Config.SleepBetweenTransMS,
                Optional = true, //Required                
                Description = "Sleep between Transactions in MS"
            });
            //ContinuousSessions
            this._cmdLineParser.Arguments.Add(new SwitchArgument('c', "Continuous", false)
            {
                DefaultValue = appSettings.Config.ContinuousSessions,
                Optional = true, //Required                
                Description = "Players will have unlimited session play"
            });
                        
            this._cmdLineParser.Arguments.Add(new ValueArgument<bool>("TruncateSets")
            {
                DefaultValue = appSettings.Config.TruncateSets,
                Optional = true, //Required                
                Description = "True to Truncate the Sets"
            });

            this._cmdLineParser.Arguments.Add(new ValueArgument<bool>("LiveFireForgetTasks")
            {
                DefaultValue = appSettings.Config.LiveFireForgetTasks,
                Optional = true, //Required                
                Description = "True to Fire and Forget Live Sets"
            });

        }
       
        public override bool ParseSetArguments(string[] args, bool throwIfNotMpaaed = true)
        {

            base.ParseSetArguments(args, false);

            foreach (var item in this.RemainingArgs)
            {
                switch (item.LongName)
                {
                    case "start-key":
                        this.AppSettings.Config.PlayerIdStartRange = ((ValueArgument<int>)item).Value;
                        break;
                    case "keys":
                        this.AppSettings.Config.NbrPlayers = ((ValueArgument<int>)item).Value;
                        break;                   
                    case "RealTime":
                        this.AppSettings.Config.EnableRealtime = true;
                        break;
                    case "Sleep":
                        this.AppSettings.Config.SleepBetweenTransMS = ((ValueArgument<int>)item).Value;
                        break;
                    case "Continuous":
                        this.AppSettings.Config.ContinuousSessions = true;
                        break;                    
                    case "TruncateSets":
                        this.AppSettings.Config.TruncateSets = ((ValueArgument<bool>)item).Value;
                        break;
                    case "LiveFireForgetTasks":
                        this.AppSettings.Config.LiveFireForgetTasks = ((ValueArgument<bool>)item).Value;
                        break;                    
                    default:
                        if(throwIfNotMpaaed)
                            ConsoleArgumentsSim.ThrowArgumentException(item);
                        break;
                }
            }

            return true;
        }        
    }
}
