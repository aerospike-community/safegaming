﻿
[assembly: log4net.Config.XmlConfigurator(Watch = true)]
namespace PlayerCommon
{
   public static class LoggerPG
    {

        static public readonly Common.Logger Instance = null;

        static LoggerPG()
        {
            log4net.GlobalContext.Properties["applicationName"] = Common.Functions.Instance.ApplicationName;

            Instance = Common.Logger.Instance;
            Instance.Log4NetInstance = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
            
        }
    }
}
