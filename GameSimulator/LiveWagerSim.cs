using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace PlayerCommon
{
    public partial class LiveWager
    {
        public LiveWager(Player player,
                            TimeSpan roundInterval,
                            string timeZoneFormatWoZone,
                            WagerResultTransaction wagerResult,
                            WagerResultTransaction wager)
        {
            this.Id = Helpers.GetLongHash(Environment.CurrentManagedThreadId);

            var tsWoZone = wagerResult.Timestamp
                                            .Round(roundInterval, MidpointRounding.ToZero)
                                            .UtcDateTime
                                            .ToString(timeZoneFormatWoZone);

            this.Aggkey = $"{player.PlayerId}:{tsWoZone}:{wager.Amount}";
            this.bet_type = wagerResult.BetType;
            this.PlayerId = player.PlayerId;
            this.result_type = wagerResult.Type.ToString();
            this.risk_score = wagerResult.RiskScore;
            this.stake_amount = wager.Amount;
            this.txn_ts = wagerResult.Timestamp;
            this.win_amount = wagerResult.Type == WagerResultTransaction.Types.Win
                                ? wagerResult.Amount
                                : 0;
            this.TransId = wagerResult.Id;
        }

    }
}
