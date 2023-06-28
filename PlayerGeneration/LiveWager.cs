using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

#if MONGODB
using MongoDB.Bson.Serialization.Attributes;
#endif

namespace PlayerGeneration
{
    public sealed class LiveWager
    {
        public LiveWager(Player player,
                            WagerResultTransaction wagerResult,
                            WagerResultTransaction wager,
                            string timeZoneFormatWoZone)
        {
            this.Id = Helpers.GetLongHash(Thread.CurrentThread.ManagedThreadId);
                       
            var tsWoZone = wagerResult.Timestamp.UtcDateTime.ToString(timeZoneFormatWoZone);

            this.Aggkey = $"{player.PlayerId}:{tsWoZone}:{wager.Amount}";
            this.bet_type = wagerResult.BetType;
            this.PlayerId = player.PlayerId;
            this.result_type = wagerResult.Type.ToString();
            this.risk_score = wagerResult.RiskScore;
            this.stake_amount = wager.Amount;
            this.txn_ts = wagerResult.Timestamp;
            this.win_amount = wagerResult.Type == WagerResultTransaction.Types.Win
                                ? wagerResult.Amount
                                : 0;
            this.TransId = wagerResult.Id;
        }


		[BsonConstructor]
        public LiveWager(long id, 
                            string aggkey, 
                            string bet_type, 
                            int playerId, 
                            string result_type, 
                            int risk_score, 
                            decimal stake_amount, 
                            DateTimeOffset txn_ts, 
                            decimal win_amount, 
                            long transId)
        {
            Id = id;
            Aggkey = aggkey;
            this.bet_type = bet_type;
            PlayerId = playerId;
            this.result_type = result_type;
            this.risk_score = risk_score;
            this.stake_amount = stake_amount;
            this.txn_ts = txn_ts;
            this.win_amount = win_amount;
            TransId = transId;
        }

#pragma warning disable IDE1006 // Naming Styles

        [BsonId]
		[BsonElement]
        public long Id { get; }

		[BsonElement]
        public string Aggkey { get; }

		[BsonElement]
        public string bet_type { get; }

		[BsonElement]
        public int PlayerId { get; }

		[BsonElement]
        public string result_type { get; }

		[BsonElement]
        public int risk_score { get; }

		[BsonElement]
        public decimal stake_amount { get; }

		[BsonElement]
        public DateTimeOffset txn_ts { get; }

		[BsonElement]
        public decimal win_amount { get; }

		[BsonElement]
        public long TransId { get; }

#pragma warning restore IDE1006 // Naming Styles
    }
}
