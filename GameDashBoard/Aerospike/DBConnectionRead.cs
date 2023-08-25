using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Mail;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aerospike.Client;
using Common;
using GameDashBoard;
using HdrHistogram;

namespace PlayerCommon
{
    partial class DBConnection : IDBConnectionGDB
    {
        #region Policies
        void CreateWritePolicy()
        {
            this.WritePolicy = new Aerospike.Client.WritePolicy()
            {
                sendKey = true,
                socketTimeout = this.ASSettings.DBOperationTimeout,
                totalTimeout = this.ASSettings.totalTimeout * 3,
                compress = this.ASSettings.EnableDriverCompression,
                maxRetries = this.ASSettings.maxRetries
            };

            Logger.Instance.Dump(WritePolicy, Logger.DumpType.Info, "\tWrite Policy", 2);
        }
        void CreateReadPolicies()
        {
            this.ReadPolicy = new Policy(this.Connection.readPolicyDefault)
            {
                socketTimeout = this.ASSettings.DBOperationTimeout,
                totalTimeout = this.ASSettings.totalTimeout * 3,
                compress = this.ASSettings.EnableDriverCompression,
                maxRetries = this.ASSettings.maxRetries,
                replica = this.ASSettings.Replica
            };

            Logger.Instance.Dump(ReadPolicy, Logger.DumpType.Info, "\tRead Policy", 2);
        }

        void CreateQueryPolicies()
        {
            this.QueryPolicy = new QueryPolicy()
            {
                socketTimeout = this.ASSettings.DBOperationTimeout,
                totalTimeout = this.ASSettings.totalTimeout * 3,
                compress = this.ASSettings.EnableDriverCompression,
                maxRetries = this.ASSettings.maxRetries,
                recordQueueSize = this.ASSettings.QueueRecordSize,
                maxConcurrentNodes = this.ASSettings.MaxConcurrentNodes,
                replica = this.ASSettings.Replica
            };

            Logger.Instance.Dump(QueryPolicy, Logger.DumpType.Info, "\tQuery Policy", 2);
        }

        void CreateListPolicies()
        {
            this.ListPolicy = new ListPolicy(ListOrder.UNORDERED, ListWriteFlags.DEFAULT);
            Logger.Instance.Dump(ListPolicy, Logger.DumpType.Info, "\tList Policy", 2);
        }
        #endregion

        public Task CreateIndexes(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        private int DisplayRecords(RecordSet recordSet, 
                                    ConsoleDisplay console,
                                    int sessionIdx,
                                    string playerIdBin,
                                    ref int currentPlayerCnt,
                                    int playerThrehold,
                                    CancellationToken token)
        {
            if (Logger.Instance.IsDebugEnabled)
                Logger.Instance.DebugFormat("DBConnection.DisplayRecords Run Start Session {0}",
                                                sessionIdx);

            int nbrRecs = 0;
            var tasks = new List<Task>();

            try
            {
                do
                {
                    token.ThrowIfCancellationRequested();

                    var key = recordSet.Key;
                    var record = recordSet.Record;

                    console.Increment($"Session {sessionIdx}", $"Key {key.userKey.Object}");

                    nbrRecs++;

                    if (!string.IsNullOrEmpty(playerIdBin)
                            && record.bins.ContainsKey(playerIdBin)
                            && currentPlayerCnt++ >= playerThrehold)
                    {
                        var playerId = record.GetInt(playerIdBin);
                        currentPlayerCnt = 0;
                        tasks.Add(Task.Run(() => this.GetPlayer(playerId, sessionIdx, token),
                                    token));
                    }

                    if (SettingsGDB.Instance.Config.SleepBetweenTransMS > 0)
                    {
                        Program.ConsoleSleep.Increment($"Session {sessionIdx}");
                        Thread.Sleep(SettingsGDB.Instance.Config.SleepBetweenTransMS);
                        Program.ConsoleSleep.Decrement($"Session {sessionIdx}");
                    }
                }
                while (recordSet.Next());
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                Program.CanceledFaultProcessing($"DBConnection.DisplayRecords session {sessionIdx}", ex, Settings.Instance.IgnoreFaults);
            }

            if (Logger.Instance.IsDebugEnabled)
                Logger.Instance.DebugFormat("DBConnection.DisplayRecords Run Waiting for Players Session {0} Count: {1}",
                                                sessionIdx, nbrRecs);

            Task.WaitAll(tasks.ToArray(), token);

            console.TaskEnd($"Session {sessionIdx}");

            if (Logger.Instance.IsDebugEnabled)
                Logger.Instance.DebugFormat("DBConnection.DisplayRecords Run End Session {0} Count: {1}",
                                                sessionIdx, nbrRecs);

            return nbrRecs;
        }

        public int GetGlobalIncrement(DateTimeOffset tranDT,
                                        int sessionIdx,
                                        int maxTransactions, 
                                        CancellationToken cancellationToken)
        {
            if(GlobalIncrementSet.IsEmpty()) return 0;

            if (Logger.Instance.IsDebugEnabled)
                Logger.Instance.DebugFormat("DBConnection.GetGlobalIncrement Run Start Session {0} Date: {1} MaxTrans: {2}",
                                                sessionIdx,
                                                tranDT,
                                                maxTransactions);

            Program.ConsoleGlobalIncrement.Increment($"Session {sessionIdx}");

            int playerCnt = 0;
            int playerTrigger = maxTransactions - (maxTransactions * (SettingsGDB.Instance.Config.PlayerFetchPct / 100));
           
            var query = new QueryPolicy(this.QueryPolicy)
            { 
                filterExp = Exp.Build(Exp.GE(Exp.IntBin("process_unixts"), Exp.Val(tranDT.ToUnixTimeSeconds())))
            };

            var stmt = new Statement()
            {
                Namespace = this.GlobalIncrementSet.Namespace,
                SetName = this.GlobalIncrementSet.SetName,                
            };

            if(SettingsGDB.Instance.Config.PageSize > 0)
                stmt.MaxRecords = SettingsGDB.Instance.Config.PageSize;

            PartitionFilter filter = PartitionFilter.All();
            PartitionStatus[] cursors = filter.Partitions;
            List<Task> displayTaks = new();
            int idx = 0;

            RecordSet recordSet;
            bool hasRecs;
            var stopWatch = new Stopwatch();
            
            for (idx = 0;
                    idx < maxTransactions || SettingsGDB.Instance.Config.ContinuousSessions;
                    idx++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    stopWatch.Restart();

                    recordSet = this.Connection.QueryPartitions(query, stmt, filter);
                    cursors = filter.Partitions;
                    hasRecs = recordSet.Next();

                    stopWatch.StopRecord(GetTag,
                                            SystemTag,
                                            this.GlobalIncrementSet.SetName,
                                            nameof(GetGlobalIncrement),
                                            sessionIdx);
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    Program.CanceledFaultProcessing($"DBConnection.GetLiveWager Get session {sessionIdx}", ex, Settings.Instance.IgnoreFaults);
                    if (Settings.Instance.IgnoreFaults)
                    {
                        continue;
                    }

                    throw;
                }

                if (hasRecs)
                {
                    DisplayRecords(recordSet,
                                    Program.ConsoleGlobalIncrement,
                                    sessionIdx,
                                    null,
                                    ref playerCnt,
                                    playerTrigger,
                                    cancellationToken);
                    
                }
                else
                {
                    if (Logger.Instance.IsDebugEnabled)
                        Logger.Instance.DebugFormat("DBConnection.GetGlobalIncrement Run No Records Query Session {0}",
                                                        sessionIdx);
                }

                if (stopWatch.ElapsedMilliseconds > Settings.Instance.WarnMaxMSLatencyDBExceeded)
                    Logger.Instance.WarnFormat("DBConnection.GetGlobalIncrement Run Exceeded Latency Threshold for Query Session {1}. Latency: {0}",
                                                stopWatch.ElapsedMilliseconds,
                                                sessionIdx);

                if (!SettingsGDB.Instance.Config.EnableRealtime
                        && SettingsGDB.Instance.Config.SessionRefreshRateSecs > 0)
                {
                    Program.ConsoleSleep.Increment($"Session {idx}");
                    Thread.Sleep(SettingsGDB.Instance.Config.SessionRefreshRateSecs);
                    Program.ConsoleSleep.Increment($"Session {idx}");
                }
            }

            if(Logger.Instance.IsDebugEnabled)
                Logger.Instance.DebugFormat("DBConnection.GetGlobalIncrement Run Display Wait Session {0} Count {1}",
                                                sessionIdx, idx);

            Task.WaitAll(displayTaks.ToArray(), cancellationToken);

            Program.ConsoleGlobalIncrement.Decrement($"Session {sessionIdx}");

            if (Logger.Instance.IsDebugEnabled)
                Logger.Instance.DebugFormat("DBConnection.GetGlobalIncrement Run End Session {0} Count {1}",
                                                sessionIdx, idx);

            return idx;
        }

        public int GetIntervention(DateTimeOffset tranDT,
                                        int sessionIdx,
                                        int maxTransactions,
                                        CancellationToken cancellationToken)
        {
            if (InterventionSet.IsEmpty()) return 0;

            if (Logger.Instance.IsDebugEnabled)
                Logger.Instance.DebugFormat("DBConnection.GetIntervention Run Start Session {0} Date: {1} MaxTrans: {2}",
                                                sessionIdx,
                                                tranDT,
                                                maxTransactions);

            Program.ConsoleIntervention.Increment($"Session {sessionIdx}");

            int playerCnt = 0;
            int playerTrigger = maxTransactions - (maxTransactions * (SettingsGDB.Instance.Config.PlayerFetchPct / 100));

            var query = new QueryPolicy(this.QueryPolicy)
            {
                filterExp = Exp.Build(Exp.GE(Exp.IntBin("process_unixts"), Exp.Val(tranDT.ToUnixTimeSeconds())))
            };

            var stmt = new Statement()
            {
                Namespace = this.InterventionSet.Namespace,
                SetName = this.InterventionSet.SetName,
            };

            if (SettingsGDB.Instance.Config.PageSize > 0)
                stmt.MaxRecords = SettingsGDB.Instance.Config.PageSize;

            PartitionFilter filter = PartitionFilter.All();
            PartitionStatus[] cursors = filter.Partitions;
            List<Task> displayTaks = new();
            int idx = 0;

            RecordSet recordSet;
            bool hasRecs;
            var stopWatch = new Stopwatch();

            for (idx = 0;
                    idx < maxTransactions || SettingsGDB.Instance.Config.ContinuousSessions;
                    idx++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    stopWatch.Restart();

                    recordSet = this.Connection.QueryPartitions(query, stmt, filter);
                    cursors = filter.Partitions;
                    hasRecs = recordSet.Next();

                    stopWatch.StopRecord(GetTag,
                                            SystemTag,
                                            this.InterventionSet.SetName,
                                            nameof(GetIntervention),
                                            sessionIdx);
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    Program.CanceledFaultProcessing($"DBConnection.GetIntervention Get session {sessionIdx}", ex, Settings.Instance.IgnoreFaults);
                    if (Settings.Instance.IgnoreFaults)
                    {
                        continue;
                    }

                    throw;
                }

                if (hasRecs)
                {
                    DisplayRecords(recordSet,
                                    Program.ConsoleIntervention,
                                    sessionIdx,
                                    "PlayerId",
                                    ref playerCnt,
                                    playerTrigger,
                                    cancellationToken);

                }
                else
                {
                    if (Logger.Instance.IsDebugEnabled)
                        Logger.Instance.DebugFormat("DBConnection.GetIntervention Run No Records Query Session {0}",
                                                        sessionIdx);
                }

                if (stopWatch.ElapsedMilliseconds > Settings.Instance.WarnMaxMSLatencyDBExceeded)
                    Logger.Instance.WarnFormat("DBConnection.GetIntervention Run Exceeded Latency Threshold for Query Session {1}. Latency: {0}",
                                                stopWatch.ElapsedMilliseconds,
                                                sessionIdx);

                if (!SettingsGDB.Instance.Config.EnableRealtime
                        && SettingsGDB.Instance.Config.SessionRefreshRateSecs > 0)
                {
                    Program.ConsoleSleep.Increment($"Session {idx}");
                    Thread.Sleep(SettingsGDB.Instance.Config.SessionRefreshRateSecs);
                    Program.ConsoleSleep.Increment($"Session {idx}");
                }
            }

            if (Logger.Instance.IsDebugEnabled)
                Logger.Instance.DebugFormat("DBConnection.GetIntervention Run Display Wait Session {0} Count {1}",
                                                sessionIdx, idx);

            Task.WaitAll(displayTaks.ToArray(), cancellationToken);

            Program.ConsoleIntervention.Decrement($"Session {sessionIdx}");

            if (Logger.Instance.IsDebugEnabled)
                Logger.Instance.DebugFormat("DBConnection.GetIntervention Run End Session {0} Count {1}",
                                                sessionIdx, idx);

            return idx;
        }

        public int GetLiveWager(DateTimeOffset tranDT,
                                    int sessionIdx,
                                    int maxTransactions,
                                    CancellationToken cancellationToken)
        {
            if(LiverWagerSet.IsEmpty()) return 0;

            if (Logger.Instance.IsDebugEnabled)
                Logger.Instance.DebugFormat("DBConnection.GetLiveWager Run Start Session {0} Date: {1} MaxTrans: {2}",
                                                sessionIdx,
                                                tranDT,
                                                maxTransactions);
            
            Program.ConsoleLiveWager.Increment($"Session {sessionIdx}");

            int playerCnt = 0;
            int playerTrigger = maxTransactions - (maxTransactions * (SettingsGDB.Instance.Config.PlayerFetchPct / 100));

            var query = new QueryPolicy(this.QueryPolicy)
            {
                filterExp = Exp.Build(Exp.GE(Exp.IntBin("txn_unixts"), Exp.Val(tranDT.ToUnixTimeSeconds())))
            };

            var stmt = new Statement()
            {
                Namespace = this.LiverWagerSet.Namespace,
                SetName = this.LiverWagerSet.SetName,
            };

            if (SettingsGDB.Instance.Config.PageSize > 0)
                stmt.MaxRecords = SettingsGDB.Instance.Config.PageSize;

            PartitionFilter filter = PartitionFilter.All();
            PartitionStatus[] cursors = filter.Partitions;
            List<Task> displayTaks = new();
            int idx = 0;

            RecordSet recordSet;
            bool hasRecs;
            var stopWatch = new Stopwatch();

            for (idx = 0;
                    idx < maxTransactions || SettingsGDB.Instance.Config.ContinuousSessions;
                    idx++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    stopWatch.Restart();

                    recordSet = this.Connection.QueryPartitions(query, stmt, filter);
                    cursors = filter.Partitions;
                    hasRecs = recordSet.Next();
                    stopWatch.StopRecord(GetTag,
                                            SystemTag,
                                            this.LiverWagerSet.SetName,
                                            nameof(GetLiveWager),
                                            sessionIdx);
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex) 
                {                    
                    Program.CanceledFaultProcessing($"DBConnection.GetLiveWager Get session {sessionIdx}", ex, Settings.Instance.IgnoreFaults);
                    if (Settings.Instance.IgnoreFaults)
                    {
                        continue;
                    }

                    throw;
                }

                if (hasRecs)
                {
                    DisplayRecords(recordSet,
                                    Program.ConsoleLiveWager,
                                    sessionIdx,
                                    "PlayerId",
                                    ref playerCnt,
                                    playerTrigger,
                                    cancellationToken);                    
                }
                else
                {
                    if (Logger.Instance.IsDebugEnabled)
                        Logger.Instance.DebugFormat("DBConnection.GetLiveWager Run No Records Query Session {0}",
                                                        sessionIdx);
                }

                if (stopWatch.ElapsedMilliseconds > Settings.Instance.WarnMaxMSLatencyDBExceeded)
                    Logger.Instance.WarnFormat("DBConnection.GetLiveWager Run Exceeded Latency Threshold for Query Session {1}. Latency: {0}",
                                                stopWatch.ElapsedMilliseconds,
                                                sessionIdx);

                if (!SettingsGDB.Instance.Config.EnableRealtime
                        && SettingsGDB.Instance.Config.SessionRefreshRateSecs > 0)
                {
                    Program.ConsoleSleep.Increment($"Session {idx}");
                    Thread.Sleep(SettingsGDB.Instance.Config.SessionRefreshRateSecs);
                    Program.ConsoleSleep.Increment($"Session {idx}");
                }
            }

            if (Logger.Instance.IsDebugEnabled)
                Logger.Instance.DebugFormat("DBConnection.GetLiveWager Run Display Wait Session {0} Count {1}",
                                                sessionIdx, idx);

            Task.WaitAll(displayTaks.ToArray(), cancellationToken);

            Program.ConsoleLiveWager.Decrement($"Session {sessionIdx}");

            if (Logger.Instance.IsDebugEnabled)
                Logger.Instance.DebugFormat("DBConnection.GetLiveWager Run End Session {0} Count {1}",
                                                sessionIdx, idx);
            
            return idx;
        }
        
        public async Task GetPlayer(int playerId, int sessionIdx, CancellationToken cancellationToken)
        {
            if (Logger.Instance.IsDebugEnabled)
                Logger.Instance.DebugFormat("DBConnection.GetPlayer Run Start Session {0} Player Id: {1}",
                                                sessionIdx,
                                                playerId);

            cancellationToken.ThrowIfCancellationRequested();

            Program.ConsoleGetPlayer.Increment($"Session {sessionIdx} Player {playerId}");

            var stopWatch =  Stopwatch.StartNew();

            var record = await this.Connection.Get(this.ReadPolicy,
                                                    cancellationToken,
                                                    new Key(this.CurrentPlayersSet.Namespace,
                                                                this.CurrentPlayersSet.SetName,
                                                                Value.Get(playerId)))
                            .ContinueWith(task =>
                             {                                 
                                 stopWatch.StopRecord(GetTag,
                                                         SystemTag,
                                                         this.CurrentPlayersSet.SetName,
                                                         nameof(GetPlayer),
                                                         playerId);

                                 if (Logger.Instance.IsDebugEnabled)
                                 {
                                     Logger.Instance.DebugFormat("DBConnection.GetPlayer Run End {0} Elapsed Time (ms): {1}",
                                                                 playerId,
                                                                 stopWatch.ElapsedMilliseconds);
                                 }

                                 if (stopWatch.ElapsedMilliseconds > Settings.Instance.WarnMaxMSLatencyDBExceeded)
                                     Logger.Instance.WarnFormat("DBConnection.GetPlayer Run Exceeded Latency Threshold for {1}. Latency: {0}",
                                                                 stopWatch.ElapsedMilliseconds,
                                                                 playerId);

                                 if (task.IsFaulted || task.IsCanceled)
                                 {
                                     Program.CanceledFaultProcessing($"DBConnection.GetPlayer Get {playerId}", task.Exception, Settings.Instance.IgnoreFaults);
                                     if (Settings.Instance.IgnoreFaults && !task.IsCanceled)
                                     {
                                         task.Exception?.Handle(e => true);
                                         return null;
                                     }
                                 }

                                 return task.Result;
                             },
                            cancellationToken,
                            TaskContinuationOptions.AttachedToParent | TaskContinuationOptions.ExecuteSynchronously,
                            TaskScheduler.Default);

            Program.ConsoleGetPlayer.Decrement($"Session {sessionIdx} Player {playerId}");

            if (Logger.Instance.IsDebugEnabled)
                Logger.Instance.DebugFormat("DBConnection.GetPlayer Run End Session {0} PlayerId {1}",
                                                sessionIdx, playerId);
            
        }
    }
}
