using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

#if MONGODB
using MongoDB.Bson.Serialization.Attributes;
#endif

namespace PlayerGeneration
{
    public sealed class Metrics
    {
        public Metrics(Player player, decimal currentBlance)
        {
            Player = player;
            CurrentBalance = currentBlance;

            {
                var random = new Random(Guid.NewGuid().GetHashCode());

                switch (player.ValueTier)
                {
                    case Tiers.VeryHigh:
                        this.CLV = random.Next(150001, 1000000);
                        break;
                    case Tiers.High:
                        this.CLV = random.Next(50001, 150000);
                        break;
                    case Tiers.Medium:
                        this.CLV = random.Next(20001, 50000);
                        break;
                    case Tiers.Low:
                        this.CLV = random.Next(10, 20000);
                        break;
                    default:
                        this.CLV = random.Next(20001, 50000);
                        break;
                }

                var randomPercent = random.Next(0, 100);

                if (randomPercent >= 0 && randomPercent <= 50)
                    this.ExpectedActiveDaysPerYear = random.Next(1, 100);
                else if (randomPercent >= 51 && randomPercent <= 70)
                    this.ExpectedActiveDaysPerYear = random.Next(101, 150);
                else if (randomPercent >= 71 && randomPercent <= 79)
                    this.ExpectedActiveDaysPerYear = random.Next(151, 200);
                else if (randomPercent >= 80 && randomPercent <= 89)
                    this.ExpectedActiveDaysPerYear = random.Next(201, 250);
                else if (randomPercent >= 90 && randomPercent <= 96)
                    this.ExpectedActiveDaysPerYear = random.Next(251, 300);
                else
                    this.ExpectedActiveDaysPerYear = random.Next(301, 355);
               
                randomPercent = random.Next(0, 100);

                if (randomPercent >= 0 && randomPercent <= 30)
                    this.AvgSessionsPerDay = 1;
                else if (randomPercent >= 31 && randomPercent <= 50)
                    this.AvgSessionsPerDay = 2;
                else if (randomPercent >= 51 && randomPercent <= 80)
                    this.AvgSessionsPerDay = 3;
                else if (randomPercent >= 81 && randomPercent <= 95)
                    this.AvgSessionsPerDay = 4;
                else
                    this.AvgSessionsPerDay = random.Next(5, 10);

            }
        }

        public Metrics(Metrics cloneMetrics)
        {
            this.Player = cloneMetrics.Player;
            this.ExpectedActiveDaysPerYear = cloneMetrics.ExpectedActiveDaysPerYear;
            this.AvgSessionsPerDay = cloneMetrics.AvgSessionsPerDay;
            this.CLV = cloneMetrics.CLV;
            this.Deposits = cloneMetrics.Deposits;
            this.Withdrawals = cloneMetrics.Withdrawals;
            this.CurrentBalance = cloneMetrics.CurrentBalance;
            this.WagerAmounts = cloneMetrics.WagerAmounts;
            this.TotalWagersPlaced = cloneMetrics.TotalWagersPlaced;
            this.WinAmounts = cloneMetrics.WinAmounts;
            this.LossAmounts = cloneMetrics.LossAmounts;
            this.PositiveSessions = cloneMetrics.PositiveSessions;
            this.NegativeSessions = cloneMetrics.NegativeSessions;
            this.TotalSessionsTime = cloneMetrics.TotalSessionsTime;
            this.Interventions = cloneMetrics.Interventions;
            this.HabitualScore = cloneMetrics.HabitualScore;                        
    }


		[BsonConstructor]
        public Metrics(//Player player,
                        int expectedActiveDaysPerYear, 
                        int avgSessionsPerDay, 
                        decimal cLV, 
                        decimal lastWagerPlaced, 
                        int deposits, 
                        int withdrawals, 
                        int totalWagersPlaced, 
                        int wagersPlacedPerMinute, 
                        decimal currentBalance, 
                        decimal wagerAmounts, 
                        decimal winAmounts, 
                        decimal lossAmounts, 
                        int positiveSessions, 
                        int negativeSessions, 
                        int totalSessionsTime, 
                        int interventions, 
                        int habitualScore, 
                        decimal totalLostToday, 
                        decimal totalWiinToday, 
                        int daily_session_time_risk_score, 
                        int soft_session_time_threshold)
        {
            //Player = player;
            //Tag = tag;
            ExpectedActiveDaysPerYear = expectedActiveDaysPerYear;
            AvgSessionsPerDay = avgSessionsPerDay;
            CLV = cLV;
            LastWagerPlaced = lastWagerPlaced;
            Deposits = deposits;
            Withdrawals = withdrawals;
            TotalWagersPlaced = totalWagersPlaced;
            WagersPlacedPerMinute = wagersPlacedPerMinute;
            CurrentBalance = currentBalance;
            WagerAmounts = wagerAmounts;
            WinAmounts = winAmounts;
            LossAmounts = lossAmounts;
            PositiveSessions = positiveSessions;
            NegativeSessions = negativeSessions;
            TotalSessionsTime = totalSessionsTime;
            Interventions = interventions;
            HabitualScore = habitualScore;
            //CurrentDay = currentDay;
            //CurrentMin = currentMin;
            TotalLostToday = totalLostToday;
            TotalWiinToday = totalWiinToday;
            this.daily_session_time_risk_score = daily_session_time_risk_score;
            this.soft_session_time_threshold = soft_session_time_threshold;
        }

        [Newtonsoft.Json.JsonIgnore]
        //[BsonElement]
        [BsonIgnore]
        private Player Player { get; }

		[BsonIgnore]
        public string Tag { get; } = "Metrics";

		[BsonElement]
        public int ExpectedActiveDaysPerYear { get; }


		[BsonElement]
        public decimal AvgStake
        {
            get
            {
                /*
                 Very High Value Stake Amt - Pick a random value between $15 and $50
                 High Value Stake Amt - Pick a random value between $10 and $30
                 Medium Value Stake Amt - Pick a random value between $2 and $20
                 Low Value Stake Amt - Pick a random value between $0.1 and $5
                 */
                var random = new Random(Guid.NewGuid().GetHashCode());

                switch (this.Player.ValueTier)
                {
                    case Tiers.VeryHigh:
                        return (decimal)random.Next(15, 50);
                    case Tiers.High:
                        return (decimal)random.Next(10, 30);
                    case Tiers.Medium:
                        return (decimal) random.Next(2, 20);
                    default:
                        break;
                }

                return (decimal) Helpers.GetRandomNumber(0.1d, 5d, random);
            }
        }

		[BsonElement]
        public int AvgSessionsPerDay { get; }

		[BsonElement]
        public decimal AvgSessionLength
        { 
            get {
                /*
                 *  (((CLV/expected_active_days_per_year)/average_sessions_per_day)/avg_stake)/bets per minute 
                 */

                if (ExpectedActiveDaysPerYear == 0 || AvgSessionsPerDay == 0 || AvgStake == 0 || WagersPlacedPerMinute == 0)
                        return 0;

                    return ((( CLV / ExpectedActiveDaysPerYear) / AvgSessionsPerDay) / AvgStake) / WagersPlacedPerMinute;
                }
        }

		[BsonElement]
        public decimal AvgWagersPerSession
        {
            get
            {
                if (ExpectedActiveDaysPerYear == 0 || AvgSessionsPerDay == 0 || AvgStake == 0)
                    return 0;
                /*
                  ((CLV/expected_active_days_per_year)/average_sessions_per_day)/avg_stake
                 */

                return ((CLV / ExpectedActiveDaysPerYear) / AvgSessionsPerDay) / AvgStake;
            }
        }

		[BsonElement]
        public decimal AvgGGRPerSession
        {
            /*
             * CLV/days per year/session per day
             */
            get
            {
                if (ExpectedActiveDaysPerYear == 0 || AvgSessionsPerDay == 0) return 0;
                
                return CLV / ExpectedActiveDaysPerYear / AvgSessionsPerDay;
            }
        }

        /// <summary>
        /// Max of: session_time_risk_score, 
        ///         session_heavy_loss_risk_score, 
        ///         daily_loss_risk_score, 
        ///         risky_staking_risk_score
        ///         daily_session time_risk_score
        /// </summary>
		[BsonElement]
        public int CurrentRiskScore 
        {
                      
            get
            { 
                return Math.Max(session_time_risk_score,
                        Math.Max((int) session_heavy_loss_risk_score,
                            Math.Max(daily_loss_risk_score,
                                Math.Max(risky_staking_risk_score,
                                            daily_session_time_risk_score))));
            }
        }

		[BsonElement]
        public decimal CLV { get; }

        /// <summary>
        /// Last wager placed.
        /// </summary>      
        public decimal LastWagerPlaced { get; set; }
        public int Deposits { get; set; }
        public int Withdrawals { get; set; }
        public int TotalWagersPlaced { get; set; }
        public int WagersPlacedPerMinute { get; set; }
        public decimal CurrentBalance { get; set; }
        public decimal WagerAmounts { get; set; }
        public decimal WinAmounts { get; set; }
        public decimal LossAmounts { get; set; }

		[BsonElement]
        public decimal GGR { get { return this.LossAmounts - this.WinAmounts; } }        
        public int PositiveSessions { get; set; }
        public int NegativeSessions { get; set; }

		[BsonElement]
        public int TotalSessions { get { return this.PositiveSessions + this.NegativeSessions; } }
        /// <summary>
        /// Total Session time in minutes.
        /// </summary>
        public int TotalSessionsTime { get; set; }
        public int Interventions { get; set; }
        public int HabitualScore { get; set; } //not see if needed

        [JsonIgnore]
		[BsonIgnore]
        public int CurrentDay { get; set; } = -1;

        [JsonIgnore]
		[BsonIgnore]
        public int CurrentMin { get; set; } = -1;
        
        public decimal TotalLostToday { get; set; }
        public decimal TotalWiinToday { get; set; }

        #region Intervention Properties

        /// <summary>
        /// If session_length / hard_session_time_threshold > 1 then 100 else (session_length / hard_session_time_threshold)*100
        /// </summary>
		[BsonElement]
        public int session_time_risk_score
        {
            get
            {
                if (hard_session_time_threshold > 0) return 0;

                if (Player.Session.SessionLength / hard_session_time_threshold > 1)
                    return 100;

                return (Player.Session.SessionLength / hard_session_time_threshold) * 100;
            }
        }

        /// <summary>
        /// If cumulative_session_ggr / hard_session_heavy_loss_threshold > 1 then 100 else (cumulative_session_ggr  / hard_session_heavy_loss_threshold)*100		
        /// </summary>

		[BsonElement]
        public decimal session_heavy_loss_risk_score
        {
            get
            {
                var factor = this.Player.Session.GGR / this.hard_session_heavy_loss_threshold;

                if (factor > 1) return 100M;

                return factor * 100M;
            }
        }

        /// <summary>
        /// If (loss_total_today - win_total_today)/hard_daily_loss_threshold > 1 then 100 else ((loss_total_today - win_total_today)/hard_daily_loss_threshold)*100		
        /// </summary>
		[BsonElement]
        public int daily_loss_risk_score
        {
            get
            {
                if (this.hard_daily_loss_threshold == 0) return 0;

                var factor = (this.TotalLostToday - this.TotalWiinToday) / this.hard_daily_loss_threshold;

                if(factor > 1) return 100;

                return (int) factor * 100;
            }
        }

        /// <summary>
        /// If avg_stake/soft_risky_staking_threshold > 1 then 100 else (avg_stake/soft_risky_staking_threshold*100
        /// </summary>
		[BsonElement]
        public int risky_staking_risk_score
        {
            get
            {
                if(this.soft_risky_staking_threshold == 0) return 0;

                if (AvgStake / soft_risky_staking_threshold > 1)
                    return 100;

                return ((int) AvgStake / soft_risky_staking_threshold) * 100;
            }
        }


		[BsonElement]
        public int daily_session_time_risk_score { get; }


		[BsonElement]
        public int soft_session_time_threshold { get; }

        /// <summary>
        /// If avg_session_length * extended_session_time_soft_intervention < min_session_time_to_trigger_soft_intervention
        ///     then min_session_time_to_trigger_soft_intervention
        /// if avg_session_length* extended_session_time_soft_intervention > max_extended session_time_soft_intervention
        ///     then max_extended session_time_soft_intervention 
        /// else avg_session_length* extended_session_time_soft_intervention
        /// </summary>
		[BsonElement]
        public decimal soft_session_heavy_loss_threshold
        {
            get
            {
                if(this.AvgSessionLength * this.Player.InterventionThresholds.extended_session_time_soft_intervention < this.Player.InterventionThresholds.min_session_time_to_trigger_soft_intervention)
                {
                    return this.Player.InterventionThresholds.min_session_time_to_trigger_soft_intervention;
                }

                if(this.AvgSessionLength * this.Player.InterventionThresholds.extended_session_time_soft_intervention > this.Player.InterventionThresholds.max_extended_session_time_soft_intervention)
                {
                    return this.Player.InterventionThresholds.max_extended_session_time_soft_intervention;
                }

                return this.AvgSessionLength * this.Player.InterventionThresholds.extended_session_time_soft_intervention;
            }
        }

        /// <summary>
        /// if avg_ggr_per_session * average_sessions_per_day * daily_losses_soft_intervention < mn_dly_lss_sft
        ///     then mn_dly_lss_sft
        /// else avg_ggr_per_session* average_sessions_per_day * daily_losses_soft_intervention
        /// </summary>
		[BsonElement]
        public decimal soft_daily_loss_threshold
        {
            get
            {
                /*
                 
                 */
                var calc = AvgGGRPerSession * AvgSessionsPerDay * Player.InterventionThresholds.daily_losses_soft_intervention;

                if (calc < Player.InterventionThresholds.min_daily_losses_soft_intervention)
                    return this.Player.InterventionThresholds.daily_losses_soft_intervention;


                return calc;
            }
        }

        /// <summary>
        /// If avg_stake * risky_staking_soft_interaction_avg_stake_multiplier < risky_staking_soft_interaction_min_threshold
        ///     then risky_staking_soft_interaction_min_threshold 
        /// else avg_stake* risky_staking_soft_interaction_avg_stake_multiplier
        /// </summary>
		[BsonElement]
        public int soft_risky_staking_threshold
        {
            get
            {
                if (AvgStake * Player.InterventionThresholds.risky_staking_soft_interaction_avg_stake_multiplier
                        < Player.InterventionThresholds.risky_staking_soft_interaction_min_threshold)
                    return (int) Player.InterventionThresholds.risky_staking_soft_interaction_min_threshold;

                return (int) AvgStake * Player.InterventionThresholds.risky_staking_soft_interaction_avg_stake_multiplier;
            }
        }

        /// <summary>
        /// total_daily_session_duration_soft_intervention
        /// </summary>
		[BsonElement]
        public int soft_daily_session_time_threshold
        {
            get
            {
                return Player.InterventionThresholds.total_daily_session_duration_soft_intervention;
            }
        }

        /// <summary>
        /// If avg_session_length * extended_session_time_hard_intervention < min_session_time_to_trigger_hard_intervention 
        ///     then min_session_time_to_trigger_hard_intervention, 
        /// if avg_session_length* extended_session_time_hard_intervention > max_extended session_time_hard_intervention 
        ///     then max_extended session_time_hard_intervention 
        /// else avg_session_length* extended_session_time_hard_intervention
        /// </summary>
		[BsonElement]
        public int hard_session_time_threshold
        {
            get
            {
                if (AvgSessionLength * Player.InterventionThresholds.extended_session_time_hard_intervention
                        < Player.InterventionThresholds.min_session_time_to_trigger_hard_intervention)
                    return Player.InterventionThresholds.min_session_time_to_trigger_hard_intervention;

                if (AvgSessionLength * Player.InterventionThresholds.extended_session_time_hard_intervention
                        > Player.InterventionThresholds.max_extended_session_time_hard_intervention)
                    return Player.InterventionThresholds.max_extended_session_time_hard_intervention;

                return (int) AvgSessionLength * Player.InterventionThresholds.extended_session_time_hard_intervention;
            }
        }

        /// <summary>
        /// If avg_ggr_per_session * heavy_loss_session_hard_intervention < min_heavy_loss_session_hard_intervention
        ///     then min_heavy_loss_session_hard_intervention,
        ///     else if avg_ggr_per_session* heavy_loss_session_hard_intervention > max_heavy_loss_session_hard_intervention then max_heavy_loss_session_hard_intervention 
        ///         else avg_ggr_per_session* extended_session_time_hard_intervention"																														
        /// </summary>
		[BsonElement]
        public decimal hard_session_heavy_loss_threshold
        {
            get
            {
                if (this.AvgGGRPerSession * this.Player.InterventionThresholds.heavy_loss_session_hard_intervention < this.Player.InterventionThresholds.min_heavy_loss_session_hard_intervention)
                {
                    return this.Player.InterventionThresholds.min_heavy_loss_session_hard_intervention;
                }
                if (this.AvgGGRPerSession * this.Player.InterventionThresholds.heavy_loss_session_hard_intervention > this.Player.InterventionThresholds.max_heavy_loss_session_hard_intervention)
                    return this.Player.InterventionThresholds.max_heavy_loss_session_hard_intervention;
                return this.AvgGGRPerSession * this.Player.InterventionThresholds.extended_session_time_hard_intervention;
            }
        }

        /// <summary>
        /// if avg_ggr_per_session * average_sessions_per_day * daily_losses_hard_intervention < mn_dly_lss_hrd
        ///     then mn_dly_lss_hrd
        /// else avg_ggr_per_session* average_sessions_per_day * daily_losses_hard_intervention
        /// </summary>
		[BsonElement]
        public decimal hard_daily_loss_threshold
        {            
            get
            {
                var calc = this.AvgGGRPerSession
                            * (decimal) this.AvgSessionsPerDay
                            * (decimal) this.Player.InterventionThresholds.daily_losses_hard_intervention;

                if (calc < Player.InterventionThresholds.min_daily_losses_hard_intervention)
                    return Player.InterventionThresholds.min_daily_losses_hard_intervention;

                return calc;
            } 
        }

        /// <summary>
        /// total_daily_session_duration_hard_intervention
        /// </summary>
		[BsonElement]
        public int hard_daily_session_time_threshold
        {
            get
            {
                return Player.InterventionThresholds.total_daily_session_duration_hard_intervention;
            }
        }

        /// <summary>
        /// total_life_time_intervention
        /// </summary>
		[BsonElement]
        public int hard_total_life_time_interventions_threshold
        {
            get
            {
                return Player.InterventionThresholds.total_life_time_interventions;
            }
        }

        #endregion

        public bool CheckForNewDay(WagerResultTransaction transAction)
        {
            bool result = false;

            if (this.CurrentDay < transAction.Timestamp.Day)
            {
                this.CurrentDay = transAction.Timestamp.Day;
                this.TotalLostToday = 0;
                this.TotalWiinToday = 0;
                result = true;
            }

            if (this.CurrentMin != transAction.Timestamp.Minute)
            {
                this.CurrentMin = transAction.Timestamp.Minute;
                this.WagersPlacedPerMinute = 0;
                result = true;
            }

            return result;
        }

    }
}
