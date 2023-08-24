using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameSimulator
{
    partial class GameSimulatorSettings
    {        
        public PlayerCommon.AerospikeSettings Aerospike;
    }

    partial class SettingsSim
    {
        static SettingsSim()
        {
            RemoveFromNotFoundSettings.Add("Mongodb:");
        }
    }
}
