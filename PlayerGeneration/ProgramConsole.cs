using System;
using System.Collections.Generic;
using System.Text;
using Common;

namespace PlayerGeneration
{
    partial class Program
    {
        static public ConsoleDisplay ConsoleGenerating = null;
        static public ConsoleDisplay ConsoleGeneratingTrans = null;
        static public ConsoleDisplay ConsolePuttingDB = null;
        static public ConsoleDisplay ConsolePuttingPlayer = null;
        static public ConsoleDisplay ConsolePuttingHistory = null;
        static public ConsoleDisplay ConsoleSleep = null;
        static public ConsoleDisplay ConsoleFileWriting = null;
        static public ConsoleDisplay ConsoleWarnings = null;
        static public ConsoleDisplay ConsoleErrors = null;
        static public ConsoleDisplay ConsoleExceptions = null;

        public enum DebugMenuItems
        {
            DebugMode = 1,
            DebugModeConsole = 2,
            NormalMode = 3,
            ExitProgram = 4
        }
    }
}