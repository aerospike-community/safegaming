using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if MONGODB
using MongoDB.Bson.Serialization.Attributes;
#else
using PlayerCommonDummy;
#endif

namespace PlayerCommon
{
    [BsonIgnoreExtraElements]
    public sealed partial class Player
    {
        [BsonConstructor]
        public Player(int playerId,
                        string userName,
                        string firstName,
                        string lastName,
                        string emailAddress,
                        string countryCode,
                        string state,
                        string county,
                        int countyFIPSCode)
        {
            PlayerId = playerId;
            UserName = userName;
            FirstName = firstName;
            LastName = lastName;
            EmailAddress = emailAddress;
            CountryCode = countryCode;
            State = state;
            County = county;
            CountyFIPSCode = countyFIPSCode;
        }

        [BsonId]
        [BsonElement]
        public int PlayerId { get; }

        [BsonElement]
        public string UserName { get; }

        [BsonElement]
        public string FirstName { get; }

        [BsonElement]
        public string LastName { get; }

        [BsonElement]
        public string EmailAddress { get; }

        [BsonElement]
        public string CountryCode { get; }

        [BsonElement]
        public string State { get; }

        [BsonElement]
        public string County { get; }

        [BsonElement]
        public int CountyFIPSCode { get; }

    }
}
