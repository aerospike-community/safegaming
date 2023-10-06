using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Common;
using Common.Patterns.Tasks;
using Aerospike.Client;
using System.Reflection;
using System.Text.Json.Serialization;

namespace PlayerCommon
{    
    public sealed partial class DBConnection : IDisposable
    {
        public const string SystemTag = "Aerospike";
        public const string PutTag = "Put";
        public const string GetTag = "Get";
        public const string OperationTag = "Operation";
        public const string TransformTag = "Transform";

        public readonly struct NamespaceSetName
        {
            public NamespaceSetName(string fullSetName)
            {
                if (!string.IsNullOrEmpty(fullSetName))
                {
                    var splitName = fullSetName.Split('.');

                    if (splitName.Length <= 1)
                        throw new ArgumentException($"Either the Namespace or the Set Name is missing. Name provided: \"{fullSetName}\"", nameof(fullSetName));

                    Namespace = splitName[0].Trim();
                    SetName = splitName[1].Trim();
                }
            }

            public readonly string Namespace = null;
            public readonly string SetName = null;

            public override string ToString()
            {
                return $"{Namespace}.{SetName}";
            }

            public bool IsEmpty()
            {
                return this.Namespace == null || this.SetName == null;
            }
        }

        public static class DBHelpers
        {

            public static Dictionary<string, object> TransForm(object instance)
            {
                var dictionary = new Dictionary<string, object>();

                foreach (var property in instance.GetType().GetProperties())
                {
                    var propName = property.Name;

                    if (Attribute.IsDefined(property, typeof(JsonIgnoreAttribute)))
                        continue;


                    if (Attribute.IsDefined(property, typeof(JsonPropertyNameAttribute)))
                    {
                        var attrValue = Attribute.GetCustomAttribute(property, typeof(JsonPropertyNameAttribute), false);
                        var newPropName = ((JsonPropertyNameAttribute)attrValue)?.Name;

                        if (!string.IsNullOrEmpty(newPropName))
                            propName = newPropName;
                    }

                    switch (property.PropertyType.Name)
                    {
                        case "Game":
                            var gameInstance = property.GetValue(instance);
                            dictionary.Add(propName, gameInstance == null ? null : TransForm(gameInstance));
                            break;
                        case "Metrics":
                        case "Session":
                        case "FinTransaction":
                        case "WagerResultTransaction":
                            dictionary.Add(propName, TransForm(property.GetValue(instance)));
                            break;
                        case "Types":
                        case "Tiers":
                            dictionary.Add(propName, property.GetValue(instance).ToString());
                            break;
                        case "DateTimeOffset":
                            //2019-09-26T07:58:30.996+0200
                            //2022-09-24T09:46:09.0000-04:00
                            dictionary.Add(propName, ((DateTimeOffset)property.GetValue(instance)).ToString(Settings.Instance.TimeStampFormatString));
                            break;
                        case "Decimal":
                            dictionary.Add(propName, ((double)Decimal.Round((decimal)property.GetValue(instance), 2)));
                            break;
                        case "Nullable`1":
                            {
                                var value = property.GetValue(instance);
                                if (value is Nullable<DateTimeOffset> && ((Nullable<DateTimeOffset>)value).HasValue)
                                    dictionary.Add(propName, ((Nullable<DateTimeOffset>)value).Value.ToString("yyyy-MM-ddTHH:mm:ss.ffffzzz"));
                                else
                                {
                                    dictionary.Add(propName, property.GetValue(instance));
                                }
                            }
                            break;
                        case "List`1":
                            {
                                var curList = (System.Collections.IEnumerable)property.GetValue(instance);
                                var newList = new List<object>();
                                foreach (var element in curList)
                                {
                                    newList.Add(TransForm(element));
                                }

                                if (propName == "WagersResults")
                                    newList.Reverse();

                                dictionary.Add(propName, newList);
                            }
                            break;
                        case "Queue`1":
                            {
                                var curQueue = new List<object>(((Queue<object>)property.GetValue(instance)));
                                var newList = new List<object>();
                                bool reverse = (((dynamic)curQueue.First()).Timestamp < ((dynamic)curQueue.Last()).Timestamp);

                                foreach (var element in curQueue)
                                {
                                    newList.Add(TransForm(element));
                                }

                                if (reverse)
                                    newList.Reverse();
                                dictionary.Add(propName, newList);
                            }
                            break;
                        default:
                            dictionary.Add(propName, property.GetValue(instance));
                            break;
                    }
                }

                return dictionary;
            }

            public static Bin[] CreateBinRecord(object item, string prefix = null, params Bin[] additionalBiins)
            {
                var bins = new List<Aerospike.Client.Bin>(additionalBiins);

                if (item is Dictionary<string, object> dict)
                {
                    foreach (var property in dict)
                    {
                        var binName = prefix == null ? property.Key : $"{prefix}.{property.Key}";

                        if (property.Value is List<object> lstValue)
                        {
                            bins.Add(new Bin(binName, lstValue));
                        }
                        else if (property.Value is Dictionary<string, object> dictValue)
                        {
                            bins.Add(new Bin(binName, dictValue));
                        }
                        else
                        {
                            bins.Add(new Bin(binName, property.Value));
                        }
                    }
                }
                else if (item is List<object> lst)
                {
                    foreach (var value in lst)
                    {
                        if (value is List<object> lstValue)
                        {
                            bins.AddRange(CreateBinRecord(lstValue, prefix));
                        }
                        else if (value is Dictionary<string, object> dictValue)
                        {
                            bins.AddRange(CreateBinRecord(dictValue, prefix));
                        }
                        else
                        {
                            bins.Add(new Bin(prefix, lst));
                            break;
                        }
                    }
                }
                else
                {
                    bins.Add(new Bin(prefix, item));
                }

                return bins.ToArray();
            }


            public static (string name, string binName, PropertyInfo pInfo)[] GetPropertyBins<T>()
            {
                return typeof(T).GetProperties()
                                    .Where(p => !Attribute.IsDefined(p, typeof(JsonIgnoreAttribute)))
                                    .Select(p => (p.Name, GetBinName(p) ?? p.Name, p)).ToArray();
            }

            public static string GetBinName(PropertyInfo p)
            {
                if (Attribute.IsDefined(p, typeof(JsonPropertyNameAttribute)))
                {
                    var attrValue = Attribute.GetCustomAttribute(p, typeof(JsonPropertyNameAttribute), false);
                    return ((JsonPropertyNameAttribute)attrValue)?.Name;
                }
                return null;
            }

        }
                    
        public DBConnection(AerospikeSettings settings,
                                ConsoleDisplay displayProgression,
                                bool autoConnect = true)
        {
            this.ASSettings = settings;
            this.ConsoleProgression = new Progression(displayProgression, "Aerospike Connection", null);
            
            this.CurrentPlayersSet = new NamespaceSetName(this.ASSettings.CurrentPlayersSetName);
#if WRITEDB
            this.PlayersHistorySet = new NamespaceSetName(this.ASSettings.PlayersHistorySetName);
            this.PlayersTransHistorySet = new NamespaceSetName(this.ASSettings.PlayersTransHistorySetName);
            this.UsedEmailCntSet = new NamespaceSetName(this.ASSettings.UsedEmailCntSetName);
            this.InterventionThresholdsSet = new NamespaceSetName(this.ASSettings.InterventionThresholdsSetName);
#endif

            this.GlobalIncrementSet = new NamespaceSetName(this.ASSettings.GlobalIncrementSetName);
            this.LiverWagerSet = new NamespaceSetName(this.ASSettings.LiveWagerSetName);
            this.InterventionSet = new NamespaceSetName(this.ASSettings.InterventionSetName);
            
            Logger.Instance.InfoFormat("DBConnection:");
            Logger.Instance.InfoFormat("\tSeed Node: {0}\tPort: {1} Use Alter Address: {2}",
                                            this.ASSettings.DBHost, 
                                            this.ASSettings.DBPort,
                                            this.ASSettings.ClientPolicy.useServicesAlternate);
            Logger.Instance.InfoFormat("\tConnection Timeout: {0}", this.ASSettings.ClientPolicy.loginTimeout);
            
            Logger.Instance.InfoFormat("\tSets:");
            if(this.CurrentPlayersSet.IsEmpty())
                Logger.Instance.Warn("\t\tCurrent Player will NOT be processed (Empty namespace/set)");
            else
                Logger.Instance.InfoFormat("\t\tPlayer: {0}", this.CurrentPlayersSet);
#if WRITEDB
            if (this.PlayersHistorySet.IsEmpty())
                Logger.Instance.Warn("\t\tPlayer History will NOT be processed (Empty namespace/set)");
            else
                Logger.Instance.InfoFormat("\t\tHistory: {0}", this.PlayersHistorySet);
            if (this.PlayersTransHistorySet.IsEmpty())
                Logger.Instance.Warn("\t\tPlayer Trans History will NOT be processed (Empty namespace/set)");
            else
                Logger.Instance.InfoFormat("\t\tHistory Trans: {0}", this.PlayersTransHistorySet);
            if (this.UsedEmailCntSet.IsEmpty())
                Logger.Instance.Info("\t\tUsed Email Counter will NOT be processed (Empty namespace/set)");
            else
                Logger.Instance.InfoFormat("\t\tUsedEmailCntSet: {0}", this.UsedEmailCntSet);
            if (this.InterventionThresholdsSet.IsEmpty())
                Logger.Instance.Warn("\t\tIntervention Thresholds will NOT be processed (Empty namespace/set)");
            else
                Logger.Instance.InfoFormat("\t\tInterventionThresholdsSet: {0}", this.InterventionThresholdsSet);
#endif

            if (this.InterventionSet.IsEmpty())
                Logger.Instance.Warn("\t\tIntervention Set will NOT be processed (Empty namespace/set)");
            else
                Logger.Instance.InfoFormat("\t\tInterventionSet: {0}", this.InterventionSet);
            if (this.GlobalIncrementSet.IsEmpty())
                Logger.Instance.Warn("\t\tLGlobal Increment will NOT be processed (Empty namespace/set)");
            else
                Logger.Instance.InfoFormat("\t\tGlobalIncrementSet: {0}", this.GlobalIncrementSet);
            if (this.LiverWagerSet.IsEmpty())
                Logger.Instance.Warn("\t\tLive Wager will NOT be processed (Empty namespace/set)");
            else
                Logger.Instance.InfoFormat("\t\tLiverWagerSet: {0}", this.LiverWagerSet);

            this.WritePolicy = new WritePolicy(this.ASSettings.ClientPolicy.writePolicyDefault);
            Logger.Instance.Dump(this.WritePolicy, Logger.DumpType.Info, "\tWrite Policy", 2);
            this.ReadPolicy = new Policy(this.ASSettings.ClientPolicy.readPolicyDefault);
            Logger.Instance.Dump(this.ReadPolicy, Logger.DumpType.Info, "\tRead Policy", 2);
            this.QueryPolicy = new QueryPolicy(this.ASSettings.ClientPolicy.queryPolicyDefault);
            Logger.Instance.Dump(this.QueryPolicy, Logger.DumpType.Info, "\tQuery Policy", 2);

#if WRITEDB
            this.CreateListPolicies();
#endif

            if (autoConnect)
                this.Connect();
        }
       
        public AerospikeSettings ASSettings { get; }
        public readonly NamespaceSetName CurrentPlayersSet;
#if WRITEDB
        public readonly NamespaceSetName PlayersHistorySet;
        public readonly NamespaceSetName PlayersTransHistorySet;
        public readonly NamespaceSetName UsedEmailCntSet;
        public readonly NamespaceSetName InterventionThresholdsSet;
        public bool UsedEmailCntEnabled { get => !this.UsedEmailCntSet.IsEmpty(); }
#else
        public bool UsedEmailCntEnabled { get => false; }
#endif

        public readonly NamespaceSetName GlobalIncrementSet;
        public readonly NamespaceSetName InterventionSet;
        public readonly NamespaceSetName LiverWagerSet;
                
        public bool IncrementGlobalEnabled { get => !GlobalIncrementSet.IsEmpty(); }
        public bool LiverWagerEnabled { get => !LiverWagerSet.IsEmpty(); }
        public bool InterventionEnabled { get => !InterventionSet.IsEmpty(); }
        
        public Progression ConsoleProgression { get; }
                
        public WritePolicy WritePolicy { get; private set; }
        public Policy ReadPolicy { get; private set; }
        public ListPolicy ListPolicy { get; private set; }
        public QueryPolicy QueryPolicy { get; private set; }

        #region Disposable
        public bool Disposed { get; private set; }

        private void Dispose(bool disposing)
        {
            if (!this.Disposed)
            {
                if (disposing)
                {
                    this.Connection?.Dispose();
                    this.Connection = null;
                    
                    //this.ConsoleProgression?.Dispose();
                    this.ConsoleProgression?.End();
                }
               
                this.Disposed = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
