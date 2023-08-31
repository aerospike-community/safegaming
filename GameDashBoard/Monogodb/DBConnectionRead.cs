using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using Common;
using GameDashBoard;
using Common.Diagnostic;
using System.Linq.Expressions;
using HdrHistogram;

namespace PlayerCommon
{
    partial class DBConnection : IDBConnectionGDB
    {
        
        public void CreateIndexes(CancellationToken cancellationToken)
        {
            Logger.Instance.Info("DBConnection.CreateIndexes Start");

            using var consoleTrunc = new Progression(this.ConsoleProgression, "Create SIdxs...");

            async Task CreateIdx<T>(DBCollection<T> collection, IndexKeysDefinition<T> indexDef)               
            {
                if (!collection.IsEmpty)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                   
                    try
                    {
                        await collection
                                .Collection
                                .Indexes
                                .CreateOneAsync(new CreateIndexModel<T>(indexDef),
                                                    cancellationToken: cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.Error($"DBConnection.CreateIndexes {collection.CollectionName}",
                                                ex);
                    }
                }
            }

            Task[] tasks = new Task[3];

            tasks[0] =  CreateIdx(this.GlobalIncrementCollection,
                                    Builders<GlobalIncrement>.IndexKeys.Ascending(c => c.IntervalUnixSecs));
            tasks[1] = CreateIdx(this.InterventionCollection,
                                    Builders<Intervention>.IndexKeys.Ascending(c => c.InterventionTimeStampUnixSecs));
            tasks[2] = CreateIdx(this.LiverWagerCollection,
                                   Builders<LiveWager>.IndexKeys.Ascending(c => c.txn_unixts));

            Logger.Instance.Info("DBConnection.CreateIndexes End");
        }               

        async Task<int> FetchRecords<T>(DBCollection<T> collection,
                                        ConsoleDisplay console,
                                        Func<T, long> field,
                                        Func<T, string> keyValue,
                                        Func<T, int?> playerIdValue,
                                        FindOptions<T> findOptions,
                                        long startTimeUnixSecs,
                                        int sessionIdx,
                                        int maxTransactions,
                                        int playerThrehold,
                                        bool useSIdx,
                                        CancellationToken cancellationToken,
                                        int currentPlayerCnt = 0,
                                        int currentTrans = 0,
                                        int nbrRecs = 0,
                                        ExpressionFieldDefinition<T, long> fieldDef = null)
        {
            static Expression<Func<D, long>> GetExpression<D>(Func<D, long> f)
                => x => f(x);

            if (Logger.Instance.IsDebugEnabled)
                Logger.Instance.DebugFormat("DBConnection.FetchRecords Run Start Session {0} Count: {3} PlayerCnt: {1} Trans: {2}",
                                                sessionIdx,
                                                currentPlayerCnt,
                                                currentTrans,
                                                nbrRecs);

            fieldDef ??= new ExpressionFieldDefinition<T, long>(GetExpression<T>(field));

            long unixtime = startTimeUnixSecs;
            var filter = collection.BuildersFilter.Gte(fieldDef, unixtime);

            bool exceptionReTry = false;
            var stopWatch = new Stopwatch();
                        
            try
            {
                for (;
                    !exceptionReTry
                        && (currentTrans < maxTransactions
                                || SettingsGDB.Instance.Config.ContinuousSessions);
                    currentTrans++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if(currentTrans == 0)
                        stopWatch.Restart();

                    using (var cursor = await collection.Collection
                                                    .FindAsync(filter, findOptions))
                    {
                        try
                        {
                            await cursor.ForEachAsync<T>(async document =>
                            {
                                unixtime = field(document);

                                if(currentTrans == 0)
                                {
                                    stopWatch.StopRecord(FindTag,
                                                            SystemTag,
                                                            collection.CollectionName,
                                                            nameof(FetchRecords),
                                                            sessionIdx);
                                }

                                nbrRecs++;

                                console.Increment($"Session {sessionIdx}", $"Key {keyValue(document)}");

                                if (currentPlayerCnt++ >= playerThrehold)
                                {
                                    currentPlayerCnt = 0;

                                    var playerId = playerIdValue(document);
                                    if (playerId.HasValue)
                                    {
                                        await this.GetPlayer(playerId.Value, sessionIdx, cancellationToken);
                                    }
                                }

                                if (SettingsGDB.Instance.Config.SleepBetweenTransMS > 0)
                                {
                                    Program.ConsoleSleep.Increment($"Session {sessionIdx}");
                                    Thread.Sleep(SettingsGDB.Instance.Config.SleepBetweenTransMS);
                                    Program.ConsoleSleep.Decrement($"Session {sessionIdx}");
                                }

                            },
                            cancellationToken: cancellationToken);
                        }
                        catch (MongoCommandException ex)
                        {
                            if (ex.Code == 136 && ex.CodeName == "CappedPositionLost")
                            {
                                if (Logger.Instance.IsDebugEnabled)
                                    Logger.Instance.InfoFormat("DBConnection.FetchRecords Exception (ReTry) Session {0} Exception: {1} Code: {2} CodeName: {4} Message: {3}",
                                                                    sessionIdx,
                                                                    ex.GetType().Name,
                                                                    ex.Code,
                                                                    ex.Message,
                                                                    ex.CodeName);
                                exceptionReTry = true;
                                break;
                            }
                            else
                                throw;
                        }
                    }

                    collection.BuildersFilter.Gt(fieldDef, unixtime);
                }

                if (exceptionReTry)
                {
                    nbrRecs += await FetchRecords(collection,
                                                    console,
                                                    field,
                                                    keyValue,
                                                    playerIdValue,
                                                    findOptions,
                                                    startTimeUnixSecs,
                                                    sessionIdx,
                                                    maxTransactions,
                                                    playerThrehold,
                                                    useSIdx,
                                                    cancellationToken,
                                                    currentPlayerCnt,
                                                    currentTrans,
                                                    nbrRecs,
                                                    fieldDef);
                }
            }
            catch (Exception ex)
            {
                Program.CanceledFaultProcessing($"DBConnection.FetchRecords session {sessionIdx} Count: {nbrRecs} PlayerCnt: {currentPlayerCnt} Trans: {currentTrans}",
                                                    ex, Settings.Instance.IgnoreFaults);
            }

            console.TaskEnd($"Session {sessionIdx}");

            if (Logger.Instance.IsDebugEnabled)
                Logger.Instance.DebugFormat("DBConnection.FetchRecords Run End Session {0} Count: {3} PlayerCnt: {1} Trans: {2}",
                                                sessionIdx,
                                                currentPlayerCnt,
                                                currentTrans,
                                                nbrRecs);

            return nbrRecs;
        }

        public int GetGlobalIncrement(DateTimeOffset tranDT,
                                        int sessionIdx,
                                        int maxTransactions, 
                                        CancellationToken cancellationToken)
        {
            if(GlobalIncrementCollection.IsEmpty) return 0;

            if (Logger.Instance.IsDebugEnabled)
                Logger.Instance.DebugFormat("DBConnection.GetGlobalIncrement Run Start Session {0} Date: {1} MaxTrans: {2}",
                                                sessionIdx,
                                                tranDT,
                                                maxTransactions);

            Program.ConsoleGlobalIncrement.Increment($"Session {sessionIdx}");

            int playerTrigger = maxTransactions - (maxTransactions * (SettingsGDB.Instance.Config.PlayerFetchPct / 100));
            var idx = Task.Run(() =>
                              FetchRecords(this.GlobalIncrementCollection,
                                            Program.ConsoleGlobalIncrement,
                                            d => d.IntervalUnixSecs,
                                            d => d.Key,
                                            d => null,
                                            this.GlobalIncrementCollection.FindOptions,
                                            tranDT.ToUnixTimeSeconds(),
                                            sessionIdx,
                                            maxTransactions,
                                            playerTrigger,
                                            SettingsGDB.Instance.Config.UseIdxs,
                                            cancellationToken),
                                cancellationToken).Result;
            
            if (Logger.Instance.IsDebugEnabled)
                Logger.Instance.DebugFormat("DBConnection.GetGlobalIncrement Run Display Wait Session {0} Tasks {1}",
                                                sessionIdx, idx);
            
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
            if (InterventionCollection.IsEmpty) return 0;

            if (Logger.Instance.IsDebugEnabled)
                Logger.Instance.DebugFormat("DBConnection.GetIntervention Run Start Session {0} Date: {1} MaxTrans: {2}",
                                                sessionIdx,
                                                tranDT,
                                                maxTransactions);

            Program.ConsoleIntervention.Increment($"Session {sessionIdx}");

            int playerTrigger = maxTransactions - (maxTransactions * (SettingsGDB.Instance.Config.PlayerFetchPct / 100));
            var idx = Task.Run(() =>
                              FetchRecords(this.InterventionCollection,
                                            Program.ConsoleIntervention,
                                            d => d.InterventionTimeStampUnixSecs,
                                            d => d.PrimaryKey.ToString(),
                                            d => d.PlayerId,
                                            this.InterventionCollection.FindOptions,
                                            tranDT.ToUnixTimeSeconds(),
                                            sessionIdx,
                                            maxTransactions,
                                            playerTrigger,
                                            SettingsGDB.Instance.Config.UseIdxs,
                                            cancellationToken),
                                cancellationToken).Result;

            if (Logger.Instance.IsDebugEnabled)
                Logger.Instance.DebugFormat("DBConnection.GetIntervention Run Display Wait Session {0} Tasks {1}",
                                                sessionIdx, idx);
            
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
            if(LiverWagerCollection.IsEmpty) return 0;

            if (Logger.Instance.IsDebugEnabled)
                Logger.Instance.DebugFormat("DBConnection.GetLiveWager Run Start Session {0} Date: {1} MaxTrans: {2}",
                                                sessionIdx,
                                                tranDT,
                                                maxTransactions);
            
            Program.ConsoleLiveWager.Increment($"Session {sessionIdx}");

            int playerTrigger = maxTransactions - (maxTransactions * (SettingsGDB.Instance.Config.PlayerFetchPct / 100));
            var idx = Task.Run(() =>
                              FetchRecords(this.LiverWagerCollection,
                                            Program.ConsoleLiveWager,
                                            d => d.txn_unixts,
                                            d => d.Id.ToString(),
                                            d => d.PlayerId,
                                            this.LiverWagerCollection.FindOptions,
                                            tranDT.ToUnixTimeSeconds(),
                                            sessionIdx,
                                            maxTransactions,
                                            playerTrigger,
                                            SettingsGDB.Instance.Config.UseIdxs,
                                            cancellationToken),
                                cancellationToken).Result;

            if (Logger.Instance.IsDebugEnabled)
                Logger.Instance.DebugFormat("DBConnection.GetLiveWager Run Display Wait Session {0} Tasks {1}",
                                                sessionIdx, idx);
            
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
            var stopWatch = Stopwatch.StartNew();
            var findFilter = this.CurrentPlayersCollection.BuildersFilter
                                    .Eq(t => t.PlayerId, playerId);

            var player = await this.CurrentPlayersCollection.Collection
                                                .FindAsync(findFilter, 
                                                            cancellationToken: cancellationToken)                                                          
                                .ContinueWith(task =>
                                 {                                 
                                     stopWatch.StopRecord(FindTag,
                                                             SystemTag,
                                                             this.CurrentPlayersCollection.CollectionName,
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
