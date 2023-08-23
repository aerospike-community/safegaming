using Common;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.Json;
using ECM = Microsoft.Extensions.Configuration;
using System.Linq;
using System.Collections;
using static Common.ConsoleWriterAsyc;

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

        const string ConfigNullValue = "!<null>!";
        static string CheckSpecialValue(string strValue)
        {
            if (string.IsNullOrEmpty(strValue)
                || strValue[0] == '#'
                || strValue.ToLower() == "<ignore>") return null;

            if (strValue.ToLower() == "<default>"
                    || strValue.ToLower() == "<null>")
                strValue = ConfigNullValue;
            else if (strValue.ToLower() == "<empty>")
                strValue = string.Empty;

            return strValue;            
        }

        public static object SettingConvertValue(Type propertyType, string configValue)
        {

            if (string.IsNullOrEmpty(configValue))
                return null;

            {
                var checkedValue = CheckSpecialValue(configValue);

                if (checkedValue is null) return null;
                if (checkedValue == ConfigNullValue)
                    configValue = null;
            }

            if (propertyType == typeof(string))
                return configValue;

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
                return SettingConvertValue(underNllableType, configValue);
            }

            throw new NotImplementedException($"Cannot convert \"appsetting\" Property Type {propertyType.Name}");
        }

        public static (object value, bool handled) SettingConvertValueType(Type propertyType,
                                                            ECM.IConfiguration config,
                                                            string value,
                                                            string fullPath,
                                                            string propName,
                                                            bool noTypeCheck = false)
        {

            if (noTypeCheck
                || propertyType.IsValueType
                || propertyType.IsEnum
                || propertyType == typeof(string)
                || Nullable.GetUnderlyingType(propertyType) is not null)
            {
                try
                {
                    var funcActions = InvokeFuncPathAction(fullPath,
                                                            config,
                                                            propName,
                                                            propertyType,
                                                            value);
                    switch (funcActions.actions)
                    {
                        case InvokePathActions.ContinueAndUseValue:
                        case InvokePathActions.Update:
                            return (funcActions.value, true);
                        case InvokePathActions.Ignore:
                            return (null, false);
                        default:
                            break;
                    }

                    value = CheckSpecialValue(value);

                    if (value is null) return (null, false);
                    if (value == ConfigNullValue)
                    {
                        return (propertyType.GetDefaultValue(), true);
                    }

                    return (SettingConvertValue(propertyType, value), true);
                }
                catch (System.Exception ex)
                {
                    throw new ArgumentException($"Invalid \"appsetting\" Property \"{fullPath ?? propName}\" of type {propertyType.Name} with value \"{value}\"", ex);
                }

            }
            return (null, false);
        }

        public static void GetSetting(ECM.IConfiguration config, ref string property, string propName)
        {
            var value = config[propName];
            var funcActions = InvokeFuncPathAction(propName,
                                                    config,
                                                    propName,
                                                    typeof(string),
                                                    value);

            switch (funcActions.actions)
            {
                case InvokePathActions.ContinueAndUseValue:                        
                case InvokePathActions.Update:
                    property = funcActions.value?.ToString();
                    updatedProps.Add(propName);
                    return;
                case InvokePathActions.Ignore:
                    return;
                default:
                    break;
            }
            
            value = CheckSpecialValue(value);

            if (value is null) return;
            if (value == ConfigNullValue)
                value = null;

            property = value;
            updatedProps.Add(propName);
        }
        
        public static void GetSetting<T>(ECM.IConfiguration config, ref T property, string propName, string fullPath = null)
                            where T : new()
        {          
            {
                var checkValueType = SettingConvertValueType(typeof(T),
                                                        config,
                                                        config[propName],
                                                        fullPath,
                                                        propName);
                if(checkValueType.handled)
                {
                    property = (T) checkValueType.value;
                    updatedProps.Add(fullPath ?? propName);
                    return;
                }
            }

            var findProp = config is ECM.IConfigurationSection configSection
                                ? configSection
                                : config.GetChildren().FirstOrDefault(x => x.Key == propName);

            if(findProp is null) return;

            var propType = property?.GetType() ?? typeof(T);            
            var funcActionsInstance = InvokeFuncPathAction(fullPath,
                                                            config,
                                                            propName,
                                                            propType,
                                                            property);
            switch (funcActionsInstance.actions)
            {
                case InvokePathActions.ContinueAndUseValue:
                    property = (T) funcActionsInstance.value; 
                    break;
                case InvokePathActions.Update:
                    property = (T)funcActionsInstance.value;
                    updatedProps.Add(fullPath ?? propName);
                    return;
                case InvokePathActions.Ignore:
                    return;
                default:
                    break;
            }

            var instanceProps = TypeHelpers.GetPropertyFields(propType);
            var newInstance = property is null ? (T)Activator.CreateInstance(propType) : property;
            
            void SetValue(PropertyFieldInfo propertyFieldInfo, ECM.IConfigurationSection childSection, object instance)
            {
                if(childSection.Value is null)
                {
                    var fndInstance = propertyFieldInfo.GetValue(instance);

                    if (fndInstance is null)
                    {
                        var funcActionsInstance = InvokeFuncPathAction(childSection.Path,
                                                                        childSection,
                                                                        childSection.Key,
                                                                        propertyFieldInfo.ItemType,
                                                                        null,
                                                                        instance);
                        switch (funcActionsInstance.actions)
                        {
                            case InvokePathActions.ContinueAndUseValue:
                                fndInstance = funcActionsInstance.value;
                                break;
                            case InvokePathActions.Update:
                                propertyFieldInfo.SetValue(instance, funcActionsInstance.value);
                                updatedProps.Add(childSection.Path);
                                return;
                            case InvokePathActions.Ignore:
                                return;
                            default:
                                break;
                        }

                        fndInstance ??= Activator.CreateInstance(propertyFieldInfo.ItemType);
                        GetSetting(childSection, ref fndInstance, childSection.Key, childSection.Path);
                        
                        if (fndInstance is null
                                && !updatedProps.Contains(childSection.Path))
                            return;

                        propertyFieldInfo.SetValue(instance, fndInstance);                        
                    }
                    else
                    {
                        GetSetting(childSection, ref fndInstance, childSection.Key, childSection.Path);
                    }
                }
                else
                {
                    var checkValueType = SettingConvertValueType(propertyFieldInfo.ItemType,
                                                            childSection,
                                                            childSection.Value,
                                                            childSection.Path,
                                                            childSection.Key,
                                                            true);
                    if (checkValueType.handled)
                    {
                        propertyFieldInfo.SetValue(instance, checkValueType.value);
                        updatedProps.Add(fullPath ?? propName);                        
                    }                    
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
                            var convertedValue = SettingConvertValue(propType.GenericTypeArguments[0], value);
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
            updatedProps.Add(fullPath ?? propName);
        }

        public static void RemoveNotFoundSettingClassProps(IEnumerable<string> removeProps)
                                => NotFoundSettingClassProps.RemoveAll(p => removeProps.Any(r => p.StartsWith(r)));
        

        public static readonly List<string> NotFoundSettingClassProps = new();
        private static readonly List<string> updatedProps = new();
        public static IEnumerable<string> UpdatedProps { get =>  updatedProps; }

        /// <summary>
        /// A list of <see cref="KeyValuePair"/> where
        /// Key -- is the complete setting/config path (e.g., root:level1:level2) or wild card value at the start or end of the path like *:level2 or root:*.
        /// Value is an function with the following arguments:
        ///     IConfiguration children below this path level,
        ///     property name,
        ///     property type,
        ///     property value,
        ///     property&apos;s parent
        ///     returns the object based on type that will be used to set the property. The action is used to determine processing.        
        /// </summary>
        public static readonly List<KeyValuePair<string, Func<ECM.IConfiguration, string, Type, object, object, (object,InvokePathActions)>>> PathActions = new();

        public static int RemoveFuncPathAction(string path)
                            => PathActions.RemoveAll(kvp => kvp.Key == path
                                                            || (path[0] == '*'
                                                                    && kvp.Key.Remove('*').EndsWith(path[1..]))
                                                            || (path.Last() == '*'
                                                                    && kvp.Key.Remove('*').EndsWith(path[..^1])));        

        public static void AddFuncPathAction(string path, Func<ECM.IConfiguration, string, Type, object, object, (object, InvokePathActions)> action)
        {
            RemoveFuncPathAction(path);

            PathActions.Add(new KeyValuePair<string, Func<ECM.IConfiguration, string, Type, object, object, (object, InvokePathActions)>>(path, action));
        }

        [Flags]
        public enum InvokePathActions
        {
            /// <summary>
            /// Continue Path Processing (do not update or exist processing)
            /// </summary>
            Continue = 0,
            /// <summary>
            /// The function handled the request. This is used in conjunction with Update or Ignore.
            /// </summary>
            Handled = 0x0001,
            /// <summary>
            /// Continue processing but use the value returned by the function
            /// </summary>
            ContinueAndUseValue = 0x0010 | Handled,            
            /// <summary>
            /// A value is returned from the function and the property should be updated.
            /// </summary>
            Update = 0x0100 | Handled,
            /// <summary>
            /// Do not continue processing nor update the property.
            /// </summary>
            Ignore = 0x1000 | Handled
        }
        public static (object value, InvokePathActions actions) InvokeFuncPathAction(string path, ECM.IConfiguration children, string propertyName, Type propertyType, object propertyValue, object propParent = null)
        {
            if (PathActions.Count == 0) return (propertyValue, InvokePathActions.Continue);

            var action = PathActions.FirstOrDefault(kvp => kvp.Key == path 
                                                            || (kvp.Key[0] == '*'
                                                                    && path.EndsWith(kvp.Key[1..]))
                                                            || (kvp.Key.Last() == '*'
                                                                    && path.EndsWith(kvp.Key[..^1])));
            var actions = InvokePathActions.Continue;
            object value = null;

            if(action.Value is not null)
            {
                var actionValue = action.Value.Invoke(children, propertyName, propertyType, propertyValue, propParent);
                value = actionValue.Item1;
                actions = actionValue.Item2;
            }

            return (value, actions);
        }

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
