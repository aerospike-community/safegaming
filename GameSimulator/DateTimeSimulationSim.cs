using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using GameSimulator;

namespace PlayerCommon
{
    [DebuggerDisplay("ToString")]
    public partial class DateTimeSimulation
    {

        public static void Initialize()
        {
            PlayTimeInterval = new PlayTimeIntervals()
            {
                MaxTimeSecs = SettingsSim.Instance.Config.PlayTimeIntervalMaxSecs,
                MinTimeSecs = SettingsSim.Instance.Config.PlayTimeIntervalMinSecs
            };
            BetTimeInterval = new BetTimeIntervals()
            {
                MaxTimeSecs = SettingsSim.Instance.Config.BetweenBetTimeIntervalMaxSecs,
                MinTimeSecs = SettingsSim.Instance.Config.BetweenBetTimeIntervalMinSecs
            };
            SessionInterval = new SessionIntervals()
            {
                MaxPlayerSessionRestOver = SettingsSim.Instance.Config.MaxPlayerSessionRestOverMins,
                MinPlayerSessionRestOver = SettingsSim.Instance.Config.MinPlayerSessionRestOverMins,
                MaxPlayerSessionRestUnder = SettingsSim.Instance.Config.MaxPlayerSessionRestUnderMins,
                MinPlayerSessionRestUnder = SettingsSim.Instance.Config.MinPlayerSessionRestUnderMins,
                MaxPlayerSessionRestTrigger = SettingsSim.Instance.Config.MaxPlayerSessionRestTriggerMins,
                MinPlayerSessionRestTrigger = SettingsSim.Instance.Config.MinPlayerSessionRestTriggerMins
            };

            if(SettingsSim.Instance.Config.EnableRealtime.HasValue && SettingsSim.Instance.Config.EnableRealtime.Value)
            {
                InitialType = Types.RealTime;
            }
            else if (SettingsSim.Instance.Config.HistoricFromDate == null)
                InitialType = Types.RealTime;
            else
            {
                InitialType = Types.Historic;

                if (SettingsSim.Instance.Config.HistoricFromDate.ToLower() == "now")
                    FromDate = DateTime.Now;
                else
                    FromDate = DateTime.Parse(SettingsSim.Instance.Config.HistoricFromDate);
                if (SettingsSim.Instance.Config.HistoricToDate.ToLower() == "now")
                    EndDate = DateTime.Now;
                else
                    EndDate = DateTime.Parse(SettingsSim.Instance.Config.HistoricToDate);

                if(FromDate >= EndDate) throw new ArgumentException($"historic Dates mismatch. From: {FromDate} To: {EndDate}");
            }

            InitialHistoricMode = SettingsSim.Instance.Config.HistoricMode;
        }

        protected DateTimeSimulation(DateTimeOffset useDateTime, DateTimeHistory previous)
        {            
        }

        public bool SwitchFromHistoric()
        {
            if(this is DateTimeHistory && DateTimeSimulation.InitialHistoricMode != HistoricMode.GoRealTime)
            {
                return this.Current > DateTimeOffset.Now;
            }

            return false;
        }

    }
}
