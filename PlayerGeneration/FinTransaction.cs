using System;
using System.Collections.Generic;
using System.Text;

#if MONGODB
using MongoDB.Bson.Serialization.Attributes;
#endif

namespace PlayerGeneration
{
    public sealed class FinTransaction
    {
        public enum Types
        {
            Deposit,
            Withdraw
        }

        public FinTransaction(Types type, decimal amount, DateTimeOffset timeStamp)
        {
            TimeStamp = timeStamp;
            Type = type;
            Amount = amount;
        }


		[BsonConstructor]
        public FinTransaction(DateTimeOffset timeStamp, 
                                Types type, 
                                decimal amount, 
                                decimal resultingBalance)
        {
            //Tag = tag;
            TimeStamp = timeStamp;
            Type = type;
            Amount = amount;
            ResultingBalance = resultingBalance;
        }


		[BsonIgnore]
        public string Tag { get; } = "FinTransaction";

		[BsonElement]
        public DateTimeOffset TimeStamp { get; }

		[BsonElement]
        public Types Type { get; }

		[BsonElement]
        public decimal Amount { get; }

		[BsonElement]
        public decimal ResultingBalance { get; internal set; }
    }
}
