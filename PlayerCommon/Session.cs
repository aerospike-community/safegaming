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
    public sealed partial class Session
    {        
        public Session(Session cloneSession)
        {
            Id = cloneSession.Id;
            Player = cloneSession.Player;
            StartingBalance = cloneSession.StartingBalance;
            Wagers = cloneSession.Wagers;
            EndingTimeStamp = cloneSession.EndingTimeStamp;
            WagerAmounts = cloneSession.WagerAmounts;
            WinAmounts = cloneSession.WinAmounts;
            LossAmounts = cloneSession.LossAmounts;
            RiskScore = cloneSession.RiskScore;
            GamesPlayed = cloneSession.GamesPlayed;

            OpeningStakeAmount = cloneSession.OpeningStakeAmount;
            EndTriggerLosses = cloneSession.EndTriggerLosses;
            EndTriggerWagers = cloneSession.EndTriggerWagers;
            EndTriggerBalance = cloneSession.EndTriggerBalance;
            EndTriggerBigWin = cloneSession.EndTriggerBigWin;
            WagerCountBingeThreshold = cloneSession.WagerCountBingeThreshold;
            WagerTransCountBingeThreshold = cloneSession.WagerTransCountBingeThreshold;
            InterventionType = cloneSession.InterventionType;
            Closed = cloneSession.Closed;
    }

		[BsonConstructor]
        public Session(long id, 
                        DateTimeOffset startTimeStamp, 
                        decimal startingBalance, 
                        int wagers, 
                        int gamesPlayed, 
                        DateTimeOffset? endingTimeStamp, 
                        decimal wagerAmounts, 
                        decimal winAmounts, 
                        decimal lossAmounts, 
                        int riskScore, 
                        decimal openingStakeAmount, 
                        decimal endTriggerLosses, 
                        decimal endTriggerWagers, 
                        bool endTriggerBalance, 
                        bool endTriggerBigWin, 
                        decimal wagerCountBingeThreshold, 
                        decimal wagerTransCountBingeThreshold, 
                        string interventionType, 
                        bool closed)
        {
            //Player = player;
            //Tag = tag;
            Id = id;
            StartTimeStamp = startTimeStamp;
            StartingBalance = startingBalance;
            Wagers = wagers;
            GamesPlayed = gamesPlayed;
            EndingTimeStamp = endingTimeStamp;
            WagerAmounts = wagerAmounts;
            WinAmounts = winAmounts;
            LossAmounts = lossAmounts;
            RiskScore = riskScore;
            OpeningStakeAmount = openingStakeAmount;
            EndTriggerLosses = endTriggerLosses;
            EndTriggerWagers = endTriggerWagers;
            EndTriggerBalance = endTriggerBalance;
            EndTriggerBigWin = endTriggerBigWin;
            WagerCountBingeThreshold = wagerCountBingeThreshold;
            WagerTransCountBingeThreshold = wagerTransCountBingeThreshold;
            InterventionType = interventionType;
            Closed = closed;
        }

        [JsonIgnore]
		[BsonIgnore]
        private Player Player { get; }

		[BsonIgnore]
        public string Tag { get; } = "Session";

		[BsonId]
		[BsonElement]
        public long Id { get; }

		[BsonElement]
        public DateTimeOffset StartTimeStamp { get; }

		[BsonElement]
        public decimal StartingBalance { get; internal set; }

		[BsonElement]
        public decimal GGR { get { return this.LossAmounts - this.WinAmounts; } }

		[BsonElement]
        public int Wagers { get; set; }

		[BsonElement]
        public int GamesPlayed { get; internal set; }

		[BsonElement]
        public DateTimeOffset? EndingTimeStamp { get; internal set; }
        public decimal WagerAmounts { get; set; }
        public decimal WinAmounts { get; set; }
        public decimal LossAmounts { get; set; }
        public int RiskScore { get; set; }


		[BsonElement]
        public decimal OpeningStakeAmount { get; }

		[BsonElement]
        public decimal EndTriggerLosses { get; }

		[BsonElement]
        public decimal EndTriggerWagers { get; }

		[BsonElement]
        public bool EndTriggerBalance { get; private set; }

		[BsonElement]
        public bool EndTriggerBigWin { get; private set; }

		[BsonElement]
        public decimal WagerCountBingeThreshold { get; }

		[BsonElement]
        public decimal WagerTransCountBingeThreshold { get; }

        /// <summary>
        /// Intervention Event
        /// </summary>
        public string InterventionType { get; set; }

		[BsonElement]
        public bool Closed { get; internal set; } = false;

        /// <summary>
        /// Total Session in minutes. If a current session, the current session in minutes.
        /// </summary>
		[BsonElement]
        public int SessionLength 
        { 
            get 
            {
                return (int)((this.EndingTimeStamp ?? Player.UseTime.Current) - this.StartTimeStamp).TotalMinutes; 
            }
        }
    }
}
