using Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

#if MONGODB
using MongoDB.Bson.Serialization.Attributes;
#else
using PlayerCommonDummy;
#endif

namespace PlayerCommon
{
    public sealed partial class WagerResultTransaction
    {
        public enum Types
        {
            Wager,
            Win,
            Loss
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
    }
}
