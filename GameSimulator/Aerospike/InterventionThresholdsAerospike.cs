using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;
using System.Threading;
using System.Reflection;
using GameSimulator;

namespace PlayerCommon
{
    partial class InterventionThresholds
    {
        private static readonly (string name, string binName, PropertyInfo pInfo)[] PropertyNameBinNames
                               = DBConnection.DBHelpers.GetPropertyBins<InterventionThresholds>();

        public InterventionThresholds(IDictionary<string, object> mappingDict)
        {

            foreach (var kvp in mappingDict)
            {
                var pInfo = PropertyNameBinNames.FirstOrDefault(p => p.binName == kvp.Key);

                if (pInfo.pInfo == null)
                {
                    Logger.Instance.WarnFormat("InterventionThresholds.InterventionThresholds(IDictionary<string,object>) Bin {0} did not match any properties for this class.",
                                                    kvp.Key);
                }
                else
                {
                    if (pInfo.pInfo.PropertyType == typeof(int))
                        pInfo.pInfo.SetValue(this, (int)(long)kvp.Value);
                    else if (pInfo.pInfo.PropertyType == typeof(decimal))
                        pInfo.pInfo.SetValue(this, (decimal)(double)kvp.Value);
                }
            }
            this.NextRefreshTime = DateTime.Now + SettingsSim.Instance.Config.InterventionThresholdsRefreshRate;
        }       
    }

}
