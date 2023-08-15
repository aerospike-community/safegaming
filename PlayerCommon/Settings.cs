using Common;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.Json;
using ECM = Microsoft.Extensions.Configuration;
using System.Linq;
using System.Text.Json.Nodes;

namespace PlayerCommon
{
    public partial class Settings
    {

        private static readonly Lazy<Settings> lazy = new(() => new Settings());
        private static readonly Lazy<ECM.IConfigurationBuilder> configurationBuilder = new(() => new ECM.ConfigurationBuilder());
        public static Settings Instance
        {
            get => lazy.Value;
        }

        public static ECM.IConfigurationBuilder ConfigurationBuilder
        {
            get => configurationBuilder.Value;
        }

        public Settings(string appJsonFile = "appsettings.json")
        {
            var configBuilderFile = ECM.JsonConfigurationExtensions.AddJsonFile(ConfigurationBuilder, appJsonFile);
            ECM.IConfiguration config = configBuilderFile.Build();

            GetSetting(config, ref this.IgnoreFaults, nameof(IgnoreFaults));            
            GetSetting(config, ref this.WarnMaxMSLatencyDBExceeded, nameof(WarnMaxMSLatencyDBExceeded));            
            GetSetting(config, ref this.TimeStampFormatString, nameof(TimeStampFormatString));
            GetSetting(config, ref this.TimeEvents, nameof(TimeEvents));
            GetSetting(config, ref this.TimingCSVFile, nameof(TimingCSVFile));
            GetSetting(config, ref this.TimingJsonFile, nameof(TimingJsonFile));
            GetSetting(config, ref EnableHistogram, nameof(EnableHistogram));
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
            
            GetSetting(config, ref this.WorkerThreads, nameof(WorkerThreads));
            GetSetting(config, ref this.CompletionPortThreads, nameof(CompletionPortThreads));
            GetSetting(config, ref this.MaxDegreeOfParallelism, nameof(MaxDegreeOfParallelism));
            
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

        private static JsonNode BuildJson(ECM.IConfiguration configuration)
        {
            if (configuration is ECM.IConfigurationSection configurationSection)
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

        public int MaxDegreeOfParallelism = -1;
        public int WorkerThreads = -1;
        public int CompletionPortThreads = 1000;
        public bool IgnoreFaults = false;
        public bool TimeEvents = true;
       
        public int WarnMaxMSLatencyDBExceeded = 50;
        
        public bool EnableHistogram = true;
        public string HGRMFile = null;
        public int HGPrecision = 3;
        public long HGLowestTickValue = 1;
        public long HGHighestTickValue = 6000000000;
        public int HGReportPercentileTicksPerHalfDistance = 5;
        public string HGReportTickToUnitRatio = "Milliseconds";
        public double HGReportUnitRatio = HdrHistogram.OutputScalingFactor.TimeStampToMilliseconds;
        public string TimingCSVFile;
        public string TimingJsonFile;

        public string TimeStampFormatString = "yyyy-MM-ddTHH:mm:ss.ffffzzz";
        public string TimeZoneFormatWoZone = "yyyy-MM-ddTHH:mm:ss.ffff";
    }
}
