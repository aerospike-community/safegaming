using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace PlayerCommon
{
    public partial class MongoDBSettings
    {
        public MongoDBSettings()
        {
            this.DBConnectionString = "mongodb://localhost";
        }

        public string DBConnectionString
        {
            get => Settings.Instance.DBConnectionString;
            set
            {
                if (string.IsNullOrEmpty(value)
                        || value == this.DBConnectionString) return;

                Settings.Instance.DBConnectionString = value;
                this.DriverSettings = MongoClientSettings.FromConnectionString(value);
            }
        }

        public MongoClientSettings DriverSettings = null;

        public string DBName = "safegaming";

        public partial class ShardOpts
        {
            public enum Types
            {
                Range = 0,
                Hashed = 1
            }

            public bool Create { get; set; }
            public Types Type { get; set; }
            public bool unique { get; set; }
            public string Options { get; set; }
        }

        public partial class CollectionOpts
        {
            public CollectionOpts() 
            {
                this.createCollectionOptions = new CreateCollectionOptions();
                this.findOptions = new FindOptions();
                this.Shard = new ShardOpts();
            }

            public CollectionOpts(string name, string configSectionName)
                : this()
            {
                this.Name = name;
                this.ConfigSectionName = configSectionName;
            }

            public string ConfigSectionName { get; }
            private string name;
            public string Name
            {
                get => name;

                set => name = value?.Trim();
            }

            public bool Drop { get; set; }
            public ShardOpts Shard { get; set; }
            public string ShardType { get; set; }

            public CreateCollectionOptions createCollectionOptions { get; set; }
            public FindOptions findOptions { get; set; }            
        }

        public CollectionOpts CurrentPlayersCollection = new("CurrentPlayers", nameof(CurrentPlayersCollection));
        public CollectionOpts PlayersHistoryCollection = new("PlayersHistory", nameof(PlayersHistoryCollection));
        public CollectionOpts PlayersTransHistoryCollection = new("PlayersTransHistory", nameof(PlayersTransHistoryCollection));
        public CollectionOpts UsedEmailCntCollection = null; //"UsedEmailCnt";
        public CollectionOpts GlobalIncrementCollection = new("GlobalIncrement", nameof(GlobalIncrementCollection));
        public CollectionOpts InterventionCollection = new("Intervention", nameof(InterventionCollection));
        public CollectionOpts LiveWagerCollection = new("LiveWager", nameof(LiveWagerCollection));
        public CollectionOpts InterventionThresholdsCollection = new("InterventionThresholds", nameof(InterventionThresholdsCollection));

        public IEnumerable<CollectionOpts> GetAllCollections()
            => new List<CollectionOpts>()
            { CurrentPlayersCollection, PlayersHistoryCollection, PlayersTransHistoryCollection,
                UsedEmailCntCollection, GlobalIncrementCollection, InterventionCollection,
                LiveWagerCollection, InterventionThresholdsCollection};
    }

}
