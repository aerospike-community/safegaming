using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aerospike.Client;
using Common;
using GameDashBoard;

namespace PlayerCommon
{
    partial class DBConnection : IDBConnectionGDB
    {
        #region Policies
        void CreateWritePolicy()
        {
            this.WritePolicy = new Aerospike.Client.WritePolicy()
            {
                sendKey = true,
                socketTimeout = this.ASSettings.DBOperationTimeout,
                totalTimeout = this.ASSettings.totalTimeout * 3,
                compress = this.ASSettings.EnableDriverCompression,
                maxRetries = this.ASSettings.maxRetries
            };

            Logger.Instance.Dump(WritePolicy, Logger.DumpType.Info, "\tWrite Policy", 2);
        }
        void CreateReadPolicies()
        {
            this.ReadPolicy = new Policy(this.Connection.readPolicyDefault);

            Logger.Instance.Dump(ReadPolicy, Logger.DumpType.Info, "\tRead Policy", 2);
        }

        void CreateListPolicies()
        {
            this.ListPolicy = new ListPolicy(ListOrder.UNORDERED, ListWriteFlags.DEFAULT);
            Logger.Instance.Dump(ListPolicy, Logger.DumpType.Info, "\tRead Policy", 2);
        }
        #endregion

        public Task CreateIndexes(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task GetGlobalIncrement(DateTimeOffset tranDT,
                                        ref long currentTransCnt, 
                                        int maxTransactions, 
                                        decimal playerPct, 
                                        CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task GetIntervention(DateTimeOffset tranDT,
                                        ref long currentTransCnt,
                                        int maxTransactions,
                                        decimal playerPct,
                                        CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task GetLiveWager(DateTimeOffset tranDT,
                                    ref long currentTransCnt,
                                    int maxTransactions,
                                    decimal playerPct,
                                    CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<Player> GetPlayer(int playerId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
