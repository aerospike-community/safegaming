using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;
using Aerospike.Client;

namespace PlayerCommon
{
    partial class DBConnection
    {
        static DBConnection()
        {
            ClientDriverClass = typeof(Aerospike.Client.AsyncClient);
            ClientDriverName = "Aerospike Driver";
        }

        public AsyncClient Connection { get; private set; }

        public void Connect()
        {
            using var progression = new Progression(this.ConsoleProgression, "Connection");

            Logger.Instance.Info("DBConnection.Connect Start");


            var policy = this.ASSettings.ClientPolicy;
            
            if (Settings.Instance.CompletionPortThreads > 0)
            {
                policy.asyncMaxCommands = Settings.Instance.CompletionPortThreads;
                policy.asyncMaxCommandAction = MaxCommandAction.DELAY;
            }
            
            Logger.Instance.Dump(policy, Logger.DumpType.Info, "\tConnection Policy", 2);

            Host[] hosts;

            if (string.IsNullOrEmpty(this.ASSettings.TLSHostName))
                hosts = new Host[] { new Host(this.ASSettings.DBHost, this.ASSettings.DBPort) };
            else
                hosts = new Host[] { new Host(this.ASSettings.DBHost, this.ASSettings.TLSHostName, this.ASSettings.DBPort) };

            Logger.Instance.Dump<Host>(hosts, Logger.DumpType.Info, "\tHosts", 2);

            this.Connection = new AsyncClient(policy, hosts);

            Logger.Instance.Info("DBConnection.Connect End");
            Logger.Instance.InfoFormat("\tNodes: {0}", string.Join(", ", Connection.Nodes.Select(n => n.NodeAddress.Address)));
            Logger.Instance.InfoFormat("\tInvalid Nodes: {0}", Connection.GetClusterStats().invalidNodeCount);
        }

        public void Truncate()
        {

            Logger.Instance.Info("DBConnection.Truncate Start");

            using var consoleTrunc = new Progression(this.ConsoleProgression, "Truncating...");

            void Truncate(NamespaceSetName namespaceSetName)
            {
                if (!namespaceSetName.IsEmpty())
                {
                    try
                    {
                        this.Connection.Truncate(null,
                                                    namespaceSetName.Namespace,
                                                    namespaceSetName.SetName,
                                                    DateTime.Now);
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.Error($"DBConnection.Truncate {namespaceSetName}",
                                                ex);
                    }
                }
            }

            Truncate(this.PlayersTransHistorySet);
            Truncate(this.PlayersHistorySet);
            Truncate(this.CurrentPlayersSet);
            Truncate(this.UsedEmailCntSet);
            Truncate(this.GlobalIncrementSet);
            Truncate(this.InterventionSet);
            Truncate(this.LiverWagerSet);

            Logger.Instance.Info("DBConnection.Truncate End");
        }

        public ClusterStats ClusterStats() => this.Connection.GetClusterStats();
    }
}
