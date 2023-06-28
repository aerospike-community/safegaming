using Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

#if MONGODB
using MongoDB.Bson.Serialization.Attributes;
#endif

namespace PlayerGeneration
{
    public sealed class WagerResultTransaction
    {
        public enum Types
        {
            Wager,
            Win,
            Loss
        }

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

        [BsonConstructor]
        public WagerResultTransaction(long id, 
                                        DateTimeOffset timestamp, 
                                        string game, 
                                        string betType, 
                                        Types type, 
                                        decimal amount, 
                                        decimal playerBalance, 
                                        decimal gGRAmount, 
                                        int riskScore, 
                                        bool intervention, 
                                        string timeBucketSec, 
                                        string timeBucketMin, 
                                        string timeBucketHour, 
                                        string timeBucketDay)
        {
            //Tag = tag;
            Id = id;
            Timestamp = timestamp;
            Game = game;
            BetType = betType;
            Type = type;
            Amount = amount;
            PlayerBalance = playerBalance;
            GGRAmount = gGRAmount;
            RiskScore = riskScore;
            Intervention = intervention;
            TimeBucketSec = timeBucketSec;
            TimeBucketMin = timeBucketMin;
            TimeBucketHour = timeBucketHour;
            TimeBucketDay = timeBucketDay;
            //PlayerId = playerId;
        }

        [BsonIgnore]
        public string Tag { get; } = "WagerResultTransaction";
        [BsonId]
        [BsonElement]
        public long Id { get; }
        [BsonElement]
        public DateTimeOffset Timestamp { get; }
        [BsonElement]
        public string Game { get; }
        [BsonElement]
        public string BetType { get; }
        [BsonElement]
        public Types Type { get; }

        /// <summary>
        /// Can be the Wager, Win, or Loss amount based on <see cref="Type"/>.
        /// </summary>
        [BsonElement]
        public decimal Amount { get; }
        public decimal PlayerBalance { get; set; }
        [BsonElement]
        public decimal GGRAmount { get; }
        [BsonElement]
        public int RiskScore { get; }
        public bool Intervention { get; set; }

        [BsonElement]
        public string TimeBucketSec { get; private set; }
        [BsonElement]
        public string TimeBucketMin { get; private set; }
        [BsonElement]
        public string TimeBucketHour { get; private set; }
        [BsonElement]
        public string TimeBucketDay { get; private set; }

        [JsonIgnore]
        //[BsonElement]
        private int PlayerId { get; }

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
