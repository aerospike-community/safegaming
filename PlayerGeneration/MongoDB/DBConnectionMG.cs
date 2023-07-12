using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using Common;
using Common.Diagnostic;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using System.Data;
using System.Numerics;

namespace PlayerGeneration
{
    public sealed partial class DBConnection : IDBConnection
    {
        const string SystemTag = "MonogoDB";
        const string InsertTag = "Put";
        const string FindTag = "Find";
        const string UpdateTag = "Update";
        const string FindUpdateTag = "FindUpdate";

        private bool disposedValue;

        public readonly struct DBCollection<T>
        {
            public DBCollection(string dbName,
                                    string collectionName,
                                    IMongoDatabase database)
            {
                dbName = dbName?.Trim();
                collectionName = collectionName?.Trim();

                if (string.IsNullOrEmpty(collectionName) || string.IsNullOrEmpty(dbName))
                {
                    this.IsEmpty = true;
                    return;
                }

                this.DBName = dbName;
                this.CollectionName = collectionName;

                this.Collection = database.GetCollection<T>(CollectionName);
                this.FilterEmpty = Builders<T>.Filter.Empty;
            }

            public readonly Type Type = typeof(T);
            public readonly string DBName = null;
            public readonly string CollectionName = null;
            public readonly IMongoCollection<T> Collection = null;
            public readonly FilterDefinitionBuilder<T> BuildersFilter => Builders<T>.Filter;
            public readonly UpdateDefinitionBuilder<T> BuildersUpdate => Builders<T>.Update;
            public readonly FilterDefinition<T> FilterEmpty = null;

            public override string ToString()
            {
                return $"{DBName}.{CollectionName}";
            }

            public readonly bool IsEmpty = false;
        }

        public class DateTimeOffsetSerializer : SerializerBase<DateTimeOffset>
        {
            public static readonly DateTimeOffsetSerializer Instance = new();

            private static class Fields
            {
                public const string DateTime = "DateTime";
                public const string LocalDateTime = "LocalDateTime";
                public const string Ticks = "Ticks";
                public const string Offset = "Offset";
            }

            public override void Serialize(
                BsonSerializationContext context,
                BsonSerializationArgs args,
                DateTimeOffset value)
            {
                context.Writer.WriteStartDocument();

                context.Writer.WriteName(Fields.DateTime);
                context.Writer.WriteDateTime(
                    BsonUtils.ToMillisecondsSinceEpoch(value.UtcDateTime));

                context.Writer.WriteName(Fields.LocalDateTime);
                context.Writer.WriteDateTime(
                    BsonUtils.ToMillisecondsSinceEpoch(value.UtcDateTime.Add(value.Offset)));

                context.Writer.WriteName(Fields.Offset);
                context.Writer.WriteInt32(value.Offset.Hours * 60 + value.Offset.Minutes);

                context.Writer.WriteName(Fields.Ticks);
                context.Writer.WriteInt64(value.Ticks);

                context.Writer.WriteEndDocument();
            }

            public override DateTimeOffset Deserialize(
                BsonDeserializationContext context,
                BsonDeserializationArgs args)
            {
                context.Reader.ReadStartDocument();

                context.Reader.ReadName();
                context.Reader.SkipValue();

                context.Reader.ReadName();
                context.Reader.SkipValue();

                context.Reader.ReadName();
                var offset = context.Reader.ReadInt32();

                context.Reader.ReadName();
                var ticks = context.Reader.ReadInt64();

                context.Reader.ReadEndDocument();

                return new DateTimeOffset(ticks, TimeSpan.FromMinutes(offset));
            }
        }


        public static (string dbName,
                        string driverName,
                        Version driverVersion) GetInfo()
        {
            var asyncClient = typeof(MongoDB.Driver.MongoClient).Assembly.GetName();
            return ("MongoDB Driver",
                    asyncClient?.Name,
                    asyncClient?.Version);
        }

        public DBConnection([NotNull] string dbConnectionString,
                            string dbName,                            
                            int connectionTiimeout,
                            int operationalTimeout,
                            int heartBeatTimeout,
                            bool compression,
                            ConsoleDisplay displayProgression = null,
                            ConsoleDisplay playerProgression = null,
                            ConsoleDisplay historyProgression = null)
        {           
            this.ConsoleProgression = new Progression(displayProgression, "MongoDB Connection", null);
            this.PlayerProgression = playerProgression;
            this.HistoryProgression = historyProgression;

            this.ConnectionString = dbConnectionString;
            this.ConnectionTimeout = connectionTiimeout;
            this.OperationalTimeout = operationalTimeout;
            this.Compression = compression;
            this.HeartbeatTimeout = heartBeatTimeout;

            Logger.Instance.InfoFormat("DBConnection:");
            Logger.Instance.InfoFormat("\tDB Connection String: {0}", ConnectionString);
            Logger.Instance.InfoFormat("\tDB Name: {0}", dbName);
            Logger.Instance.InfoFormat("\tConnection Timeout: {0}", ConnectionTimeout);
            Logger.Instance.InfoFormat("\tOperational Timeout: {0}", OperationalTimeout);
            Logger.Instance.InfoFormat("\tHeartbeat Timeout: {0}", HeartbeatTimeout);
            Logger.Instance.InfoFormat("\tCompression: {0}", Compression);

            BsonSerializer.RegisterSerializer(new DecimalSerializer(BsonType.Decimal128));
            //BsonSerializer.RegisterSerializer(new DateTimeOffsetSerializer());
            
            this.Client = new MongoClient(this.ConnectionString);
            this.Database = this.Client.GetDatabase(dbName);

            this.CurrentPlayersCollection = new DBCollection<Player>(dbName,
                                                                        Settings.Instance.CurrentPlayersCollection,
                                                                        this.Database);
            this.PlayersHistoryCollection = new DBCollection<PlayerHistory>(dbName,
                                                                            Settings.Instance.PlayersHistoryCollection,
                                                                            this.Database);
            this.PlayersTransHistoryCollection = new DBCollection<PlayersTransHistory>(dbName,
                                                                                        Settings.Instance.PlayersTransHistoryCollection,
                                                                                        this.Database);
            this.UsedEmailCntCollection = new DBCollection<UsedEmailCnt>(dbName,
                                                                            Settings.Instance.UsedEmailCntCollection,
                                                                            this.Database);
            this.GlobalIncrementCollection = new DBCollection<GlobalIncrement>(dbName,
                                                                                Settings.Instance.GlobalIncrementCollection,
                                                                                this.Database);
            this.InterventionCollection = new DBCollection<Intervention>(dbName,
                                                                            Settings.Instance.InterventionCollection,
                                                                            this.Database);
            this.LiverWagerCollection = new DBCollection<LiveWager>(dbName,
                                                                    Settings.Instance.LiveWagerCollection,
                                                                    this.Database);
            this.InterventionThresholdsCollection = new DBCollection<InterventionThresholds>(dbName,
                                                                                            Settings.Instance.InterventionThresholdsCollection,
                                                                                            this.Database);

            Logger.Instance.InfoFormat("\tCollections:");
            if(this.CurrentPlayersCollection.IsEmpty)
                Logger.Instance.Warn("\t\tCurrent Player will NOT be processed (Empty namespace/set)");
            else
                Logger.Instance.InfoFormat("\t\tPlayer: {0}", this.CurrentPlayersCollection);
            if (this.PlayersHistoryCollection.IsEmpty)
                Logger.Instance.Warn("\t\tPlayer History will NOT be processed (Empty namespace/set)");
            else
                Logger.Instance.InfoFormat("\t\tHistory: {0}", this.PlayersHistoryCollection);
            if (this.PlayersTransHistoryCollection.IsEmpty)
                Logger.Instance.Warn("\t\tPlayer Trans History will NOT be processed (Empty namespace/set)");
            else
                Logger.Instance.InfoFormat("\t\tHistory Trans: {0}", this.PlayersTransHistoryCollection);
            if (this.UsedEmailCntCollection.IsEmpty)
                Logger.Instance.Info("\t\tUsed Email Counter will NOT be processed (Empty namespace/set)");
            else
                Logger.Instance.InfoFormat("\t\tUsedEmailCnt: {0}", this.UsedEmailCntCollection);
            if (this.InterventionCollection.IsEmpty)
                Logger.Instance.Warn("\t\tIntervention will NOT be processed (Empty namespace/set)");
            else
                Logger.Instance.InfoFormat("\t\tIntervention: {0}", this.InterventionCollection);
            if (this.GlobalIncrementCollection.IsEmpty)
                Logger.Instance.Warn("\t\tLGlobal Increment will NOT be processed (Empty namespace/set)");
            else
                Logger.Instance.InfoFormat("\t\tGlobalIncrement: {0}", this.GlobalIncrementCollection);
            if (this.LiverWagerCollection.IsEmpty)
                Logger.Instance.Warn("\t\tLive Wager will NOT be processed (Empty namespace/set)");
            else
                Logger.Instance.InfoFormat("\t\tLiverWager: {0}", this.LiverWagerCollection);
            if (this.InterventionThresholdsCollection.IsEmpty)
                Logger.Instance.Warn("\t\tIntervention Thresholds will NOT be processed (Empty namespace/set)");
            else
                Logger.Instance.InfoFormat("\t\tInterventionThresholds: {0}", this.InterventionThresholdsCollection);

            Logger.Instance.InfoFormat("\tClusterId: {0} Description: {1}", 
                                        this.Client.Cluster.ClusterId,
                                        this.Client.Cluster.Description);
            Logger.Instance.InfoFormat("\tDatabase Namespace: {0}",
                                        this.Database.DatabaseNamespace);            
        }

        public string ConnectionString { get; }        
        public int ConnectionTimeout { get; }
        public int OperationalTimeout { get; }
        public bool Compression { get; }
        public int HeartbeatTimeout { get; }
        
        public Progression ConsoleProgression { get; }
        public ConsoleDisplay PlayerProgression { get; }
        public ConsoleDisplay HistoryProgression { get; }

        public IMongoClient Client { get; }
        public IMongoDatabase Database { get; }
       
        public readonly DBCollection<Player> CurrentPlayersCollection;
        public readonly DBCollection<PlayerHistory> PlayersHistoryCollection;
        public readonly DBCollection<PlayersTransHistory> PlayersTransHistoryCollection;
        public readonly DBCollection<UsedEmailCnt> UsedEmailCntCollection;
        public bool UsedEmailCntEnabled { get => !this.UsedEmailCntCollection.IsEmpty; }
        public readonly DBCollection<GlobalIncrement> GlobalIncrementCollection;
        public readonly DBCollection<Intervention> InterventionCollection;
        public readonly DBCollection<LiveWager> LiverWagerCollection;
        public readonly DBCollection<InterventionThresholds> InterventionThresholdsCollection;

        public void Truncate()
        {

            void Truncate<T>(DBCollection<T> collectionTruncate)
            {
                if(!collectionTruncate.IsEmpty)
                {
                    this.Database.DropCollection(collectionTruncate.CollectionName);        
                }
            }

            Logger.Instance.Info("DBConnection.TruncateCollections Start");

            using var consoleTrunc = new Progression(this.ConsoleProgression, "Truncating...");

            Truncate(CurrentPlayersCollection);
            Truncate(PlayersHistoryCollection);
            Truncate(PlayersTransHistoryCollection);
            Truncate(UsedEmailCntCollection);
            Truncate(GlobalIncrementCollection);
            Truncate(InterventionCollection);
            Truncate(LiverWagerCollection);
            Truncate(InterventionThresholdsCollection);

            Logger.Instance.Info("DBConnection.TruncateCollections End");
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

            if (!this.CurrentPlayersCollection.IsEmpty)            
            {                
                var stopWatch = Stopwatch.StartNew();
                
                await CurrentPlayersCollection.Collection
                        .InsertOneAsync(player, cancellationToken: cancellationToken)                                                               
                    .ContinueWith(task =>
                    {
                        stopWatch.StopRecord(InsertTag,
                                                SystemTag,
                                                CurrentPlayersCollection.CollectionName,
                                                nameof(UpdateCurrentPlayers),
                                                player.PlayerId);

                        if (Logger.Instance.IsDebugEnabled)
                        {
                            Logger.Instance.DebugFormat("DBConnection.UpdateCurrentPlayers Run End IsertOne {0} Elapsed Time (ms): {1}",
                                                        player.PlayerId,
                                                        stopWatch.ElapsedMilliseconds);
                        }

                        if (stopWatch.ElapsedMilliseconds > Settings.Instance.WarnMaxMSLatencyDBExceeded)
                            Logger.Instance.WarnFormat("DBConnection.UpdateCurrentPlayers Run Exceeded Latency Threshold for InsertOne {1}. Latency: {0}",
                                                        stopWatch.ElapsedMilliseconds,
                                                        player.PlayerId);

                        if (Settings.Instance.WarnIfObjectSizeBytes > 0)
                        {
                            //Hate this, but this is easiest way to get object size...
                            var str = player.ToJSON();

                            if (str.Length > Settings.Instance.WarnIfObjectSizeBytes)
                            {
                                Logger.Instance.WarnFormat("DBConnection.UpdateHistory(Player) Run Exceeded Object Size Threshold (WarnIfObjectSizeBytes {1}) for InsertOne {2}. Size: {0}",
                                                                str.Length,
                                                                Settings.Instance.WarnIfObjectSizeBytes,
                                                                player.PlayerId);
                            }
                        }

                        if (task.IsFaulted || task.IsCanceled)
                        {
                            Program.CanceledFaultProcessing($"DBConnection.UpdateCurrentPlayers InsertOne {player.PlayerId}", task.Exception, Settings.Instance.IgnoreFaults);
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


            async Task CallDBUpdate<T>(IMongoCollection<T> collection,
                                    FilterDefinition<T> findFilter,
                                    UpdateDefinition<T> updateFilter,
                                    bool upsert = false)
            {
                var stopWatch = Stopwatch.StartNew();

                await collection.UpdateOneAsync(findFilter,
                                                        updateFilter,
                                                        options: upsert
                                                                    ? new UpdateOptions() {  IsUpsert = true}
                                                                    : null,
                                                        cancellationToken: cancellationToken)
                            .ContinueWith(task =>
                            {
                                stopWatch.StopRecord(UpdateTag,
                                                        SystemTag,
                                                        collection.CollectionNamespace.CollectionName,
                                                        nameof(UpdateChangedCurrentPlayer),
                                                        player.PlayerId);

                                if (Logger.Instance.IsDebugEnabled)
                                {
                                    Logger.Instance.DebugFormat("DBConnection.UpdateChangedCurrentPlayer Run End UpdateOneAsync {0} Elapsed Time (ms): {1}",
                                                                player.PlayerId,
                                                                stopWatch.ElapsedMilliseconds);
                                }

                                if (stopWatch.ElapsedMilliseconds > Settings.Instance.WarnMaxMSLatencyDBExceeded)
                                    Logger.Instance.WarnFormat("DBConnection.UpdateChangedCurrentPlayer Run Exceeded Latency Threshold for UpdateOneAsync {1}. Latency: {0}",
                                                                stopWatch.ElapsedMilliseconds,
                                                                player.PlayerId);

                                if (Settings.Instance.WarnIfObjectSizeBytes > 0)
                                {
                                    //Hate this, but this is easiest way to get object size...
                                    var str = player.ToJSON();

                                    if (str.Length > Settings.Instance.WarnIfObjectSizeBytes)
                                    {
                                        Logger.Instance.WarnFormat("DBConnection.UpdateChangedCurrentPlayer Run Exceeded Object Size Threshold (WarnIfObjectSizeBytes {1}) for UpdateOneAsync {2}. Size: {0}",
                                                                        str.Length,
                                                                        Settings.Instance.WarnIfObjectSizeBytes,
                                                                        player.PlayerId);
                                    }
                                }

                                if (task.IsFaulted || task.IsCanceled)
                                {
                                    Program.CanceledFaultProcessing($"DBConnection.UpdateChangedCurrentPlayer UpdateOneAsync {player.PlayerId}", task.Exception, Settings.Instance.IgnoreFaults);
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

            async Task CallDBInsert<T>(IMongoCollection<T> collection,
                                        T insertDoc)
            {
                var stopWatch = Stopwatch.StartNew();

                await collection.InsertOneAsync(insertDoc, cancellationToken: cancellationToken)
                            .ContinueWith(task =>
                            {
                                stopWatch.StopRecord(InsertTag,
                                                        SystemTag,
                                                        collection.CollectionNamespace.CollectionName,
                                                        nameof(UpdateChangedCurrentPlayer),
                                                        player.PlayerId);

                                if (Logger.Instance.IsDebugEnabled)
                                {
                                    Logger.Instance.DebugFormat("DBConnection.UpdateChangedCurrentPlayer Run End InsertOne {0} Elapsed Time (ms): {1}",
                                                                player.PlayerId,
                                                                stopWatch.ElapsedMilliseconds);
                                }

                                if (stopWatch.ElapsedMilliseconds > Settings.Instance.WarnMaxMSLatencyDBExceeded)
                                    Logger.Instance.WarnFormat("DBConnection.UpdateChangedCurrentPlayer Run Exceeded Latency Threshold for InsertOne {1}. Latency: {0}",
                                                                stopWatch.ElapsedMilliseconds,
                                                                player.PlayerId);

                                if (Settings.Instance.WarnIfObjectSizeBytes > 0)
                                {
                                    //Hate this, but this is easiest way to get object size...
                                    var str = player.ToJSON();

                                    if (str.Length > Settings.Instance.WarnIfObjectSizeBytes)
                                    {
                                        Logger.Instance.WarnFormat("DBConnection.UpdateChangedCurrentPlayer Run Exceeded Object Size Threshold (WarnIfObjectSizeBytes {1}) for InsertOne {2}. Size: {0}",
                                                                        str.Length,
                                                                        Settings.Instance.WarnIfObjectSizeBytes,
                                                                        player.PlayerId);
                                    }
                                }

                                if (task.IsFaulted || task.IsCanceled)
                                {
                                    Program.CanceledFaultProcessing($"DBConnection.UpdateChangedCurrentPlayer InsertOne {player.PlayerId}", task.Exception, Settings.Instance.IgnoreFaults);
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

            #region Player Update
            var updateFilter = CurrentPlayersCollection.BuildersUpdate.Set(t => t.Tag, player.Tag);

            if (updateSession)
            {
                if (Logger.Instance.IsDebugEnabled)
                    Logger.Instance.DebugFormat("DBConnection.UpdateChangedCurrentPlayer Update Session Player: {0}",
                                                    player.PlayerId);

                updateFilter = updateFilter.Set(t => t.Session, player.Session);                
            }

            if (updateFin < 0 && player.FinTransactions.Any())
            {
                if (Logger.Instance.IsDebugEnabled)
                    Logger.Instance.DebugFormat("DBConnection.UpdateChangedCurrentPlayer Update Fin Player: {0}",
                                                    player.PlayerId);

                updateFilter = updateFilter.Set(t => t.FinTransactions, player.FinTransactions);
            }

            if (updateGame)
            {
                if (Logger.Instance.IsDebugEnabled)
                    Logger.Instance.DebugFormat("DBConnection.UpdateChangedCurrentPlayer Update Game Player: {0}",
                                                    player.PlayerId);
                updateFilter = updateFilter.Set(t => t.Game, player.Game);                
            }

            if (updateWagerResult < 0 && player.WagersResults.Any())
            {
                /*if (Logger.Instance.IsDebugEnabled)
                    Logger.Instance.DebugFormat("DBConnection.UpdateChangedCurrentPlayer Transform WagersResults-List Player: {0}",
                                                    player.PlayerId);

                var wagerresultsTransformed = player.WagersResults
                                                .Select(x => Helpers.TransForm(x)).ToList();

                wagerresultsTransformed.Reverse();

                binstoUpdate.Add(new Bin("WagersResults", new Value.ListValue(wagerresultsTransformed)));
                */
                throw new NotSupportedException("Cannot support dumping of all player trans");
            }

            {
                if (Logger.Instance.IsDebugEnabled)
                    Logger.Instance.DebugFormat("DBConnection.UpdateChangedCurrentPlayer Update Metrics Player: {0}",
                                                    player.PlayerId);

                updateFilter = updateFilter.Set(t => t.Metrics, player.Metrics)
                                            .Set(t => t.ActiveSession, player.ActiveSession)
                                            .Set(t => t.BingeFlag, player.BingeFlag)
                                            .Set(t => t.Archived, player.Archived);                
            }

            if(!this.CurrentPlayersCollection.IsEmpty)
                await CallDBUpdate(CurrentPlayersCollection.Collection,
                                    CurrentPlayersCollection.BuildersFilter
                                            .Eq(t => t.PlayerId, player.PlayerId),
                                    updateFilter);

            #endregion

            #region List Updates
            if (updateFin > 0 && player.FinTransactions.Any() && !this.CurrentPlayersCollection.IsEmpty)
            {
                if (Logger.Instance.IsDebugEnabled)
                    Logger.Instance.DebugFormat("DBConnection.UpdateChangedCurrentPlayer Fin-List Player: {0}",
                                                    player.PlayerId);

                await CallDBUpdate(CurrentPlayersCollection.Collection,
                                    CurrentPlayersCollection.BuildersFilter.Eq(t => t.PlayerId, player.PlayerId),
                                    CurrentPlayersCollection.BuildersUpdate
                                        .PushEach(t => t.FinTransactions,
                                                    player.FinTransactions.Take(updateFin).Reverse(),
                                                    position: 0));
                                            
                //Settings.Instance.KeepNbrFinTransActions);
            }

            if (updateWagerResult > 0 && player.WagersResults.Any())
            {
                if (Logger.Instance.IsDebugEnabled)
                    Logger.Instance.DebugFormat("DBConnection.UpdateChangedCurrentPlayer WagerResults-List Player: {0}",
                                                    player.PlayerId);

                var wagers = player.WagersResults.TakeLast(updateWagerResult);

                if (!this.CurrentPlayersCollection.IsEmpty)
                {
                    await CallDBUpdate(CurrentPlayersCollection.Collection,
                                       CurrentPlayersCollection.BuildersFilter.Eq(t => t.PlayerId, player.PlayerId),
                                       CurrentPlayersCollection.BuildersUpdate
                                           .PushEach(t => t.WagersResults,
                                                            wagers.Reverse(),
                                                       position: 0));

                    // Settings.Instance.KeepNbrWagerResultTransActions);
                }

                if(wagers.Last().Type != WagerResultTransaction.Types.Wager)
                {
                    var pkey = wagers.Last().Id;

                    if (Logger.Instance.IsDebugEnabled)
                        Logger.Instance.DebugFormat("DBConnection.UpdateChangedCurrentPlayer WagerResults-History-List Player: {0} TransId: {1}",
                                                        player.PlayerId,
                                                        pkey);

                    {                        
                        if (!this.PlayersTransHistoryCollection.IsEmpty)
                        {
                            var playerSnapshot = new Player(player);
                            var playerTransHistory = new PlayersTransHistory(playerSnapshot);

                            await CallDBInsert(PlayersTransHistoryCollection.Collection, playerTransHistory);
                        }
                    }
                    if (!this.PlayersHistoryCollection.IsEmpty)
                    {                        
                        await CallDBUpdate(PlayersHistoryCollection.Collection,
                                        PlayersHistoryCollection.BuildersFilter.Eq(t => t.PlayerId, player.PlayerId),
                                        PlayersHistoryCollection.BuildersUpdate
                                            .SetOnInsert(t => t.PlayerId, player.PlayerId)
                                            .SetOnInsert(t => t.County , player.County)
                                            .SetOnInsert(t => t.State , player.State)
                                            .SetOnInsert(t => t.Tag, "PlayersHistory")
                                            .PushEach(t => t.WagerIds,
                                                           new long[] { pkey },
                                                       position: 0),
                                            upsert: true);
                        //Settings.Instance.PlayerHistoryLastNbrTrans);
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
            }
            
            Guid currentPlayerId = Guid.Empty;

            var transactionsForPlayer = new List<long>();
            
            foreach (var player in history)
            {                   
                cancellationToken.ThrowIfCancellationRequested();
                    
                this.HistoryProgression.Increment("HistoryTrans", $"Putting Player History Tran {player.PlayerId}...");

                var playerTransHistory = new PlayersTransHistory(player);

                //Update History Transactions
                if (!PlayersTransHistoryCollection.IsEmpty)                
                {                    
                    var stopWatch = Stopwatch.StartNew();

                    await PlayersTransHistoryCollection.Collection
                                .InsertOneAsync(playerTransHistory, cancellationToken:  cancellationToken)
                        .ContinueWith(task =>
                        {
                            stopWatch.StopRecord(InsertTag,
                                                    SystemTag,
                                                    PlayersTransHistoryCollection.CollectionName,
                                                    nameof(UpdateHistory),
                                                    playerTransHistory.WagerId);

                            if (Logger.Instance.IsDebugEnabled)
                            {
                                Logger.Instance.DebugFormat("DBConnection.UpdateHistory(Player) Run End InsertOne {0} Elapsed Time (ms): {1}",
                                                            playerTransHistory.WagerId,
                                                            stopWatch.ElapsedMilliseconds);
                            }

                            if (stopWatch.ElapsedMilliseconds > Settings.Instance.WarnMaxMSLatencyDBExceeded)
                                Logger.Instance.WarnFormat("DBConnection.UpdateHistory(Player) Run Exceeded Latency Threshold for InsertOne {2}-{1}. Latency: {0}",
                                                            stopWatch.ElapsedMilliseconds,
                                                            playerTransHistory.WagerId,
                                                            forPlayerId);

                            if (Settings.Instance.WarnIfObjectSizeBytes > 0)
                            {
                                //Hate this, but this is easiest way to get object size...
                                var str = player.ToJSON();

                                if (str.Length > Settings.Instance.WarnIfObjectSizeBytes)
                                {
                                    Logger.Instance.WarnFormat("DBConnection.UpdateHistory(Player) Run Exceeded Object Size Threshold (WarnIfObjectSizeBytes {1}) for InsertOne {3}-{2}. Size: {0}",
                                                                    str.Length,
                                                                    Settings.Instance.WarnIfObjectSizeBytes,
                                                                    playerTransHistory.WagerId,
                                                                    forPlayerId);
                                }
                            }

                            if (task.IsFaulted || task.IsCanceled)
                            {
                                Program.CanceledFaultProcessing($"DBConnection.UpdateHistory(Player) InsertOne {playerTransHistory.WagerId}", task.Exception, Settings.Instance.IgnoreFaults);
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
                
                if (!transactionsForPlayer.Contains(playerTransHistory.WagerId))
                {
                    if(Logger.Instance.IsDebugEnabled)
                        Logger.Instance.DebugFormat("DBConnection.UpdateHistory(Trans) Run ForEach Add TransId {0}",
                                                        playerTransHistory.WagerId);

                    transactionsForPlayer.Add(playerTransHistory.WagerId);
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
                if (!PlayersHistoryCollection.IsEmpty)
                {
                   var playerHistory = new PlayerHistory(forPlayerId,
                                                            transactionsForPlayer,
                                                            forPlayer.State,
                                                            forPlayer.County);
                    var stopWatch = Stopwatch.StartNew();

                    await PlayersHistoryCollection.Collection
                            .InsertOneAsync(playerHistory, cancellationToken: cancellationToken)
                        .ContinueWith(task =>
                        {
                            stopWatch.StopRecord(InsertTag,
                                                    SystemTag,
                                                    PlayersHistoryCollection.CollectionName,
                                                    nameof(UpdateHistory),
                                                    forPlayerId);

                            if (Logger.Instance.IsDebugEnabled)
                            {
                                Logger.Instance.DebugFormat("DBConnection.UpdateHistory(Player) Run End InsertOne {0} Elapsed Time (ms): {1}",
                                                            forPlayerId,
                                                            stopWatch.ElapsedMilliseconds);
                            }

                            if (stopWatch.ElapsedMilliseconds > Settings.Instance.WarnMaxMSLatencyDBExceeded)
                                Logger.Instance.WarnFormat("DBConnection.UpdateHistory(Player) Run Exceeded Latency Threshold for InsertOne {1}. Latency: {0}",
                                                            stopWatch.ElapsedMilliseconds,
                                                            forPlayerId);

                            if (Settings.Instance.WarnIfObjectSizeBytes > 0 && transactionsForPlayer.Count > 0)
                            {
                                //Hate this, but this is easiest way to get object size...
                                var lstSize = transactionsForPlayer.Count * 32;

                                if (lstSize > Settings.Instance.WarnIfObjectSizeBytes)
                                {
                                    Logger.Instance.WarnFormat("DBConnection.UpdateHistory(Player) Run Exceeded Object Size Threshold (WarnIfObjectSizeBytes {1}) for InsertOne {2}. Size: {0} List Length: {3}",
                                                                    lstSize,
                                                                    Settings.Instance.WarnIfObjectSizeBytes,
                                                                    forPlayerId,
                                                                    transactionsForPlayer.Count);
                                }
                            }

                            if (task.IsFaulted || task.IsCanceled)
                            {
                                Program.CanceledFaultProcessing($"DBConnection.UpdateHistory(Player) InsertOne {forPlayerId}", task.Exception, Settings.Instance.IgnoreFaults);
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
                Logger.Instance.Info("DBConnection.UpdateHistory(Trans) End");
            
            this.HistoryProgression.Decrement("HistoryTrans");

            return;
        }

        public async Task<string> DeterineEmail(string firstName, string lastName, string domain, CancellationToken token)
        {           
            var email = $"{firstName}.{lastName}@{domain}";

            if (UsedEmailCntCollection.IsEmpty) return email;

            if (Logger.Instance.IsDebugEnabled)
                Logger.Instance.DebugFormat("DBConnection.DeterineEmail Start {0}", email);

            var emailProg = new Progression(this.ConsoleProgression, email);
            
            var stopWatch =Stopwatch.StartNew();

            var usedEmailCnt = await  UsedEmailCntCollection.Collection
                                        .FindOneAndUpdateAsync(UsedEmailCntCollection
                                                                    .BuildersFilter.Eq(t => t.EMail, email),
                                                                UsedEmailCntCollection
                                                                    .BuildersUpdate
                                                                    .SetOnInsert(t => t.EMail, email)
                                                                    .Inc(t => t.Count, 1),
                                                                new FindOneAndUpdateOptions<UsedEmailCnt, UsedEmailCnt>()
                                                                    { IsUpsert = true, ReturnDocument = ReturnDocument.After },
                                                                cancellationToken: token)
                                    .ContinueWith(task =>
                                    {
                                        stopWatch.StopRecord(FindUpdateTag,
                                                                SystemTag,
                                                                UsedEmailCntCollection.CollectionName,
                                                                nameof(DeterineEmail),
                                                                email);

                                        if (Logger.Instance.IsDebugEnabled)
                                        {
                                            Logger.Instance.DebugFormat("DBConnection.DeterineEmail Run End FindOneAndUpdate {0} Elapsed Time (ms): {1}",
                                                                        email,
                                                                        stopWatch.ElapsedMilliseconds);
                                        }

                                        if (stopWatch.ElapsedMilliseconds > Settings.Instance.WarnMaxMSLatencyDBExceeded)
                                            Logger.Instance.WarnFormat("DBConnection.DeterineEmail Run Exceeded Latency Threshold for FindOneAndUpdate {0}. Latency: {1}",
                                                                        email,
                                                                        stopWatch.ElapsedMilliseconds);


                                        if (task.IsFaulted || task.IsCanceled)
                                        {
                                            Program.CanceledFaultProcessing($"DBConnection.DeterineEmail FindOneAndUpdate {email}", task.Exception, Settings.Instance.IgnoreFaults);
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

            var count = usedEmailCnt?.Count ?? -1;

            if(count > 1)
                email = $"{firstName}.{lastName}{count}@{domain}";               
            
            if (Logger.Instance.IsDebugEnabled)
                Logger.Instance.DebugFormat("DBConnection.DeterineEmail End Exists {0}", email);
            
            emailProg.Decrement();

            return email;
        }

        public async Task IncrementGlobalSet(GlobalIncrement glbIncr,
                                                CancellationToken token)
        {
            if(GlobalIncrementCollection.IsEmpty) return;

            if (Logger.Instance.IsDebugEnabled)
                Logger.Instance.DebugFormat("DBConnection.IncrementGlobalSet Start {0}", glbIncr.Key);

            var incrProg = new Progression(this.ConsoleProgression, $"Incrementing Global Set {glbIncr.Key}");            
            var stopWatch = Stopwatch.StartNew();

            await GlobalIncrementCollection.Collection
                    .UpdateOneAsync(GlobalIncrementCollection.BuildersFilter
                                                .Eq(t => t.Key, glbIncr.Key),
                                            GlobalIncrementCollection.BuildersUpdate
                                                .SetOnInsert(t => t.Key, glbIncr.Key)
                                                .SetOnInsert(t => t.StateName, glbIncr.StateName)
                                                .SetOnInsert(t => t.State, glbIncr.State)
                                                .SetOnInsert(t => t.County, glbIncr.County)
                                                .SetOnInsert(t => t.CountyCode, glbIncr.CountyCode)
                                                .Set(t => t.IntervalTimeStamp, glbIncr.IntervalTimeStamp)
                                                .Inc(t => t.GGR, glbIncr.GGR)
                                                .Inc(t => t.Interventions, glbIncr.Interventions)
                                                .Inc(t => t.Transactions, glbIncr.Transactions),
                                            new UpdateOptions()
                                            { IsUpsert = true },
                                            cancellationToken: token)                
                .ContinueWith(task =>
                {
                    stopWatch.StopRecord(UpdateTag,
                                            SystemTag,
                                            GlobalIncrementCollection.CollectionName,
                                            nameof(IncrementGlobalSet),
                                            glbIncr.Key);

                    if (Logger.Instance.IsDebugEnabled)
                    {
                        Logger.Instance.DebugFormat("DBConnection.IncrementGlobalSet Run End UpdateOneAsync {0} Elapsed Time (ms): {1}",
                                                    glbIncr.Key,
                                                    stopWatch.ElapsedMilliseconds);
                    }

                    if (stopWatch.ElapsedMilliseconds > Settings.Instance.WarnMaxMSLatencyDBExceeded)
                        Logger.Instance.WarnFormat("DBConnection.IncrementGlobalSet Run Exceeded Latency Threshold for UpdateOneAsync {0}. Latency: {1}",
                                                    glbIncr.Key,
                                                    stopWatch.ElapsedMilliseconds);


                    if (task.IsFaulted || task.IsCanceled)
                    {
                        Program.CanceledFaultProcessing($"DBConnection.IncrementGlobalSet UpdateOneAsync {glbIncr.Key}", task.Exception, Settings.Instance.IgnoreFaults);
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
            
            if (!this.InterventionCollection.IsEmpty)
            {
                var stopWatch = Stopwatch.StartNew();

                await InterventionCollection.Collection
                        .InsertOneAsync(intervention, cancellationToken:  cancellationToken)
                    .ContinueWith(task =>
                    {
                        stopWatch.StopRecord(UpdateTag,
                                                SystemTag,
                                                InterventionCollection.CollectionName,
                                                nameof(UpdateIntervention),
                                                intervention.PlayerId);

                        if (Logger.Instance.IsDebugEnabled)
                        {
                            Logger.Instance.DebugFormat("DBConnection.UpdateIntervention Run End InsertOne {0} Elapsed Time (ms): {1}",
                                                        intervention.PlayerId,
                                                        stopWatch.ElapsedMilliseconds);
                        }

                        if (stopWatch.ElapsedMilliseconds > Settings.Instance.WarnMaxMSLatencyDBExceeded)
                            Logger.Instance.WarnFormat("DBConnection.UpdateIntervention Run Exceeded Latency Threshold for InsertOne {1}. Latency: {0}",
                                                        stopWatch.ElapsedMilliseconds,
                                                        intervention.PlayerId);                            

                        if (task.IsFaulted || task.IsCanceled)
                        {
                            Program.CanceledFaultProcessing($"DBConnection.UpdateIntervention InsertOne {intervention.PlayerId}", task.Exception, Settings.Instance.IgnoreFaults);
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

            if (!this.LiverWagerCollection.IsEmpty)
            {
                var liveWager = new LiveWager(player,
                                                wagerResult,
                                                wager,
                                                Settings.Instance.TimeZoneFormatWoZone);
                var stopWatch = Stopwatch.StartNew();

                await LiverWagerCollection.Collection
                        .InsertOneAsync(liveWager, cancellationToken:  cancellationToken)
                    .ContinueWith(task =>
                    {
                        stopWatch.StopRecord(InsertTag,
                                                SystemTag,
                                                LiverWagerCollection.CollectionName,
                                                nameof(UpdateLiveWager),
                                                player.PlayerId);

                        if (Logger.Instance.IsDebugEnabled)
                        {
                            Logger.Instance.DebugFormat("DBConnection.UpdateLiveWager Run End InsertOne {0} Elapsed Time (ms): {1}",
                                                        player.PlayerId,
                                                        stopWatch.ElapsedMilliseconds);
                        }

                        if (stopWatch.ElapsedMilliseconds > Settings.Instance.WarnMaxMSLatencyDBExceeded)
                            Logger.Instance.WarnFormat("DBConnection.UpdateLiveWager Run Exceeded Latency Threshold for InsertOne {1}. Latency: {0}",
                                                        stopWatch.ElapsedMilliseconds,
                                                        player.PlayerId);

                        if (task.IsFaulted || task.IsCanceled)
                        {
                            Program.CanceledFaultProcessing($"DBConnection.UpdateLiveWager InsertOne {player.PlayerId}", task.Exception, Settings.Instance.IgnoreFaults);
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
            if (Logger.Instance.IsDebugEnabled)
                Logger.Instance.DebugFormat("DBConnection.ReFreshInterventionThresholds Run Start Current Version: {0}, Last Refresh Time: {1:HH:mm:ss.ffff}",
                                                interventionThresholds?.Version ?? -1,
                                                interventionThresholds?.RefreshedTime ?? DateTime.MinValue);

            cancellationToken.ThrowIfCancellationRequested();

            this.PlayerProgression.Increment("ReFresh InterventionThresholds", $"Checking...");

            if (!this.InterventionThresholdsCollection.IsEmpty)
            {                
                var stopWatch = Stopwatch.StartNew();
                var key = interventionThresholds?.Version ?? 0;
                var findFilter = InterventionThresholdsCollection.BuildersFilter
                                    .Eq(t => t.Version, key);

                interventionThresholds = await InterventionThresholdsCollection.Collection
                                                .FindAsync(findFilter, cancellationToken: cancellationToken)                    
                                            .ContinueWith(task =>
                                            {
                                                stopWatch.StopRecord(FindTag,
                                                                        SystemTag,
                                                                        InterventionThresholdsCollection.CollectionName,
                                                                        nameof(ReFreshInterventionThresholds),
                                                                        key);

                                                if (Logger.Instance.IsDebugEnabled)
                                                {
                                                    Logger.Instance.DebugFormat("DBConnection.ReFreshInterventionThresholds Run End Find {0} Elapsed Time (ms): {1}",
                                                                                interventionThresholds?.Version ?? -1,
                                                                                stopWatch.ElapsedMilliseconds);
                                                }

                                                if (stopWatch.ElapsedMilliseconds > Settings.Instance.WarnMaxMSLatencyDBExceeded)
                                                    Logger.Instance.WarnFormat("DBConnection.ReFreshInterventionThresholds Run Exceeded Latency Threshold for Find {1}. Latency: {0}",
                                                                                stopWatch.ElapsedMilliseconds,
                                                                                interventionThresholds?.Version ?? 0);

                                                if (task.IsFaulted || task.IsCanceled)
                                                {
                                                    Program.CanceledFaultProcessing($"DBConnection.interventionThresholds Find {interventionThresholds?.Version ?? -1}", task.Exception, Settings.Instance.IgnoreFaults);
                                                    if (Settings.Instance.IgnoreFaults && !task.IsCanceled)
                                                    {
                                                        task.Exception?.Handle(e => true);
                                                        return null;
                                                    }                                                    
                                                }

                                                return task.Result?.FirstOrDefault();
                                            },
                                            cancellationToken,
                                            TaskContinuationOptions.AttachedToParent | TaskContinuationOptions.ExecuteSynchronously,
                                            TaskScheduler.Default);
            }

            this.PlayerProgression.Decrement("ReFresh InterventionThresholds");

            if(Logger.Instance.IsDebugEnabled)
                Logger.Instance.DebugFormat("DBConnection.ReFreshInterventionThresholds Run End with returned version: {0}",
                                                interventionThresholds == null ? -2 : interventionThresholds?.Version ?? -1);

            return interventionThresholds;
        }

        #region Disposable
        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {                    
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
