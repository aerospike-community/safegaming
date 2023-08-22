using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;

#if MONGODB
using MongoDB.Bson.Serialization.Attributes;
#else
using PlayerCommonDummy;
#endif

namespace PlayerCommon
{
    public partial class Intervention
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
                return $"{PlayerId}:{InterventionTimeStamp.UtcDateTime.ToString(Settings.Instance.TimeZoneFormatWoZone)}:{Type}";
            }
        }

		[BsonElement]
        public int PlayerId { get; }

        [JsonPropertyName("clv")]
        [BsonElement("clv")]
        public decimal CLV { get; }

        [JsonPropertyName("session_start")]
        [BsonElement("session_start")]
        public DateTimeOffset SessionTimestamp { get; }

        [JsonPropertyName("interv_time")]
        [BsonElement("interv_time")]
        public DateTimeOffset InterventionTimeStamp { get; }

        [JsonPropertyName("interv_unixts")]
        [BsonElement("interv_unixts")]
        public long InterventionTimeStampUnixSecs { get => this.InterventionTimeStamp.ToUnixTimeSeconds(); }

        /// <summary>
        /// HARD/SOFT
        /// </summary>
        [JsonPropertyName("interv_type")]
        [BsonElement("interv_type")]
        public string Type { get; }

        /// <summary>
        /// "Time Spent Model", "Unusual Session", "Self Hard Model RED"
        /// </summary>
        [JsonPropertyName("interv_model")]
        [BsonElement("interv_model")]
        public string Model { get; }

        /// <summary>
        /// "Extended Session Time", "Unusual Staking", "Heavy Session Losses", "Extended Daily Gambling Time", "Heavy Daily Losses"
        /// </summary>
        [JsonPropertyName("interv_reason")]
        [BsonElement("interv_reason")]
        public string Reason { get; }

        [JsonPropertyName("session_ggr")]
        [BsonElement("session_ggr")]
        public decimal GGR { get; }

        [JsonPropertyName("game_name")]
        [BsonElement("game_name")]
        public string Game { get; }

        [JsonPropertyName("game_type")]
        [BsonElement("game_type")]
        public string GameType { get; }

        [JsonPropertyName("county_name")]
        [BsonElement("county_name")]
        public string County { get; }

        [JsonPropertyName("country_code")]
        [BsonElement("country_code")]
        public string CouintyCode { get; }

        [JsonPropertyName("fips_code")]
        [BsonElement("fips_code")]
        public int CountyFIPSCode { get; }

        [JsonPropertyName("state_code")]
        [BsonElement("state_code")]
        public string State { get; }

        [JsonPropertyName("state_name")]
        [BsonElement("state_name")]
        public string StateName { get; }

		[BsonElement]
        public long TransId { get; }

    }
}
