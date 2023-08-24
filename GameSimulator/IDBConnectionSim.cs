using Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PlayerCommon
{
    public interface IDBConnectionSim : IDBConnection
    {        
        void Truncate();

        Task UpdateCurrentPlayers(Player player,
                                    bool updateHistory,
                                    CancellationToken cancellationToken);

        Task UpdateChangedCurrentPlayer(Player player,
                                        CancellationToken cancellationToken,
                                        bool updateSession = false,
                                        int updateFin = 0,
                                        bool updateGame = false,
                                        int updateWagerResult = 0);

        Task<string> DeterineEmail(string firstName,
                                    string lastName,
                                    string domain,
                                    CancellationToken token);

        Task IncrementGlobalSet(GlobalIncrement glbIncr,
                                CancellationToken token);

        Task UpdateIntervention(Intervention intervention,
                                CancellationToken cancellationToken);

        Task UpdateLiveWager(Player player,
                                WagerResultTransaction wagerResult,
                                WagerResultTransaction wager,
                                CancellationToken cancellationToken);

        Task<InterventionThresholds> ReFreshInterventionThresholds(InterventionThresholds interventionThresholds,
                                                                    CancellationToken cancellationToken);

        Task<bool> InterventionThresholdsRefreshCheck(InterventionThresholds current,
                                                        CancellationToken token,
                                                        bool forceRefresh = false);

        ConsoleDisplay PlayerProgression { get; set; }
        ConsoleDisplay HistoryProgression { get; set; }

    }
    
}
