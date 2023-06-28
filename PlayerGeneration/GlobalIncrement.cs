using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Common;

#if MONGODB
using MongoDB.Bson.Serialization.Attributes;
#endif

namespace PlayerGeneration
{
    public struct GlobalIncrement
    {
        
        public GlobalIncrement(Player player, 
                                WagerResultTransaction wagerTranx,
                                DateTimeOffset intervalTimeStamp,
                                long intervalUsed,
                                string key = null)
        {
            this.IntervalTimeStamp = intervalTimeStamp;
            this.IntervalUsed = intervalUsed;
            this.State = this.StateName = player.State;
            this.County = player.County;
            this.CountyCode = player.CountyFIPSCode;
            this.GGR = wagerTranx.GGRAmount;
            this.Interventions = wagerTranx.Intervention ? 1 : 0;
            this.Transactions = 1;

            this.Key = key ?? GenerateKey(State, CountyCode, IntervalTimeStamp);
        }
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

        [JsonProperty("process_time")]
        [BsonElement("process_time")]
        public DateTimeOffset IntervalTimeStamp { get; }

        [JsonProperty("state_code")]
        [BsonElement("state_code")]
        public string State { get; }

        [JsonProperty("county_code")]
        [BsonElement("county_code")]
        public int CountyCode { get; }

        [JsonProperty("county_name")]
        [BsonElement("county_name")]
        public string County { get; }

        [JsonProperty("state_name")]
        [BsonElement("state_name")]
        public string StateName { get; }

        [JsonProperty("ggr_amount")]
        [BsonElement("ggr_amount")]
        public decimal GGR { get; set; }

        [JsonProperty("interventions")]
        [BsonElement("interventions")]
        public long Interventions { get; set; }

        [JsonProperty("trn_count")]
        [BsonElement("trn_count")]
        public long Transactions { get; set; }

        [BsonElement]
        public long IntervalUsed { get; }
        
        public GlobalIncrement Increment(WagerResultTransaction wagerTransaction)
        {
            this.GGR += wagerTransaction.GGRAmount;
            if (wagerTransaction.Intervention)
                ++this.Interventions;
            ++this.Transactions;

            return this;
        }
        
        public static string GenerateKey(string state,
                                            int countyCode,
                                            DateTimeOffset timestamp)
        {
            return $"{state}|{countyCode}|{timestamp.ToString(Settings.Instance.TimeStampFormatString)}";            
        }

        public async static Task AddUpdate(Player player,
                                            WagerResultTransaction wagerTrans,
                                            TimeSpan incrementInterval,
                                            IDBConnection dBConnection,
                                            System.Threading.CancellationToken token)
        {
            var intervalTS = wagerTrans.Timestamp.Round(incrementInterval, MidpointRounding.ToZero);
            var glbKey = GlobalIncrement.GenerateKey(player.State,
                                                        player.CountyFIPSCode,
                                                        intervalTS);

            await dBConnection.IncrementGlobalSet(new GlobalIncrement(player,
                                                                        wagerTrans,
                                                                        intervalTS,
                                                                        (long) incrementInterval.TotalSeconds,
                                                                        glbKey),                                               
                                                token);

            //GlobalIncrementTasks.AddOrUpdate(glbKey,
            //                                new GlobalIncrement(player, wagerTrans, intervalTS, glbKey),
            //                                (key, instance) => instance.Increment(wagerTrans));
        }
       
    }
}
