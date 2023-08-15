using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PlayerCommon
{
    public sealed partial class DBConnection
    {
        public static readonly Type ClientDriverClass;
        public static readonly string ClientDriverName;

        public static (string dbName,
                        string driverName,
                        Version driverVersion) GetInfo()
        {
            var asyncClient = ClientDriverClass.Assembly.GetName();
            return (ClientDriverName,
                    asyncClient?.Name,
                    asyncClient?.Version);
        }
    }
}
