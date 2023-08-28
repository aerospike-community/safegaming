using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PlayerCommon
{
    partial class MongoDBSettings
    {
        public MongoDB.Driver.FindOptions FindOptions;
    }

}

namespace GameDashBoard
{    
    partial class GameDashBoardSettings
    {       
        public PlayerCommon.MongoDBSettings Mongodb;
    }

    partial class SettingsGDB
    {
        static SettingsGDB()
        {
            RemoveFromNotFoundSettings.Add("Aerospike:");
        }
    }
}
