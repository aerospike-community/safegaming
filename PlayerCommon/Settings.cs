using Common;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.Json;
using ECM = Microsoft.Extensions.Configuration;
using System.Linq;
using System.Collections;

namespace PlayerCommon
{
    public partial class Settings
    {

        private static readonly Lazy<ECM.IConfigurationBuilder> configurationBuilder = new(() => new ECM.ConfigurationBuilder());
        public static Settings Instance
        {
            get; private set;
        }

        public static ECM.IConfigurationBuilder ConfigurationBuilder
        {
            get => configurationBuilder.Value;
        }

        public ECM.IConfiguration ConfigurationBuilderFile { get; }
        
        public Settings(string appJsonFile = "appsettings.json")
        {
            lock(ConfigurationBuilder)
            {
                Instance = this;
            }
            this.AppJsonFile = appJsonFile;

            var configBuilderFile = ECM.JsonConfigurationExtensions.AddJsonFile(ConfigurationBuilder, appJsonFile);
            this.ConfigurationBuilderFile = configBuilderFile.Build();

            GetSetting(this.ConfigurationBuilderFile, ref IgnoreFaults, nameof(IgnoreFaults));            
            GetSetting(this.ConfigurationBuilderFile, ref WarnMaxMSLatencyDBExceeded, nameof(WarnMaxMSLatencyDBExceeded));            
            GetSetting(this.ConfigurationBuilderFile, ref TimeStampFormatString, nameof(TimeStampFormatString));
            GetSetting(this.ConfigurationBuilderFile, ref TimeEvents, nameof(TimeEvents));
            GetSetting(this.ConfigurationBuilderFile, ref TimingCSVFile, nameof(TimingCSVFile));
            GetSetting(this.ConfigurationBuilderFile, ref TimingJsonFile, nameof(TimingJsonFile));
            GetSetting(this.ConfigurationBuilderFile, ref EnableHistogram, nameof(EnableHistogram));
            GetSetting(this.ConfigurationBuilderFile, ref HGRMFile, nameof(HGRMFile));
            GetSetting(this.ConfigurationBuilderFile, ref HGRMFile, nameof(HGRMFile));
            GetSetting(this.ConfigurationBuilderFile, ref HGPrecision, nameof(HGPrecision));
            GetSetting(this.ConfigurationBuilderFile, ref HGLowestTickValue, nameof(HGLowestTickValue));
            GetSetting(this.ConfigurationBuilderFile, ref HGHighestTickValue, nameof(HGHighestTickValue));
            GetSetting(this.ConfigurationBuilderFile, ref HGReportPercentileTicksPerHalfDistance, nameof(HGReportPercentileTicksPerHalfDistance));
            GetSetting(this.ConfigurationBuilderFile, ref HGReportTickToUnitRatio, nameof(HGReportTickToUnitRatio));

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
            
            GetSetting(this.ConfigurationBuilderFile, ref this.WorkerThreads, nameof(WorkerThreads));
            GetSetting(this.ConfigurationBuilderFile, ref this.CompletionPortThreads, nameof(CompletionPortThreads));
            GetSetting(this.ConfigurationBuilderFile, ref this.MaxDegreeOfParallelism, nameof(MaxDegreeOfParallelism));
            
            TimeZoneFormatWoZone = TimeStampFormatString.Replace('z', ' ').TrimEnd();

            TimeEvents = !(string.IsNullOrEmpty(TimingJsonFile) && string.IsNullOrEmpty(TimingCSVFile));
        }
        
        public static void GetSetting(ECM.IConfiguration config, ref string property, string propName)
        {
            var configValue = config[propName];

            if(string.IsNullOrEmpty(configValue)
                || configValue[0] == '#'
                || configValue.ToLower() == "<ignore>") return;
            
            if (configValue.ToLower() == "<default>"
                    || configValue.ToLower() == "<null>")
                configValue = null;
            else if (configValue.ToLower() == "<empty>")
                configValue = string.Empty;

            property = configValue;
            updatedProps.Add(propName);
        }
        
        public static void GetSetting<T>(ECM.IConfiguration config, ref T property, string propName)
                            where T : new()
        {
            object ConvertValue(Type propertyType, string configValue)
            {
                if (string.IsNullOrEmpty(configValue)) return null;

                if (configValue[0] == '#') return null;
                if (configValue.ToLower() == "<ignore>") return null;
                if (configValue.ToLower() == "<default>")
                {
                    if (propertyType.IsValueType)
                        return propertyType.GetDefaultValue();
                    return "!<null>!";
                }
                if (configValue.ToLower() == "<null>") return "!<null>!";

                if (propertyType == typeof(string))
                {
                    if (configValue.ToLower() == "<empty>")
                        return string.Empty;

                    return configValue;
                }

                if (propertyType == typeof(DateTimeOffset))
                {
                    return DateTimeOffset.Parse(configValue);
                }
                else if (propertyType == typeof(TimeSpan))
                {
                    return TimeSpan.Parse(configValue);
                }
                else if (propertyType.IsEnum)
                {
                    return Enum.Parse(propertyType, configValue);
                }
                else if (propertyType.IsAssignableTo(typeof(IConvertible)))
                {
                    if (propertyType == typeof(Int32))
                        return Convert.ToInt32(configValue);
                    else if (propertyType == typeof(Int16))
                        return Convert.ToInt16(configValue);
                    else if (propertyType == typeof(Int64))
                        return Convert.ToInt64(configValue);
                    else if (propertyType == typeof(Decimal))
                        return Convert.ToDecimal(configValue);
                    else if (propertyType == typeof(Double))
                        return Convert.ToDouble(configValue);
                    else if (propertyType == typeof(bool))
                        return Convert.ToBoolean(configValue);
                    else if (propertyType == typeof(DateTime))
                        return Convert.ToDateTime(configValue);

                    return Convert.ChangeType(configValue, propertyType);
                }

                var underNllableType = Nullable.GetUnderlyingType(propertyType);

                if (underNllableType is not null)
                {
                    return ConvertValue(underNllableType, configValue);
                }

                throw new NotImplementedException($"Cannot convert \"appsetting\" Property Type {propertyType.Name}");
            }

            if (typeof(T).IsValueType
                || typeof(T).IsEnum
                || Nullable.GetUnderlyingType(typeof(T)) is not null)
            {
                try
                {
                    var convertedValue = ConvertValue(typeof(T), config[propName]);

                    if(convertedValue is null) return;
                    if (convertedValue.Equals("!<null>!"))
                        convertedValue = null;

                    property = (T) convertedValue;                    
                }
                catch (System.Exception ex)
                {                   
                    throw new ArgumentException($"Invalid \"appsetting\" Property \"{propName}\" of type {typeof(T).Name}", ex);
                }
                return;
            }

            var findProp = config is ECM.IConfigurationSection configSection
                                ? configSection
                                : config.GetChildren().FirstOrDefault(x => x.Key == propName);

            if(findProp is null) return;

            var propType = property?.GetType() ?? typeof(T);
            var instanceProps = TypeHelpers.GetPropertyFields(propType);
            var newInstance = property is null ? (T)Activator.CreateInstance(propType) : property;
                        
            object CreateObject(Type type, ECM.IConfigurationSection childSection)
            {
                var instance = Activator.CreateInstance(type);
                GetSetting(childSection, ref instance, childSection.Key);
                return instance;
            }

            void SetValue(PropertyFieldInfo propertyFieldInfo, ECM.IConfigurationSection childSection, object instance)
            {
                if(childSection.Value is null)
                {
                    var fndInstance = propertyFieldInfo.GetValue(instance);

                    if (fndInstance is null)
                    {
                        var convertedValue = CreateObject(propertyFieldInfo.ItemType, childSection);

                        if (convertedValue is null) return;

                        propertyFieldInfo.SetValue(instance, convertedValue);
                        updatedProps.Add(childSection.Path);
                    }
                    else
                    {
                        GetSetting(childSection, ref fndInstance, childSection.Key);
                    }
                }
                else
                {
                    var convertedValue = ConvertValue(propertyFieldInfo.ItemType,
                                                        childSection.Value);

                    if (convertedValue is null) return;
                    if(convertedValue.Equals("!<null>!"))
                        convertedValue = null;

                    propertyFieldInfo.SetValue(instance, convertedValue);
                    updatedProps.Add(childSection.Path);
                }
            }

            var children = findProp.GetChildren();
            PropertyFieldInfo instanceProp = null;

            try
            {
                if (newInstance is IList itemList)
                {
                    int idx = 0;
                    string value;

                    do
                    {
                        value = children.FirstOrDefault(k => k.Key == idx.ToString())?.Value;

                        if (value is not null)
                        {
                            var convertedValue = ConvertValue(propType.GenericTypeArguments[0], value);
                            if (convertedValue is not null)
                                itemList.Add(convertedValue);
                        }
                        ++idx;
                    }
                    while (value != null);
                }
                else
                {
                    foreach (var child in children)
                    {
                        instanceProp = instanceProps.FirstOrDefault(p => p.Name == child.Key);
                        if (instanceProp is null)
                        {
                            NotFoundSettingClassProps.Add(child.Path);
                            continue;
                        }
                        SetValue(instanceProp, child, newInstance);
                    }
                }
            }
            catch(System.Exception ex)
            {
                var exceptionFldName = propName;
                var exceptionType = propType;
                if (instanceProp is not null)
                {
                    exceptionFldName += $":{instanceProp.Name}";
                    exceptionType = instanceProp.ItemType;
                }

                throw new ArgumentException($"Invalid \"appsetting\" Property \"{exceptionFldName}\" of type {exceptionType.Name}", ex);
            }

            property = newInstance;
            updatedProps.Add(propName);
        }

        public static void RemoveNotFoundSettingClassProps(IEnumerable<string> removeProps)
                                => NotFoundSettingClassProps.RemoveAll(p => removeProps.Any(r => p.StartsWith(r)));
        

        public static List<string> NotFoundSettingClassProps { get; } = new();
        private static readonly List<string> updatedProps = new();
        public static IEnumerable<string> UpdatedProps { get =>  updatedProps; }

        public string AppJsonFile { get; }

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

        public string DBConnectionString;
    }
}
