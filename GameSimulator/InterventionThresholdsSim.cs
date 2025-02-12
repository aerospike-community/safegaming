﻿using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GameSimulator;

namespace PlayerCommon
{
    public partial class InterventionThresholds
    {
        internal static InterventionThresholds Instance = null;
        
        private InterventionThresholds()
        {
            NextRefreshTime = DateTime.Now + SettingsSim.Instance.Config.InterventionThresholdsRefreshRate;
        }
        
        public static async Task<bool> RefreshCheck(IDBConnectionSim dbConnection,
                                                    CancellationToken token,
                                                    bool forceRefresh = false)
        {
            if(dbConnection is null || SettingsSim.Instance.Config.InterventionThresholdsRefreshRateSecs <= 0)
                return false;

            return await dbConnection.InterventionThresholdsRefreshCheck(Instance,
                                                                            token,
                                                                            forceRefresh);
        }

        /// <summary>
        /// Should only be called once per process!!!
        /// </summary>
        /// <param name="dBConnection"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public static async Task<bool> Initialize(IDBConnectionSim dBConnection, CancellationToken token)
        {
            await RefreshCheck(dBConnection, token, true);

            if (Instance == null)
            {
                Logger.Instance.Warn("InterventionThresholds.Initialize failed to Create an Instance from DB! Using default instance.");
                Instance = new InterventionThresholds();
                return false;
            }

            return true;
        }
    }
}
