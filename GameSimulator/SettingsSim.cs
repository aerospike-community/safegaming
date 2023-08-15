using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace GameSimulator
{
    public partial class SettingsSim : PlayerCommon.Settings
    {
        public new static SettingsSim Instance
        {
            get => (SettingsSim)PlayerCommon.Settings.Instance;
        }

        public SettingsSim(string appJsonFile = "appsettings.json")
            : base(appJsonFile)
        {

            PlayerCommon.Settings.GetSetting<GameSimulatorSettings>(this.ConfigurationBuilderFile,
                                                                    ref this.Config,
                                                                    "GameSimulator");
        }

        public partial class GameSimulatorSettings
        {
            public bool LiveFireForgetTasks = true;

            public int NbrPlayers = 100;
            public int MinPlayerSessions = 1;
            public int MaxPlayerSessions = 10;
            public int MinPlayerSessionRestTrigger = 20;

            public int MinPlayerSessionRestOverMins = 120;
            public int MinPlayerSessionRestUnderMins = 30;
            public int MinPlayerSessionRestTriggerMins = 20;
            public int MaxPlayerSessionRestTriggerMins = 240;
            public int MaxPlayerSessionRestOverMins = 2880;
            public int MaxPlayerSessionRestUnderMins = 720;
            public int BetweenBetTimeIntervalMinSecs = 4;
            public int BetweenBetTimeIntervalMaxSecs = 10;
            public int PlayTimeIntervalMinSecs = 6;
            public int PlayTimeIntervalMaxSecs = 10;
            public string HistoricFromDate = "2022-06-01 00:00";
            public string HistoricToDate = "Now";
            public bool? EnableRealtime = null;

            public int SleepBetweenTransMS = 0;
            public bool ContinuousSessions = false;

            public int MinTransPerSession = 5;
            public int MaxTransPerSession = 10;
            public readonly List<string> OnlyTheseGamingStates;
            public int PlayerIdStartRange = 500;

            public int PlayerHistoryLastNbrTrans = 10;
            public bool GenerateUniqueEmails = true;
            public readonly int GlobalIncrementIntervalSecs = 1;
            public TimeSpan GlobalIncremenIntervals { get => new(0, 0, GlobalIncrementIntervalSecs); }
            public int InterventionThresholdsRefreshRateSecs = 300;
            public TimeSpan InterventionThresholdsRefreshRate { get => new(0, 0, InterventionThresholdsRefreshRateSecs); }

            public int WarnMaxMSLatencyDBExceeded = 50;
            public int WarnIfObjectSizeBytes = -1;
            public int KeepNbrWagerResultTransActions = 10;
            public int KeepNbrFinTransActions = 2;
            public bool UpdateDB = true;
            public bool TruncateSets = true;
            //public string PlayerJsonFile = "~\\Player.json";
            //public string HistoryJsonFile  = "~\\PlayerHistory.json";
            public string StateJsonFile = ".\\state_database.json";

            public int RouletteWinTurns = 68;
            public int SlotsWinTurns = 68;
            public int SlotsChanceTrigger = 62;
            public int HistoricTimeStartMonth = 6;
            public int HistoricTimeEndMonth = DateTime.Now.Month;
            public PlayerCommon.DateTimeSimulation.HistoricMode HistoricMode = PlayerCommon.DateTimeSimulation.HistoricMode.GoIntoFuture;
        }

        public GameSimulatorSettings Config = new GameSimulatorSettings();
    }
}
