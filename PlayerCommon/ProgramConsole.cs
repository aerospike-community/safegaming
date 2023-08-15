using System;
using System.Collections.Generic;
using System.Text;
using Common;

namespace PlayerCommon
{
    partial class Program
    {
        static public ConsoleDisplay ConsoleFileWriting = null;
        static public ConsoleDisplay ConsoleWarnings = null;
        static public ConsoleDisplay ConsoleErrors = null;
        static public ConsoleDisplay ConsoleExceptions = null;

        public enum DebugMenuItems
        {
            DebugMode = 1,
            DebugModeConsole = 2,
            NormalMode = 3,
            LaunchDebugger = 4,
            ExitProgram = 5
        }
    }
}