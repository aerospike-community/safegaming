using System;
using System.Collections.Generic;
using System.Text;
using Common;
using TSC = Common.Patterns.Collections.ThreadSafe;

namespace PlayerCommon
{
    partial class Program
    {
        static public volatile bool AlreadyCanceled = false;
        

        static public void CanceledFaultProcessing(string tag, System.Exception ex, bool ignoreFalut, bool isCanceled)
        {            
            if (!AlreadyCanceled)
            {
                if (isCanceled)
                {
                    AlreadyCanceled = true;

                    if(ex != null) 
                    {
                        Logger.Instance.Error($"{tag} Fault Detected", ex);
                        ConsoleExceptions.Increment($"{tag} {ex.Message}");
                    }
                    Logger.Instance.Error($"{tag} Cancel Detected...");
                    ConsoleErrors.Increment($"{tag} Canceling...");
                }
                else if (ex is null)
                {
                    Logger.Instance.Error($"{tag} Fault Detected... Will try to continue...");
                    ConsoleErrors.Increment($"{tag} Faulted...");
                }
                else
                {
                    Logger.Instance.Error($"{tag} Fault Detected", ex);                   
                    ConsoleExceptions.Increment($"{tag} {ex.Message}");
                
                    if (ignoreFalut)
                    {
                        Logger.Instance.Warn($"Ignoring Fault on {tag}, continue processing...");
                        Logger.Instance.Flush(5000);
                    }
                    else
                    {
                        ConsoleDisplay.End();
                        //GCMonitor.GetInstance().StopGCMonitoring();
                        Logger.Instance.Info($"{Common.Functions.Instance.ApplicationName} Main Ended from Fault or Canceled");
                        Logger.Instance.Flush(5000);
                        ConsoleDisplay.Console.SetReWriteToWriterPosition();

                        var histOutputFile = WritePrefFiles();
                        Terminate(histOutputFile, Logger.GetApplicationLogFile(), ex.GetType().Name);

                        Environment.Exit(-1);
                    }
                }
            }
        }
    }
}
