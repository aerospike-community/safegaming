using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Common.Diagnostic;
using Common.Patterns.Tasks;
using Aerospike.Client;
using System.Reflection;
using Newtonsoft.Json;

namespace PlayerGeneration
{    
    public sealed partial class DBConnection : IDBConnection
    {
        const string SystemTag = "Aerospike";
        const string PutTag = "Put";
        const string GetTag = "Get";
        const string OperationTag = "Operation";
        const string TransformTag = "Transform";

        private bool disposedValue;

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

            this.WritePolicy = new Aerospike.Client.WritePolicy()
            {
                sendKey = true,
                socketTimeout = this.OperationalTimeout,
                totalTimeout = this.OperationalTimeout * 3,
                compress = Settings.Instance.EnableDriverCompression,
                maxRetries = Settings.Instance.maxRetries
            };

            Logger.Instance.Dump(WritePolicy, Logger.DumpType.Info, "\tWrite Policy", 2);

            this.ReadPolicy = new Policy(this.Connection.readPolicyDefault);

            Logger.Instance.Dump(ReadPolicy, Logger.DumpType.Info, "\tRead Policy", 2);

            this.ListPolicy = new ListPolicy(ListOrder.UNORDERED, ListWriteFlags.DEFAULT);
            Logger.Instance.Dump(ListPolicy, Logger.DumpType.Info, "\tRead Policy", 2);

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

        public async Task UpdateCurrentPlayers(Player player,
                                                bool updateHistory,
                                                CancellationToken cancellationToken)
        {            
            if (Logger.Instance.IsDebugEnabled)
                Logger.Instance.DebugFormat("DBConnection.UpdateCurrentPlayers Run Start Player: {0}",
                                                player.PlayerId);

            cancellationToken.ThrowIfCancellationRequested();

            this.PlayerProgression.Increment("Player", $"Transforming/Putting Players {player.UserName}...");

            if (Logger.Instance.IsDebugEnabled)
                Logger.Instance.DebugFormat("DBConnection.UpdateCurrentPlayers Run Start Transform Player: {0}",
                                                player.PlayerId);

            var stopWatch = Stopwatch.StartNew();
            var propDict = DBHelpers.TransForm(player);

            stopWatch.StopRecord(TransformTag,
                                    SystemTag,
                                    this.CurrentPlayersSet.SetName,
                                    nameof(UpdateCurrentPlayers),
                                    player.PlayerId);

            if (Logger.Instance.IsDebugEnabled)
                Logger.Instance.DebugFormat("DBConnection.UpdateCurrentPlayers Run End Transform Player: {0} Dict Cnt: {1}",
                                                player.PlayerId,
                                                propDict.Count);

            if (!this.CurrentPlayersSet.IsEmpty())            
            {                
                stopWatch.Restart();

                await this.Connection.Put(WritePolicy,
                                                    cancellationToken,
                                                    new Key(this.CurrentPlayersSet.Namespace,
                                                        this.CurrentPlayersSet.SetName,
                                                        player.PlayerId),
                                                    DBHelpers.CreateBinRecord(propDict))
                    .ContinueWith(task =>
                    {
                        stopWatch.StopRecord(PutTag,
                                                SystemTag,
                                                this.CurrentPlayersSet.SetName,
                                                nameof(UpdateCurrentPlayers),
                                                player.PlayerId);

                        if (Logger.Instance.IsDebugEnabled)
                        {
                            Logger.Instance.DebugFormat("DBConnection.UpdateCurrentPlayers Run End Put {0} Elapsed Time (ms): {1}",
                                                        player.PlayerId,
                                                        stopWatch.ElapsedMilliseconds);
                        }

                        if (stopWatch.ElapsedMilliseconds > Settings.Instance.WarnMaxMSLatencyDBExceeded)
                            Logger.Instance.WarnFormat("DBConnection.UpdateCurrentPlayers Run Exceeded Latency Threshold for Put {1}. Latency: {0}",
                                                        stopWatch.ElapsedMilliseconds,
                                                        player.PlayerId);

                        if (Settings.Instance.WarnIfObjectSizeBytes > 0)
                        {
                            //Hate this, but this is easiest way to get object size...
                            var str = player.ToJSON();

                            if (str.Length > Settings.Instance.WarnIfObjectSizeBytes)
                            {
                                Logger.Instance.WarnFormat("DBConnection.UpdateHistory(Player) Run Exceeded Object Size Threshold (WarnIfObjectSizeBytes {1}) for Put {2}. Size: {0}",
                                                                str.Length,
                                                                Settings.Instance.WarnIfObjectSizeBytes,
                                                                player.PlayerId);
                            }
                        }

                        if (task.IsFaulted || task.IsCanceled)
                        {
                            Program.CanceledFaultProcessing($"DBConnection.UpdateCurrentPlayers Put {player.PlayerId}", task.Exception, Settings.Instance.IgnoreFaults);
                            if (Settings.Instance.IgnoreFaults && !task.IsCanceled)
                            {
                                task.Exception?.Handle(e => true);
                            }
                            else
                                return false;
                        }

                        return true;
                    },
                    cancellationToken,
                    TaskContinuationOptions.AttachedToParent | TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
            }

            this.PlayerProgression.Decrement("Player");

            if (updateHistory)
            {
                await this.UpdateHistory(player,
                                            player.History,
                                            cancellationToken);
            }
            
            if (Logger.Instance.IsDebugEnabled)
                Logger.Instance.DebugFormat("DBConnection.UpdateCurrentPlayers Run End Player: {0}",
                                                player.PlayerId);

            return;
        }

       
        /// <summary>
        /// This will only update the Session, Fin, Game sections based on the bool argument.
        /// The Metrics is always updated
        /// </summary>
        /// <param name="player"></param>
        /// <param name="updateFin"></param>
        /// <param name="updateGame"></param>
        /// <param name="updateWagerResult"><</param>
        /// <param name="updateSession"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task UpdateChangedCurrentPlayer(Player player,
                                                        CancellationToken cancellationToken,
                                                        bool updateSession = false,
                                                        int updateFin = 0,
                                                        bool updateGame = false,
                                                        int updateWagerResult = 0)
        {
            if (Logger.Instance.IsDebugEnabled)
                Logger.Instance.DebugFormat("DBConnection.UpdateChangedCurrentPlayer Run Start Player: {0} updateSession: {1}, updateFin {2}, updateGame: {3}, updateLastNbrTrans: {4}",
                                                player.PlayerId,
                                                updateSession,
                                                updateFin,
                                                updateGame,
                                                updateWagerResult);

            cancellationToken.ThrowIfCancellationRequested();

            this.PlayerProgression.Increment("Player", $"Transforming/Putting Players Components {player.UserName}...");


            async Task CallDBPut(Key key, IEnumerable<Bin> updateBins)
            {
               var stopWatch = Stopwatch.StartNew();

                await this.Connection.Put(WritePolicy,
                                                    cancellationToken,
                                                    key,
                                                    updateBins.ToArray())
                    .ContinueWith(task =>
                    {
                        stopWatch.StopRecord(PutTag,
                                                SystemTag,
                                                key.setName,
                                                nameof(UpdateChangedCurrentPlayer),
                                                key.userKey.Object);

                        if (Logger.Instance.IsDebugEnabled)
                        {
                            Logger.Instance.DebugFormat("DBConnection.UpdateChangedCurrentPlayer Run End Put {0} Elapsed Time (ms): {1}",
                                                        key,
                                                        stopWatch.ElapsedMilliseconds);
                        }

                        if (stopWatch.ElapsedMilliseconds > Settings.Instance.WarnMaxMSLatencyDBExceeded)
                            Logger.Instance.WarnFormat("DBConnection.UpdateChangedCurrentPlayer Run Exceeded Latency Threshold for Put {1}. Latency: {0}",
                                                        stopWatch.ElapsedMilliseconds,
                                                        key);

                        if (Settings.Instance.WarnIfObjectSizeBytes > 0)
                        {
                            //Hate this, but this is easiest way to get object size...
                            var str = player.ToJSON();

                            if (str.Length > Settings.Instance.WarnIfObjectSizeBytes)
                            {
                                Logger.Instance.WarnFormat("DBConnection.UpdateChangedCurrentPlayer Run Exceeded Object Size Threshold (WarnIfObjectSizeBytes {1}) for Put {2}. Size: {0}",
                                                                str.Length,
                                                                Settings.Instance.WarnIfObjectSizeBytes,
                                                                key);
                            }
                        }

                        if (task.IsFaulted || task.IsCanceled)
                        {
                            Program.CanceledFaultProcessing($"DBConnection.UpdateChangedCurrentPlayer Put {player.PlayerId}", task.Exception, Settings.Instance.IgnoreFaults);
                            if (Settings.Instance.IgnoreFaults && !task.IsCanceled)
                            {
                                task.Exception?.Handle(e => true);
                            }
                            else
                                return false;
                        }

                        return true;
                    },
                    cancellationToken,
                    TaskContinuationOptions.AttachedToParent | TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
            }

            async Task CallDBInsertList(Key key,
                                    string binName,
                                    IEnumerable<object> updateList,
                                    int keepSize = 0)
            {
                var stopWatch = Stopwatch.StartNew();

                var record = await this.Connection                               
                                        .Operate(this.WritePolicy,
                                                    cancellationToken,
                                                    key,
                                                    ListOperation.InsertItems(this.ListPolicy,
                                                                                binName,
                                                                                0,
                                                                                updateList.Reverse().ToList()))
                                        .ContinueWith(task =>
                                        {
                                            stopWatch.StopRecord(OperationTag,
                                                                    SystemTag,
                                                                    key.setName,
                                                                    nameof(UpdateChangedCurrentPlayer),
                                                                    key.userKey.Object);

                                            if (Logger.Instance.IsDebugEnabled)
                                            {
                                                Logger.Instance.DebugFormat("DBConnection.UpdateChangedCurrentPlayer Run End List {0} Elapsed Time (ms): {1}",
                                                                            key,
                                                                            stopWatch.ElapsedMilliseconds);
                                            }

                                            if (stopWatch.ElapsedMilliseconds > Settings.Instance.WarnMaxMSLatencyDBExceeded)
                                                Logger.Instance.WarnFormat("DBConnection.UpdateChangedCurrentPlayer Run Exceeded Latency Threshold for List {1}. Latency: {0}",
                                                                            stopWatch.ElapsedMilliseconds,
                                                                            key);

                                            if (Settings.Instance.WarnIfObjectSizeBytes > 0)
                                            {
                                                //Hate this, but this is easiest way to get object size...
                                                var str = player.ToJSON();

                                                if (str.Length > Settings.Instance.WarnIfObjectSizeBytes)
                                                {
                                                    Logger.Instance.WarnFormat("DBConnection.UpdateChangedCurrentPlayer Run Exceeded Object Size Threshold (WarnIfObjectSizeBytes {1}) for List {2}. Size: {0}",
                                                                                    str.Length,
                                                                                    Settings.Instance.WarnIfObjectSizeBytes,
                                                                                    key);
                                                }
                                            }

                                            if (task.IsFaulted || task.IsCanceled)
                                            {
                                                Program.CanceledFaultProcessing($"DBConnection.UpdateChangedCurrentPlayer List {player.PlayerId}", task.Exception, Settings.Instance.IgnoreFaults);
                                                if (Settings.Instance.IgnoreFaults && !task.IsCanceled)
                                                {
                                                    task.Exception?.Handle(e => true);
                                                    return null;
                                                }
                                            }

                                            return task.Result;
                                        },
                    cancellationToken,
                    TaskContinuationOptions.AttachedToParent | TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
                   
                if(keepSize > 0  && record.bins?.ContainsKey(binName) == true)
                {
                    var lstCnt = (long) record.bins[binName];

                    if(lstCnt > keepSize)
                    {
                        var remove = (int) (lstCnt - keepSize);

                        await this.Connection.Operate(this.WritePolicy,
                                                        cancellationToken,
                                                        key,
                                                        ListOperation.RemoveByIndexRange(binName,
                                                                                            remove * -1,
                                                                                            remove,
                                                                                            ListReturnType.NONE))
                            .ContinueWith(task =>
                            {
                                stopWatch.StopRecord(OperationTag,
                                                        SystemTag,
                                                        key.setName,
                                                        nameof(UpdateChangedCurrentPlayer),
                                                        key.userKey.Object);

                                if (Logger.Instance.IsDebugEnabled)
                                {
                                    Logger.Instance.DebugFormat("DBConnection.UpdateChangedCurrentPlayer Remove List {0} Elapsed Time (ms): {1}",
                                                                key,
                                                                stopWatch.ElapsedMilliseconds);
                                }

                                if (stopWatch.ElapsedMilliseconds > Settings.Instance.WarnMaxMSLatencyDBExceeded)
                                    Logger.Instance.WarnFormat("DBConnection.UpdateChangedCurrentPlayer Run Exceeded Latency Threshold for Remove List {1}. Latency: {0}",
                                                                stopWatch.ElapsedMilliseconds,
                                                                key);                                

                                if (task.IsFaulted || task.IsCanceled)
                                {
                                    Program.CanceledFaultProcessing($"DBConnection.UpdateChangedCurrentPlayer Remove List {player.PlayerId}", task.Exception, Settings.Instance.IgnoreFaults);
                                    if (Settings.Instance.IgnoreFaults && !task.IsCanceled)
                                    {
                                        task.Exception?.Handle(e => true);
                                        return null;
                                    }
                                }

                                return task.Result;
                            },
                    cancellationToken,
                    TaskContinuationOptions.AttachedToParent | TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
                    }
                }               
            }

            #region Player Bins Update
            var binstoUpdate = new List<Bin>();

            if(updateSession)
            {
                if (Logger.Instance.IsDebugEnabled)
                    Logger.Instance.DebugFormat("DBConnection.UpdateChangedCurrentPlayer Transform Session Player: {0}",
                                                    player.PlayerId);

                var stopWatch = Stopwatch.StartNew();
                var sessionTransformed = DBHelpers.TransForm(player.Session);

                binstoUpdate.Add(new Bin("Session", sessionTransformed));

                stopWatch.StopRecord(TransformTag,
                                        SystemTag,
                                        this.CurrentPlayersSet.SetName,
                                        nameof(UpdateChangedCurrentPlayer),
                                        player.PlayerId);
            }

            if (updateFin < 0 && player.FinTransactions.Any())
            {
                if (Logger.Instance.IsDebugEnabled)
                    Logger.Instance.DebugFormat("DBConnection.UpdateChangedCurrentPlayer Transform Fin Player: {0}",
                                                    player.PlayerId);

                var stopWatch = Stopwatch.StartNew();
                var finTransformed = player.FinTransactions
                                            .Select(x => DBHelpers.TransForm(x)).ToList();
                stopWatch.StopRecord(TransformTag,
                                        SystemTag,
                                        this.CurrentPlayersSet.SetName,
                                        nameof(UpdateChangedCurrentPlayer),
                                        player.PlayerId);

                binstoUpdate.Add(new Bin("FinTransactions", new Value.ListValue(finTransformed)));
            }

            if (updateGame)
            {
                if (Logger.Instance.IsDebugEnabled)
                    Logger.Instance.DebugFormat("DBConnection.UpdateChangedCurrentPlayer Transform Game Player: {0}",
                                                    player.PlayerId);

                var stopWatch = Stopwatch.StartNew();
                var gameTransformed = DBHelpers.TransForm(player.Game);

                binstoUpdate.Add(new Bin("Game", gameTransformed));

                stopWatch.StopRecord(TransformTag,
                                        SystemTag,
                                        this.CurrentPlayersSet.SetName,
                                        nameof(UpdateChangedCurrentPlayer),
                                        player.PlayerId);
            }

            if (updateWagerResult < 0 && player.WagersResults.Any())
            {
                /*if (Logger.Instance.IsDebugEnabled)
                    Logger.Instance.DebugFormat("DBConnection.UpdateChangedCurrentPlayer Transform WagersResults-List Player: {0}",
                                                    player.PlayerId);

                var wagerresultsTransformed = player.WagersResults
                                                .Select(x => DBHelpers.TransForm(x)).ToList();

                wagerresultsTransformed.Reverse();

                binstoUpdate.Add(new Bin("WagersResults", new Value.ListValue(wagerresultsTransformed)));
                */
                throw new NotSupportedException("Cannot support dumping of all player trans");
            }

            {
                if (Logger.Instance.IsDebugEnabled)
                    Logger.Instance.DebugFormat("DBConnection.UpdateChangedCurrentPlayer Transform Metrics Player: {0}",
                                                    player.PlayerId);

                var stopWatch = Stopwatch.StartNew();
                var metricsTransformed = DBHelpers.TransForm(player.Metrics);
                binstoUpdate.Add(new Bin("Metrics", metricsTransformed));

                binstoUpdate.Add(new Bin("ActiveSession", player.ActiveSession));
                binstoUpdate.Add(new Bin("BingeFlag", player.BingeFlag));
                binstoUpdate.Add(new Bin("Archived", player.Archived));

                stopWatch.StopRecord(TransformTag,
                                        SystemTag,
                                        this.CurrentPlayersSet.SetName,
                                        nameof(UpdateChangedCurrentPlayer),
                                        player.PlayerId);
            }

            if(!this.CurrentPlayersSet.IsEmpty())
                await CallDBPut(new Key(this.CurrentPlayersSet.Namespace, this.CurrentPlayersSet.SetName, player.PlayerId),
                                    binstoUpdate);

            #endregion

            #region List Updates
            if (updateFin > 0 && player.FinTransactions.Any() && !this.CurrentPlayersSet.IsEmpty())
            {
                if (Logger.Instance.IsDebugEnabled)
                    Logger.Instance.DebugFormat("DBConnection.UpdateChangedCurrentPlayer Transform Fin-List Player: {0}",
                                                    player.PlayerId);

                var stopWatch = Stopwatch.StartNew();
                var finTransformed = player.FinTransactions.Take(updateFin)
                                        .Select(x => DBHelpers.TransForm(x));

                stopWatch.StopRecord(TransformTag,
                                        SystemTag,
                                        this.CurrentPlayersSet.SetName,
                                        nameof(UpdateChangedCurrentPlayer),
                                        player.PlayerId);

                await CallDBInsertList(new Key(this.CurrentPlayersSet.Namespace, this.CurrentPlayersSet.SetName, player.PlayerId),
                                            "FinTransactions",
                                            finTransformed,
                                            Settings.Instance.KeepNbrFinTransActions);
            }

            if (updateWagerResult > 0 && player.WagersResults.Any())
            {
                if (Logger.Instance.IsDebugEnabled)
                    Logger.Instance.DebugFormat("DBConnection.UpdateChangedCurrentPlayer Transform WagerResults-List Player: {0}",
                                                    player.PlayerId);

                var stopWatch = Stopwatch.StartNew();
                var wagers = player.WagersResults.TakeLast(updateWagerResult);
                var wagerresultTransformed = wagers.Select(x => DBHelpers.TransForm(x));

                stopWatch.StopRecord(TransformTag,
                                        SystemTag,
                                        this.CurrentPlayersSet.SetName,
                                        nameof(UpdateChangedCurrentPlayer),
                                        player.PlayerId);

                if (!this.CurrentPlayersSet.IsEmpty())
                {
                    await CallDBInsertList(new Key(this.CurrentPlayersSet.Namespace, this.CurrentPlayersSet.SetName, player.PlayerId),
                                                "WagersResults",
                                                wagerresultTransformed,
                                                Settings.Instance.KeepNbrWagerResultTransActions);
                }

                if(wagers.Last().Type != WagerResultTransaction.Types.Wager)
                {
                    var pkey = wagers.Last().Id;

                    if (Logger.Instance.IsDebugEnabled)
                        Logger.Instance.DebugFormat("DBConnection.UpdateChangedCurrentPlayer Transform WagerResults-History-List Player: {0} TransId: {1}",
                                                        player.PlayerId,
                                                        pkey);

                    {
                        var playerSnapshot = new Player(player);

                        stopWatch.Restart();
                        var transform = DBHelpers.TransForm(playerSnapshot);

                        stopWatch.StopRecord(TransformTag,
                                        SystemTag,
                                        this.PlayersHistorySet.SetName,
                                        nameof(UpdateChangedCurrentPlayer),
                                        player.PlayerId);

                        if (!this.PlayersTransHistorySet.IsEmpty())
                        {
                            await CallDBPut(new Key(this.PlayersTransHistorySet.Namespace, this.PlayersTransHistorySet.SetName, pkey),
                                            new Bin[]
                                            {
                                            new Bin("Transaction", transform),
                                            new Bin("PlayerId", player.PlayerId),
                                            new Bin("SessionId", player.Session.Id)
                                            });
                        }
                    }
                    if (!this.PlayersHistorySet.IsEmpty())
                    {
                        await CallDBInsertList(new Key(this.PlayersHistorySet.Namespace, this.PlayersHistorySet.SetName, player.PlayerId),
                                                "PlayerTrans",
                                                new object[] { pkey },
                                                Settings.Instance.PlayerHistoryLastNbrTrans);
                    }
                }
                
            }

            if (Logger.Instance.IsDebugEnabled)
                Logger.Instance.DebugFormat("DBConnection.UpdateChangedCurrentPlayer Run Start Transform Player: {0}",
                                                player.PlayerId);

            #endregion

            this.PlayerProgression.Decrement("Player");

            if (Logger.Instance.IsDebugEnabled)
                Logger.Instance.DebugFormat("DBConnection.UpdateChangedCurrentPlayer Run End Player: {0}",
                                                player.PlayerId);
            return;
        }

        public volatile bool skipLogMsgBypassingHistoryTrans;

        public async Task UpdateHistory(Player forPlayer,
                                            IEnumerable<Player> history,
                                            CancellationToken cancellationToken)
        {
            var forPlayerId = forPlayer.PlayerId;

            if (Logger.Instance.IsDebugEnabled)
            {
                Logger.Instance.Debug("DBConnection.UpdateHistory(Trans) Start");
                Logger.Instance.DebugFormat("\tPlayer: {0} History: {1}",
                                            forPlayerId,
                                            history.Count());
                Logger.Instance.DebugFormat("\tUsed Threads: {0} Completion Ports: {1}",
                                                Connection.GetClusterStats().threadsInUse,
                                                Connection.GetClusterStats().completionPortsInUse);
            }
            
            Guid currentPlayerId = Guid.Empty;

            var transactionsForPlayer = new List<long>();
            
            foreach (var player in history)
            {                   
                cancellationToken.ThrowIfCancellationRequested();
                    
                this.HistoryProgression.Increment("HistoryTrans", $"Putting Player History Tran {player.PlayerId}...");

                if (Logger.Instance.IsDebugEnabled)
                    Logger.Instance.Debug("DBConnection.UpdateHistory(Trans) Run ForEach Start Transform");

                var transId = player.WagersResults.Last().Id;

                var stopWatch = Stopwatch.StartNew();
                var expando = DBHelpers.TransForm(player);

                stopWatch.StopRecord(TransformTag,
                                        SystemTag,
                                        this.PlayersTransHistorySet.SetName,
                                        nameof(UpdateHistory),
                                        player.PlayerId);

                if (Logger.Instance.IsDebugEnabled)
                    Logger.Instance.DebugFormat("DBConnection.UpdateHistory(Trans) Run ForEach End Transform TransId: {0}",
                                                transId);

                //Update History Transactions
                if (!PlayersTransHistorySet.IsEmpty())                
                {                    
                    stopWatch.Restart();

                    await this.Connection.Put(this.WritePolicy,
                                                    cancellationToken,
                                                    new Key(this.PlayersTransHistorySet.Namespace,
                                                                this.PlayersTransHistorySet.SetName,
                                                                transId),
                                                    new Bin("Transaction", expando),
                                                    new Bin("PlayerId", player.PlayerId),
                                                    new Bin("SessionId", player.Session.Id))
                        .ContinueWith(task =>
                        {
                            stopWatch.StopRecord(PutTag,
                                                    SystemTag,
                                                    this.PlayersTransHistorySet.SetName,
                                                    nameof(UpdateHistory),                                                    
                                                    player.PlayerId);

                            if (Logger.Instance.IsDebugEnabled)
                            {
                                Logger.Instance.DebugFormat("DBConnection.UpdateHistory(Player) Run End Put {0} Elapsed Time (ms): {1}",
                                                            transId,
                                                            stopWatch.ElapsedMilliseconds);
                            }

                            if (stopWatch.ElapsedMilliseconds > Settings.Instance.WarnMaxMSLatencyDBExceeded)
                                Logger.Instance.WarnFormat("DBConnection.UpdateHistory(Player) Run Exceeded Latency Threshold for Put {2}-{1}. Latency: {0}",
                                                            stopWatch.ElapsedMilliseconds,
                                                            transId,
                                                            forPlayerId);

                            if (Settings.Instance.WarnIfObjectSizeBytes > 0)
                            {
                                //Hate this, but this is easiest way to get object size...
                                var str = player.ToJSON();

                                if (str.Length > Settings.Instance.WarnIfObjectSizeBytes)
                                {
                                    Logger.Instance.WarnFormat("DBConnection.UpdateHistory(Player) Run Exceeded Object Size Threshold (WarnIfObjectSizeBytes {1}) for Put {3}-{2}. Size: {0}",
                                                                    str.Length,
                                                                    Settings.Instance.WarnIfObjectSizeBytes,
                                                                    transId,
                                                                    forPlayerId);
                                }
                            }

                            if (task.IsFaulted || task.IsCanceled)
                            {
                                Program.CanceledFaultProcessing($"DBConnection.UpdateHistory(Player) Put {transId}", task.Exception, Settings.Instance.IgnoreFaults);
                                if (Settings.Instance.IgnoreFaults && !task.IsCanceled)
                                {
                                    task.Exception?.Handle(e => true);
                                }
                                else
                                    return false;
                            }

                            return true;
                        },
                        cancellationToken,
                        TaskContinuationOptions.AttachedToParent | TaskContinuationOptions.ExecuteSynchronously,
                        TaskScheduler.Default);                    
                }
                
                if (!transactionsForPlayer.Contains(transId))
                {
                    if(Logger.Instance.IsDebugEnabled)
                        Logger.Instance.DebugFormat("DBConnection.UpdateHistory(Trans) Run ForEach Add TransId {0}",
                                                        transId);

                    transactionsForPlayer.Add(transId);
                }
                
                this.HistoryProgression.Decrement("HistoryTrans");

                Logger.Instance.Debug("DBConnection.UpdateHistory(Trans) Run End ForEach");

                this.HistoryProgression.Decrement("HistoryTrans");
            }

            this.HistoryProgression.Increment("HistoryTrans", $"Putting History {forPlayerId}...");

            if (Settings.Instance.PlayerHistoryLastNbrTrans != 0)
            {
                Logger.Instance.Debug("DBConnection.UpdateHistory(Player) Start");

                transactionsForPlayer.Reverse();

                if(Settings.Instance.PlayerHistoryLastNbrTrans > 0
                        && transactionsForPlayer.Count > Settings.Instance.PlayerHistoryLastNbrTrans)
                    transactionsForPlayer = transactionsForPlayer.GetRange(0, Settings.Instance.PlayerHistoryLastNbrTrans);

                //Update Player history
                if (!PlayersHistorySet.IsEmpty())
                {
                    var stopWatch = Stopwatch.StartNew();                    

                    await this.Connection.Put(this.WritePolicy,
                                                    cancellationToken,
                                                    new Key(this.PlayersHistorySet.Namespace,
                                                                this.PlayersHistorySet.SetName,
                                                                forPlayerId),
                                                    new Bin("PlayerTrans", transactionsForPlayer),
                                                    new Bin("State", forPlayer.State),
                                                    new Bin("County", forPlayer.County))
                        .ContinueWith(task =>
                        {
                            stopWatch.StopRecord(PutTag,
                                                    SystemTag,
                                                    this.PlayersTransHistorySet.SetName,
                                                    nameof(UpdateHistory),
                                                    forPlayerId);

                            if (Logger.Instance.IsDebugEnabled)
                            {
                                Logger.Instance.DebugFormat("DBConnection.UpdateHistory(Player) Run End Put {0} Elapsed Time (ms): {1}",
                                                            forPlayerId,
                                                            stopWatch.ElapsedMilliseconds);
                            }

                            if (stopWatch.ElapsedMilliseconds > Settings.Instance.WarnMaxMSLatencyDBExceeded)
                                Logger.Instance.WarnFormat("DBConnection.UpdateHistory(Player) Run Exceeded Latency Threshold for Put {1}. Latency: {0}",
                                                            stopWatch.ElapsedMilliseconds,
                                                            forPlayerId);

                            if (Settings.Instance.WarnIfObjectSizeBytes > 0 && transactionsForPlayer.Count > 0)
                            {
                                //Hate this, but this is easiest way to get object size...
                                var lstSize = transactionsForPlayer.Count * 32;

                                if (lstSize > Settings.Instance.WarnIfObjectSizeBytes)
                                {
                                    Logger.Instance.WarnFormat("DBConnection.UpdateHistory(Player) Run Exceeded Object Size Threshold (WarnIfObjectSizeBytes {1}) for Put {2}. Size: {0} List Length: {3}",
                                                                    lstSize,
                                                                    Settings.Instance.WarnIfObjectSizeBytes,
                                                                    forPlayerId,
                                                                    transactionsForPlayer.Count);
                                }
                            }

                            if (task.IsFaulted || task.IsCanceled)
                            {
                                Program.CanceledFaultProcessing($"DBConnection.UpdateHistory(Player) Put {forPlayerId}", task.Exception, Settings.Instance.IgnoreFaults);
                                if (Settings.Instance.IgnoreFaults && !task.IsCanceled)
                                {
                                    task.Exception?.Handle(e => true);
                                }
                                else
                                    return false;
                            }

                            return true;
                        },
                        cancellationToken,
                        TaskContinuationOptions.AttachedToParent | TaskContinuationOptions.ExecuteSynchronously,
                        TaskScheduler.Default);                    
                }

                if (Logger.Instance.IsDebugEnabled)
                {
                    Logger.Instance.DebugFormat("DBConnection.UpdateHistory(Player) End Trans Ids: {0}", transactionsForPlayer.Count);                   
                }
            }

            if (Logger.Instance.IsDebugEnabled)
            {
                Logger.Instance.Info("DBConnection.UpdateHistory(Trans) End");
                Logger.Instance.DebugFormat("\tUsed Threads: {0} Completion Ports: {1}",
                                                       Connection.GetClusterStats().threadsInUse,
                                                       Connection.GetClusterStats().completionPortsInUse);
            }

            this.HistoryProgression.Decrement("HistoryTrans");

            return;
        }

        readonly Expression incrEmailCnt = Exp.Build(Exp.Cond(Exp.BinExists("count"), Exp.Add(Exp.IntBin("count"), Exp.Val(1)),
                                                                Exp.Val(1)));

        public async Task<string> DeterineEmail(string firstName, string lastName, string domain, CancellationToken token)
        {           
            var email = $"{firstName}.{lastName}@{domain}";

            if (UsedEmailCntSet.IsEmpty()) return email;

            if (Logger.Instance.IsDebugEnabled)
                Logger.Instance.DebugFormat("DBConnection.DeterineEmail Start {0}", email);

            var emailProg = new Progression(this.ConsoleProgression, email);
            
            var key = new Key(this.UsedEmailCntSet.Namespace, this.UsedEmailCntSet.SetName, email);

            var stopWatch = Stopwatch.StartNew();

            var record = await this.Connection.Operate(this.WritePolicy,
                                                        token,
                                                        key,
                                                        ExpOperation.Write("count", incrEmailCnt, ExpWriteFlags.DEFAULT),
                                                        Operation.Get("count"))
                                .ContinueWith(task =>
                                {
                                    stopWatch.StopRecord(OperationTag,
                                                            SystemTag,
                                                            key.setName,
                                                            nameof(DeterineEmail),
                                                            email);

                                    if (Logger.Instance.IsDebugEnabled)
                                    {
                                        Logger.Instance.DebugFormat("DBConnection.DeterineEmail Run End Operation {0} Elapsed Time (ms): {1}",
                                                                    email,
                                                                    stopWatch.ElapsedMilliseconds);
                                    }

                                    if (stopWatch.ElapsedMilliseconds > Settings.Instance.WarnMaxMSLatencyDBExceeded)
                                        Logger.Instance.WarnFormat("DBConnection.DeterineEmail Run Exceeded Latency Threshold for Operation {0}. Latency: {1}",
                                                                    email,
                                                                    stopWatch.ElapsedMilliseconds);


                                    if (task.IsFaulted || task.IsCanceled)
                                    {
                                        Program.CanceledFaultProcessing($"DBConnection.DeterineEmail Operation {email}", task.Exception, Settings.Instance.IgnoreFaults);
                                        if (Settings.Instance.IgnoreFaults && !task.IsCanceled)
                                        {
                                            task.Exception?.Handle(e => true);
                                            return null;
                                        }
                                    }

                                    return task.Result;
                                },
                                    token,
                                    TaskContinuationOptions.AttachedToParent | TaskContinuationOptions.ExecuteSynchronously,
                                    TaskScheduler.Default);

            var count = ((IEnumerable<Object>)record?.bins["count"]).ElementAt(1) ?? -1;

            email = $"{firstName}.{lastName}{count}@{domain}";               
            
            if (Logger.Instance.IsDebugEnabled)
                Logger.Instance.DebugFormat("DBConnection.DeterineEmail End Exists {0}", email);
            
            emailProg.Decrement();

            return email;
        }

        public async Task IncrementGlobalSet(GlobalIncrement glbIncr,
                                                CancellationToken token)
        {
            if(GlobalIncrementSet.IsEmpty()) return;

            if (Logger.Instance.IsDebugEnabled)
                Logger.Instance.DebugFormat("DBConnection.IncrementGlobalSet Start {0}", glbIncr.Key);

            var incrProg = new Progression(this.ConsoleProgression, $"Incrementing Global Set {glbIncr.Key}");

            var stopWatch = Stopwatch.StartNew();

            var key = new Key(this.GlobalIncrementSet.Namespace, this.GlobalIncrementSet.SetName, glbIncr.Key);

            var ggr_amount = (double)decimal.Round(glbIncr.GGR, 2);
            var ggr_amountExp = Exp.Build(Exp.Cond(Exp.BinExists("ggr_amount"), 
                                                            Exp.Add(Exp.FloatBin("ggr_amount"),
                                                                    Exp.Val(ggr_amount) ),
                                                    Exp.Val(ggr_amount) ));

            var interventionsExp = Exp.Build(Exp.Cond(Exp.BinExists("interventions"), 
                                                            Exp.Add(Exp.IntBin("interventions"),
                                                                        Exp.Val(glbIncr.Interventions) ),
                                                            Exp.Val(glbIncr.Interventions) ));

            var trn_countExp = Exp.Build(Exp.Cond(Exp.BinExists("trn_count"),
                                                                    Exp.Add(Exp.IntBin("trn_count"),
                                                                                Exp.Val(glbIncr.Transactions) ),
                                                                    Exp.Val(glbIncr.Transactions) ));
            
            await this.Connection.Operate(this.WritePolicy,
                                            token,
                                            key,
                                            ExpOperation.Write("ggr_amount", ggr_amountExp, ExpWriteFlags.DEFAULT),
                                            ExpOperation.Write("interventions", interventionsExp, ExpWriteFlags.DEFAULT),
                                            ExpOperation.Write("trn_count", trn_countExp, ExpWriteFlags.DEFAULT),
                                            ExpOperation.Write("process_time", Exp.Build(Exp.Val(glbIncr.IntervalTimeStamp.ToString(Settings.Instance.TimeStampFormatString))), ExpWriteFlags.DEFAULT),
                                            ExpOperation.Write("state_code", Exp.Build(Exp.Val(glbIncr.State)), ExpWriteFlags.DEFAULT),
                                            ExpOperation.Write("county_code", Exp.Build(Exp.Val(glbIncr.CountyCode)), ExpWriteFlags.DEFAULT),
                                            ExpOperation.Write("county_name", Exp.Build(Exp.Val(glbIncr.County)), ExpWriteFlags.DEFAULT),
                                            ExpOperation.Write("state_name", Exp.Build(Exp.Val(glbIncr.StateName)), ExpWriteFlags.DEFAULT))
                .ContinueWith(task =>
                {
                    stopWatch.StopRecord(OperationTag,
                                            SystemTag,
                                            key.setName,
                                            nameof(IncrementGlobalSet),                                           
                                            glbIncr.Key);

                    if (Logger.Instance.IsDebugEnabled)
                    {
                        Logger.Instance.DebugFormat("DBConnection.IncrementGlobalSet Run End Operation {0} Elapsed Time (ms): {1}",
                                                    glbIncr.Key,
                                                    stopWatch.ElapsedMilliseconds);
                    }

                    if (stopWatch.ElapsedMilliseconds > Settings.Instance.WarnMaxMSLatencyDBExceeded)
                        Logger.Instance.WarnFormat("DBConnection.IncrementGlobalSet Run Exceeded Latency Threshold for Operation {0}. Latency: {1}",
                                                    glbIncr.Key,
                                                    stopWatch.ElapsedMilliseconds);


                    if (task.IsFaulted || task.IsCanceled)
                    {
                        Program.CanceledFaultProcessing($"DBConnection.IncrementGlobalSet Operation {glbIncr.Key}", task.Exception, Settings.Instance.IgnoreFaults);
                        if (Settings.Instance.IgnoreFaults && !task.IsCanceled)
                        {
                            task.Exception?.Handle(e => true);
                        }
                        else
                            return false;
                    }

                    return true;
                },
                    token,
                    TaskContinuationOptions.AttachedToParent | TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
            
            if (Logger.Instance.IsDebugEnabled)
                Logger.Instance.DebugFormat("DBConnection.IncrementGlobalSet End Exists {0}", glbIncr.Key);            
        }

        public async Task UpdateIntervention(Intervention intervention,
                                                CancellationToken cancellationToken)
        {
            if (Logger.Instance.IsDebugEnabled)
                Logger.Instance.DebugFormat("DBConnection.UpdateIntervention Run Start Player: {0}",
                                                intervention.PlayerId);

            cancellationToken.ThrowIfCancellationRequested();

            this.PlayerProgression.Increment("Intervention", $"Transforming/Putting...");

            if (Logger.Instance.IsDebugEnabled)
                Logger.Instance.DebugFormat("DBConnection.UpdateIntervention Run Start Transform Player: {0}",
                                                intervention.PlayerId);

            var stopWatch = Stopwatch.StartNew();
            var propDict = DBHelpers.TransForm(intervention);

            stopWatch.StopRecord(TransformTag,
                                    SystemTag,
                                    this.InterventionSet.SetName,
                                    nameof(UpdateIntervention),
                                    intervention.PlayerId);

            if (Logger.Instance.IsDebugEnabled)
                Logger.Instance.DebugFormat("DBConnection.UpdateIntervention Run End Transform Player: {0} Dict Cnt: {1}",
                                                intervention.PlayerId,
                                                propDict.Count);

            if (!this.InterventionSet.IsEmpty())
            {
                stopWatch.Restart();

                await this.Connection.Put(WritePolicy,
                                                    cancellationToken,
                                                    new Key(this.InterventionSet.Namespace,
                                                                this.InterventionSet.SetName,
                                                                intervention.PrimaryKey),
                                                    DBHelpers.CreateBinRecord(propDict))
                    .ContinueWith(task =>
                    {
                        stopWatch.StopRecord(PutTag,
                                                SystemTag,
                                                this.InterventionSet.SetName,
                                                nameof(UpdateIntervention),
                                                intervention.PlayerId);

                        if (Logger.Instance.IsDebugEnabled)
                        {
                            Logger.Instance.DebugFormat("DBConnection.UpdateIntervention Run End Put {0} Elapsed Time (ms): {1}",
                                                        intervention.PlayerId,
                                                        stopWatch.ElapsedMilliseconds);
                        }

                        if (stopWatch.ElapsedMilliseconds > Settings.Instance.WarnMaxMSLatencyDBExceeded)
                            Logger.Instance.WarnFormat("DBConnection.UpdateIntervention Run Exceeded Latency Threshold for Put {1}. Latency: {0}",
                                                        stopWatch.ElapsedMilliseconds,
                                                        intervention.PlayerId);                            

                        if (task.IsFaulted || task.IsCanceled)
                        {
                            Program.CanceledFaultProcessing($"DBConnection.UpdateIntervention Put {intervention.PlayerId}", task.Exception, Settings.Instance.IgnoreFaults);
                            if (Settings.Instance.IgnoreFaults && !task.IsCanceled)
                            {
                                task.Exception?.Handle(e => true);
                            }
                            else
                                return false;
                        }

                        return true;
                    },
                    cancellationToken,
                    TaskContinuationOptions.AttachedToParent | TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
            }

            this.PlayerProgression.Decrement("Intervention");

            Logger.Instance.Debug("DBConnection.UpdateIntervention Run End");

            return;
        }

        private static string TimeZoneFormatWoZone = null;
        public async Task UpdateLiveWager(Player player,
                                            WagerResultTransaction wagerResult,
                                            WagerResultTransaction wager,
                                            CancellationToken cancellationToken)
        {
            if (Logger.Instance.IsDebugEnabled)
                Logger.Instance.DebugFormat("DBConnection.UpdateLiveWager Run Start Player: {0}",
                                                player.PlayerId);

            cancellationToken.ThrowIfCancellationRequested();

            this.PlayerProgression.Increment("UpdateLiveWager", $"Transforming/Putting...");
            
            if (!this.LiverWagerSet.IsEmpty())
            {
                var stopWatch = Stopwatch.StartNew();                

                var ts = wagerResult.Timestamp.ToString(Settings.Instance.TimeStampFormatString);
                    
                TimeZoneFormatWoZone ??= Settings.Instance.TimeStampFormatString.Replace('z', ' ').TrimEnd();

                var tsWoZone = wagerResult.Timestamp.UtcDateTime.ToString(TimeZoneFormatWoZone);
                var pk = Helpers.GetLongHash(Environment.CurrentManagedThreadId);

                await this.Connection.Put(WritePolicy,
                                                    cancellationToken,
                                                    new Key(this.LiverWagerSet.Namespace,
                                                                this.LiverWagerSet.SetName,
                                                                pk),
                                                    new Bin("Aggkey", $"{player.PlayerId}:{tsWoZone}:{wager.Amount}"),
                                                    new Bin("bet_type", wagerResult.BetType),
                                                    new Bin("PlayerId", player.PlayerId),
                                                    new Bin("result_type", wagerResult.Type.ToString()),
                                                    new Bin("risk_score", wagerResult.RiskScore),
                                                    new Bin("stake_amount", (double) wager.Amount),
                                                    new Bin("txn_ts", ts),
                                                    new Bin("win_amount",
                                                                wagerResult.Type == WagerResultTransaction.Types.Win
                                                                    ? (double) wagerResult.Amount
                                                                    : 0d),
                                                    new Bin("TransId", wagerResult.Id))
                    .ContinueWith(task =>
                    {
                        stopWatch.StopRecord(PutTag,
                                                SystemTag,
                                                this.LiverWagerSet.SetName,
                                                nameof(UpdateLiveWager),
                                                pk.ToString());

                        if (Logger.Instance.IsDebugEnabled)
                        {
                            Logger.Instance.DebugFormat("DBConnection.UpdateLiveWager Run End Put {0} Elapsed Time (ms): {1}",
                                                        player.PlayerId,
                                                        stopWatch.ElapsedMilliseconds);
                        }

                        if (stopWatch.ElapsedMilliseconds > Settings.Instance.WarnMaxMSLatencyDBExceeded)
                            Logger.Instance.WarnFormat("DBConnection.UpdateLiveWager Run Exceeded Latency Threshold for Put {1}. Latency: {0}",
                                                        stopWatch.ElapsedMilliseconds,
                                                        player.PlayerId);

                        if (task.IsFaulted || task.IsCanceled)
                        {
                            Program.CanceledFaultProcessing($"DBConnection.UpdateLiveWager Put {player.PlayerId}", task.Exception, Settings.Instance.IgnoreFaults);
                            if (Settings.Instance.IgnoreFaults && !task.IsCanceled)
                            {
                                task.Exception?.Handle(e => true);
                            }
                            else
                                return false;
                        }

                        return true;
                    },
                    cancellationToken,
                    TaskContinuationOptions.AttachedToParent | TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);               
            }

            this.PlayerProgression.Decrement("UpdateLiveWager");

            Logger.Instance.Debug("DBConnection.UpdateLiveWager Run End");

            return;
        }


        public async Task<InterventionThresholds> ReFreshInterventionThresholds(InterventionThresholds interventionThresholds,
                                                                                CancellationToken cancellationToken)
        {
            Record record = null;

            if (Logger.Instance.IsDebugEnabled)
                Logger.Instance.DebugFormat("DBConnection.ReFreshInterventionThresholds Run Start Current Version: {0}, Last Refresh Time: {1:HH:mm:ss.ffff}",
                                                interventionThresholds?.Version ?? -1,
                                                interventionThresholds?.RefreshedTime ?? DateTime.MinValue);

            cancellationToken.ThrowIfCancellationRequested();

            this.PlayerProgression.Increment("ReFresh InterventionThresholds", $"Checking...");

            if (!this.InterventionThresholdsSet.IsEmpty())
            {
                var stopWatch = Stopwatch.StartNew();

                record = await this.Connection.Get(this.ReadPolicy,
                                                    cancellationToken,
                                                    new Key(this.InterventionThresholdsSet.Namespace,
                                                                this.InterventionThresholdsSet.SetName,
                                                                interventionThresholds?.Version ?? 0))
                                .ContinueWith(task =>
                                {
                                    stopWatch.StopRecord(GetTag,
                                                            SystemTag,
                                                            this.InterventionThresholdsSet.SetName,
                                                            nameof(ReFreshInterventionThresholds),
                                                            interventionThresholds?.Version ?? 0);

                                    if (Logger.Instance.IsDebugEnabled)
                                    {
                                        Logger.Instance.DebugFormat("DBConnection.ReFreshInterventionThresholds Run End Get {0} Elapsed Time (ms): {1}",
                                                                    interventionThresholds?.Version ?? -1,
                                                                    stopWatch.ElapsedMilliseconds);
                                    }

                                    if (stopWatch.ElapsedMilliseconds > Settings.Instance.WarnMaxMSLatencyDBExceeded)
                                        Logger.Instance.WarnFormat("DBConnection.ReFreshInterventionThresholds Run Exceeded Latency Threshold for Get {1}. Latency: {0}",
                                                                    stopWatch.ElapsedMilliseconds,
                                                                    interventionThresholds?.Version ?? 0);

                                    if (task.IsFaulted || task.IsCanceled)
                                    {
                                        Program.CanceledFaultProcessing($"DBConnection.interventionThresholds Get {interventionThresholds?.Version ?? -1}", task.Exception, Settings.Instance.IgnoreFaults);
                                        if (Settings.Instance.IgnoreFaults && !task.IsCanceled)
                                        {
                                            task.Exception?.Handle(e => true);
                                            return null;
                                        }
                                    }

                                    return task.Result;
                                },
                                cancellationToken,
                                TaskContinuationOptions.AttachedToParent | TaskContinuationOptions.ExecuteSynchronously,
                                TaskScheduler.Default);                
            }

            InterventionThresholds instance;

            if (record is null || record.bins.Count == 0)
                instance = null;
            else 
                instance =  new InterventionThresholds(record.bins);

            this.PlayerProgression.Decrement("ReFresh InterventionThresholds");

            if (Logger.Instance.IsDebugEnabled)
                Logger.Instance.DebugFormat("DBConnection.ReFreshInterventionThresholds Run End with returned version: {0}; Bins: {1}",
                                                record == null ? -2 : interventionThresholds?.Version ?? -1,
                                                record?.bins.Count);
            return instance;
        }      

        #region Disposable
        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    this.Connection?.Dispose();
                    this.Connection = null;
                    
                    //this.ConsoleProgression?.Dispose();
                    this.ConsoleProgression?.End();
                }
               
                disposedValue = true;
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
