using Common;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.Json;
using ECM = Microsoft.Extensions.Configuration;
using static PlayerGeneration.DateTimeSimulation;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Text.Json.Nodes;

namespace PlayerGeneration
{
    public partial class Settings
    {

        private static readonly Lazy<Settings> lazy = new(() => new Settings());
        public static Settings Instance
        {
            get
            {
                return lazy.Value;
            }
        }


        public Settings(string appJsonFile = "appsettings.json")
            : this(new ECM.ConfigurationBuilder())
        {
            var configBuilderFile = ECM.JsonConfigurationExtensions.AddJsonFile(this.ConfigBuilder, appJsonFile);
            ECM.IConfiguration config = configBuilderFile.Build();

            GetSetting(config, ref this.StateJsonFile, nameof(StateJsonFile));

            GetSetting(config, ref this.UpdateDB, nameof(UpdateDB));
            GetSetting(config, ref this.TruncateSets, nameof(TruncateSets));
            GetSetting(config, ref this.IgnoreFaults, nameof(IgnoreFaults));
            
            if(!UpdatedWarnMaxMSLatencyDBExceeded)
                GetSetting(config, ref this.WarnMaxMSLatencyDBExceeded, nameof(WarnMaxMSLatencyDBExceeded));
            if(!UpdatedWarnIfObjectSizeBytes)
                GetSetting(config, ref this.WarnIfObjectSizeBytes, nameof(WarnIfObjectSizeBytes));
            
            GetSetting(config, ref this.TimeStampFormatString, nameof(TimeStampFormatString));
            GetSetting(config, ref this.HistoricTimeEndMonth, nameof(HistoricTimeEndMonth));
            if (this.HistoricTimeEndMonth <= 0) this.HistoricTimeEndMonth = DateTime.Now.Month;
            GetSetting(config, ref this.HistoricTimeStartMonth, nameof(HistoricTimeStartMonth));
            if (this.HistoricTimeStartMonth <= 0) this.HistoricTimeStartMonth = 1;
            GetSetting(config, ref this.OnlyTheseGamingStates, nameof(OnlyTheseGamingStates));
            GetSetting(config, ref this.KeepNbrFinTransActions, nameof(KeepNbrFinTransActions));
            GetSetting(config, ref this.KeepNbrWagerResultTransActions, nameof(KeepNbrWagerResultTransActions));

            //GetSetting(config, ref this.HistoryJsonFile, nameof(HistoryJsonFile));
            //GetSetting(config, ref this.PlayerJsonFile, nameof(PlayerJsonFile));

            if (!UpdatedTimingEvents)
                GetSetting(config, ref this.TimeEvents, nameof(TimeEvents));
            if (!UpdatedTimingCSVFile)
                GetSetting(config, ref this.TimingCSVFile, nameof(TimingCSVFile));
            if(!UpdatedTimingJsonFile)
                GetSetting(config, ref this.TimingJsonFile, nameof(TimingJsonFile));

            if (!UpdatedEnableHistogram)
                GetSetting(config, ref EnableHistogram, nameof(EnableHistogram));
            if(!UpdatedHGRMFile)
                GetSetting(config, ref HGRMFile, nameof(HGRMFile));
            GetSetting(config, ref HGRMFile, nameof(HGRMFile));
            GetSetting(config, ref HGPrecision, nameof(HGPrecision));
            GetSetting(config, ref HGLowestTickValue, nameof(HGLowestTickValue));
            GetSetting(config, ref HGHighestTickValue, nameof(HGHighestTickValue));
            GetSetting(config, ref HGReportPercentileTicksPerHalfDistance, nameof(HGReportPercentileTicksPerHalfDistance));
            GetSetting(config, ref HGReportTickToUnitRatio, nameof(HGReportTickToUnitRatio));

            if (string.IsNullOrEmpty(HGReportTickToUnitRatio))
            {
                HGReportUnitRatio = HdrHistogram.OutputScalingFactor.None;
            }
            else
            {
                switch (HGReportTickToUnitRatio.ToLower())
                {
                    case "ticks":
                    case "tick":
                        HGReportUnitRatio = HdrHistogram.OutputScalingFactor.None;
                        break;
                    case "nanoseconds":
                    case "nanosecond":
                    case "nano":
                    case "nanos":
                    case "ns":
                        HGReportUnitRatio = TimeSpan.NanosecondsPerTick;
                        break;
                    case "microseconds":
                    case "microsecond":
                    case "mics":
                    case "mic":
                    case "μs":
                        HGReportUnitRatio = HdrHistogram.OutputScalingFactor.TimeStampToMicroseconds;
                        break;
                    case "milliseconds":
                    case "millisecond":
                    case "mills":
                    case "mill":
                    case "ms":
                        HGReportUnitRatio = HdrHistogram.OutputScalingFactor.TimeStampToMilliseconds;
                        break;
                    case "seconds":
                    case "second":
                    case "sec":
                    case "secs":
                    case "s":
                        HGReportUnitRatio = HdrHistogram.OutputScalingFactor.TimeStampToSeconds;
                        break;
                    default:
                        HGReportUnitRatio = double.Parse(HGReportTickToUnitRatio);
                        break;
                }
            }

            GetSetting(config, ref this.NbrPlayers, nameof(NbrPlayers));
            GetSetting(config, ref this.MaxTransPerSession, nameof(MaxTransPerSession));
            GetSetting(config, ref this.MinTransPerSession, nameof(MinTransPerSession));
            GetSetting(config, ref this.MaxPlayerSessions, "UpToPlayerSessions");
            GetSetting(config, ref this.MaxPlayerSessions, nameof(MaxPlayerSessions));
            GetSetting(config, ref this.MinPlayerSessions, nameof(MinPlayerSessions));
            GetSetting(config, ref this.PlayerIdStartRange, nameof(PlayerIdStartRange));
            
            GetSetting(config, ref this.RouletteWinTurns, nameof(RouletteWinTurns));
            GetSetting(config, ref this.SlotsChanceTrigger, nameof(SlotsChanceTrigger));
            GetSetting(config, ref this.SlotsWinTurns, nameof(SlotsWinTurns));

            GetSetting(config, ref this.WorkerThreads, nameof(WorkerThreads));
            GetSetting(config, ref this.CompletionPortThreads, nameof(CompletionPortThreads));
            GetSetting(config, ref this.MaxDegreeOfParallelismGeneration, nameof(MaxDegreeOfParallelismGeneration));
            if(!UpdatedLiveFireForgetTasks)
                GetSetting(config, ref this.LiveFireForgetTasks, nameof(LiveFireForgetTasks));

            GetSetting(config, ref this.InterventionThresholdsRefreshRateSecs, nameof(InterventionThresholdsRefreshRateSecs));
            GetSetting(config, ref this.GlobalIncrementIntervalSecs, nameof(GlobalIncrementIntervalSecs));
            GetSetting(config, ref this.GenerateUniqueEmails, nameof(GenerateUniqueEmails));
            GetSetting(config, ref this.PlayerHistoryLastNbrTrans, nameof(PlayerHistoryLastNbrTrans));

            GetSetting(config, ref MinPlayerSessionRestOverMins, nameof(MinPlayerSessionRestOverMins));
            GetSetting(config, ref MaxPlayerSessionRestOverMins, nameof(MaxPlayerSessionRestOverMins));
            GetSetting(config, ref MaxPlayerSessionRestTriggerMins, nameof(MaxPlayerSessionRestTriggerMins));
            GetSetting(config, ref MinPlayerSessionRestTriggerMins, nameof(MinPlayerSessionRestTriggerMins));
            GetSetting(config, ref MinPlayerSessionRestUnderMins, nameof(MinPlayerSessionRestUnderMins));
            GetSetting(config, ref MaxPlayerSessionRestUnderMins, nameof(MaxPlayerSessionRestUnderMins));
            GetSetting(config, ref BetweenBetTimeIntervalMinSecs, nameof(BetweenBetTimeIntervalMinSecs));
            GetSetting(config, ref BetweenBetTimeIntervalMaxSecs, nameof(BetweenBetTimeIntervalMaxSecs));
            GetSetting(config, ref PlayTimeIntervalMinSecs, nameof(PlayTimeIntervalMinSecs));
            GetSetting(config, ref PlayTimeIntervalMaxSecs, nameof(PlayTimeIntervalMaxSecs));
            GetSetting(config, ref HistoricFromDate, nameof(HistoricFromDate));
            GetSetting(config, ref HistoricToDate, nameof(HistoricToDate));

            {
                string historicMode = null;
                GetSetting(config, ref historicMode, nameof(HistoricMode));

                if(string.IsNullOrEmpty(historicMode))
                    this.HistoricMode = HistoricMode.GoIntoFuture;
                else
                {
                    this.HistoricMode = (HistoricMode)Enum.Parse(typeof(HistoricMode), historicMode);
                }
            }

            if (HistoricFromDate == null) this.EnableRealtime = true;

            GetSetting(config, ref this.SleepBetweenTransMS, nameof(SleepBetweenTransMS));
            GetSetting(config, ref this.ContinuousSessions, nameof(ContinuousSessions));

            TimeZoneFormatWoZone = TimeStampFormatString.Replace('z', ' ').TrimEnd();

            TimeEvents = !(string.IsNullOrEmpty(TimingJsonFile) && string.IsNullOrEmpty(TimingCSVFile));

        }

        public static void GetSetting(ECM.IConfiguration config,
                                            ref int property,
                                            ref bool updatedProperty,
                                            string propName)
        {
            var value = config[propName];

            if (string.IsNullOrEmpty(value)) return;
            
            property = int.Parse(value);
            updatedProperty = true;
        }

        public static void GetSetting(ECM.IConfiguration config,
                                            ref long property,
                                            ref bool updatedProperty,
                                            string propName)
        {
            var value = config[propName];

            if (string.IsNullOrEmpty(value)) return;

            property = long.Parse(value);
            updatedProperty = true;
        }

        public static void GetSetting(ECM.IConfiguration config,
                                            ref bool property,
                                            ref bool updatedProperty,
                                            string propName)
        {
            var value = config[propName];

            if (string.IsNullOrEmpty(value)) return;

            property = bool.Parse(value);
            updatedProperty = true;
        }

        public static void GetSetting(ECM.IConfiguration config,
                                            ref string property,
                                            ref bool updatedProperty,
                                            string propName)
        {
            var value = config[propName];

            if (string.IsNullOrEmpty(value)) return;

            property = value;
            updatedProperty = true;
        }

        public static void GetSetting(ECM.IConfiguration config, ref int property, string propName)
        {
            var value = config[propName];

            if (string.IsNullOrEmpty(value)) return;

            property = int.Parse(value);
        }

        public static void GetSetting(ECM.IConfiguration config, ref long property, string propName)
        {
            var value = config[propName];

            if (string.IsNullOrEmpty(value)) return;

            property = long.Parse(value);
        }


        public static void GetSetting(ECM.IConfiguration config, ref decimal property, string propName)
        {
            var value = config[propName];

            if (string.IsNullOrEmpty(value)) return;

            property = decimal.Parse(value);
        }

        public static void GetSetting(ECM.IConfiguration config, ref bool property, string propName)
        {
            var value = config[propName];

            if (string.IsNullOrEmpty(value)) return;

            property = bool.Parse(value);
        }

        public static void GetSetting(ECM.IConfiguration config, ref string property, string propName)
        {
            var value = config[propName];

            if(value == null) return;

            property = value == String.Empty ? null : value;
        }

        public static void GetSetting(ECM.IConfiguration config, ref List<string> property, string propName)
        {
            //{[OnlyTheseGamingStates:0, 1]}
            string value;
            var items = new List<string>();
            int idx = 0;

            do
            {
                value = config[$"{propName}:{idx++}"];

                if (value != null)
                    items.Add(value);
            }
            while (value != null);
           
            if (items.IsEmpty()) return;

            property =  items;
        }

        private readonly static JsonSerializerOptions jsonSerializerOptions = new()
        {
            IncludeFields = true,
            UnknownTypeHandling = JsonUnknownTypeHandling.JsonElement,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            //NumberHandling = JsonNumberHandling.AllowReadingFromString,
            WriteIndented = true
        };

        private static JsonNode BuildJson(IConfiguration configuration)
        {
            if (configuration is IConfigurationSection configurationSection)
            {
                if (configurationSection.Value != null)
                {
                    var value = configurationSection.Value;

                    if(string.IsNullOrEmpty(value)) return null;
                    
                    if(long.TryParse(value, out var lngValue))
                        return JsonValue.Create(lngValue);
                    if (double.TryParse(value, out var dblValue))
                        return JsonValue.Create(dblValue);
                    if (bool.TryParse(value, out var bValue))
                        return JsonValue.Create(bValue);

                    return JsonValue.Create(configurationSection.Value);                    
                }
            }

            var children = configuration.GetChildren().AsEnumerable();
            if (!children.Any())
            {                
                return null;
            }

            if (children.First().Key == "0")
            {
                var result = new JsonArray();
                foreach (var child in children)
                {
                    result.Add(BuildJson(child));
                }

                return result;
            }
            else
            {
                var result = new JsonObject();
                foreach (var child in children)
                {                   
                    result.Add(new KeyValuePair<string, JsonNode>(child.Key, BuildJson(child)));
                }

                return result;
            }
        }


        public static void GetSetting<T>(ECM.IConfiguration config, ref T property, string propName, JsonSerializerOptions deserializerOptions = null)
                            where T : new()
        {
            var section = config.GetSection(propName);
            
            if (section is null || string.IsNullOrEmpty(section.Value)) return;

            var json = BuildJson(section);
            var value = json.ToJsonString();

            property = (T) JsonSerializer.Deserialize(value, typeof(T), deserializerOptions ?? jsonSerializerOptions);
        }


        public readonly bool UpdatedWarnMaxMSLatencyDBExceeded;
        public readonly bool UpdatedWarnIfObjectSizeBytes;
        public readonly bool UpdatedTimingEvents;
        public readonly bool UpdatedTimingCSVFile;
        public readonly bool UpdatedTimingJsonFile;
        public readonly bool UpdatedEnableHistogram;
        public readonly bool UpdatedHGRMFile;
        public readonly bool UpdatedLiveFireForgetTasks;

        public int MaxDegreeOfParallelismGeneration = -1;
        public int WorkerThreads = -1;
        public int CompletionPortThreads = 1000;
        public bool IgnoreFaults = false;
        public bool TimeEvents = true;
        public bool LiveFireForgetTasks = true;

        public int NbrPlayers = 100;
        public int MinPlayerSessions = 1;
        public int MaxPlayerSessions = 10;
        public int MinPlayerSessionRestTrigger = 20;  
               
        public int MinPlayerSessionRestOverMins = 120;
        public int MinPlayerSessionRestUnderMins = 30;
        public int MinPlayerSessionRestTriggerMins = 20;
        public int MaxPlayerSessionRestTriggerMins = 240;
        public int MaxPlayerSessionRestOverMins = 2880;
        public int MaxPlayerSessionRestUnderMins = 720;
        public int BetweenBetTimeIntervalMinSecs = 4;
        public int BetweenBetTimeIntervalMaxSecs = 10;
        public int PlayTimeIntervalMinSecs = 6;
        public int PlayTimeIntervalMaxSecs = 10;
        public string HistoricFromDate = "2022-06-01 00:00";
        public string HistoricToDate = "Now";
        public bool? EnableRealtime = null;

        public int SleepBetweenTransMS = 0;
        public bool ContinuousSessions = false;

        public int MinTransPerSession = 5;
        public int MaxTransPerSession = 10;
        public readonly List<string> OnlyTheseGamingStates;
        public int PlayerIdStartRange = 500;

        public int PlayerHistoryLastNbrTrans = 10;
        public bool GenerateUniqueEmails = true;
        public readonly int GlobalIncrementIntervalSecs = 1;
        public TimeSpan GlobalIncremenIntervals { get => new(0, 0, GlobalIncrementIntervalSecs); }
        public int InterventionThresholdsRefreshRateSecs = 300;
        public TimeSpan InterventionThresholdsRefreshRate { get => new(0, 0, InterventionThresholdsRefreshRateSecs); }

        public int WarnMaxMSLatencyDBExceeded = 50;
        public int WarnIfObjectSizeBytes = -1;
        public int KeepNbrWagerResultTransActions = 10;
        public int KeepNbrFinTransActions = 2;
        
        public bool UpdateDB  = true;
        public bool TruncateSets = true;
        //public string PlayerJsonFile = "~\\Player.json";
        //public string HistoryJsonFile  = "~\\PlayerHistory.json";
        public string StateJsonFile = ".\\state_database.json";

        public string TimingJsonFile = null;
        public string TimingCSVFile = null;

        public bool EnableHistogram = true;
        public string HGRMFile = null;
        public int HGPrecision = 3;
        public long HGLowestTickValue = 1;
        public long HGHighestTickValue = 6000000000;
        public int HGReportPercentileTicksPerHalfDistance = 5;
        public string HGReportTickToUnitRatio = "Milliseconds";
        public double HGReportUnitRatio = HdrHistogram.OutputScalingFactor.TimeStampToMilliseconds;

        public int RouletteWinTurns = 68;       
        public int SlotsWinTurns = 68;
        public int SlotsChanceTrigger = 62;
        
        public readonly string TimeStampFormatString = "yyyy-MM-ddTHH:mm:ss.ffffzzz";
        public int HistoricTimeStartMonth = 6;
        public int HistoricTimeEndMonth = DateTime.Now.Month;
        public HistoricMode HistoricMode = DateTimeSimulation.HistoricMode.GoIntoFuture;
        public string TimeZoneFormatWoZone = "yyyy-MM-ddTHH:mm:ss.ffff";
    }
}
