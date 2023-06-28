using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#if MONGODB
using MongoDB.Bson.Serialization.Attributes;
#endif

namespace PlayerGeneration
{
    public sealed class Game
    {
		[BsonConstructor]
        public Game(string name)
        {
            Name = name;

            if (Name == "Roulette") //"Roulette"
                Roulette = new Roulette();
            else
                Slots = new Slots();
        }

        public Game(Game clone)
        {
            Name =clone.Name;
            Roulette = clone.Roulette;
            Slots = clone.Slots;
        }
       
		[BsonIgnore]
        public string Tag { get; } = "Game";

		[BsonElement]
        public string Name { get; }

		[BsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public Roulette Roulette { get; }

        [Newtonsoft.Json.JsonIgnore]
		[BsonIgnore]
        public Slots Slots { get; }

		[BsonElement]
        public decimal MinimumWager { get { return this.Slots == null ? Roulette.MinimumWager : Slots.MinimumWager; } }

		[BsonElement]
        public decimal MaximumWager { get { return this.Slots == null ? Roulette.MaximumWager : Slots.MaximumWager; } }

        public Tuple<decimal, bool, string> Play(decimal wager)
        {
            return this.Roulette?.Execute(wager)
                        ?? this.Slots?.Execute(wager)
                        ?? new Tuple<decimal,bool,string>(wager, true, "NA");
        }
    }

    public sealed class Roulette
    {        

        public Roulette() { }
        public Roulette(Roulette clone)
        {
            this.Wins = clone.Wins;
            this.Losses = clone.Losses;
        }

        public enum BetTypes
        {
            None = 0,
            Column = 1,
            Dozen = 2,
            EvenBets = 3,
            SingleNumber = 4, 
            TwoNumbers = 5,
            ThreeNumbers = 6,
            FourNumbers = 7,
            FiveNumbers = 8,
            SixNumbers = 9
        }

        public struct ChanceByType
        {
            public BetTypes BetType { get; set; }
            public decimal ChanceWinning { get; set; }
            public int WinMultiplier { get; set; }
        }

        /*
            Bet Event Num	Bet Event	    Chance of win US	Win Multiplier
            1	            Column	        32%	                2
            2	            Dozen	        32%	                2
            3	            Even bets	    47%	                1
            4	            Single number	3%	                35
            5	            Two numbers	    5%	                17
            6	            Three numbers	8%	                11
            7	            Four numbers	11%	                8
            8	            Five numbers	13%	                6
            9	            Six numbers	    16%	                5
         */

        public static ChanceByType[] Chances { get;  } = new ChanceByType[]
        {
            new ChanceByType() { BetType = BetTypes.Column, ChanceWinning = 0.32M, WinMultiplier = 2 },
            new ChanceByType() { BetType = BetTypes.Dozen, ChanceWinning = 0.32M, WinMultiplier = 2 },
            new ChanceByType() { BetType = BetTypes.EvenBets, ChanceWinning = 0.47M, WinMultiplier = 1 },
            new ChanceByType() { BetType = BetTypes.SingleNumber, ChanceWinning = 0.3M, WinMultiplier = 35 },
            new ChanceByType() { BetType = BetTypes.TwoNumbers, ChanceWinning = 0.5M, WinMultiplier = 17 },
            new ChanceByType() { BetType = BetTypes.ThreeNumbers, ChanceWinning = 0.8M, WinMultiplier = 11 },
            new ChanceByType() { BetType = BetTypes.FourNumbers, ChanceWinning = 0.11M, WinMultiplier = 8 },
            new ChanceByType() { BetType = BetTypes.FiveNumbers, ChanceWinning = 0.13M, WinMultiplier = 6 },
            new ChanceByType() { BetType = BetTypes.SixNumbers, ChanceWinning = 0.16M, WinMultiplier = 5 }
        };

        public static volatile int Turns = 0;

        public static decimal MinimumWager { get; } = 0.1M;
        public static decimal MaximumWager { get; } = 50;
        private readonly Random Random = new(Guid.NewGuid().GetHashCode());

        public int Wins { get; private set; }
        public int Losses { get; private set; }

        public Tuple<decimal,bool, string> Execute(decimal wager)
        {
            ChanceByType chanceType;
            bool win = false;

            if (Turns >= Settings.Instance.RouletteWinTurns)
            {
                var idx = Random.Next(0, Chances.Length - 1);
                chanceType = Chances[idx];
                ++Wins;
                Turns = 0;
                win = true;
            }
            else
            {
                var chance = Decimal.Round((decimal) Random.NextDouble(), 2);
                var chances = Chances.Where(c => c.ChanceWinning == chance);

                if(chances.Any())
                {
                    var idx = Random.Next(0, chances.Count() - 1);
                    chanceType = chances.ElementAt(idx);
                    Turns = 0;
                    ++Wins;
                    win = true;
                }
                else
                {
                    ++Losses;
                    ++Turns;
                    var idx = Random.Next(0, Chances.Length - 1);
                    chanceType = Chances[idx];
                }
            }

            return new Tuple<decimal, bool, string>(win ? Decimal.Round(chanceType.WinMultiplier * wager, 2) : wager,
                                                        win,
                                                        chanceType.BetType.ToString());
        }
    }

    public sealed class Slots
    {

        public Slots() { }
        public Slots(Slots clone)
        {
            this.Wins = clone.Wins;
            this.Losses = clone.Losses;
            this.Turns = clone.Turns;
        }

        public struct ChanceByType
        {
            public double MinChanceWinning { get; set; }
            public double MaxChanceWinning { get; set; }
            public int WinMultiplier { get; set; }
            public int Amount { get; set; }
        }

        /*
         Win Chance	33%	
            1 * Win Multiplier 	0.000000000000%	63.0000000000000%
            2 * Win Multiplier 	63.0000000000001%	86.0000000000000%
            3 * Win Multiplier 	86.0000000000001%	95.0000000000000%
            4 * Win Multiplier 	95.0000000000001%	98.0000000000000%
            5 * Win Multiplier 	98.0000000000001%	99.3000000000000%
            6 * Win Multiplier 	99.3000000000001%	99.7500000000000%
            7 * Win Multiplier 	99.7500000000001%	99.9088118036323%
            8 * Win Multiplier 	99.9088118036324%	99.9664537373976%
            9 * Win Multiplier 	99.9664537373977%	99.9876590197793%
            10 * Win Multiplier 	99.9876590197794%	99.9954600072117%
            11 * Win Multiplier 	99.9954600072118%	99.9983298301089%
            12 * Win Multiplier 	99.9983298301090%	99.9993855789526%
            13 * Win Multiplier 	99.9993855789527%	99.9997739672473%
            14 * Win Multiplier 	99.9997739672474%	99.9999168473160%
            15 * Win Multiplier 	99.9999168473161%	99.9999694099559%
            16 * Win Multiplier 	99.9999694099560%	99.9999887466705%
            17 * Win Multiplier 	99.9999887466706%	99.9999958602502%
            18 * Win Multiplier 	99.9999958602503%	99.9999984771900%
            19 * Win Multiplier 	99.9999984771901%	99.9999994399083%
            20 * Win Multiplier 	99.9999994399084%	99.9999997940726%
            $10 win	99.9999997940727%	99.9999999243623%
            $50 win	99.9999999243624%	99.9999999722933%
            $100 win	99.9999999722934%	99.9999999899261%
            $250 win	99.9999999899262%	99.9999999964128%
            $10000 win	99.9999999964129%	100.0000000000000%

         */
        public static ChanceByType[] Chances { get; } = new ChanceByType[]
        {
            new ChanceByType() { MinChanceWinning = 0, MaxChanceWinning= 63.0000000000000, WinMultiplier = 1 },
            new ChanceByType() { MinChanceWinning = 63.0000000000000, MaxChanceWinning= 86.0000000000000, WinMultiplier = 2 },
            new ChanceByType() { MinChanceWinning = 86.0000000000001, MaxChanceWinning= 95.0000000000000, WinMultiplier = 3 },
            new ChanceByType() { MinChanceWinning = 95.0000000000001, MaxChanceWinning= 98.0000000000000, WinMultiplier = 4 },
            new ChanceByType() { MinChanceWinning = 98.0000000000001, MaxChanceWinning= 99.3000000000000, WinMultiplier = 5 },
            new ChanceByType() { MinChanceWinning = 99.3000000000001, MaxChanceWinning= 99.7500000000000, WinMultiplier = 6 },
            new ChanceByType() { MinChanceWinning = 99.7500000000001, MaxChanceWinning= 99.9088118036323, WinMultiplier = 7 },
            new ChanceByType() { MinChanceWinning = 99.9088118036324, MaxChanceWinning= 099.9664537373976, WinMultiplier = 8 },
            new ChanceByType() { MinChanceWinning = 99.9664537373977, MaxChanceWinning= 99.9876590197793, WinMultiplier = 9 },
            new ChanceByType() { MinChanceWinning = 99.9876590197794, MaxChanceWinning= 99.9954600072117, WinMultiplier = 10 },
            new ChanceByType() { MinChanceWinning = 99.9954600072118, MaxChanceWinning= 99.9983298301089, WinMultiplier = 11 },
            new ChanceByType() { MinChanceWinning = 99.9983298301090, MaxChanceWinning= 99.9993855789526, WinMultiplier = 12 },
            new ChanceByType() { MinChanceWinning = 99.9993855789527, MaxChanceWinning= 99.9997739672473, WinMultiplier = 13 },
            new ChanceByType() { MinChanceWinning = 99.9997739672474, MaxChanceWinning= 99.9999168473160, WinMultiplier = 14 },
            new ChanceByType() { MinChanceWinning = 99.9999168473161, MaxChanceWinning= 99.9999694099559, WinMultiplier = 15 },
            new ChanceByType() { MinChanceWinning = 99.9999694099560, MaxChanceWinning= 99.9999887466705, WinMultiplier = 16 },
            new ChanceByType() { MinChanceWinning = 99.9999887466706, MaxChanceWinning= 99.9999958602502, WinMultiplier = 17 },
            new ChanceByType() { MinChanceWinning = 99.9999958602503, MaxChanceWinning= 99.9999984771900, WinMultiplier = 18 },
            new ChanceByType() { MinChanceWinning = 99.9999984771901, MaxChanceWinning= 99.9999994399083, WinMultiplier = 19 },
            new ChanceByType() { MinChanceWinning = 99.9999994399084, MaxChanceWinning= 99.9999997940726, WinMultiplier = 20 },
            new ChanceByType() { MinChanceWinning = 99.9999997940727, MaxChanceWinning= 99.9999999243623, Amount = 10 },
            new ChanceByType() { MinChanceWinning = 99.9999999243624, MaxChanceWinning= 99.9999999722933, Amount = 50 },
            new ChanceByType() { MinChanceWinning = 99.9999999722934, MaxChanceWinning= 99.9999999899261, Amount = 100 },
            new ChanceByType() { MinChanceWinning = 99.9999999899262, MaxChanceWinning= 99.9999999964128, Amount = 250 },
            new ChanceByType() { MinChanceWinning = 99.9999999964129, MaxChanceWinning= 100, Amount = 10000 }
        };

        [Newtonsoft.Json.JsonIgnore]
        public volatile int Turns = 0;
        public int Wins { get; private set; }
        public int Losses { get; private set; }

        public static decimal MinimumWager { get; } = 0.1M;
        public static decimal MaximumWager { get; } = 50;
        public static readonly Random Random = new(Guid.NewGuid().GetHashCode());

        public Tuple<decimal, bool, string> Execute(decimal wager)
        {
            ChanceByType chanceType;
            decimal amount = wager;
            bool win = false;

            if (Turns >= Settings.Instance.SlotsWinTurns) 
            {
                var idx = Random.Next(0, Chances.Length - 1);
                chanceType = Chances[idx];
                if (chanceType.WinMultiplier > 0)
                    amount *= chanceType.WinMultiplier;
                else
                    amount += chanceType.Amount;
                ++Wins;
                win = true;
                Turns = 0;
            }
            else
            {
                var chance = Helpers.GetRandomNumber(0, 100);

                if (chance > Settings.Instance.SlotsChanceTrigger)
                {
                    var chances = Chances.FirstOrDefault(c => chance > c.MinChanceWinning && chance <= c.MaxChanceWinning);

                    if (chances.WinMultiplier > 0 || chances.Amount > 0)
                    {
                        chanceType = chances;
                        if (chances.WinMultiplier > 0)
                            amount *= decimal.Round(chances.WinMultiplier, 2);
                        else
                            amount += chances.Amount;
                        ++Wins;
                        win = true;
                        Turns = 0;
                    }
                    else
                    {
                        ++Losses;
                        var idx = Random.Next(0, Chances.Length - 1);
                        chanceType = Chances[idx];
                        //++Turns;
                    }
                }
                else
                {
                    ++Losses;
                    var idx = Random.Next(0, Chances.Length - 1);
                    chanceType = Chances[idx];
                    //++Turns;
                }
            }

            return new Tuple<decimal, bool, string>(amount,
                                                        win,
                                                        chanceType.WinMultiplier.ToString());
        }
    }

}
