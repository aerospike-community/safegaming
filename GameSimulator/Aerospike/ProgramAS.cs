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

            InitializationAction = () =>
            {
                Settings.Instance.DBConnectionString = $"Host={SettingsSim.Instance.Config.Aerospike.DBHost};Port={SettingsSim.Instance.Config.Aerospike.DBPort};";
            };

            PreConsoleDisplayAction = () =>
            {
                ConsoleDisplay.Console.WriteLine("ADB Connection Timeout: {0}, Max: {1} Min: {2} Idle: {3} Operation Timeout: {4} Compression: {5} Max Latency Warning: {6}",
                                                    SettingsSim.Instance.Config.Aerospike.ClientPolicy.timeout,
                                                    SettingsSim.Instance.Config.Aerospike.ClientPolicy.asyncMaxConnsPerNode,
                                                    SettingsSim.Instance.Config.Aerospike.ClientPolicy.asyncMinConnsPerNode,
                                                    SettingsSim.Instance.Config.Aerospike.ClientPolicy.maxSocketIdle,
                                                    SettingsSim.Instance.Config.Aerospike.DBOperationTimeout,
                                                    SettingsSim.Instance.Config.Aerospike.EnableDriverCompression,
                                                    SettingsSim.Instance.WarnMaxMSLatencyDBExceeded);
            };

            CreateDBConnection = (displayProgression, settingsSim) =>
                                    new DBConnection(settingsSim.Config.Aerospike,
                                                        displayProgression);
        }
    }
}
