using Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

#if MONGODB
using MongoDB.Bson.Serialization.Attributes;
#endif

namespace PlayerGeneration
{
    public enum Tiers
    {
        None = 0,
        Low = 4,
        Medium = 3,
        High = 2,
        VeryHigh = 1
    }

    public sealed class Player
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

        public Player(string userName,
                        string firstName,
                        string lastName,
                        string emailAddress,
                        string countryCode,
                        string state,
                        County county,
                        Tiers valueTier,
                        decimal currentBalance,
                        int initialTenure,
                        DateTimeSimulation dateTimeHistory,                       
                        InterventionThresholds interventionThresholds,
                        bool bingeFlag,
                        int? playerId = null)
        {
            PlayerId = playerId ?? Interlocked.Increment(ref CurrentPlayerId);
            UserName = userName;
            FirstName = firstName;
            LastName = lastName;
            EmailAddress = emailAddress;
            CountryCode = countryCode;
            State = state;
            County = county.Name;
            CountyFIPSCode = county.FIPSCode;
            ValueTier = valueTier;                        
            Session = null;
            Game = null;
            FinTransactions = new List<FinTransaction>();
            WagersResults = new List<WagerResultTransaction>();
            InitialTenure = initialTenure;
            UseTime = dateTimeHistory;
            InterventionThresholds = interventionThresholds;
            BingeFlag = bingeFlag;

            Metrics = new Metrics(this, currentBalance);
            History = new List<Player>();
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

        [Newtonsoft.Json.JsonIgnore]
		[BsonIgnore]
        internal DateTimeSimulation UseTime { get; }
        [Newtonsoft.Json.JsonIgnore]
		[BsonIgnore]
        internal int NbrSessionsToday { get; private set; }

        [Newtonsoft.Json.JsonIgnore]
		[BsonIgnore]
        internal InterventionThresholds InterventionThresholds { get; private set; }
        [Newtonsoft.Json.JsonIgnore]
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

        [Newtonsoft.Json.JsonIgnore]
		[BsonIgnore]
        public List<Player> History { get; }

		[BsonElement]
        public List<FinTransaction> FinTransactions { get; }

		[BsonElement]
        public List<WagerResultTransaction> WagersResults { get; }


        public decimal CalculateWager(Random random)
        {
            var wager = this.Session.OpeningStakeAmount;

            /*
                If Binge = 1 then double wager after wager >= wager_count_binge_threshold. Double wager every 5 - 20 bets (random selection) to the limit of 50.
                If last win_amount was 20x >= than wager then 50% chance of player doubling wager with a max of $50
                If cumulative_session_ggr >= 20x wager then 50% chance of player doubling wager with a max of $50
                If cumulative_session_ggr <= -20x wager then 10% chance of player doubling wager to a max of $50
             */

            if (this.BingeFlag)
            {
                if(wager >= this.Session.WagerCountBingeThreshold)
                {
                    wager *= 2;
                }
                else if(this.Session.Wagers % this.Session.WagerTransCountBingeThreshold == 0)
                {
                    wager *= 2;
                }
            }
            else
            {
                var lastWagerResultTrans = this.WagersResults.LastOrDefault();

                if (lastWagerResultTrans?.Type == WagerResultTransaction.Types.Win)
                {
                    var winAmt = lastWagerResultTrans.Amount;

                    if (winAmt * 20 >= wager)
                    {
                        var chance = random.Next(1, 100);

                        if (chance >= 50)
                            wager = Math.Min(wager * 2, 50);
                    }
                }
                else if (this.Session.GGR >= 20 * wager)
                {
                    var chance = random.Next(1, 100);

                    if (chance >= 50)
                        wager = Math.Min(wager * 2, 50);
                }
                else if (this.Session.GGR <= -20 * wager)
                {
                    var chance = random.Next(1, 100);

                    if (chance >= 10)
                        wager = Math.Min(wager * 2, 50);
                }
            }

            return decimal.Round(wager, 2);
        }

        public Player NewGame(Game newGame)
        {            
            this.Game = newGame;
            this.Session.GamesPlayed++;

            return this;
        }

        public Session CreateSession(bool newPlay = true, bool continuePlay = false)
        {
            if (this.Session != null)
            {
                if (this.ActiveSession)
                    this.CloseSession();
                else
                {
                    this.WagersResults.Clear();
                    this.FinTransactions.Clear();
                }
            }

            if (newPlay && !continuePlay)
                this.NbrSessionsToday = 1;
            else 
                this.NbrSessionsToday += 1;

            if (this.UseTime.Ishistoric)
            {
                if (this.NbrSessionsToday > this.Metrics.AvgSessionsPerDay)
                {
                    this.NbrSessionsToday = 1;
                    this.UseTime.AddDay();
                }
                else if (this.Session != null)
                {
                    var oldDay = this.UseTime.Current.Day;

                    this.UseTime.SessionIncrement();

                    if (oldDay < this.UseTime.Current.Day)
                    {
                        this.NbrSessionsToday = 1;
                    }
                }
            }
            else if(this.UseTime.IfNewDay())
            {
                this.NbrSessionsToday = 1;
            }

            this.Session = new Session(this.UseTime.Current, this);
            this.Session.StartingBalance = this.Metrics.CurrentBalance;
            this.ActiveSession = true;
            this.Metrics.CurrentDay = Session.StartTimeStamp.Day;

            return this.Session;
        }

        public Player CloseSession(bool clearWagersResults = true, bool skipSessionIncr = false)
        {
            if (this.Session != null)
            {
                if (this.Session.LossAmounts > this.Session.WinAmounts)
                    ++this.Metrics.NegativeSessions;
                else if (this.Session.WinAmounts > this.Session.LossAmounts)
                    ++this.Metrics.PositiveSessions;

                if(!skipSessionIncr)
                    UseTime.SessionIncrement();

                this.Session.EndingTimeStamp = UseTime.Current;
                this.Metrics.TotalSessionsTime += Session.SessionLength;
                this.ActiveSession = false;
                this.Session.Closed = true;

                if (clearWagersResults)
                {
                    this.WagersResults.Clear();
                    this.FinTransactions.Clear();
                }
            }

            return this;
        }

        public Player NewWagerResultTransaction(WagerResultTransaction newTrans)
        {
            this.Metrics.CheckForNewDay(newTrans);

            if (newTrans.Type == WagerResultTransaction.Types.Wager)
            {                
                if(this.WagersResults.Count >= Settings.Instance.KeepNbrWagerResultTransActions)
                {
                    this.WagersResults.RemoveAt(0);
                    this.WagersResults.RemoveAt(0);                    
                }
                
                ++this.Metrics.TotalWagersPlaced;
                ++this.Session.Wagers;
                ++this.Metrics.WagersPlacedPerMinute;
                this.Metrics.WagerAmounts += newTrans.Amount;
                this.Session.WagerAmounts += newTrans.Amount;
                this.Metrics.LastWagerPlaced = newTrans.Amount;
            }
            else if(newTrans.Type == WagerResultTransaction.Types.Win)
            {                
                this.Metrics.WinAmounts += newTrans.Amount;
                this.Session.WinAmounts += newTrans.Amount;
                this.Metrics.CurrentBalance += newTrans.Amount;
                this.Metrics.TotalWiinToday += newTrans.Amount;                
            }
            else if (newTrans.Type == WagerResultTransaction.Types.Loss)
            {
                this.Metrics.LossAmounts += newTrans.Amount;
                this.Session.LossAmounts += newTrans.Amount;
                this.Metrics.CurrentBalance -= newTrans.Amount;
                this.Metrics.TotalLostToday += newTrans.Amount;
            }

            newTrans.PlayerBalance = this.Metrics.CurrentBalance;
            
            this.WagersResults.Add(newTrans);

            if (newTrans.Type != WagerResultTransaction.Types.Wager)
            {
                //Snap for History by cloning current
                var snapShot = new Player(this);
                this.History.Add(snapShot);                
            }

            return this;
        }

        public Player AddFinancialTransaction(FinTransaction financialTrx)
        {

            if (this.FinTransactions.Count >= Settings.Instance.KeepNbrFinTransActions)
            {
                this.FinTransactions.RemoveAt(0);                
            }

            if (financialTrx.Type == FinTransaction.Types.Withdraw)
            {
                ++this.Metrics.Withdrawals;
                this.Metrics.CurrentBalance -= financialTrx.Amount;
            }
            else
            {
                ++this.Metrics.Deposits;
                this.Metrics.CurrentBalance += financialTrx.Amount;
            }

            financialTrx.ResultingBalance = this.Metrics.CurrentBalance;
            this.FinTransactions.Add(financialTrx);
            return this;
        }

        /// <summary>
        ///  Should be called when the Player has been completed and ready to be saved into the DB
        /// </summary>
        /// <returns></returns>
        public Player Completed()
        {           
            if(Logger.Instance.IsDebugEnabled)
                Logger.Instance.DebugFormat("Player.Completed {0} FinTrans: {1} WagerResultTrans: {2}",
                                            this.PlayerId,
                                            this.FinTransactions.Count,
                                            this.WagersResults.Count);
            return this;
        }
    }
}
