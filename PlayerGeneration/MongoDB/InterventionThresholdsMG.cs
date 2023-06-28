using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;
using System.Threading;

namespace PlayerGeneration
{
    partial class InterventionThresholds
    {
        public static async Task<bool> RefreshCheck(IDBConnection dbConnection,
                                                        CancellationToken token,
                                                        bool forceRefresh = false)
        {
            bool result = false;
            var currentInstance = Instance;

            if (Updating == 0
                    && (forceRefresh
                            || currentInstance == null
                            || currentInstance.NextRefreshTime <= DateTime.Now))
            {
                if (Interlocked.Exchange(ref Updating, 1) == 1) return false;

                try
                {
                    var newInstance = await dbConnection.ReFreshInterventionThresholds(currentInstance, token);

                    if (newInstance != null)
                    {
                        Interlocked.Exchange(ref Instance, newInstance);
                        var incCnt = Interlocked.Increment(ref UpdateCnt);

                        if (Logger.Instance.IsDebugEnabled)
                            Logger.Instance.DebugFormat("InterventionThresholds.RefreshCheck updated {6} from Version: {0} ({1:HH\\:mm\\:ss.ffff} - {2:HH\\:mm\\:ss.ffff}) to {3} ({4:HH\\:mm\\:ss.ffff} - {5:HH\\:mm\\:ss.ffff})",
                                                            currentInstance?.Version ?? -1,
                                                            currentInstance?.RefreshedTime ?? DateTime.MinValue,
                                                            currentInstance?.NextRefreshTime ?? DateTime.MinValue,
                                                            Instance.Version,
                                                            Instance.RefreshedTime,
                                                            Instance.NextRefreshTime,
                                                            incCnt);
                    }
                }
                finally
                {
                    Interlocked.Exchange(ref Updating, 0);
                }
            }

            return result;
        }
    }
}
