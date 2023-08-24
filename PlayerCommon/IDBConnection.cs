using Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace PlayerCommon
{
    public interface IDBConnection : IDisposable
    {
        
        Progression ConsoleProgression { get; }
        
        bool UsedEmailCntEnabled { get; }
        bool IncrementGlobalEnabled { get; }
        bool LiverWagerEnabled { get; }
        bool InterventionEnabled { get; }

    }
    
}
