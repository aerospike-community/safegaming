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
        
        Task<IEnumerable<LiveWager>> GetLiveWager(DateTimeOffset tranDT,
                                                    CancellationToken cancellationToken);

        Task<IEnumerable<GlobalIncrement>> GetGlobalIncrement(DateTimeOffset tranDT,
                                                                CancellationToken cancellationToken);

        Task<IEnumerable<Intervention>> GetIntervention(DateTimeOffset tranDT,
                                                        CancellationToken cancellationToken);

        Task CreateIndexes(CancellationToken cancellationToken);
    }
}
