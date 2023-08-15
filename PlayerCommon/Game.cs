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
        [JsonIgnore]
        public Roulette Roulette { get; }

        [JsonIgnore]
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
}
