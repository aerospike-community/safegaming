using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ECM = Microsoft.Extensions.Configuration;

namespace PlayerGeneration
{
    partial class Settings
    {
        public Settings(ECM.IConfigurationBuilder configBuilder,
                            string appJsonFile = "appsettingsMG.json")
        {
            this.ConfigBuilder = configBuilder;
            this.ConfigBuilder = ECM.FileConfigurationExtensions.SetBasePath(this.ConfigBuilder,
                                                                                AppDomain.CurrentDomain.BaseDirectory);
            var configBuilderFile = ECM.JsonConfigurationExtensions.AddJsonFile(this.ConfigBuilder, appJsonFile);
            ECM.IConfiguration config = configBuilderFile.Build();

            GetSetting(config, ref this.DBConnectionString, nameof(DBConnectionString));
            GetSetting(config, ref this.ConnectionTimeout, nameof(ConnectionTimeout));
            GetSetting(config, ref this.DBOperationTimeout, nameof(DBOperationTimeout));
            GetSetting(config, ref this.EnableDriverCompression, nameof(EnableDriverCompression));

            GetSetting(config, ref this.DBName, nameof(DBName));
            GetSetting(config, ref this.CurrentPlayersCollection, nameof(CurrentPlayersCollection));
            GetSetting(config, ref this.PlayersHistoryCollection, nameof(PlayersHistoryCollection));
            GetSetting(config, ref this.PlayersTransHistoryCollection, nameof(PlayersTransHistoryCollection));
            GetSetting(config, ref this.UsedEmailCntCollection, nameof(UsedEmailCntCollection));
            GetSetting(config, ref this.GlobalIncrementCollection, nameof(GlobalIncrementCollection));
            GetSetting(config, ref this.InterventionCollection, nameof(InterventionCollection));
            GetSetting(config, ref this.LiveWagerCollection, nameof(LiveWagerCollection));
            GetSetting(config, ref this.InterventionThresholdsCollection, nameof(InterventionThresholdsCollection));

            //Overrides
            GetSetting(config,
                            ref this.WarnMaxMSLatencyDBExceeded, 
                            ref this.UpdatedWarnMaxMSLatencyDBExceeded,
                            nameof(WarnMaxMSLatencyDBExceeded));
            GetSetting(config,
                        ref this.WarnIfObjectSizeBytes,
                        ref this.UpdatedWarnIfObjectSizeBytes,
                        nameof(WarnIfObjectSizeBytes));

            GetSetting(config,
                           ref this.TimeEvents,
                           ref this.UpdatedTimingEvents,
                           nameof(TimeEvents));
            GetSetting(config,
                           ref this.TimingCSVFile,
                           ref this.UpdatedTimingCSVFile,
                           nameof(TimingCSVFile));
            GetSetting(config,
                           ref this.TimingJsonFile,
                           ref this.UpdatedTimingJsonFile,
                           nameof(TimingJsonFile));

            GetSetting(config,
                           ref this.EnableHistogram,
                           ref this.UpdatedEnableHistogram,
                           nameof(EnableHistogram));
            GetSetting(config,
                           ref this.HGRMFile,
                           ref this.UpdatedHGRMFile,
                           nameof(HGRMFile));

        }
        public readonly ECM.IConfigurationBuilder ConfigBuilder;

        public readonly string DBConnectionString = "mongodb://localhost";
        public int ConnectionTimeout = 5000;
        public int DBOperationTimeout = 5000;
        public bool EnableDriverCompression = false;

        public readonly string DBName = "safegaming";
        public readonly string CurrentPlayersCollection = "CurrentPlayers";
        public readonly string PlayersHistoryCollection = "PlayersHistory";
        public readonly string PlayersTransHistoryCollection = "PlayersTransHistory";
        public readonly string UsedEmailCntCollection = null; //"UsedEmailCnt";
        public readonly string GlobalIncrementCollection = "GlobalIncrement";
        public readonly string InterventionCollection = "Intervention";
        public readonly string LiveWagerCollection = "LiveWager";
        public readonly string InterventionThresholdsCollection = "InterventionThresholds";
        
    }
}
