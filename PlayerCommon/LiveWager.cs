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
    public sealed partial class LiveWager
    {
        
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
        public long txn_unixts { get => this.txn_ts.ToUnixTimeSeconds(); }

        [BsonElement]
        public decimal win_amount { get; }

		[BsonElement]
        public long TransId { get; }

#pragma warning restore IDE1006 // Naming Styles
    }
}
