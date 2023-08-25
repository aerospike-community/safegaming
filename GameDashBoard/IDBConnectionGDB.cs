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
        
        Task GetPlayer(int playerId,
                        int sessionIdx,
                        CancellationToken cancellationToken);
        
        int GetLiveWager(DateTimeOffset tranDT,
                            int sessionIdx,
                            int maxTransactions,
                            CancellationToken cancellationToken);

        int GetGlobalIncrement(DateTimeOffset tranDT,
                                int sessionIdx,
                                int maxTransactions,
                                CancellationToken cancellationToken);

        int GetIntervention(DateTimeOffset tranDT,
                            int sessionIdx,
                            int maxTransactions,
                            CancellationToken cancellationToken);

        Task CreateIndexes(CancellationToken cancellationToken);
    }
}
