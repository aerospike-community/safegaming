using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;
using GameDashBoard;

namespace PlayerCommon
{
    partial class Program
    {
        static Program()
        {           
            CreateAppSettingsInstance = (appJsonFile) =>
                string.IsNullOrEmpty(appJsonFile)
                    ? new SettingsGDB()
                    : new SettingsGDB(appJsonFile);
            
            PreConsoleDisplayAction = () =>
            {
                ConsoleDisplay.Console.WriteLine("MGDB Connection Timeout: {0}, Socket Timeout: {1} Max Latency Warning: {2}",
                                                    SettingsGDB.Instance.Config.Mongodb.DriverSettings?.ConnectTimeout,
                                                    SettingsGDB.Instance.Config.Mongodb.DriverSettings?.SocketTimeout,
                                                    SettingsGDB.Instance.WarnMaxMSLatencyDBExceeded);
            };

            CreateDBConnection = (settings, displayProgression) =>
                                    new DBConnection(displayProgression, settings.Config.Mongodb);
        }
    }
}
