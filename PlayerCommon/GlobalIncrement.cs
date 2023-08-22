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
    public partial struct GlobalIncrement
    {
        [BsonConstructor]
        public GlobalIncrement(string key, 
                                DateTimeOffset intervalTimeStamp, 
                                string state, 
                                int countyCode, 
                                string county, 
                                string stateName, 
                                decimal gGR, 
                                long interventions, 
                                long transactions, 
                                long intervalUsed)
        {
            Key = key;
            IntervalTimeStamp = intervalTimeStamp;
            State = state;
            CountyCode = countyCode;
            County = county;
            StateName = stateName;
            GGR = gGR;
            Interventions = interventions;
            Transactions = transactions;
            IntervalUsed = intervalUsed;
        }

        [JsonIgnore]
		[BsonId]
        [BsonElement]
        public string Key
        {
            get;
        }

        [JsonPropertyName("process_time")]
        [BsonElement("process_time")]
        public DateTimeOffset IntervalTimeStamp { get; }

        [JsonPropertyName("process_unixts")]
        [BsonElement("process_unixts")]
        public long IntervalUnixSecs{ get => this.IntervalTimeStamp.ToUnixTimeSeconds(); }

        [JsonPropertyName("state_code")]
        [BsonElement("state_code")]
        public string State { get; }

        [JsonPropertyName("county_code")]
        [BsonElement("county_code")]
        public int CountyCode { get; }

        [JsonPropertyName("county_name")]
        [BsonElement("county_name")]
        public string County { get; }

        [JsonPropertyName("state_name")]
        [BsonElement("state_name")]
        public string StateName { get; }

        [JsonPropertyName("ggr_amount")]
        [BsonElement("ggr_amount")]
        public decimal GGR { get; set; }

        [JsonPropertyName("interventions")]
        [BsonElement("interventions")]
        public long Interventions { get; set; }

        [JsonPropertyName("trn_count")]
        [BsonElement("trn_count")]
        public long Transactions { get; set; }

        [BsonElement]
        public long IntervalUsed { get; }
                
    }
}
