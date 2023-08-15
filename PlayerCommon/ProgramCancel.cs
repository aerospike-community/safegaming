﻿using System;
using System.Collections.Generic;
using System.Text;
using Common;

namespace PlayerCommon
{
    partial class Program
    {
        static public volatile bool AlreadyCanceled = false;
        static bool _AlreadyCanceled = false;

        static public void CanceledFaultProcessing(string tag, System.Exception ex, bool ignoreFalut = false)
        {
            if (!AlreadyCanceled && !Common.Patterns.Threading.LockFree.Exchange(ref _AlreadyCanceled, true))
            {
                AlreadyCanceled = true;

                if (ex == null)
                {
                    Logger.Instance.Error($"{tag} Fault Detected... Will try to continue...");
                    ConsoleErrors.Increment($"{tag} Faulted...");
                }
                else
                {
                    Logger.Instance.Error($"{tag} Fault Detected", ex);
                    ConsoleExceptions.Increment($"{tag} {ex.Message}");
                }

                if (ex != null)
                {

                    if (ignoreFalut)
                    {
                        Logger.Instance.Warn($"Ignoring Fault on {tag}, continue processing...");
                        Logger.Instance.Flush(5000);
                    }
                    else
                    {
                        ConsoleDisplay.End();
                        //GCMonitor.GetInstance().StopGCMonitoring();
                        Logger.Instance.Info("PlayerGeneration Main Ended from Fault or Canceled");
                        Logger.Instance.Flush(5000);
                        ConsoleDisplay.Console.SetReWriteToWriterPosition();
                        Environment.Exit(-1);
                    }
                }
            }
        }
    }
}
