using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ECM = Microsoft.Extensions.Configuration;

namespace PlayerGeneration
{
    partial class Settings
    {
        private Settings(ECM.IConfigurationBuilder configBuilder,
                            string appJsonFile = "appsettingsAerospike.json") 
        {
            this.ConfigBuilder = configBuilder;
            this.ConfigBuilder = ECM.FileConfigurationExtensions.SetBasePath(this.ConfigBuilder,
                                                                                AppDomain.CurrentDomain.BaseDirectory);
            var configBuilderFile = ECM.JsonConfigurationExtensions.AddJsonFile(this.ConfigBuilder, appJsonFile);
            ECM.IConfiguration config = configBuilderFile.Build();

            GetSetting(config, ref this.asyncBufferSize, nameof(asyncBufferSize));
            GetSetting(config, ref this.asyncMaxCommands, nameof(asyncMaxCommands));
            GetSetting(config, ref this.connPoolsPerNode, nameof(connPoolsPerNode));
            GetSetting(config, ref this.maxErrorRate, nameof(maxErrorRate));
            GetSetting(config, ref this.maxRetries, nameof(maxRetries));
            GetSetting(config, ref this.errorRateWindow, nameof(errorRateWindow));
            GetSetting(config, ref this.tendInterval, nameof(tendInterval));
            GetSetting(config, ref this.EnableDriverCompression, nameof(EnableDriverCompression));
            GetSetting(config, ref this.ConnectionTimeout, nameof(ConnectionTimeout));
            GetSetting(config, ref this.DBOperationTimeout, nameof(DBOperationTimeout));

            GetSetting(config, ref this.CurrentPlayersSetName, nameof(CurrentPlayersSetName));
            GetSetting(config, ref this.PlayersHistorySetName, nameof(PlayersHistorySetName));
            GetSetting(config, ref this.PlayersTransHistorySetName, nameof(PlayersTransHistorySetName));
            GetSetting(config, ref this.UsedEmailCntSetName, nameof(UsedEmailCntSetName));
            
            GetSetting(config, ref this.GlobalIncrementSetName, nameof(GlobalIncrementSetName));
            GetSetting(config, ref this.InterventionSetName, nameof(InterventionSetName));
            GetSetting(config, ref this.LiveWagerSetName, nameof(LiveWagerSetName));
            GetSetting(config, ref this.InterventionThresholdsSetName, nameof(InterventionThresholdsSetName));

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

        public readonly string DBHost = "localhost";
        public readonly int DBPort = 3000;

        public int ConnectionTimeout = 5000;
        public int DBOperationTimeout = 5000;
        public int MaxConnectionPerNode = -1;
        public int MinConnectionPerNode = -1;
        public int MaxSocketIdle = -1;
        public int totalTimeout;
        public int asyncMaxCommands = 500; //less than CompletionPortThreads
        public bool EnableDriverCompression = false;

        public int asyncBufferSize = 1048576; //1MB
        public int connPoolsPerNode = 2;
        public int maxRetries = 2;
        public int maxErrorRate = 100;
        public int errorRateWindow = 1;
        public int tendInterval = 1000;
        public bool DBUseExternalIPAddresses = false;

        public readonly string CurrentPlayersSetName = "test.CurrentPlayers";
        public readonly string PlayersHistorySetName = "test.PlayersHistory";        
        public readonly string PlayersTransHistorySetName = "test.PlayersTransHistory";
        public readonly string UsedEmailCntSetName = null; //"test.UsedEmailCnt";        
        public readonly string GlobalIncrementSetName = "test.GlobalIncrement";       
        public readonly string InterventionSetName = "test.Intervention";
        public readonly string LiveWagerSetName = "test.LiveWager";
        public readonly string InterventionThresholdsSetName = "test.InterventionThresholds";
        
    }
}
