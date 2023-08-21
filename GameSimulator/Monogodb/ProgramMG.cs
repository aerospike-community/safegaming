using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;
using GameSimulator;

namespace PlayerCommon
{
    partial class Program
    {
        static Program()
        {
            CreateAppSettingsInstance = (appJsonFile) =>
                string.IsNullOrEmpty(appJsonFile)
                    ? new SettingsSim()
                    : new SettingsSim(appJsonFile);
            
            PreConsoleDisplayAction = () =>
            {
                ConsoleDisplay.Console.WriteLine("MGDB Connection Timeout: {0}, Socket Timeout: {1} Max Latency Warning: {2}",
                                                    SettingsSim.Instance.Config.Mongodb.DriverSettings?.ConnectTimeout,
                                                    SettingsSim.Instance.Config.Mongodb.DriverSettings?.SocketTimeout,
                                                    SettingsSim.Instance.WarnMaxMSLatencyDBExceeded);
            };

            CreateDBConnection = (displayProgression, playerProgression, historyProgression) =>
                                    new DBConnection(displayProgression: ConsolePuttingDB,
                                                        playerProgression: ConsolePuttingPlayer,
                                                        historyProgression: ConsolePuttingHistory);
        }
    }
}
