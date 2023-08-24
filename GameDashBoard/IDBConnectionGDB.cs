using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GameDashBoard;

namespace PlayerCommon
{
    public interface IDBConnectionGDB : IDBConnection
    {
        
        Task<Player> GetPlayer(int playerId,
                                CancellationToken cancellationToken);
        
        Task GetLiveWager(DateTimeOffset tranDT,
                            ref long currentTransCnt,
                            int maxTransactions,
                            decimal playerPct,
                            CancellationToken cancellationToken);

        Task GetGlobalIncrement(DateTimeOffset tranDT,
                                ref long currentTransCnt,           
                                int maxTransactions,
                                decimal playerPct,
                                CancellationToken cancellationToken);

        Task GetIntervention(DateTimeOffset tranDT,
                                ref long currentTransCnt,
                                int maxTransactions,
                                decimal playerPct,
                                CancellationToken cancellationToken);

        Task CreateIndexes(CancellationToken cancellationToken);
    }
}
