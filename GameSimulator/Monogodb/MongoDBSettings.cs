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
                this.writeConcern = new WriteConcern();
                this.readConcern = new ReadConcern();
                this.Shard = new ShardOpts();
            }

            public CollectionOpts(string name)
                : this()
            {
                this.Name = name;
            }

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
            public WriteConcern writeConcern { get; set; }
            public ReadConcern readConcern { get; set; }
        }


        public CollectionOpts CurrentPlayersCollection = new CollectionOpts("CurrentPlayers");
        public CollectionOpts PlayersHistoryCollection = new CollectionOpts("PlayersHistory");
        public CollectionOpts PlayersTransHistoryCollection = new CollectionOpts("PlayersTransHistory");
        public CollectionOpts UsedEmailCntCollection = null; //"UsedEmailCnt";
        public CollectionOpts GlobalIncrementCollection = new CollectionOpts("GlobalIncrement");
        public CollectionOpts InterventionCollection = new CollectionOpts("Intervention");
        public CollectionOpts LiveWagerCollection = new CollectionOpts("LiveWager");
        public CollectionOpts InterventionThresholdsCollection = new CollectionOpts("InterventionThresholds");
    }

}
