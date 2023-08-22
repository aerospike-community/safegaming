using System.Reflection.Metadata;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PlayerCommon;

namespace GameDashBoard
{
    public interface IDBConnectionGDB : IDBConnection
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="playerId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<Player> GetPlayer(int playerId,
                                CancellationToken cancellationToken);
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="playerId"></param>
        /// <param name="tranDT"></param>
        /// <returns>        
        /// </returns>
        Task<IEnumerable<LiveWager>> GetLiveWager(DateTimeOffset tranDT,
                                                    CancellationToken cancellationToken);

        Task<IEnumerable<GlobalIncrement>> GetGlobalIncrement(DateTimeOffset tranDT,
                                                                CancellationToken cancellationToken);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="playerId"></param>
        /// <param name="tranDT"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>        
        /// </returns>
        Task<IEnumerable<Intervention>> GetIntervention(DateTimeOffset tranDT,
                                                        CancellationToken cancellationToken);
    }
}
