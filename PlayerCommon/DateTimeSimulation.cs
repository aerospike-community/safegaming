﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace PlayerCommon
{
    [DebuggerDisplay("ToString")]
    public abstract partial class DateTimeSimulation
    {

        public enum Types
        {
            Historic = 0,
            RealTime = 1
        }

        public enum HistoricMode
        {
            GoIntoFuture = 0,
            GoRealTime = 1,
            Stop = 2
        }

        public struct PlayTimeIntervals
        {
            public int MaxTimeSecs;
            public int MinTimeSecs;
        }

        public struct BetTimeIntervals
        {
            public int MaxTimeSecs;
            public int MinTimeSecs;
        }


        public struct SessionIntervals
        {
            public int MinPlayerSessionRestTrigger;
            public int MinPlayerSessionRestUnder;
            public int MinPlayerSessionRestOver;

            public int MaxPlayerSessionRestTrigger;
            public int MaxPlayerSessionRestUnder;
            public int MaxPlayerSessionRestOver;

        }

        public static Types InitialType { get; private set; }
        public static HistoricMode InitialHistoricMode { get; private set; }
        public static string FromDateStr { get; private set; }
        public static string EndDateStr { get; private set; }
        public static DateTime FromDate { get; private set; }
        public static DateTime EndDate { get; private set; }
        
        protected readonly Random RandomTime = new(Guid.NewGuid().GetHashCode());

        public static PlayTimeIntervals PlayTimeInterval { get; private set; }
        public static BetTimeIntervals BetTimeInterval { get; private set; }
        public static SessionIntervals SessionInterval { get; private set; }

        protected DateTimeSimulation()
        {
        }
        
        protected DateTimeSimulation(DateTimeOffset useDateTime)
        {            
        }

        
        protected DateTimeSimulation(DateTimeSimulation clone)
        {
        }
        
        public Types Type { get; protected set; }

        public bool IsRealtime { get => this.Type == Types.RealTime; }
        public bool Ishistoric { get => this.Type == Types.Historic; }

        public DateTimeOffset Current
        {
            get => getCurrentTime();
            protected set => setCurrentTiime(value);
        }

        public abstract DateTimeSimulation AddMS(int ms = 1);

        public abstract DateTimeSimulation AddMin(int mins = 1);
        public abstract DateTimeSimulation AddSec(int secs = 1);
        public abstract DateTimeSimulation AddDay(int days = 1);

        public abstract DateTimeSimulation Clone();

        /// <summary>
        /// Increments the <see cref="Current"/> time to simulate the time of a play (e.g., pulling the lever for a slot) 
        /// based on <see cref="PlayTimeIntervals"/>
        /// </summary>
        /// <returns></returns>
        public abstract DateTimeSimulation PlayDelayIncrement();

        /// <summary>
        /// Increments the <see cref="Current"/> time to simulate the time between plays
        /// <see cref="BetTimeIntervals"/>
        /// </summary>
        /// <returns></returns>
        public abstract DateTimeSimulation BetIncrement();

        /// <summary>
        /// Simulates a player session
        /// </summary>
        /// <returns></returns>
        public abstract DateTimeSimulation SessionIncrement();

        protected abstract DateTimeOffset getCurrentTime();
        protected abstract void setCurrentTiime(DateTimeOffset newTiime);

        public abstract bool IfNewDay();

        public override string ToString()
        {
            return this.Current.ToString(Settings.Instance.TimeStampFormatString);
        }

        public override bool Equals(object obj)
        {
            if(obj is null) return false;
            if(ReferenceEquals(obj, this)) return true;

            if(obj is DateTime dt) return Current.Equals(dt);
            if (obj is DateTimeOffset dto) return Current.Equals(dto);
            if (obj is DateTimeSimulation s) return Current.Equals(s.Current);

            return false;
        }

        public override int GetHashCode()
        {
            return Current.GetHashCode();
        }
    }
}
