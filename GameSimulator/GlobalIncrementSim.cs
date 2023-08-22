using Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameSimulator;

namespace PlayerCommon
{
    public partial struct GlobalIncrement
    {

        public GlobalIncrement(Player player,
                                WagerResultTransaction wagerTranx,
                                DateTimeOffset intervalTimeStamp,
                                TimeSpan intervalUsed,
                                string tzFormatWoZone,
                                string key = null)
        {
            this.IntervalTimeStamp = intervalTimeStamp;
            this.IntervalUsed = (long) intervalUsed.TotalSeconds;
            this.State = this.StateName = player.State;
            this.County = player.County;
            this.CountyCode = player.CountyFIPSCode;
            this.GGR = wagerTranx.GGRAmount;
            this.Interventions = wagerTranx.Intervention ? 1 : 0;
            this.Transactions = 1;

            this.Key = key ?? GenerateKey(State,
                                            CountyCode,
                                            IntervalTimeStamp,
                                            intervalUsed,
                                            tzFormatWoZone);
        }
        
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
                                            DateTimeOffset timestamp,
                                            TimeSpan incrementInterval,
                                            string tzFormatWoZone)
        {
            var ts = timestamp.UtcDateTime
                                .Round(incrementInterval, MidpointRounding.ToZero)
                                .ToString(tzFormatWoZone);

            return $"{state}|{countyCode}|{ts}";
        }

        public async static Task AddUpdate(Player player,
                                            WagerResultTransaction wagerTrans,
                                            TimeSpan incrementInterval,
                                            string tzFormatWoZone,
                                            IDBConnectionSim dBConnection,
                                            ConcurrentBag<Task> livefireforgetCollection,                                            
                                            System.Threading.CancellationToken token)
        {            
            var giInstance = new GlobalIncrement(player,
                                                    wagerTrans,
                                                    wagerTrans.Timestamp,
                                                    incrementInterval,
                                                    tzFormatWoZone);

            if (livefireforgetCollection is null)
            {
                await dBConnection.IncrementGlobalSet(giInstance, token);
            }
            else
            {
                livefireforgetCollection.Add(dBConnection.IncrementGlobalSet(giInstance, token));
            }

            //GlobalIncrementTasks.AddOrUpdate(glbKey,
            //                                new GlobalIncrement(player, wagerTrans, intervalTS, glbKey),
            //                                (key, instance) => instance.Increment(wagerTrans));
        }

    }
}
