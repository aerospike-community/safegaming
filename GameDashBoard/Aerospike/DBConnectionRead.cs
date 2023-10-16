using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aerospike.Client;
using Common;
using GameDashBoard;

namespace PlayerCommon
{
    partial class DBConnection : IDBConnectionGDB
    {
        #region Policies
        
        void CreateListPolicies()
        {
            this.ListPolicy = new ListPolicy(ListOrder.UNORDERED, ListWriteFlags.DEFAULT);
            Logger.Instance.Dump(ListPolicy, Logger.DumpType.Info, "\tList Policy", 2);
        }
        #endregion

        public void CreateIndexes(CancellationToken cancellationToken)
        {
            Logger.Instance.Info("DBConnection.CreateIndexes Start");

            using var consoleTrunc = new Progression(this.ConsoleProgression, "Create SIdxs...");

            void CreateIdx(NamespaceSetName namespaceSetName)
            {
                if (!namespaceSetName.IsEmpty())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    string binName;

                    if (namespaceSetName.SetName == "Intervention")
                        binName = "interv_unixts";
                    else if (namespaceSetName.SetName == "LiveWager")
                        binName = "txn_unixts";                    
                    else
                        binName = "process_unixts";

                    try
                    {
                        this.Connection.CreateIndex(this.WritePolicy,
                                                        namespaceSetName.Namespace,
                                                        namespaceSetName.SetName,
                                                        $"{namespaceSetName.SetName}_unix_timestamp",
                                                        binName,
                                                        IndexType.NUMERIC).Wait();
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.Error($"DBConnection.CreateIndexes {namespaceSetName}",
                                                ex);
                    }
                }
            }

            CreateIdx(this.GlobalIncrementSet);
            CreateIdx(this.InterventionSet);
            CreateIdx(this.LiverWagerSet);

            Logger.Instance.Info("DBConnection.CreateIndexes End");
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
                Program.CanceledFaultProcessing($"DBConnection.DisplayRecords session {sessionIdx}", ex, Settings.Instance.IgnoreFaults,false);
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
            QueryPolicy query;

            var stmt = new Statement()
            {
                Namespace = this.GlobalIncrementSet.Namespace,
                SetName = this.GlobalIncrementSet.SetName,
                RecordsPerSecond = this.ASSettings.RecordsPerSecond
            };

            if(SettingsGDB.Instance.Config.UseIdxs)
            {
                stmt.SetIndexName($"{this.GlobalIncrementSet.SetName}_unix_timestamp");
                stmt.SetFilter(Filter.Range("process_unixts", tranDT.ToUnixTimeSeconds(), long.MaxValue));
                query = this.QueryPolicy;
            }
            else
            {
                query = new QueryPolicy(this.QueryPolicy)
                {
                    filterExp = Exp.Build(Exp.GE(Exp.IntBin("process_unixts"), Exp.Val(tranDT.ToUnixTimeSeconds())))
                };
            }

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
                                            this.GlobalIncrementSet.SetName,
                                            nameof(GetGlobalIncrement),
                                            sessionIdx);
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    Program.CanceledFaultProcessing($"DBConnection.GetLiveWager Get session {sessionIdx}", ex, Settings.Instance.IgnoreFaults, false);
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
            QueryPolicy query;

            var stmt = new Statement()
            {
                Namespace = this.InterventionSet.Namespace,
                SetName = this.InterventionSet.SetName,
                RecordsPerSecond = this.ASSettings.RecordsPerSecond
            };

            if (SettingsGDB.Instance.Config.UseIdxs)
            {
                stmt.SetIndexName($"{this.InterventionSet.SetName}_unix_timestamp");
                stmt.SetFilter(Filter.Range("interv_unixts", tranDT.ToUnixTimeSeconds(), long.MaxValue));
                query = this.QueryPolicy;
            }
            else
            {
                query = new QueryPolicy(this.QueryPolicy)
                {
                    filterExp = Exp.Build(Exp.GE(Exp.IntBin("interv_unixts"), Exp.Val(tranDT.ToUnixTimeSeconds())))
                };
            }

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
                    Program.CanceledFaultProcessing($"DBConnection.GetIntervention Get session {sessionIdx}", ex, Settings.Instance.IgnoreFaults, false);
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
            QueryPolicy query;

            var stmt = new Statement()
            {
                Namespace = this.LiverWagerSet.Namespace,
                SetName = this.LiverWagerSet.SetName,
                RecordsPerSecond = this.ASSettings.RecordsPerSecond
            };

            if (SettingsGDB.Instance.Config.UseIdxs)
            {
                stmt.SetIndexName($"{this.LiverWagerSet.SetName}_unix_timestamp");
                stmt.SetFilter(Filter.Range("txn_unixts", tranDT.ToUnixTimeSeconds(), long.MaxValue));
                query = this.QueryPolicy;
            }
            else
            {
                query = new QueryPolicy(this.QueryPolicy)
                {
                    filterExp = Exp.Build(Exp.GE(Exp.IntBin("txn_unixts"), Exp.Val(tranDT.ToUnixTimeSeconds())))
                };
            }

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
                    Program.CanceledFaultProcessing($"DBConnection.GetLiveWager Get session {sessionIdx}", ex, Settings.Instance.IgnoreFaults, false);
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

                                 if (task.IsFaulted || task.IsCanceled)
                                 {
                                     Program.CanceledFaultProcessing($"DBConnection.GetPlayer Get {playerId}", task.Exception, Settings.Instance.IgnoreFaults, false);
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
