using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PlayerCommon
{
    public sealed class DateTimeHistory : DateTimeSimulation
    {

        public DateTimeHistory(DateTimeOffset useDateTime)
            : base(useDateTime)
        {
            Current = useDateTime;
            CurrentDay = Current.Day;
            Type = Types.Historic;
        }

        public DateTimeHistory(DateTimeHistory clone)
            : base(clone)
        {
            Current = clone.Current;
            TimeSpanSinceCreation = clone.TimeSpanSinceCreation;
            CurrentDay = clone.CurrentDay;
            Type = clone.Type;
        }

        public override DateTimeSimulation Clone()
        {
            return new DateTimeHistory(this);
        }

        public TimeSpan TimeSpanSinceCreation { get; private set; } = TimeSpan.Zero;
        private int CurrentDay;

        public override bool IfNewDay()
        {
            if (CurrentDay != _currentTiime.Day)
            {
                CurrentDay = _currentTiime.Day;
                return true;
            }

            return false;
        }

        public override DateTimeSimulation AddMS(int ms = 1)
        {
            this.UpdateCurrentTime(_currentTiime.AddMilliseconds(ms), this);
            return this;
        }

        public override DateTimeSimulation AddMin(int mins = 1)
        {
            this.UpdateCurrentTime(_currentTiime.AddMinutes(mins), this);
            return this;
        }

        public override DateTimeSimulation AddSec(int secs = 1)
        {
            this.UpdateCurrentTime(_currentTiime.AddSeconds(secs), this);
            return this;
        }

        public override DateTimeSimulation AddDay(int days = 1)
        {
            this.UpdateCurrentTime(_currentTiime.AddDays(days), this);
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
            var playedMins = (int)TimeSpanSinceCreation.TotalMinutes;
            var minMins = playedMins <= SessionInterval.MinPlayerSessionRestTrigger
                                ? SessionInterval.MinPlayerSessionRestUnder
                                : SessionInterval.MinPlayerSessionRestOver;
            var maxMins = playedMins >= SessionInterval.MaxPlayerSessionRestTrigger
                            ? SessionInterval.MaxPlayerSessionRestUnder
                            : SessionInterval.MaxPlayerSessionRestOver;

            var mins = this.RandomTime.Next(minMins, maxMins);

            return AddMin(mins);
        }

        private DateTimeOffset _currentTiime = DateTimeOffset.UtcNow;

        protected override DateTimeOffset getCurrentTime()
        {
            return this._currentTiime;
        }

        protected override void setCurrentTiime(DateTimeOffset newTiime)
        {
            this._currentTiime = newTiime;
        }

        private void UpdateCurrentTime(DateTimeOffset useDateTime, DateTimeHistory previous)
        {
            this._currentTiime = useDateTime;
            TimeSpanSinceCreation += useDateTime - previous.Current;
        }

        public static DateTimeSimulation GenerateDateTime(TimeSpan? playerTZ)
        {
            var randomTime = new Random(Guid.NewGuid().GetHashCode());

            var totalTicks = randomTime.NextInt64(FromDate.Ticks, EndDate.Ticks);

            return new DateTimeHistory(new DateTimeOffset(totalTicks, playerTZ ?? TimeSpan.Zero));
        }

    }
}
