using Aerospike.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerCommon
{
    public class AerospikeSettings
    {
        public AsyncClientPolicy ClientPolicy { get; set; } = new AsyncClientPolicy();

        public string DBHost = "localhost";
        public int DBPort = 3000;
        public bool DaaS = false;

        public int? DBOperationTimeout;
        public bool? EnableDriverCompression;
        public int? SocketTimeout;
        public int? MaxRetries;
        public int? SleepBetweenRetries;
        public string TLSHostName;

        public int RecordsPerSecond;

        public string CurrentPlayersSetName = null; //"test.CurrentPlayers";
        public string PlayersHistorySetName = null; //"test.PlayersHistory";
        public string PlayersTransHistorySetName = null; //"test.PlayersTransHistory";
        public string UsedEmailCntSetName = null; //"test.UsedEmailCnt";        
        public string GlobalIncrementSetName = null; //"test.GlobalIncrement";
        public string InterventionSetName = null; //"test.Intervention";
        public string LiveWagerSetName = null; //"test.LiveWager";
        public string InterventionThresholdsSetName = null; //"test.InterventionThresholds";
    }
}
