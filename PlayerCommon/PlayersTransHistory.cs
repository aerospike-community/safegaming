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
    public sealed partial class PlayersTransHistory
    {
        public PlayersTransHistory(Player player)
        {
            this.Player = player;
            this.WagerId = player.WagersResults.Last().Id;
            this.PlayerId = player.PlayerId;
            this.SessionId = player.Session.Id;
        }


		[BsonConstructor]
        public PlayersTransHistory(long wagerId, 
                                    int playerId, 
                                    long sessionId, 
                                    Player player)
        {
            WagerId = wagerId;
            PlayerId = playerId;
            SessionId = sessionId;
            Player = player;
        }


		[BsonId]
		[BsonElement]        
        public long WagerId { get; }

		[BsonElement]
        public int PlayerId { get; }

		[BsonElement]
        public long SessionId { get; }

		[BsonElement]
        public Player Player { get; }
    }
}
