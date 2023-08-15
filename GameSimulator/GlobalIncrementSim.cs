using Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerCommon
{
    public partial struct GlobalIncrement
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
                                            IDBConnectionSim dBConnection,
                                            ConcurrentBag<Task> livefireforgetCollection,
                                            System.Threading.CancellationToken token)
        {
            var intervalTS = wagerTrans.Timestamp.Round(incrementInterval, MidpointRounding.ToZero);
            var glbKey = GlobalIncrement.GenerateKey(player.State,
                                                        player.CountyFIPSCode,
                                                        intervalTS);

            var giInstance = new GlobalIncrement(player,
                                                    wagerTrans,
                                                    intervalTS,
                                                    (long)incrementInterval.TotalSeconds,
                                                    glbKey);

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
