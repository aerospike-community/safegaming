using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;
using Aerospike.Client;
using Common.Diagnostic;
using System.Data.Common;

namespace PlayerCommon
{
    partial class DBConnection
    {
        static readonly ClusterStats FakeClusterStats;

        static DBConnection()
        {
            ClientDriverClass = typeof(Aerospike.Client.AsyncClientProxy);
            ClientDriverName = "Aerospike DBaaS Driver";
            FakeClusterStats = new ClusterStats(new NodeStats[0], 0);
        }

        public AsyncClientProxy Connection { get; private set; }

        public void Connect()
        {
            using var progression = new Progression(this.ConsoleProgression, "Connection");

            Logger.Instance.Info("DBConnection.Connect.DBaaS Start");


            var policy = this.ASSettings.ClientPolicy;

            if (Settings.Instance.CompletionPortThreads > 0)
            {
                policy.asyncMaxCommands = Settings.Instance.CompletionPortThreads;
                policy.asyncMaxCommandAction = MaxCommandAction.DELAY;
            }

            Logger.Instance.Dump(policy, Logger.DumpType.Info, "\tConnection Policy", 2);

            if (policy.tlsPolicy != null)
            {
                Logger.Instance.Dump(policy.tlsPolicy, Logger.DumpType.Info, "\t\tTLS Policy", 2);
            }

            Host[] hosts;

            if (string.IsNullOrEmpty(this.ASSettings.TLSHostName))
                hosts = new Host[] { new Host(this.ASSettings.DBHost, this.ASSettings.DBPort) };
            else
                hosts = new Host[] { new Host(this.ASSettings.DBHost, this.ASSettings.TLSHostName, this.ASSettings.DBPort) };

            Logger.Instance.Dump<Host>(hosts, Logger.DumpType.Info, "\tHosts", 2);

            this.Connection = new AsyncClientProxy(policy, hosts);

            Logger.Instance.Info("DBConnection.Connect.DBaaS End");            
        }

        public void Truncate()
        {
            Logger.Instance.Info("DBConnection.Truncate Not Supported");
            this.ConsoleProgression.Write("Truncate Not Supported for DBaaS");
        }

        
        public ClusterStats ClusterStats() => FakeClusterStats;


    }
}
