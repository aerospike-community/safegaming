using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

#if MONGODB
using MongoDB.Bson.Serialization.Attributes;
#endif

namespace PlayerGeneration
{
    public class Intervention
    {

		[BsonConstructor]
        public Intervention(int playerId, 
                                decimal cLV,
                                DateTimeOffset sessionTimestamp,
                                DateTimeOffset interventionTimeStamp,
                                string type,
                                string model,
                                string reason,
                                decimal gGR,
                                string game,
                                string gameType,
                                string county,
                                string couintyCode,
                                int countyFIPSCode,
                                string state,
                                string stateName,
                                long transId)
        {
            PrimaryKey = Helpers.GetLongHash(playerId);
            PlayerId = playerId;
            CLV = cLV;
            SessionTimestamp = sessionTimestamp;
            InterventionTimeStamp = interventionTimeStamp;
            Type = type;
            Model = model;
            Reason = reason;
            GGR = gGR;
            Game = game;
            GameType = gameType;
            County = county;
            CouintyCode = couintyCode;
            CountyFIPSCode = countyFIPSCode;
            State = state;
            StateName = stateName;
            TransId = transId;
        }

        [JsonIgnore]
		[BsonId]
		[BsonElement]
        public long PrimaryKey { get; }

		[BsonElement]
        public string AggKey
        {
            get{
                return $"{PlayerId}:{InterventionTimeStamp.ToString(Settings.Instance.TimeStampFormatString)}:{Type}";
            }
        }

		[BsonElement]
        public int PlayerId { get; }

        [JsonProperty("clv")]
        [BsonElement("clv")]
        public decimal CLV { get; }

        [JsonProperty("session_start")]
        [BsonElement("session_start")]
        public DateTimeOffset SessionTimestamp { get; }

        [JsonProperty("interv_time")]
        [BsonElement("interv_time")]
        public DateTimeOffset InterventionTimeStamp { get; }

        /// <summary>
        /// HARD/SOFT
        /// </summary>
        [JsonProperty("interv_type")]
        [BsonElement("interv_type")]
        public string Type { get; }

        /// <summary>
        /// "Time Spent Model", "Unusual Session", "Self Hard Model RED"
        /// </summary>
        [JsonProperty("interv_model")]
        [BsonElement("interv_model")]
        public string Model { get; }

        /// <summary>
        /// "Extended Session Time", "Unusual Staking", "Heavy Session Losses", "Extended Daily Gambling Time", "Heavy Daily Losses"
        /// </summary>
        [JsonProperty("interv_reason")]
        [BsonElement("interv_reason")]
        public string Reason { get; }

        [JsonProperty("session_ggr")]
        [BsonElement("session_ggr")]
        public decimal GGR { get; }

        [JsonProperty("game_name")]
        [BsonElement("game_name")]
        public string Game { get; }

        [JsonProperty("game_type")]
        [BsonElement("game_type")]
        public string GameType { get; }

        [JsonProperty("county_name")]
        [BsonElement("county_name")]
        public string County { get; }

        [JsonProperty("country_code")]
        [BsonElement("country_code")]
        public string CouintyCode { get; }

        [JsonProperty("fips_code")]
        [BsonElement("fips_code")]
        public int CountyFIPSCode { get; }

        [JsonProperty("state_code")]
        [BsonElement("state_code")]
        public string State { get; }

        [JsonProperty("state_name")]
        [BsonElement("state_name")]
        public string StateName { get; }

		[BsonElement]
        public long TransId { get; }

        public static async Task Determine(Player player,
                                            WagerResultTransaction wagerTrans,
                                            IDBConnection dBConnection,
                                            CancellationToken token)
        {
            if(player.Session.GGR > player.Metrics.hard_session_heavy_loss_threshold)
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
            else if (player.Session.InterventionType == null &&  player.Session.GGR > player.Metrics.soft_session_heavy_loss_threshold)
            {
                player.UseTime.AddSec();

                wagerTrans.Intervention = true;
                player.Metrics.Interventions++;
                player.Session.InterventionType = "SOFT";

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
