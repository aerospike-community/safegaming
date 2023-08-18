using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GameSimulator;

namespace PlayerCommon
{
    public partial class Intervention
    {        
        public static async Task Determine(Player player,
                                            WagerResultTransaction wagerTrans,
                                            IDBConnectionSim dBConnection,
                                            CancellationToken token)
        {
            if (player.Session.GGR > player.Metrics.hard_session_heavy_loss_threshold)
            {
                player.UseTime.AddSec();

                wagerTrans.Intervention = true;
                player.Metrics.Interventions++;
                player.Session.InterventionType = "HARD";
                player.CloseSession(false, true);

                await dBConnection.UpdateIntervention(new Intervention(player.PlayerId,
                                                                        player.Metrics.CLV,
                                                                        player.Session.StartTimeStamp,
                                                                        player.UseTime.Current,
                                                                        "HARD",
                                                                        null,
                                                                        "Heavy Session Losses",
                                                                        player.Session.GGR,
                                                                        wagerTrans.Game,
                                                                        wagerTrans.BetType,
                                                                        player.County,
                                                                        player.CountryCode,
                                                                        player.CountyFIPSCode,
                                                                        player.State,
                                                                        player.State,
                                                                        wagerTrans.Id),
                                                        token);
            }
            else if (player.Session.InterventionType == null && player.Session.GGR > player.Metrics.soft_session_heavy_loss_threshold)
            {
                player.UseTime.AddSec();

                wagerTrans.Intervention = true;
                player.Metrics.Interventions++;
                player.Session.InterventionType = "SOFT";

                if(dBConnection is not null)
                    await dBConnection.UpdateIntervention(new Intervention(player.PlayerId,
                                                                            player.Metrics.CLV,
                                                                            player.Session.StartTimeStamp,
                                                                            player.UseTime.Current,
                                                                            "SOFT",
                                                                            null,
                                                                            "Heavy Session Losses",
                                                                            player.Session.GGR,
                                                                            wagerTrans.Game,
                                                                            wagerTrans.BetType,
                                                                            player.County,
                                                                            player.CountryCode,
                                                                            player.CountyFIPSCode,
                                                                            player.State,
                                                                            player.State,
                                                                            wagerTrans.Id),
                                                            token);
            }

        }

    }
}
