﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Common;
using System.Threading;

namespace PlayerCommon
{
    partial class Program
    {
        static public readonly CancellationTokenSource cancellationTokenSource = new();

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            Logger.Instance.Warn("Application Aborted");
            Program.ConsoleErrors?.Increment("Aborted");
            System.Diagnostics.Debug.WriteLine($"'{Common.Functions.Instance.AssemblyFullName}': '{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}': Application Aborted");
            cancellationTokenSource.Cancel();
            
            Logger.Instance.Flush(5000);
        }

        private static void TraceException(object exception, Common.Logger.Log4NetInfo lastLogLine)
        {
            if (exception == null) return;

            if (exception is System.Exception ex) TraceException(ex, lastLogLine);
            else System.Diagnostics.Trace.Write(exception);
        }

        private static void TraceException(System.Exception exception, Common.Logger.Log4NetInfo lastLogLine, bool writeLastLogLine = true)
        {
            if (exception == null) return;

            object lockInstance = (object)lastLogLine.LoggingEvents ?? exception;

            lock (lockInstance)
            {
                System.Diagnostics.Trace.Indent();

               if (writeLastLogLine)
                    System.Diagnostics.Trace.WriteLine(string.Format("Last Logged Line: {0}",
                                                                      lastLogLine.LoggingEvents == null
                                                                          ? string.Empty
                                                                          : string.Join("\r\n\t", lastLogLine.LoggingEvents.Select(m => m.RenderedMessage))));
               
                System.Diagnostics.Trace.WriteLine(string.Format("{0}: {1}", exception.GetType().FullName, exception.Message));
                var stackTrace = new System.Diagnostics.StackTrace(exception, true);

                foreach (var stack in stackTrace.GetFrames())
                {
                    System.Diagnostics.Trace.WriteLine(string.Format("\tat {0} in {1}:line {2}: Column {3}",
                                                                    stack.GetMethod(),
                                                                    stack.GetFileName(),
                                                                    stack.GetFileLineNumber(),
                                                                    stack.GetFileColumnNumber()));

                }

                if (exception.InnerException != null)
                {
                    System.Diagnostics.Trace.WriteLine(string.Format("Inner Exception {0}: {1}", exception.InnerException.GetType().FullName, exception.InnerException.Message));
                    TraceException(exception.InnerException, lastLogLine, false);
                }

                System.Diagnostics.Trace.Unindent();
            }
        }

        static public void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            ++ExceptionCount;

            ConsoleDisplay.End();

            //GCMonitor.GetInstance().StopGCMonitoring();

            ConsoleDisplay.Console.SetReWriteToWriterPosition();
            ConsoleDisplay.Console.WriteLine();

            ConsoleDisplay.Console.WriteLine();

            var traceFile = Common.File.FilePathRelative.Make(string.Format(".\\UnhandledException-{0:yyyy-MM-dd-HH-mm-ss}-{1}.log",
                                                                            RunDateTime,
                                                                            e.ExceptionObject is System.Exception exType
                                                                                ? exType.GetType().Name
                                                                                : "Unknown"));
            ConsoleDisplay.Console.WriteLine($"Exception file at \"{traceFile.PathResolved}\"");

            System.Diagnostics.Trace.Listeners.Add(new System.Diagnostics.TextWriterTraceListener(traceFile.PathResolved));
            System.Diagnostics.Trace.TraceError("Unhandled Exception");
            
            TraceException(e.ExceptionObject, LastLogLine);
            
            System.Diagnostics.Trace.WriteLine("Execution Halted");
            System.Diagnostics.Trace.Flush();
                            
            Exception exTypeObj = e.ExceptionObject as System.Exception;

            if (exTypeObj is null)
            {
                Logger.Instance.FatalFormat("Unhandled Exception Occurred! Exception Is \"{0}\" ({1}) Terminating Processing...",
                                            e.ExceptionObject?.GetType(),
                                            e.ExceptionObject is System.Exception exObj ? exObj.Message : "<Not an Exception Object>");

                ConsoleDisplay.Console.WriteLine("Unhandled Exception of \"{0}\" occurred", exTypeObj?.GetType().Name);
                Logger.Instance.Info($"{Common.Functions.Instance.ApplicationName} Console Application Main Ended due to unhandled exception");
                Logger.Instance.Flush(5000);

                Environment.Exit(-1);
            }

            Logger.Instance.Error("Exception detected is NOT Handled!");
            Logger.Instance.Error($"Unhandled Exception file at \"{traceFile.PathResolved}\"");
            CanceledFaultProcessing("Unhandled", exTypeObj, false, false);
        }

        private static Common.Logger.Log4NetInfo LastLogLine;
#pragma warning disable CA2211
        public static int ExceptionCount = 0;
        public static int WarningCount = 0;
#pragma warning restore CA2211

        private static void Instance_OnLoggingEvent(Common.Logger sender, Common.LoggingEventArgs eventArgs)
        {
            foreach (var item in eventArgs.LogInfo.LoggingEvents)
            {
                if (item.Level == log4net.Core.Level.Error || item.Level == log4net.Core.Level.Fatal)
                {
                    ExceptionCount++;
                    Interlocked.MemoryBarrier();
                    if (item.ExceptionObject == null)
                        ConsoleErrors?.Increment(string.Format(@"Log: {0:yyyy-MM-dd\ HH\:mm\:ss.fff}", item.TimeStamp));
                    else
                        ConsoleExceptions?.Increment(item.ExceptionObject.GetType().Name);
                }
                else if (item.Level == log4net.Core.Level.Warn)
                {
                    WarningCount++;
                    Interlocked.MemoryBarrier();
                    ConsoleWarnings?.Increment(string.Format(@"Log: {0:yyyy-MM-dd\ HH\:mm\:ss.fff}", item.TimeStamp));
                }

            }
            LastLogLine = eventArgs.LogInfo;
        }
    }
}
