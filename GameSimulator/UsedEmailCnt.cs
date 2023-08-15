using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#if MONGODB
using MongoDB.Bson.Serialization.Attributes;
#else
using PlayerCommonDummy;
#endif

namespace GameSimulator
{
    public sealed class UsedEmailCnt
    {
        public UsedEmailCnt(string firstName, string lastName, string domain)
        {
            this.EMail = $"{firstName}.{lastName}@{domain}";
        }

        [BsonConstructor]
        public UsedEmailCnt(string eMail,
                            int count)
        {
            EMail = eMail;
            Count = count;
        }

        [BsonElement]
        [BsonId]
        public string EMail { get; }

        [BsonElement]
        public int Count { get; } = 0;
    }
}
