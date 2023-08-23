﻿using System;
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

            InitializationAction = () =>
            {
                Settings.Instance.DBConnectionString = $"Host={SettingsGDB.Instance.Config.Aerospike.DBHost};Port={SettingsSim.Instance.Config.Aerospike.DBPort};";
            };

            PreConsoleDisplayAction = () =>
            {
                ConsoleDisplay.Console.WriteLine("ADB Connection Timeout: {0}, Max: {1} Min: {2} Idle: {3} Operation Timeout: {4} Compression: {5} Max Latency Warning: {6}",
                                                    SettingsGDB.Instance.Config.Aerospike.ConnectionTimeout,
                                                    SettingsGDB.Instance.Config.Aerospike.MaxConnectionPerNode,
                                                    SettingsGDB.Instance.Config.Aerospike.MinConnectionPerNode,
                                                    SettingsGDB.Instance.Config.Aerospike.MaxSocketIdle,
                                                    SettingsGDB.Instance.Config.Aerospike.DBOperationTimeout,
                                                    SettingsGDB.Instance.Config.Aerospike.EnableDriverCompression,
                                                    SettingsGDB.Instance.WarnMaxMSLatencyDBExceeded);
            };

            CreateDBConnection = (displayProgression, playerProgression, historyProgression) =>
                                    new DBConnection(SettingsGDB.Instance.Config.Aerospike.DBHost,
                                                        SettingsGDB.Instance.Config.Aerospike.DBPort,
                                                        SettingsGDB.Instance.Config.Aerospike.ConnectionTimeout,
                                                        SettingsGDB.Instance.Config.Aerospike.DBOperationTimeout,
                                                        SettingsGDB.Instance.Config.Aerospike.DBUseExternalIPAddresses,
                                                        displayProgression: displayProgression,
                                                        playerProgression: playerProgression,
                                                        historyProgression: historyProgression);
        }
    }
}
