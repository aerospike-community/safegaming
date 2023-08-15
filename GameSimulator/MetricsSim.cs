using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerCommon
{
    partial class Metrics
    {
        public Metrics(Player player, decimal currentBlance)
        {
            Player = player;
            CurrentBalance = currentBlance;

            {
                var random = new Random(Guid.NewGuid().GetHashCode());

                switch (player.ValueTier)
                {
                    case Tiers.VeryHigh:
                        this.CLV = random.Next(150001, 1000000);
                        break;
                    case Tiers.High:
                        this.CLV = random.Next(50001, 150000);
                        break;
                    case Tiers.Medium:
                        this.CLV = random.Next(20001, 50000);
                        break;
                    case Tiers.Low:
                        this.CLV = random.Next(10, 20000);
                        break;
                    default:
                        this.CLV = random.Next(20001, 50000);
                        break;
                }

                var randomPercent = random.Next(0, 100);

                if (randomPercent >= 0 && randomPercent <= 50)
                    this.ExpectedActiveDaysPerYear = random.Next(1, 100);
                else if (randomPercent >= 51 && randomPercent <= 70)
                    this.ExpectedActiveDaysPerYear = random.Next(101, 150);
                else if (randomPercent >= 71 && randomPercent <= 79)
                    this.ExpectedActiveDaysPerYear = random.Next(151, 200);
                else if (randomPercent >= 80 && randomPercent <= 89)
                    this.ExpectedActiveDaysPerYear = random.Next(201, 250);
                else if (randomPercent >= 90 && randomPercent <= 96)
                    this.ExpectedActiveDaysPerYear = random.Next(251, 300);
                else
                    this.ExpectedActiveDaysPerYear = random.Next(301, 355);

                randomPercent = random.Next(0, 100);

                if (randomPercent >= 0 && randomPercent <= 30)
                    this.AvgSessionsPerDay = 1;
                else if (randomPercent >= 31 && randomPercent <= 50)
                    this.AvgSessionsPerDay = 2;
                else if (randomPercent >= 51 && randomPercent <= 80)
                    this.AvgSessionsPerDay = 3;
                else if (randomPercent >= 81 && randomPercent <= 95)
                    this.AvgSessionsPerDay = 4;
                else
                    this.AvgSessionsPerDay = random.Next(5, 10);

            }
        }

        public bool CheckForNewDay(WagerResultTransaction transAction)
        {
            bool result = false;

            if (this.CurrentDay < transAction.Timestamp.Day)
            {
                this.CurrentDay = transAction.Timestamp.Day;
                this.TotalLostToday = 0;
                this.TotalWiinToday = 0;
                result = true;
            }

            if (this.CurrentMin != transAction.Timestamp.Minute)
            {
                this.CurrentMin = transAction.Timestamp.Minute;
                this.WagersPlacedPerMinute = 0;
                result = true;
            }

            return result;
        }
    }
}
