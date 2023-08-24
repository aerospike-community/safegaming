using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameDashBoard
{
    public partial class GameDashBoardSettings
    {
        public bool ReadDB = true;
        public bool CreateIdxs = true;
        public bool UseIdxs = true;

        public List<string> OnlyPlayerIds;
        public List<string> OnlyStateCounties;
        public int NumberOfDashboardSessions;
        public int SessionRefreshRateSecs;
        public int MaxNbrTransPerSession;
        public int MinNbrTransPerSession;
        public int SleepBetweenTransMS;
        public int PageSize = -1;

        public DateTimeOffset StartDate = DateTimeOffset.Now;
        public bool ContinuousSessions;
    }

    public partial class SettingsGDB : PlayerCommon.Settings
    {
        public new static SettingsGDB Instance
        {
            get => (SettingsGDB)PlayerCommon.Settings.Instance;
        }

        public static List<string> RemoveFromNotFoundSettings = new();

        public SettingsGDB(string appJsonFile = "appsettings.json")
            : base(appJsonFile)
        {

            PlayerCommon.Settings.GetSetting(this.ConfigurationBuilderFile,
                                                ref this.Config,
                                                "GameDashBoard");
            PlayerCommon.Settings.RemoveNotFoundSettingClassProps(RemoveFromNotFoundSettings);
        }

        public GameDashBoardSettings Config = new();
    }
}
