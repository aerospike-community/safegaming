using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameDashBoard
{
    partial class GameDashBoardSettings
    {       
        public PlayerCommon.AerospikeSettings Aerospike;
    }

    partial class SettingsGDB
    {
        static SettingsGDB()
        {
            RemoveFromNotFoundSettings.Add("Mongodb:");
        }
    }
}
