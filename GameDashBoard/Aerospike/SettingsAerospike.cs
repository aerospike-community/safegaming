using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameDashBoard
{
    partial class GameDashBoardSettings
    {
        public class AerospikeSettings
        {
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

            public string CurrentPlayersSetName = "test.CurrentPlayers";
            public string PlayersHistorySetName = "test.PlayersHistory";
            public string PlayersTransHistorySetName = "test.PlayersTransHistory";
            public string UsedEmailCntSetName = null; //"test.UsedEmailCnt";        
            public string GlobalIncrementSetName = "test.GlobalIncrement";
            public string InterventionSetName = "test.Intervention";
            public string LiveWagerSetName = "test.LiveWager";
            public string InterventionThresholdsSetName = "test.InterventionThresholds";
        }

        public AerospikeSettings Aerospike;
    }

    partial class SettingsGDB
    {
        static SettingsGDB()
        {
            RemoveFromNotFoundSettings.Add("Mongodb:");
        }
    }
}
