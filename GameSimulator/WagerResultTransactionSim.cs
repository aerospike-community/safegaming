using Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace PlayerCommon
{
    public partial class WagerResultTransaction
    {
        
        public WagerResultTransaction(string game,
                                        string betType,
                                        Types type,
                                        decimal wagerAmt,
                                        decimal ggrAmt,
                                        int riskScore,
                                        bool intervention,
                                        DateTimeOffset timeStamp,
                                        int playerId,
                                        decimal? playerBalance = null)
        {
            this.PlayerId = playerId;
            this.Id = Helpers.GetLongHash(playerId);
            this.Timestamp = timeStamp;
            this.Game = game;
            this.BetType = betType;
            this.Type = type;
            this.Amount = wagerAmt;
            this.PlayerBalance = playerBalance ?? 0;
            this.GGRAmount = ggrAmt;
            this.RiskScore = riskScore;
            this.Intervention = intervention;
        }

        public WagerResultTransaction(WagerResultTransaction cloneTrx,
                                        Types newType,
                                        DateTimeOffset timeStamp,
                                        string betType = null,
                                        int? riskScore = null,
                                        bool? intervention = null,
                                        decimal? playerAmount = null,
                                        decimal? wagerAmt = null,
                                        decimal? newGGR = null)
        {
            this.PlayerId = cloneTrx.PlayerId;
            this.Id = Helpers.GetLongHash(PlayerId);
            this.Timestamp = timeStamp;
            this.Type = newType;
            this.RiskScore = riskScore ?? cloneTrx.RiskScore;
            this.Intervention = intervention ?? cloneTrx.Intervention;
            this.Game = cloneTrx.Game;
            this.BetType = betType ?? cloneTrx.BetType;
            this.Amount = wagerAmt ?? cloneTrx.Amount;
            this.PlayerBalance = playerAmount ?? cloneTrx.PlayerBalance;

            if (newGGR.HasValue)
            {
                this.GGRAmount = newGGR.Value;
            }

            if (this.Type == Types.Win)
                this.GGRAmount -= wagerAmt ?? cloneTrx.Amount;
            else if (this.Type == Types.Loss)
                this.GGRAmount += wagerAmt ?? cloneTrx.Amount;
        }
        
        internal void UpdateTimeBuckets(string state, string county)
        {
            this.TimeBucketMin = string.Format("{0}|{1}|{2}",
                                                state,
                                                county,
                                                this.Timestamp
                                                    .Round(new TimeSpan(0, 1, 0), MidpointRounding.ToZero)
                                                    .ToString(Settings.Instance.TimeStampFormatString));
            this.TimeBucketSec = string.Format("{0}|{1}|{2}",
                                                state,
                                                county,
                                                this.Timestamp
                                                    .Round(new TimeSpan(0, 0, 1), MidpointRounding.ToZero)
                                                    .ToString(Settings.Instance.TimeStampFormatString));
            this.TimeBucketHour = string.Format("{0}|{1}|{2}",
                                                state,
                                                county,
                                                this.Timestamp
                                                    .Round(new TimeSpan(1, 0, 0), MidpointRounding.ToZero)
                                                    .ToString(Settings.Instance.TimeStampFormatString));
            this.TimeBucketDay = string.Format("{0}|{1}|{2}",
                                                state,
                                                county,
                                                this.Timestamp
                                                    .Round(new TimeSpan(24, 0, 0), MidpointRounding.ToZero)
                                                    .ToString(Settings.Instance.TimeStampFormatString));
        }
    }
}
