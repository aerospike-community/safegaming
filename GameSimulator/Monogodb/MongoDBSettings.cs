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
        public string CurrentPlayersCollection = "CurrentPlayers";
        public string PlayersHistoryCollection = "PlayersHistory";
        public string PlayersTransHistoryCollection = "PlayersTransHistory";
        public string UsedEmailCntCollection = null; //"UsedEmailCnt";
        public string GlobalIncrementCollection = "GlobalIncrement";
        public string InterventionCollection = "Intervention";
        public string LiveWagerCollection = "LiveWager";
        public string InterventionThresholdsCollection = "InterventionThresholds";
    }

}
