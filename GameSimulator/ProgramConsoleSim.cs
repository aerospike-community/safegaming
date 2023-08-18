using System;
using System.Collections.Generic;
using System.Text;
using Common;
using GameSimulator;

namespace PlayerCommon
{
    partial class Program
    {
        static public ConsoleDisplay ConsoleGenerating = null;
        static public ConsoleDisplay ConsoleGeneratingTrans = null;
        static public ConsoleDisplay ConsolePuttingDB = null;
        static public ConsoleDisplay ConsolePuttingPlayer = null;
        static public ConsoleDisplay ConsolePuttingHistory = null;
        static public ConsoleDisplay ConsoleSleep = null; 
        
        static Program()
        {
            InitializationAction = () =>
            {
                Settings.Instance.DBConnectionString = $"Host={SettingsSim.Instance.Config.Aerospike.DBHost};Port={SettingsSim.Instance.Config.Aerospike.DBPort};";
            };

            PreConsoleDisplayAction = () =>
            {
                ConsoleDisplay.Console.WriteLine("ADB Connection Timeout: {0}, Max: {1} Min: {2} Idle: {3} Operation Timeout: {4} Compression: {5} Max Latency Warning: {6}",
                                                    SettingsSim.Instance.Config.Aerospike.ConnectionTimeout,
                                                    SettingsSim.Instance.Config.Aerospike.MaxConnectionPerNode,
                                                    SettingsSim.Instance.Config.Aerospike.MinConnectionPerNode,                                                   
                                                    SettingsSim.Instance.Config.Aerospike.MaxSocketIdle,
                                                    SettingsSim.Instance.Config.Aerospike.DBOperationTimeout,
                                                    SettingsSim.Instance.Config.Aerospike.EnableDriverCompression,
                                                    SettingsSim.Instance.WarnMaxMSLatencyDBExceeded);
            };

            CreateDBConnection = (displayProgression, playerProgression, historyProgression) =>
                                    new DBConnection(SettingsSim.Instance.Config.Aerospike.DBHost,
                                                        SettingsSim.Instance.Config.Aerospike.DBPort,
                                                        SettingsSim.Instance.Config.Aerospike.ConnectionTimeout,
                                                        SettingsSim.Instance.Config.Aerospike.DBOperationTimeout,
                                                        SettingsSim.Instance.Config.Aerospike.DBUseExternalIPAddresses,
                                                        displayProgression: displayProgression,
                                                        playerProgression: playerProgression,
                                                        historyProgression: historyProgression);
        }
    }
}