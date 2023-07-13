﻿using System;
using System.Collections.Generic;
using System.Text;

#if MONGODB
using MongoDB.Bson.Serialization.Attributes;
#endif

namespace PlayerGeneration
{
    public sealed class Session
    {
        static readonly Random TriggerRandom = new Random();

        public Session(DateTimeOffset startTimeStamp,
                        Player player)
        {
            Id = Helpers.GetLongHash(player.PlayerId);
            StartTimeStamp = startTimeStamp;
            StartingBalance = player.Metrics.CurrentBalance;            
            //RiskScore = player.Metrics.CurrentRiskScore;
            Player = player;

            #region Trigger Values
            /*
             * session_opening_stake_amt
                If avg_stake is greater than $1 then:
                //if random number (0-1) is greater than 0.159 and less than or equal to 0.5 then avg_stake - (avg_stake*random number between 0 and 0.2)
                //if random number (0-1) is  less than or equal to 0.159 then avg_stake - (avg_stake*random number between 0.2 and 0.4)
                //if random number (0-1) is greater than 0.5 and less than or equal to 0.841 then avg_stake + (avg_stake*random number between 0 and 0.2)
                //if random number (0-1) is greater than 0.841 and less than or equal to 0.977 then avg_stake + (avg_stake*random number between 0.2 and 0.4)
                /if random number (0-1) is greater than 0.977 and less than or equal to 0.998 then avg_stake + (avg_stake*random number between 0.4 and 0.6)
                /if random number (0-1) is greater than 0.998 then avg_stake + (avg_stake*random number between 0.6 and 2)
                Cap max stake amount at $50
                Min stake amount is $0.10

                If avg_stake is less than $1 then: 
                /if random number (0-1) is greater than 0.5 and less than or equal to 0.841 then avg_stake + (avg_stake*random number between 0 and 1)
                /if random number (0-1) is greater than 0.841 and less than or equal to 0.977 then avg_stake + (avg_stake*random number between 1 and 2)
                /if random number (0-1) is greater than 0.977 and less than or equal to 0.998 then avg_stake + (avg_stake*random number between 2 and 3)
                if random number (0-1) is greater than 0.998 then avg_stake + (avg_stake*random number between 3 and 20)
                Cap max stake amount at $50
                Min stake amount is $0.10
             */
            if (player.Metrics.AvgStake > 1)
            {
                var randomNbr = TriggerRandom.NextDouble();

                if (randomNbr > 0.159 && randomNbr <= 0.5)
                {
                    var randomNbr2 = (decimal) Helpers.GetRandomNumber(0, 0.2);
                    this.OpeningStakeAmount = player.Metrics.AvgStake * randomNbr2;
                }
                else if (randomNbr <= 0.159)
                {
                    var randomNbr2 = (decimal) Helpers.GetRandomNumber(0.2, 0.4);
                    this.OpeningStakeAmount = player.Metrics.AvgStake - (player.Metrics.AvgStake * randomNbr2);
                }
                else if (randomNbr > 0.5 && randomNbr <= 0.841)
                {
                    var randomNbr2 = (decimal) Helpers.GetRandomNumber(0, 0.2);                   
                    this.OpeningStakeAmount = player.Metrics.AvgStake * randomNbr2;
                }
                else if (randomNbr > 0.841 && randomNbr <= 0.977)
                {
                    var randomNbr2 = (decimal) Helpers.GetRandomNumber(0.2, 0.4);
                    this.OpeningStakeAmount = player.Metrics.AvgStake + (player.Metrics.AvgStake * randomNbr2);
                }
                else if (randomNbr > 0.977 && randomNbr <= 0.998)
                {
                    var randomNbr2 = (decimal) Helpers.GetRandomNumber(0.4, 0.6);
                    this.OpeningStakeAmount = player.Metrics.AvgStake + (player.Metrics.AvgStake * randomNbr2);
                }
                else if (randomNbr > 0.998)
                {
                    var randomNbr2 = (decimal)Helpers.GetRandomNumber(0.6, 2.0);
                    this.OpeningStakeAmount = player.Metrics.AvgStake * randomNbr2;
                }

                if (this.OpeningStakeAmount < 0.10M)
                    this.OpeningStakeAmount = 0.10M;
                else if (this.OpeningStakeAmount > 50M)
                    this.OpeningStakeAmount = 50M;
            }
            else
            {
                var randomNbr = TriggerRandom.NextDouble();

                if (randomNbr > 0.5 && randomNbr <= 0.841)
                {
                    var randomNbr2 = (decimal)Helpers.GetRandomNumber(0, 1);
                    this.OpeningStakeAmount = player.Metrics.AvgStake + (player.Metrics.AvgStake * randomNbr2);
                }                
                else if (randomNbr > 0.841 && randomNbr <= 0.977)
                {
                    var randomNbr2 = (decimal)Helpers.GetRandomNumber(1, 2);
                    this.OpeningStakeAmount = player.Metrics.AvgStake + (player.Metrics.AvgStake * randomNbr2);
                }
                else if (randomNbr > 0.977 && randomNbr <= 0.998)
                {
                    var randomNbr2 = (decimal)Helpers.GetRandomNumber(2, 3);
                    this.OpeningStakeAmount = player.Metrics.AvgStake + (player.Metrics.AvgStake * randomNbr2);
                }               
                else if (randomNbr > 0.998)
                {
                    var randomNbr2 = (decimal)Helpers.GetRandomNumber(3, 20);
                    this.OpeningStakeAmount = player.Metrics.AvgStake + (player.Metrics.AvgStake * randomNbr2);
                }

                if (this.OpeningStakeAmount < 0.10M)
                    this.OpeningStakeAmount = 0.10M;
                else if (this.OpeningStakeAmount > 50M)
                    this.OpeningStakeAmount = 50M;
            }

            this.OpeningStakeAmount = Decimal.Round(this.OpeningStakeAmount, 2);

            /*
             * session_end_trigger_losses
             /if random number (0-1) is less than or equal to 0.420 then avg_ggr_per_session - (avg_ggr_per_session*random number between 0.00 and 0.50)
            /if random number (0-1) is greater than 0.420 and less than or equal to 0.841 then avg_ggr_per_session + (avg_ggr_per_session*random number between 0.00 and 1.00)
            /if random number (0-1) is greater than 0.841 and less than or equal to 0.977 then avg_ggr_per_session + (avg_ggr_per_session*random number between 1.0 and 2.0)
            /if random number (0-1) is greater than 0.977 and less than or equal to 0.998 then avg_ggr_per_session + (avg_ggr_per_session*random number between 2 and 3)
            /if random number (0-1) is greater than 0.998 then avg_ggr_per_session + (avg_ggr_per_session*random number between 3 and 10)

             */
            {
                var randomNbr = TriggerRandom.NextDouble();

                if (randomNbr <= 0.420)
                {
                    var randomNbr2 = (decimal)Helpers.GetRandomNumber(0, 0.5);
                    this.EndTriggerLosses = player.Metrics.AvgGGRPerSession - (player.Metrics.AvgGGRPerSession * randomNbr2);
                }
                else if (randomNbr > 0.420 && randomNbr <= 0.841)
                {
                    var randomNbr2 = (decimal)Helpers.GetRandomNumber(0, 1);
                    this.EndTriggerLosses = player.Metrics.AvgGGRPerSession + (player.Metrics.AvgGGRPerSession * randomNbr2);
                }
                else if (randomNbr > 0.841 && randomNbr <= 0.977)
                {
                    var randomNbr2 = (decimal)Helpers.GetRandomNumber(1, 2);
                    this.EndTriggerLosses = player.Metrics.AvgGGRPerSession + (player.Metrics.AvgGGRPerSession * randomNbr2);
                }
                else if (randomNbr > 0.977 && randomNbr <= 0.998)
                {
                    var randomNbr2 = (decimal)Helpers.GetRandomNumber(2, 3);
                    this.EndTriggerLosses = player.Metrics.AvgGGRPerSession + (player.Metrics.AvgGGRPerSession * randomNbr2);
                }               
                else if (randomNbr > 0.998)
                {
                    var randomNbr2 = (decimal)Helpers.GetRandomNumber(3, 10);
                    this.EndTriggerLosses = player.Metrics.AvgGGRPerSession + (player.Metrics.AvgGGRPerSession * randomNbr2);
                }
            }
           
            /*
             session_end_trigger_wagers
                if random number (0-1) is less than or equal to 0.420 then avg_wagers_per_session - (avg_wagers_per_session*random number between 0.00 and 0.50)
                if random number (0-1) is greater than 0.420 and less than or equal to 0.841 then avg_wagers_per_session + (avg_wagers_per_session*random number between 0.00 and 1.00)
                if random number (0-1) is greater than 0.841 and less than or equal to 0.977 then avg_wagers_per_session + (avg_wagers_per_session*random number between 1.0 and 2.0)
                if random number (0-1) is greater than 0.977 and less than or equal to 0.998 then avg_wagers_per_session + (avg_wagers_per_session*random number between 2 and 3)
                if random number (0-1) is greater than 0.998 then avg_wagers_per_session + (avg_wagers_per_session*random number between 3 and 10)

             */
            {
                var randomNbr = TriggerRandom.NextDouble();

                if (randomNbr <= 0.420)
                {
                    var randomNbr2 = (decimal)Helpers.GetRandomNumber(0, 0.5);
                    this.EndTriggerWagers = player.Metrics.AvgWagersPerSession - (player.Metrics.AvgWagersPerSession * randomNbr2);
                }
                else if (randomNbr > 0.420 && randomNbr <= 0.841)
                {
                    var randomNbr2 = (decimal)Helpers.GetRandomNumber(0, 1);
                    this.EndTriggerWagers = player.Metrics.AvgWagersPerSession + (player.Metrics.AvgWagersPerSession * randomNbr2);
                }
                else if (randomNbr > 0.841 && randomNbr <= 0.977)
                {
                    var randomNbr2 = (decimal)Helpers.GetRandomNumber(1, 2);
                    this.EndTriggerWagers = player.Metrics.AvgWagersPerSession + (player.Metrics.AvgWagersPerSession * randomNbr2);
                }
                else if (randomNbr > 0.977 && randomNbr <= 0.998)
                {
                    var randomNbr2 = (decimal)Helpers.GetRandomNumber(2, 3);
                    this.EndTriggerWagers = player.Metrics.AvgWagersPerSession + (player.Metrics.AvgWagersPerSession * randomNbr2);
                }
                else if (randomNbr > 0.998)
                {
                    var randomNbr2 = (decimal)Helpers.GetRandomNumber(3, 10);
                    this.EndTriggerWagers = player.Metrics.AvgWagersPerSession + (player.Metrics.AvgWagersPerSession * randomNbr2);
                }
            }

            this.WagerCountBingeThreshold = (decimal) Helpers.GetRandomNumber(1, (double) EndTriggerWagers);
            this.WagerTransCountBingeThreshold = (int) Helpers.GetRandomNumber(5, 20);

            #endregion
        }

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

        [Newtonsoft.Json.JsonIgnore]
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

        public bool CheckEndTriggerBigWin(Random random)
        {
            /*
                if very high value tier win_amount over 2500 or a cumulative_session_ggr >= 2500
                if high value tier any win_amount over 1500 or a cumulative_session_ggr >= 1500
                if medium value tier win_amount over 500 or a cumulative_session_ggr >= 500
                if low value tier win_amount over 150 or a cumulative_session_ggr >= 150
                50% chance of player stopping if they hit this limit
            */
            if ((this.Player.ValueTier == Tiers.VeryHigh
                        && (WinAmounts > 2500 || GGR >= 2500))
                    || (this.Player.ValueTier == Tiers.High
                            && (WinAmounts > 1500 || GGR >= 1500))
                    || (this.Player.ValueTier == Tiers.Medium
                            && (WinAmounts > 500 || GGR >= 500))
                    || (this.Player.ValueTier == Tiers.Low
                            && (WinAmounts > 150 || GGR >= 150)))
            {
                var chance = random.Next(1, 100);

                if (chance >= 50) return this.EndTriggerBigWin = true;
            }

            return false;
        }

        public (bool endSession, bool reDeposit) CheckEndTriggerBalance(decimal wager, Random random)
        {
            /*
             if balance-stake_amt <= 0 on the next spin then there is 50% chance that the player redeposit
                and continues the session.
            If player doesn't deposit then session ends.
             */

            if(this.Player.Metrics.CurrentBalance - wager < 0)
            {
                var chance = random.Next(1, 100);

                if (chance >= 50) return (false, true);

                this.EndTriggerBalance = true;
                
                return (true, false);
            }

            return (false, false);
        }
    }
}