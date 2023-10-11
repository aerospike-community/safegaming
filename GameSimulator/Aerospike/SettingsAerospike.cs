using Microsoft.Extensions.Configuration;
using PlayerCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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

            PlayerCommon.Settings.AddFuncPathAction("GameSimulator:Aerospike:DBOperationTimeout",
                (IConfiguration config, string path, string propName, Type propType, object propValue, object propParent)
                    =>
                {
                    if(!string.IsNullOrEmpty((string)propValue) && propParent is AerospikeSettings asSetting)
                    {
                        var timeOut = int.Parse(propValue.ToString());

                        if (!Settings.UpdatedPropExists("*readPolicyDefault.totalTimeout"))
                        {
                            asSetting.ClientPolicy.readPolicyDefault.totalTimeout = timeOut;
                        }
                        if (!Settings.UpdatedPropExists("*writePolicyDefault.totalTimeout"))
                        {
                            asSetting.ClientPolicy.writePolicyDefault.totalTimeout = timeOut;
                        }
                        if (!Settings.UpdatedPropExists("*queryPolicyDefault.totalTimeout"))
                        {
                            asSetting.ClientPolicy.queryPolicyDefault.totalTimeout = timeOut;
                        }                        
                    }
                    
                    return (propValue, InvokePathActions.Continue);
                });
            PlayerCommon.Settings.AddFuncPathAction("GameSimulator:Aerospike:SocketTimeout",
                (IConfiguration config, string path, string propName, Type propType, object propValue, object propParent)
                    =>
                {
                    if (!string.IsNullOrEmpty((string)propValue) && propParent is AerospikeSettings asSetting)
                    {
                        var timeOut = int.Parse(propValue.ToString());

                        if (!Settings.UpdatedPropExists("*readPolicyDefault.socketTimeout"))
                        {
                            asSetting.ClientPolicy.readPolicyDefault.socketTimeout = timeOut;
                        }
                        if (!Settings.UpdatedPropExists("*writePolicyDefault.socketTimeout"))
                        {
                            asSetting.ClientPolicy.writePolicyDefault.socketTimeout = timeOut;
                        }
                        if (!Settings.UpdatedPropExists("*queryPolicyDefault.socketTimeout"))
                        {
                            asSetting.ClientPolicy.queryPolicyDefault.socketTimeout = timeOut;
                        }
                    }

                    return (propValue, InvokePathActions.Continue);
                });
            PlayerCommon.Settings.AddFuncPathAction("GameSimulator:Aerospike:MaxRetries",
                (IConfiguration config, string path, string propName, Type propType, object propValue, object propParent)
                    =>
                {
                    if (!string.IsNullOrEmpty((string)propValue) && propParent is AerospikeSettings asSetting)
                    {
                        var retries = int.Parse(propValue.ToString());

                        if (!Settings.UpdatedPropExists("*readPolicyDefault.maxRetries"))
                        {
                            asSetting.ClientPolicy.readPolicyDefault.maxRetries = retries;
                        }
                        if (!Settings.UpdatedPropExists("*writePolicyDefault.maxRetries"))
                        {
                            asSetting.ClientPolicy.writePolicyDefault.maxRetries = retries;
                        }
                        if (!Settings.UpdatedPropExists("*queryPolicyDefault.maxRetries"))
                        {
                            asSetting.ClientPolicy.queryPolicyDefault.maxRetries = retries;
                        }
                    }

                    return (propValue, InvokePathActions.Continue);
                });
            PlayerCommon.Settings.AddFuncPathAction("GameSimulator:Aerospike:SleepBetweenRetries",
                (IConfiguration config, string path, string propName, Type propType, object propValue, object propParent)
                    =>
                {
                    if (!string.IsNullOrEmpty((string)propValue) && propParent is AerospikeSettings asSetting)
                    {
                        var sleep = int.Parse(propValue.ToString());

                        if (!Settings.UpdatedPropExists("*readPolicyDefault.sleepBetweenRetries"))
                        {
                            asSetting.ClientPolicy.readPolicyDefault.sleepBetweenRetries = sleep;
                        }
                        if (!Settings.UpdatedPropExists("*writePolicyDefault.sleepBetweenRetries"))
                        {
                            asSetting.ClientPolicy.writePolicyDefault.sleepBetweenRetries = sleep;
                        }
                        if (!Settings.UpdatedPropExists("*queryPolicyDefault.sleepBetweenRetries"))
                        {
                            asSetting.ClientPolicy.queryPolicyDefault.sleepBetweenRetries = sleep;
                        }
                    }

                    return (propValue, InvokePathActions.Continue);
                });
            PlayerCommon.Settings.AddFuncPathAction("GameSimulator:Aerospike:EnableDriverCompression",
                (IConfiguration config, string path, string propName, Type propType, object propValue, object propParent)
                    =>
                {
                    if (!string.IsNullOrEmpty((string)propValue) && propParent is AerospikeSettings asSetting)
                    {
                        var compress = bool.Parse(propValue.ToString());

                        if (!Settings.UpdatedPropExists("*readPolicyDefault.compress"))
                        {
                            asSetting.ClientPolicy.readPolicyDefault.compress = compress;
                        }
                        if (!Settings.UpdatedPropExists("*writePolicyDefault.compress"))
                        {
                            asSetting.ClientPolicy.writePolicyDefault.compress = compress;
                        }
                        if (!Settings.UpdatedPropExists("*queryPolicyDefault.compress"))
                        {
                            asSetting.ClientPolicy.queryPolicyDefault.compress = compress;
                        }
                    }

                    return (propValue, InvokePathActions.Continue);
                });

            OnInitialization += SettingsSim_OnInitialization;
        }

        private static void SettingsSim_OnInitialization(SettingsSim settings)
        {
            if(settings.Config.Aerospike.DaaS)
            {
                if (settings.Config.Aerospike.DBPort == 3000)
                    settings.Config.Aerospike.DBPort = 4000;
            }
            if (settings.Config.Aerospike.InterventionThresholdsSetName is null)
                settings.Config.InterventionThresholdsRefreshRateSecs = -1;
        }
    }
}
