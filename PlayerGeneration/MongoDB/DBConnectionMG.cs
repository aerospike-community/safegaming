using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using MongoDB.Driver;
using Common;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using PlayerGenerationMG;

namespace PlayerGeneration
{
    public sealed partial class DBConnection : IDBConnection
    {
        public const string SystemTag = "MonogoDB";
        public const string InsertTag = "Put";
        public const string FindTag = "Find";
        public const string UpdateTag = "Update";
        public const string FindUpdateTag = "FindUpdate";

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
                            MGDriverSettings driverSettings,
                            ConsoleDisplay displayProgression = null,
                            ConsoleDisplay playerProgression = null,
                            ConsoleDisplay historyProgression = null)
        {           
            this.ConsoleProgression = new Progression(displayProgression, "MongoDB Connection", null);
            this.PlayerProgression = playerProgression;
            this.HistoryProgression = historyProgression;

            this.ConnectionString = dbConnectionString;
           
            Logger.Instance.InfoFormat("DBConnection:");
            Logger.Instance.InfoFormat("\tDB Connection String: {0}", ConnectionString);
            Logger.Instance.InfoFormat("\tDB Name: {0}", dbName);
           

            BsonSerializer.RegisterSerializer(new DecimalSerializer(BsonType.Decimal128));
            //BsonSerializer.RegisterSerializer(new DateTimeOffsetSerializer());

            var mgConnectionString = MongoClientSettings.FromConnectionString(ConnectionString);

            driverSettings?.SetConnectionSettings(mgConnectionString);

            {
                var client = new MongoClient(mgConnectionString);

                this.ClientSession = client.StartSession();
                this.Client = this.ClientSession.Client;
                this.Database = this.Client.GetDatabase(dbName);                
            }

            Logger.Instance.Dump(this.ClientSession, comments: "MongoDB Client Session:");
            Logger.Instance.Dump(this.ClientSession.ServerSession, comments: "MongoDB Server Session:");

            Logger.Instance.Dump(this.ClientSession.Options, comments: "MongoDB Client Session Settings:");
            Logger.Instance.Dump(this.Client.Settings, comments: "MongoDB Client Settings:");

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
        
        public Progression ConsoleProgression { get; }
        public ConsoleDisplay PlayerProgression { get; }
        public ConsoleDisplay HistoryProgression { get; }

        public IMongoClient Client { get; }
        public IMongoDatabase Database { get; }
        public IClientSessionHandle ClientSession { get; }

        public readonly DBCollection<Player> CurrentPlayersCollection;
        public readonly DBCollection<PlayerHistory> PlayersHistoryCollection;
        public readonly DBCollection<PlayersTransHistory> PlayersTransHistoryCollection;
        public readonly DBCollection<UsedEmailCnt> UsedEmailCntCollection;
        public bool UsedEmailCntEnabled { get => !this.UsedEmailCntCollection.IsEmpty; }
        public bool IncrementGlobalEnabled { get => !GlobalIncrementCollection.IsEmpty; }
        public bool LiverWagerEnabled { get => !LiverWagerCollection.IsEmpty; }

        public readonly DBCollection<GlobalIncrement> GlobalIncrementCollection;
        public readonly DBCollection<Intervention> InterventionCollection;
        public readonly DBCollection<LiveWager> LiverWagerCollection;
        public readonly DBCollection<InterventionThresholds> InterventionThresholdsCollection;

        public void Truncate()
        {

            static void Truncate<T>(DBCollection<T> collectionTruncate)
            {
                if(!collectionTruncate.IsEmpty)
                {
                    collectionTruncate.Collection.DeleteMany(collectionTruncate.FilterEmpty);       
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

        
        #region Disposable
        public bool Disposed { get; private set; }
        private void Dispose(bool disposing)
        {
            if (!this.Disposed)
            {
                if (disposing)
                {                    
                    this.ClientSession?.Dispose();
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
