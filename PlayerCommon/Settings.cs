using Common;
using System;
using System.Collections.Generic;
using ECM = Microsoft.Extensions.Configuration;
using System.Linq;
using System.Collections;
using Microsoft.Extensions.Configuration.Json;

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

            var configBuilderFile = ECM.JsonConfigurationExtensions.AddJsonFile(ConfigurationBuilder, appJsonFile, false);

            if(appJsonFile != "appsettings.json")
            {
                var asConfig = ConfigurationBuilder
                                .Sources.OfType<JsonConfigurationSource>()
                                .FirstOrDefault(x => x.Path == "appsettings.json");
                if(asConfig is not null)
                    asConfig.Optional = true;
            }

            this.ConfigurationBuilderFile = configBuilderFile.Build();

            GetSetting(this.ConfigurationBuilderFile, ref IgnoreFaults, nameof(IgnoreFaults), this);            
            GetSetting(this.ConfigurationBuilderFile, ref WarnMaxMSLatencyDBExceeded, nameof(WarnMaxMSLatencyDBExceeded), this);
            GetSetting(this.ConfigurationBuilderFile, ref TimeStampFormatString, nameof(TimeStampFormatString), this);
            GetSetting(this.ConfigurationBuilderFile, ref TimeEvents, nameof(TimeEvents), this);
            GetSetting(this.ConfigurationBuilderFile, ref TimingCSVFile, nameof(TimingCSVFile), this);
            GetSetting(this.ConfigurationBuilderFile, ref TimingJsonFile, nameof(TimingJsonFile), this);
            GetSetting(this.ConfigurationBuilderFile, ref EnableHistogram, nameof(EnableHistogram), this);
            GetSetting(this.ConfigurationBuilderFile, ref HGRMFile, nameof(HGRMFile), this);
            GetSetting(this.ConfigurationBuilderFile, ref HGRMFile, nameof(HGRMFile), this);
            GetSetting(this.ConfigurationBuilderFile, ref HGPrecision, nameof(HGPrecision), this);
            GetSetting(this.ConfigurationBuilderFile, ref HGLowestTickValue, nameof(HGLowestTickValue), this);
            GetSetting(this.ConfigurationBuilderFile, ref HGHighestTickValue, nameof(HGHighestTickValue), this);
            GetSetting(this.ConfigurationBuilderFile, ref HGReportPercentileTicksPerHalfDistance, nameof(HGReportPercentileTicksPerHalfDistance), this);
            GetSetting(this.ConfigurationBuilderFile, ref HGReportTickToUnitRatio, nameof(HGReportTickToUnitRatio), this);

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
            
            GetSetting(this.ConfigurationBuilderFile, ref this.WorkerThreads, nameof(WorkerThreads), this);
            GetSetting(this.ConfigurationBuilderFile, ref this.CompletionPortThreads, nameof(CompletionPortThreads), this);
            GetSetting(this.ConfigurationBuilderFile, ref this.MaxDegreeOfParallelism, nameof(MaxDegreeOfParallelism), this);
            
            TimeZoneFormatWoZone = TimeStampFormatString.Replace('z', ' ').TrimEnd();

            TimeEvents = !(string.IsNullOrEmpty(TimingJsonFile) && string.IsNullOrEmpty(TimingCSVFile));
        }

        #region Config Setting Parsing Options
        public const string ConfigNullValue = "!<null>!";
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
                    return ConfigNullValue;

                configValue = checkedValue;
            }

            if (propertyType == typeof(string))
                return configValue;

            configValue = configValue.Trim();

            if (propertyType == typeof(DateTimeOffset))
            {
                if (configValue.ToLower() == "now")
                    return DateTimeOffset.Now;

                return DateTimeOffset.Parse(configValue);
            }
            else if (propertyType == typeof(TimeSpan))
            {
                if (configValue.ToLower() == "now")
                    return DateTime.Now.TimeOfDay;

                if(char.IsLetter(configValue.Last()))
                {
                    try
                    {
                        var letterPos = configValue.IndexOf(c => char.IsLetter(c));
                        var uom = configValue[letterPos..].Trim().ToLower();
                        var num = configValue[..letterPos].Trim();
                        switch (uom)
                        {
                            case "tick":
                            case "ticks":
                            case "t":
                                return TimeSpan.FromTicks(long.Parse(num));
                            case "us":
                            case "usec":
                            case "usecs":
                            case "microseconds":
                            case "microsecond":
                                return TimeSpan.FromMicroseconds(double.Parse(num));
                            case "ns":
                            case "nsec":
                            case "nsecs":
                            case "nano":
                            case "nanosecond":
                            case "nanoseconds":
                                return TimeSpan.FromMicroseconds(double.Parse(num) * 0.001d);
                            case "ms":
                            case "msec":
                            case "msecs":
                            case "milliseconds":
                            case "millisecond":
                                return TimeSpan.FromMilliseconds(double.Parse(num));
                            case "s":
                            case "sec":
                            case "secs":
                            case "seconds":
                            case "second":
                                return TimeSpan.FromSeconds(double.Parse(num));
                            case "m":
                            case "mins":
                            case "min":
                            case "minute":
                            case "minutes":
                                return TimeSpan.FromMinutes(double.Parse(num));
                            case "h":
                            case "hrs":
                            case "hr":
                            case "hours":
                            case "hour":
                                return TimeSpan.FromHours(double.Parse(num));
                            case "d":
                            case "day":
                            case "days":
                                return TimeSpan.FromDays(double.Parse(num));
                            default:
                                throw new ArgumentException($"Undefined unit of time \"{uom}\"");
                        }
                    }
                    catch(Exception ex)
                    {
                        throw new Exception($"Value \"{configValue}\" is not a valid TimeSpan with UOM", ex);
                    }
                }
                else if(configValue.All(c => char.IsNumber(c)))
                {
                    return TimeSpan.FromMilliseconds(double.Parse(configValue));
                }
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
                {
                    if (configValue.ToLower() == "now")
                        return DateTime.Now;
                    return Convert.ToDateTime(configValue);
                }

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
                                                                            object parent,
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
                                                            value,
                                                            parent);
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

                    var convertedValue = SettingConvertValue(propertyType, value);

                    if (convertedValue is null) return (null, false);
                    if (value == ConfigNullValue)
                    {
                        return (propertyType.GetDefaultValue(), true);
                    }

                    return (convertedValue, true);
                }
                catch (System.Exception ex)
                {
                    throw new ArgumentException($"Invalid \"appsetting\" Property \"{fullPath ?? propName}\" of type {propertyType.Name} with value \"{value}\"", ex);
                }

            }
            return (null, false);
        }

        public static void GetSetting(ECM.IConfiguration config, ref string property, string propName, object parentInstance)
        {
            var value = config[propName];
            var funcActions = InvokeFuncPathAction(propName,
                                                    config,
                                                    propName,
                                                    typeof(string),
                                                    value,
                                                    parentInstance);

            switch (funcActions.actions)
            {
                case InvokePathActions.ContinueAndUseValue:                        
                case InvokePathActions.Update:
                    property = funcActions.value?.ToString();
                    updatedProps.Add(propName);
                    SetPathSaveObj(propName, value);
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
            SetPathSaveObj(propName, value);
        }
        
        public static void GetSetting<T>(ECM.IConfiguration config,
                                            ref T property,
                                            string propName,
                                            object parentInstance,
                                            string fullPath = null)
                            where T : new()
        {          
            {
                (object value, bool handled) = SettingConvertValueType(typeof(T),
                                                                        config,
                                                                        config[propName],
                                                                        fullPath,
                                                                        propName,
                                                                        parentInstance);
                if(handled)
                {
                    property = (T) value;
                    updatedProps.Add(fullPath ?? propName);
                    SetPathSaveObj(fullPath ?? propName, value);
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
                                                            property,
                                                            parentInstance);
            switch (funcActionsInstance.actions)
            {
                case InvokePathActions.ContinueAndUseValue:
                    property = (T) funcActionsInstance.value; 
                    break;
                case InvokePathActions.Update:
                    property = (T)funcActionsInstance.value;
                    updatedProps.Add(fullPath ?? propName);
                    SetPathSaveObj(fullPath ?? propName, (T)funcActionsInstance.value);
                    return;
                case InvokePathActions.Ignore:
                    return;
                default:
                    break;
            }

            var instanceProps = TypeHelpers.GetPropertyFields(propType)
                                    .Where(p => p.IsPublicWrite);
            var newInstance = property is null ? (T)Activator.CreateInstance(propType) : property;
            
            static void SetValue(PropertyFieldInfo propertyFieldInfo,
                                    ECM.IConfigurationSection childSection,
                                    object instance)
            {
                if(childSection.Value is null)
                {
                    var fndInstance = propertyFieldInfo.GetValue(instance);

                    if (fndInstance is null)
                    {
                        (object value, InvokePathActions actions) = InvokeFuncPathAction(childSection.Path,
                                                                                            childSection,
                                                                                            childSection.Key,
                                                                                            propertyFieldInfo.ItemType,
                                                                                            null,
                                                                                            instance);
                        switch (actions)
                        {
                            case InvokePathActions.ContinueAndUseValue:
                                fndInstance = value;
                                break;
                            case InvokePathActions.Update:
                                propertyFieldInfo.SetValue(instance, value);
                                updatedProps.Add(childSection.Path);
                                SetPathSaveObj(childSection.Path, value);
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
                        SetPathSaveObj(childSection.Path, value);
                    }
                    else
                    {
                        GetSetting(childSection,
                                        ref fndInstance,
                                        childSection.Key,                                        
                                        instance,
                                        childSection.Path);
                    }
                }
                else
                {
                    (object value, bool handled) = SettingConvertValueType(propertyFieldInfo.ItemType,
                                                                            childSection,
                                                                            childSection.Value,
                                                                            childSection.Path,
                                                                            childSection.Key,
                                                                            instance,
                                                                            true);

                    if (handled)
                    {
                        propertyFieldInfo.SetValue(instance, value);
                        updatedProps.Add(childSection.Path);
                        SetPathSaveObj(childSection.Path, value);
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
                            {
                                if (value == ConfigNullValue)
                                {
                                    itemList.Add(propType.GenericTypeArguments[0].GetDefaultValue());
                                }
                                else
                                    itemList.Add(convertedValue);
                            }                                
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
            SetPathSaveObj(fullPath ?? propName, newInstance);
        }

        public static void RemoveNotFoundSettingClassProps(IEnumerable<string> removeProps)
                                => NotFoundSettingClassProps.RemoveAll(p => removeProps.Any(r => p.StartsWith(r)));
        

        public static readonly List<string> NotFoundSettingClassProps = new();
        private static readonly List<string> updatedProps = new();
        public static IEnumerable<string> UpdatedProps { get =>  updatedProps; }

        public static bool UpdatedPropExists(string path)
                            => UpdatedProps.Any(p => p == path
                                                        || (path[0] == '*'
                                                                && p.Remove('*').EndsWith(path[1..]))
                                                        || (path.Last() == '*'
                                                                && p.Remove('*').EndsWith(path[..^1])));

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
        public static readonly List<KeyValuePair<string, Func<ECM.IConfiguration, string, string, Type, object, object, (object,InvokePathActions)>>> PathActions = new();

        static string FuncPathTypeName(Type propType) => $"Type<{propType.FullName}>";

        public static int RemoveFuncPathAction(string path)
                            => PathActions.RemoveAll(kvp => kvp.Key == path
                                                            || (path[0] == '*'
                                                                    && kvp.Key.Remove('*').EndsWith(path[1..]))
                                                            || (path.Last() == '*'
                                                                    && kvp.Key.Remove('*').EndsWith(path[..^1])));

        public static int RemoveFuncPathAction(Type propType)
                            => PathActions.RemoveAll(kvp => kvp.Key == FuncPathTypeName(propType));

        /// <summary>
        /// Adds a config path and if/when the path is encounter during processing the <paramref name="action"/> is invoked.  
        /// </summary>
        /// <param name="path">Configuration Path e.g., FldA:FldB:FldC</param>
        /// <param name="action">
        ///     1st --IConfiguration children below this path level,
        ///     2nd -- path,
        ///     3rd -- property name,
        ///     4th -- property type,
        ///     5th -- property value,
        ///     6th -- property&apos;s parent
        ///     returns the object based on type that will be used to set the property. The action is used to determine processing.
        /// </param>
        /// <exception cref="ArgumentNullException"></exception>
        public static void AddFuncPathAction(string path, Func<ECM.IConfiguration, string, string, Type, object, object, (object, InvokePathActions)> action)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            
            RemoveFuncPathAction(path);

            PathActions.Add(new KeyValuePair<string, Func<ECM.IConfiguration, string, string, Type, object, object, (object, InvokePathActions)>>(path, action));
        }

        /// <summary>
        /// Adds a  property type and if/when the type is encounter during processing the <paramref name="action"/> is invoked.  
        /// </summary>
        /// <param name="propType">The system type of the property you wish to <paramref name="action"/> on...</param>
        /// <param name="action">
        ///     1st --IConfiguration children below this path level,
        ///     2nd -- Full Path,
        ///     3rd -- property name,
        ///     4th -- property type,
        ///     5th -- property value,
        ///     6th -- property&apos;s parent
        ///     returns the object based on type that will be used to set the property. The action is used to determine processing.
        /// </param>
        /// <exception cref="ArgumentNullException"></exception>
        public static void AddFuncPathAction(Type propType, Func<ECM.IConfiguration, string, string, Type, object, object, (object, InvokePathActions)> action)
        {
            if (propType is null) throw new ArgumentNullException(nameof(propType));

            RemoveFuncPathAction(propType);

            PathActions.Add(new KeyValuePair<string, Func<ECM.IConfiguration, string, string, Type, object, object, (object, InvokePathActions)>>(FuncPathTypeName(propType), action));
        }

        public static readonly List<KeyValuePair<string, object>> PathSaveObject = new();

        public static void RemovePathSaveObj(string path)
            => PathSaveObject.RemoveAll(kvp => kvp.Key == path
                                                || (path[0] == '*'
                                                        && kvp.Key.Remove('*').EndsWith(path[1..]))
                                                || (path.Last() == '*'
                                                        && kvp.Key.Remove('*').EndsWith(path[..^1])));

        /// <summary>
        /// Adds a config path and when this is encounter any associated object will be saved (cached). 
        /// </summary>
        /// <param name="path">Configuration Path e.g., FldA:FldB:FldC</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static void AddPathSaveObj(string path)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));

            RemovePathSaveObj(path);

            PathSaveObject.Add(new KeyValuePair<string, object>(path, null));
        }

        public static object GetPathSaveObj(string path)
                                => PathSaveObject.FirstOrDefault(kvp => kvp.Key == path
                                                                        || (path[0] == '*'
                                                                                && kvp.Key.Remove('*').EndsWith(path[1..]))
                                                                        || (path.Last() == '*'
                                                                                && kvp.Key.Remove('*').EndsWith(path[..^1])))
                                                .Value;
        public static IEnumerable<KeyValuePair<string,Object>> GetAllPathSaveObj(string path)
                                                                    => PathSaveObject.Where(kvp => kvp.Key == path
                                                                                                    || (path[0] == '*'
                                                                                                            && kvp.Key.Remove('*').EndsWith(path[1..]))
                                                                                                    || (path.Last() == '*'
                                                                                                            && kvp.Key.Remove('*').EndsWith(path[..^1])));

        /// <summary>
        /// Sets (caches) an object based on config path.
        /// </summary>
        /// <param name="path">Configuration Path e.g., FldA:FldB:FldC</param>
        /// <param name="saveObj">Object to cache</param>
        /// <param name="replace">True to replace existing object</param>
        /// <returns>
        /// True if saved, false otherwise.
        /// <returns>
        public static bool SetPathSaveObj(string path, object saveObj, bool replace = true)
        {
            if(string.IsNullOrEmpty(path)) return false;

            var fndKVP = PathSaveObject.FirstOrDefault(kvp => kvp.Key == path
                                                                || (path[0] == '*'
                                                                        && kvp.Key.Remove('*').EndsWith(path[1..]))
                                                                || (path.Last() == '*'
                                                                        && kvp.Key.Remove('*').EndsWith(path[..^1])));

            if (fndKVP.Key is null) return false;

            if(!replace &&  fndKVP.Value is not null) return false;

            PathSaveObject.RemoveAll(kvp => kvp.Key == fndKVP.Key);
            PathSaveObject.Add(new KeyValuePair<string, object>(fndKVP.Key, saveObj));
            return true;
        }

        [Flags]
        public enum InvokePathActions
        {
            /// <summary>
            /// Continue Path Processing (do not update or exit processing)
            /// </summary>
            Continue = 0,
            /// <summary>
            /// The path was a defined action and the action was invoked. 
            /// It is used in conjunction with ContinueAndUseValue, Update, or Ignore.
            /// </summary>
            PropFnd = 0x0001,
            /// <summary>
            /// Continue processing but use the value returned by the function
            /// </summary>
            ContinueAndUseValue = 0x0010 | PropFnd,            
            /// <summary>
            /// A value is returned from the function and the property should be updated.
            /// </summary>
            Update = 0x0100 | PropFnd,
            /// <summary>
            /// Do not continue processing nor update the property.
            /// </summary>
            Ignore = 0x1000 | PropFnd
        }
        public static (object value, InvokePathActions actions) InvokeFuncPathAction(string path, ECM.IConfiguration children, string propertyName, Type propertyType, object propertyValue, object propParent)
        {
            if (PathActions.Count == 0) return (propertyValue, InvokePathActions.Continue);
           
            var action = PathActions.FirstOrDefault(kvp => kvp.Key == path 
                                                            || kvp.Key == FuncPathTypeName(propertyType)
                                                            || (kvp.Key[0] == '*'
                                                                    && path.EndsWith(kvp.Key[1..]))
                                                            || (kvp.Key.Last() == '*'
                                                                    && path.EndsWith(kvp.Key[..^1])));
            var actions = InvokePathActions.Continue;
            object value = null;

            if(action.Value is not null)
            {
                var actionValue = action.Value.Invoke(children, path, propertyName, propertyType, propertyValue, propParent);
                value = actionValue.Item1;
                actions = actionValue.Item2;
            }

            return (value, actions);
        }
        #endregion

        #region Config Properties
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
        #endregion
    }
}
