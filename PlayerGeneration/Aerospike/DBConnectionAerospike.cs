using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Common;
using Common.Patterns.Tasks;
using Aerospike.Client;
using System.Reflection;
using Newtonsoft.Json;

namespace PlayerGeneration
{    
    public sealed partial class DBConnection : IDBConnection
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
                        throw new ArgumentException($"Either the Namespace or the Set Name is missing. Name provided: \"{fullSetName}\"", "Namepace.Set Name");

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


                    if (Attribute.IsDefined(property, typeof(JsonPropertyAttribute)))
                    {
                        var attrValue = Attribute.GetCustomAttribute(property, typeof(Newtonsoft.Json.JsonPropertyAttribute), false);
                        var newPropName = ((JsonPropertyAttribute)attrValue)?.PropertyName;

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
                if (Attribute.IsDefined(p, typeof(JsonPropertyAttribute)))
                {
                    var attrValue = Attribute.GetCustomAttribute(p, typeof(JsonPropertyAttribute), false);
                    return ((JsonPropertyAttribute)attrValue)?.PropertyName;
                }
                return null;
            }

        }
        

        public static (string dbName, 
                        string driverName, 
                        Version driverVersion) GetInfo()
        {
            var asyncClient = typeof(Aerospike.Client.AsyncClient).Assembly.GetName();
            return ("Aerospike Driver",
                    asyncClient?.Name,
                    asyncClient?.Version);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="seedNode"></param>
        /// <param name="port"></param>
        /// <param name="connectionTiimeout"></param>
        /// <param name="operationalTimeout"></param>
        /// <param name="useExternalIp"></param>        
        /// <param name="displayProgression"></param>
        /// <param name="playerProgression"></param>
        /// <param name="historyProgression"></param>
        /// <param name="autoConnect"></param>
        public DBConnection([NotNull] string seedNode,
                            int port,
                            int connectionTiimeout,
                            int operationalTimeout,
                            bool useExternalIp,
                            ConsoleDisplay displayProgression = null,
                            ConsoleDisplay playerProgression = null,
                            ConsoleDisplay historyProgression = null,
                            bool autoConnect = true)
        {
            this.ConnectionTiimeout = connectionTiimeout;
            this.OperationalTimeout = operationalTimeout;
            this.Seednode = seedNode;
            this.Port = port;
            this.UseExternalIP = useExternalIp;

            this.CurrentPlayersSet = new NamespaceSetName(Settings.Instance.CurrentPlayersSetName);
            this.PlayersHistorySet = new NamespaceSetName(Settings.Instance.PlayersHistorySetName);
            this.PlayersTransHistorySet = new NamespaceSetName(Settings.Instance.PlayersTransHistorySetName);
            this.UsedEmailCntSet = new NamespaceSetName(Settings.Instance.UsedEmailCntSetName);
            this.GlobalIncrementSet = new NamespaceSetName(Settings.Instance.GlobalIncrementSetName);
            this.LiverWagerSet = new NamespaceSetName(Settings.Instance.LiveWagerSetName);
            this.InterventionSet = new NamespaceSetName(Settings.Instance.InterventionSetName);
            this.InterventionThresholdsSet = new NamespaceSetName(Settings.Instance.InterventionThresholdsSetName);

            this.ConsoleProgression = new Progression(displayProgression, "Aerospike Connection", null);
            this.PlayerProgression = playerProgression;
            this.HistoryProgression = historyProgression;
            
            Logger.Instance.InfoFormat("DBConnection:");
            Logger.Instance.InfoFormat("\tSeed Node: {0}\tPort: {1} Use Alter Address: {2}", Seednode, Port, UseExternalIP);
            Logger.Instance.InfoFormat("\tConnection Timeout: {0}", ConnectionTiimeout);
            
            Logger.Instance.InfoFormat("\tSets:");
            if(this.CurrentPlayersSet.IsEmpty())
                Logger.Instance.Warn("\t\tCurrent Player will NOT be processed (Empty namespace/set)");
            else
                Logger.Instance.InfoFormat("\t\tPlayer: {0}", this.CurrentPlayersSet);
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
            if (this.InterventionThresholdsSet.IsEmpty())
                Logger.Instance.Warn("\t\tIntervention Thresholds will NOT be processed (Empty namespace/set)");
            else
                Logger.Instance.InfoFormat("\t\tInterventionThresholdsSet: {0}", this.InterventionThresholdsSet);

            if (autoConnect)
                this.Connect();
        }

        public string Seednode { get; }
        public int Port { get; }
        public int ConnectionTiimeout { get; }
        public int OperationalTimeout { get; }
        public bool UseExternalIP { get; }

        public readonly NamespaceSetName CurrentPlayersSet;
        public readonly NamespaceSetName PlayersHistorySet;
        public readonly NamespaceSetName PlayersTransHistorySet;
        public readonly NamespaceSetName UsedEmailCntSet;
        public bool UsedEmailCntEnabled { get => !this.UsedEmailCntSet.IsEmpty(); }
        public bool IncrementGlobalEnabled { get => !GlobalIncrementSet.IsEmpty(); }
        public bool LiverWagerEnabled { get => !LiverWagerSet.IsEmpty(); }

        public readonly NamespaceSetName GlobalIncrementSet;
        public readonly NamespaceSetName InterventionSet;
        public readonly NamespaceSetName LiverWagerSet;
        public readonly NamespaceSetName InterventionThresholdsSet;

        public Progression ConsoleProgression { get; }
        public ConsoleDisplay PlayerProgression { get; }
        public ConsoleDisplay HistoryProgression { get; }

        public AsyncClient Connection { get; private set; }

        public void Connect()
        {
            using var progression = new Progression(this.ConsoleProgression, "Connection");

            Logger.Instance.Info("DBConnection.Connect Start");

            
            var policy = new AsyncClientPolicy();

            if(Settings.Instance.MaxSocketIdle >= 0)
                policy.maxSocketIdle = Settings.Instance.MaxSocketIdle;
            if (Settings.Instance.MaxConnectionPerNode > 0)
                policy.asyncMaxConnsPerNode = Settings.Instance.MaxConnectionPerNode;
            if (Settings.Instance.MinConnectionPerNode >= 0)
                policy.asyncMinConnsPerNode = Settings.Instance.MinConnectionPerNode;
            if (Settings.Instance.CompletionPortThreads > 0)
            {               
                policy.asyncMaxCommands = Settings.Instance.CompletionPortThreads;
                policy.asyncMaxCommandAction = MaxCommandAction.DELAY;
            }
            if(Settings.Instance.asyncBufferSize > 0)
                policy.asyncBufferSize = Settings.Instance.asyncBufferSize;
            if (Settings.Instance.connPoolsPerNode > 0)
                policy.connPoolsPerNode = Settings.Instance.connPoolsPerNode;

            policy.timeout = this.ConnectionTiimeout;
            policy.loginTimeout = this.ConnectionTiimeout;
            policy.useServicesAlternate = this.UseExternalIP;
            policy.maxErrorRate = Settings.Instance.maxErrorRate;
            policy.errorRateWindow = Settings.Instance.errorRateWindow;
            policy.tendInterval = Settings.Instance.tendInterval;

            Logger.Instance.Dump(policy, Logger.DumpType.Info, "\tConnection Policy", 2);            

            this.Connection = new AsyncClient(policy, this.Seednode, this.Port);

            
            this.CreateWritePolicy();
            this.CreateReadPolicies();
            this.CreateListPolicies();

            Logger.Instance.Info("DBConnection.Connect End");
            Logger.Instance.InfoFormat("\tNodes: {0}", string.Join(", ", Connection.Nodes.Select(n => n.NodeAddress.Address)));
            Logger.Instance.InfoFormat("\tInvalid Nodes: {0}", Connection.GetClusterStats().invalidNodeCount);
        }
        
        public WritePolicy WritePolicy { get; private set; }
        public Policy ReadPolicy { get; private set; }
        public ListPolicy ListPolicy { get; private set; }

        public void Truncate()
        {

            Logger.Instance.Info("DBConnection.Truncate Start");

            using var consoleTrunc = new Progression(this.ConsoleProgression, "Truncating...");

            void Truncate(NamespaceSetName namespaceSetName)
            {
                if (!namespaceSetName.IsEmpty())
                {
                    try
                    {
                        this.Connection.Truncate(null,
                                                    namespaceSetName.Namespace,
                                                    namespaceSetName.SetName,
                                                    DateTime.Now);
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.Error($"DBConnection.Truncate {namespaceSetName}",
                                                ex);
                    }
                }
            }

            Truncate(this.PlayersTransHistorySet);
            Truncate(this.PlayersHistorySet);            
            Truncate(this.CurrentPlayersSet);
            Truncate(this.UsedEmailCntSet);
            Truncate(this.GlobalIncrementSet);
            Truncate(this.InterventionSet);
            Truncate(this.LiverWagerSet);
            
            Logger.Instance.Info("DBConnection.Truncate End");
        }

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
