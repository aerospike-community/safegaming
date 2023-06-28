using Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PlayerGeneration
{
    public interface IDBConnection : IDisposable
    {
        
        Progression ConsoleProgression { get; }
        ConsoleDisplay PlayerProgression { get; }
        ConsoleDisplay HistoryProgression { get; }

        bool UsedEmailCntEnabled { get; }

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

    }
    
}
