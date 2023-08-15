using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PlayerCommonDummy
{
    public sealed class Roulette
    {
        public decimal MinimumWager { get;}
        public decimal MaximumWager { get; }
#pragma warning disable CA1822 // Mark members as static
        public Tuple<decimal, bool, string> Execute(decimal wager) => new(wager, true, "NA");
#pragma warning restore CA1822 // Mark members as static
    }

    public sealed class Slots
    {
        public decimal MinimumWager { get; }
        public decimal MaximumWager { get; }
#pragma warning disable CA1822 // Mark members as static
        public Tuple<decimal, bool, string> Execute(decimal wager) => new(wager, true, "NA");
#pragma warning restore CA1822 // Mark members as static
    }

}
