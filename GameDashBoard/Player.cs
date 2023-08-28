using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerCommon
{
    public sealed partial class Player
    {
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

        public int PlayerId { get; }
        
        public string UserName { get; }

        public string FirstName { get; }

        public string LastName { get; }

        public string EmailAddress { get; }

        public string CountryCode { get; }

        public string State { get; }

        public string County { get; }

        public int CountyFIPSCode { get; }

    }
}
