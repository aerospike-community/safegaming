using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace PlayerCommon
{
    public sealed class DateTimeRealTime : DateTimeSimulation
    {
        public DateTimeRealTime()
        {
            CurrentDay = CreationTime.Day;
            Type = Types.RealTime;
        }

        private DateTime CreationTime = DateTime.Now;
        private int CurrentDay;

        public override DateTimeSimulation Clone()
        {
            return new DateTimeRealTime()
            {
                CreationTime = this.CreationTime,
                CurrentDay = this.CurrentDay,
            };
        }

        public int HoursRunning
        {
            get { return (int)(DateTime.Now - CreationTime).TotalHours; }
        }

        public override bool IfNewDay()
        {
            if (CurrentDay != DateTime.Now.Day)
            {
                CurrentDay = DateTime.Now.Day;
                return true;
            }

            return false;
        }

        public override DateTimeSimulation AddMS(int ms = 1)
        {
            Logger.Instance.DebugFormat("Sleeping {0} ms", ms);

            Program.ConsoleSleep.Increment("MS", ms);

            Thread.Sleep(ms);

            Program.ConsoleSleep.Decrement("MS");

            return this;
        }
        public override DateTimeSimulation AddMin(int mins = 1)
        {
            Logger.Instance.DebugFormat("Sleeping {0} mins", mins);

            Program.ConsoleSleep.Increment("Mins", mins);

            if (mins > 5)
                Logger.Instance.InfoFormat("Sleeping for {0} mins", mins);

            Thread.Sleep(new TimeSpan(0, mins, 0));

            Program.ConsoleSleep.Decrement("Mins");
            return this;
        }

        public override DateTimeSimulation AddSec(int secs = 1)
        {
            Logger.Instance.DebugFormat("Sleeping {0} secs", secs);

            Program.ConsoleSleep.Increment("Secs", secs);

            Thread.Sleep(new TimeSpan(0, 0, secs));

            Program.ConsoleSleep.Decrement("Secs");

            return this;
        }

        public override DateTimeSimulation AddDay(int days = 1)
        {
            Logger.Instance.DebugFormat("Sleeping {0} days (not really using 3 mins per day)", days);

            Program.ConsoleSleep.Increment("Simulated Days", days);

            AddMin(days * 3);

            Program.ConsoleSleep.Decrement("Simulated Days");

            return this;
        }

        /// <summary>
        /// Increments the <see cref="Current"/> time to simulate the time of a play (e.g., pulling the lever for a slot) 
        /// based on <see cref="PlayTimeIntervals"/>
        /// </summary>
        /// <returns></returns>
        public override DateTimeSimulation PlayDelayIncrement()
        {
            var seconds = this.RandomTime.Next(PlayTimeInterval.MinTimeSecs, PlayTimeInterval.MaxTimeSecs);

            return AddSec(seconds);
        }

        /// <summary>
        /// Increments the <see cref="Current"/> time to simulate the time between plays
        /// <see cref="BetTimeIntervals"/>
        /// </summary>
        /// <returns></returns>
        public override DateTimeSimulation BetIncrement()
        {
            var seconds = this.RandomTime.Next(BetTimeInterval.MinTimeSecs, BetTimeInterval.MaxTimeSecs);

            return AddSec(seconds);
        }

        /// <summary>
        /// Simulates a player session
        /// </summary>
        /// <returns></returns>
        public override DateTimeSimulation SessionIncrement()
        {
            if (SessionInterval.MinPlayerSessionRestTrigger <= 0 || SessionInterval.MaxPlayerSessionRestTrigger <= 0)
                return this;

            var playedMins = (int)(DateTime.Now - CreationTime).TotalMinutes;
            var minMins = playedMins <= SessionInterval.MinPlayerSessionRestTrigger
                                ? SessionInterval.MinPlayerSessionRestUnder
                                : SessionInterval.MinPlayerSessionRestOver;
            var maxMins = playedMins >= SessionInterval.MaxPlayerSessionRestTrigger
                            ? SessionInterval.MaxPlayerSessionRestUnder
                            : SessionInterval.MaxPlayerSessionRestOver;

            var mins = this.RandomTime.Next(minMins, maxMins);

            return AddMin(mins);
        }

        protected override DateTimeOffset getCurrentTime()
        {
            return DateTimeOffset.Now.ToOffset(this.PlayerTZOffset ?? TimeSpan.Zero);
        }

        protected override void setCurrentTiime(DateTimeOffset newTiime)
        {
            throw new NotImplementedException();
        }

        public TimeSpan? PlayerTZOffset { get; private set; }

        public static DateTimeSimulation GenerateDateTime(TimeSpan? playerTZ)
        {
            return new DateTimeRealTime()
            {
                PlayerTZOffset = playerTZ
            };
        }



    }
}
