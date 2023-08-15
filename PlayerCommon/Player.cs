using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    public enum Tiers
    {
        None = 0,
        Low = 4,
        Medium = 3,
        High = 2,
        VeryHigh = 1
    }

    public sealed partial class Player
    {

        public static volatile int CurrentPlayerId = 0;

		[BsonConstructor]
        public Player(int playerId, 
                        string userName, 
                        string firstName, 
                        string lastName, 
                        string emailAddress, 
                        string countryCode, 
                        string state, 
                        string county, 
                        int countyFIPSCode, 
                        Tiers valueTier, 
                        int initialTenure, 
                        Metrics metrics, 
                        Session session, 
                        Game game, 
                        bool activeSession, 
                        bool bingeFlag, 
                        bool archived,                         
                        List<FinTransaction> finTransactions, 
                        List<WagerResultTransaction> wagersResults)
        {
            //UseTime = useTime;
            //NbrSessionsToday = nbrSessionsToday;
            //InterventionThresholds = interventionThresholds;
            //BingeTransCnt = bingeTransCnt;
            //Tag = tag;
            PlayerId = playerId;
            UserName = userName;
            FirstName = firstName;
            LastName = lastName;
            EmailAddress = emailAddress;
            CountryCode = countryCode;
            State = state;
            County = county;
            CountyFIPSCode = countyFIPSCode;
            ValueTier = valueTier;
            InitialTenure = initialTenure;
            Metrics = metrics;
            Session = session;
            Game = game;
            ActiveSession = activeSession;
            BingeFlag = bingeFlag;
            Archived = archived;
           // History = history;
            FinTransactions = finTransactions;
            WagersResults = wagersResults;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="clonePlayer"></param>
        public Player(Player clonePlayer, bool archeived = true, bool onlyCurrentTrans = true)
        {
            PlayerId = clonePlayer.PlayerId;
            UserName = clonePlayer.UserName;
            FirstName = clonePlayer.FirstName;
            LastName = clonePlayer.LastName;
            EmailAddress = clonePlayer.EmailAddress;
            CountryCode = clonePlayer.CountryCode;
            State = clonePlayer.State;
            County = clonePlayer.County;
            ValueTier = clonePlayer.ValueTier;
            CountyFIPSCode = clonePlayer.CountyFIPSCode;
            InitialTenure = clonePlayer.InitialTenure;
            Metrics = new Metrics(clonePlayer.Metrics);
            Session = new Session(clonePlayer.Session);
            Game = new Game(clonePlayer.Game);
            UseTime = clonePlayer.UseTime;
            Archived = archeived;
            FinTransactions = new List<FinTransaction>(clonePlayer.FinTransactions);
            BingeFlag = clonePlayer.BingeFlag;

            if (onlyCurrentTrans)
            {
                WagersResults = new List<WagerResultTransaction>(
                                    clonePlayer.WagersResults.Skip(clonePlayer.WagersResults.Count - 2));
            }
            else
                WagersResults = new List<WagerResultTransaction>(clonePlayer.WagersResults);
        }

        [JsonIgnore]
        [BsonIgnore]
        internal DateTimeSimulation UseTime { get; }

        [JsonIgnore]
		[BsonIgnore]
        internal int NbrSessionsToday { get; private set; }

        [JsonIgnore]
		[BsonIgnore]
        internal InterventionThresholds InterventionThresholds { get; private set; }
        [JsonIgnore]
		[BsonIgnore]
        internal int BingeTransCnt { get; }

		[BsonIgnore]
        public string Tag { get; } = "Player";

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

		[BsonElement]
        public Tiers ValueTier { get; }

		[BsonElement]
        public int InitialTenure { get; }

		[BsonElement]
        public Metrics Metrics { get; }

		[BsonElement]
        public Session Session { get; private set; }

		[BsonElement]
        public Game Game { get; private set; }

		[BsonElement]
        public bool ActiveSession { get; private set; }

		[BsonElement]
        public bool BingeFlag { get; set; }

		[BsonElement]
        public bool Archived { get; private set; }

        [JsonIgnore]
		[BsonIgnore]
        public List<Player> History { get; }

		[BsonElement]
        public List<FinTransaction> FinTransactions { get; }

		[BsonElement]
        public List<WagerResultTransaction> WagersResults { get; }

    }
}
