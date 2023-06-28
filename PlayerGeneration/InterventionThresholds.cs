using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;
using Newtonsoft.Json;
using System.Threading;

#if MONGODB
using MongoDB.Bson.Serialization.Attributes;
#endif

namespace PlayerGeneration
{
    public sealed partial class InterventionThresholds
    {

        internal static volatile InterventionThresholds Instance = null;
        private static volatile int Updating = 0;
        private static long UpdateCnt = 0;
        
        private InterventionThresholds()
        {
            NextRefreshTime = DateTime.Now + Settings.Instance.InterventionThresholdsRefreshRate;
        }

		[BsonConstructor]
        public InterventionThresholds(int version,
                                        int min_session_time_to_trigger_soft_intervention,
                                        int min_session_time_to_trigger_hard_intervention, 
                                        int extended_session_time_soft_intervention, 
                                        int extended_session_time_hard_intervention, 
                                        int max_extended_session_time_soft_intervention, 
                                        int max_extended_session_time_hard_intervention, 
                                        decimal min_heavy_loss_session_soft_intervention, 
                                        decimal min_heavy_loss_session_hard_intervention, 
                                        int heavy_loss_session_soft_intervention, 
                                        int heavy_loss_session_hard_intervention, 
                                        decimal max_heavy_loss_session_soft_intervention, 
                                        decimal max_heavy_loss_session_hard_intervention, 
                                        decimal min_daily_losses_soft_intervention, 
                                        int daily_losses_soft_intervention, 
                                        decimal min_daily_losses_hard_intervention, 
                                        int daily_losses_hard_intervention, 
                                        decimal risky_staking_soft_interaction_min_threshold, 
                                        int risky_staking_soft_interaction_avg_stake_multiplier, 
                                        int total_daily_session_duration_soft_intervention, 
                                        int total_daily_session_duration_hard_intervention, 
                                        int total_life_time_interventions)
        {
            Version = version;
            //RefreshedTime = refreshedTime;
            //NextRefreshTime = nextRefreshTime;
            this.min_session_time_to_trigger_soft_intervention = min_session_time_to_trigger_soft_intervention;
            this.min_session_time_to_trigger_hard_intervention = min_session_time_to_trigger_hard_intervention;
            this.extended_session_time_soft_intervention = extended_session_time_soft_intervention;
            this.extended_session_time_hard_intervention = extended_session_time_hard_intervention;
            this.max_extended_session_time_soft_intervention = max_extended_session_time_soft_intervention;
            this.max_extended_session_time_hard_intervention = max_extended_session_time_hard_intervention;
            this.min_heavy_loss_session_soft_intervention = min_heavy_loss_session_soft_intervention;
            this.min_heavy_loss_session_hard_intervention = min_heavy_loss_session_hard_intervention;
            this.heavy_loss_session_soft_intervention = heavy_loss_session_soft_intervention;
            this.heavy_loss_session_hard_intervention = heavy_loss_session_hard_intervention;
            this.max_heavy_loss_session_soft_intervention = max_heavy_loss_session_soft_intervention;
            this.max_heavy_loss_session_hard_intervention = max_heavy_loss_session_hard_intervention;
            this.min_daily_losses_soft_intervention = min_daily_losses_soft_intervention;
            this.daily_losses_soft_intervention = daily_losses_soft_intervention;
            this.min_daily_losses_hard_intervention = min_daily_losses_hard_intervention;
            this.daily_losses_hard_intervention = daily_losses_hard_intervention;
            this.risky_staking_soft_interaction_min_threshold = risky_staking_soft_interaction_min_threshold;
            this.risky_staking_soft_interaction_avg_stake_multiplier = risky_staking_soft_interaction_avg_stake_multiplier;
            this.total_daily_session_duration_soft_intervention = total_daily_session_duration_soft_intervention;
            this.total_daily_session_duration_hard_intervention = total_daily_session_duration_hard_intervention;
            this.total_life_time_interventions = total_life_time_interventions;
        }

		[BsonId]
		[BsonElement]
        public int Version { get; set; }

        [JsonIgnore]
		[BsonIgnore]
        public DateTime RefreshedTime { get; } = DateTime.Now;

        [JsonIgnore]
		[BsonIgnore]
        public DateTime NextRefreshTime { get; }

#pragma warning disable IDE1006 // Naming Styles

        [JsonProperty("mn_ss_tm_sft")]
        [BsonElement("mn_ss_tm_sft")]
        public int min_session_time_to_trigger_soft_intervention { get; set; } = 60;

        [JsonProperty("mn_ss_tm_hrd")]
        [BsonElement("mn_ss_tm_hrd")]
        public int min_session_time_to_trigger_hard_intervention { get; set; } = 120;
        [JsonProperty("xt_ss_tm_sft")]
        [BsonElement("xt_ss_tm_sft")]
        public int extended_session_time_soft_intervention { get; set; } = 2;
        [JsonProperty("xt_ss_tm_hrd")]
        [BsonElement("xt_ss_tm_hrd")]
        public int extended_session_time_hard_intervention { get; set; } = 3;
        [JsonProperty("mx_xt_ss_sft")]
        [BsonElement("mx_xt_ss_sft")]
        public int max_extended_session_time_soft_intervention { get; set; } =  120;
        [JsonProperty("mx_xt_ss_hrd")]
        [BsonElement("mx_xt_ss_hrd")]
        public int max_extended_session_time_hard_intervention { get; set; } = 300;
        
        [JsonProperty("mn_lss_ss_sft")]
        [BsonElement("mn_lss_ss_sft")]
        public decimal min_heavy_loss_session_soft_intervention { get; set; } = 100M;
        [JsonProperty("mn_lss_ss_hrd")]
        [BsonElement("mn_lss_ss_hrd")]
        public decimal min_heavy_loss_session_hard_intervention { get; set; } = 250M;
        [JsonProperty("hv_lss_ss_sft")]
        [BsonElement("hv_lss_ss_sft")]
        public int heavy_loss_session_soft_intervention { get; set; } = 2;
        [JsonProperty("hv_lss_ss_hrd")]
        [BsonElement("hv_lss_ss_hrd")]
        public int heavy_loss_session_hard_intervention { get; set; } = 3;
        [JsonProperty("mx_lss_ss_sft")]
        [BsonElement("mx_lss_ss_sft")]
        public decimal max_heavy_loss_session_soft_intervention { get; set; } = 5000M;
        [JsonProperty("mx_lss_ss_hrd")]
        [BsonElement("mx_lss_ss_hrd")]
        public decimal max_heavy_loss_session_hard_intervention { get; set; } = 7500M;

        [JsonProperty("mn_dly_lss_sft")]
        [BsonElement("mn_dly_lss_sft")]
        public decimal min_daily_losses_soft_intervention { get; set; } = 200M;
        [JsonProperty("dly_lss_sft")]
        [BsonElement("dly_lss_sft")]
        public int daily_losses_soft_intervention { get; set; } = 2;
        [JsonProperty("mn_dly_lss_hrd")]
        [BsonElement("mn_dly_lss_hrd")]
        public decimal min_daily_losses_hard_intervention { get; set; } = 500M;
        [JsonProperty("dly_lss_hrd")]
        [BsonElement("dly_lss_hrd")]
        public int daily_losses_hard_intervention { get; set; } = 3;
        
        [JsonProperty("rsk_stk_sft_mn")]
        [BsonElement("rsk_stk_sft_mn")]
        public decimal risky_staking_soft_interaction_min_threshold { get; set; } = 10M;        
        [JsonProperty("rsk_srk_sft_av")]
        [BsonElement("rsk_srk_sft_av")]
        public int risky_staking_soft_interaction_avg_stake_multiplier { get; set; } = 2;
        
        [JsonProperty("dly_ss_dur_sft")]
        [BsonElement("dly_ss_dur_sft")]
        public int total_daily_session_duration_soft_intervention { get; set; } = 240;
        [JsonProperty("dly_ss_dur_hrd")]
        [BsonElement("dly_ss_dur_hrd")]
        public int total_daily_session_duration_hard_intervention { get; set; } = 420;
        
        [JsonProperty("tot_life_itvs")]
        [BsonElement("tot_life_itvs")]
        public int total_life_time_interventions { get; set; } = 3;

#pragma warning restore IDE1006 // Naming Styles

        /// <summary>
        /// Should only be called once per process!!!
        /// </summary>
        /// <param name="dBConnection"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public static async Task<bool> Initialize(IDBConnection dBConnection, CancellationToken token)
        {
            await RefreshCheck(dBConnection, token, true);

            if (Instance == null)
            {
                Logger.Instance.Warn("InterventionThresholds.Initialize failed to Create an Instance from DB! Using default instance.");
                Instance = new InterventionThresholds();
                return false;
            }

            return true;
        }
    }
}
