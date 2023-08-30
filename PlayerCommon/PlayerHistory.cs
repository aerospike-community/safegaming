using System;
using System.Collections.Generic;
using System.Linq;

#if MONGODB
using MongoDB.Bson.Serialization.Attributes;
#else
using PlayerCommonDummy;
#endif

namespace PlayerCommon
{
    public sealed  partial class PlayerHistory
    {
        [BsonConstructor]
        public PlayerHistory(int playerId, 
                                List<long> wagerIds,
                                string state,
                                string county)
        {
            PlayerId = playerId;
            WagerIds = wagerIds ?? new List<long>();
            State = state;
            County = county;
        }

        [BsonElement]
        public string Tag { get; } = "PlayerHistory";

        [BsonId]
        [BsonElement]
        public int PlayerId { get; }

		[BsonElement]
        public string State { get; }

		[BsonElement]
        public string County { get; }

		[BsonElement]
        public List<long> WagerIds { get; }
    }
}
