using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Aerospike.Client;
using GameSimulator;

namespace PlayerCommon
{
    partial class DBConnection : IDBConnectionSim
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
            this.ReadPolicy = new Policy(this.Connection.readPolicyDefault);

            Logger.Instance.Dump(ReadPolicy, Logger.DumpType.Info, "\tRead Policy", 2);
        }

        void CreateListPolicies()
        {
            this.ListPolicy = new ListPolicy(ListOrder.UNORDERED, ListWriteFlags.DEFAULT);
            Logger.Instance.Dump(ListPolicy, Logger.DumpType.Info, "\tRead Policy", 2);
        }
        #endregion

        public ConsoleDisplay PlayerProgression { get; set; }
        public ConsoleDisplay HistoryProgression { get; set; }

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

        public async Task UpdateCurrentPlayers(Player player,
                                                bool updateHistory,
                                                CancellationToken cancellationToken)
        {
            if (Logger.Instance.IsDebugEnabled)
                Logger.Instance.DebugFormat("DBConnection.UpdateCurrentPlayers Run Start Player: {0}",
                                                player.PlayerId);

            cancellationToken.ThrowIfCancellationRequested();

            this.PlayerProgression.Increment("Player", $"Transforming/Putting Players {player.UserName}...");

            if (Logger.Instance.IsDebugEnabled)
                Logger.Instance.DebugFormat("DBConnection.UpdateCurrentPlayers Run Start Transform Player: {0}",
                                                player.PlayerId);

            var stopWatch = Stopwatch.StartNew();
            var propDict = DBHelpers.TransForm(player);

            stopWatch.StopRecord(TransformTag,
                                    SystemTag,
                                    this.CurrentPlayersSet.SetName,
                                    nameof(UpdateCurrentPlayers),
                                    player.PlayerId);

            if (Logger.Instance.IsDebugEnabled)
                Logger.Instance.DebugFormat("DBConnection.UpdateCurrentPlayers Run End Transform Player: {0} Dict Cnt: {1}",
                                                player.PlayerId,
                                                propDict.Count);

            if (!this.CurrentPlayersSet.IsEmpty())
            {
                stopWatch.Restart();

                await this.Connection.Put(WritePolicy,
                                                    cancellationToken,
                                                    new Key(this.CurrentPlayersSet.Namespace,
                                                        this.CurrentPlayersSet.SetName,
                                                        player.PlayerId),
                                                    DBHelpers.CreateBinRecord(propDict))
                    .ContinueWith(task =>
                    {
                        stopWatch.StopRecord(PutTag,
                                                SystemTag,
                                                this.CurrentPlayersSet.SetName,
                                                nameof(UpdateCurrentPlayers),
                                                player.PlayerId);

                        if (Logger.Instance.IsDebugEnabled)
                        {
                            Logger.Instance.DebugFormat("DBConnection.UpdateCurrentPlayers Run End Put {0} Elapsed Time (ms): {1}",
                                                        player.PlayerId,
                                                        stopWatch.ElapsedMilliseconds);
                        }

                        if (stopWatch.ElapsedMilliseconds > Settings.Instance.WarnMaxMSLatencyDBExceeded)
                            Logger.Instance.WarnFormat("DBConnection.UpdateCurrentPlayers Run Exceeded Latency Threshold for Put {1}. Latency: {0}",
                                                        stopWatch.ElapsedMilliseconds,
                                                        player.PlayerId);
                       
                        if (task.IsFaulted || task.IsCanceled)
                        {
                            Program.CanceledFaultProcessing($"DBConnection.UpdateCurrentPlayers Put {player.PlayerId}", task.Exception, Settings.Instance.IgnoreFaults);
                            if (Settings.Instance.IgnoreFaults && !task.IsCanceled)
                            {
                                task.Exception?.Handle(e => true);
                            }
                            else
                                return false;
                        }

                        return true;
                    },
                    cancellationToken,
                    TaskContinuationOptions.AttachedToParent | TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
            }

            this.PlayerProgression.Decrement("Player");

            if (updateHistory)
            {
                await this.UpdateHistory(player,
                                            player.History,
                                            cancellationToken);
            }

            if (Logger.Instance.IsDebugEnabled)
                Logger.Instance.DebugFormat("DBConnection.UpdateCurrentPlayers Run End Player: {0}",
                                                player.PlayerId);

            return;
        }


        /// <summary>
        /// This will only update the Session, Fin, Game sections based on the bool argument.
        /// The Metrics is always updated
        /// </summary>
        /// <param name="player"></param>
        /// <param name="updateFin"></param>
        /// <param name="updateGame"></param>
        /// <param name="updateWagerResult"><</param>
        /// <param name="updateSession"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task UpdateChangedCurrentPlayer(Player player,
                                                        CancellationToken cancellationToken,
                                                        bool updateSession = false,
                                                        int updateFin = 0,
                                                        bool updateGame = false,
                                                        int updateWagerResult = 0)
        {
            if (Logger.Instance.IsDebugEnabled)
                Logger.Instance.DebugFormat("DBConnection.UpdateChangedCurrentPlayer Run Start Player: {0} updateSession: {1}, updateFin {2}, updateGame: {3}, updateLastNbrTrans: {4}",
                                                player.PlayerId,
                                                updateSession,
                                                updateFin,
                                                updateGame,
                                                updateWagerResult);

            cancellationToken.ThrowIfCancellationRequested();

            this.PlayerProgression.Increment("Player", $"Transforming/Putting Players Components {player.UserName}...");


            async Task CallDBPut(Key key, IEnumerable<Bin> updateBins)
            {
                var stopWatch = Stopwatch.StartNew();

                await this.Connection.Put(WritePolicy,
                                                    cancellationToken,
                                                    key,
                                                    updateBins.ToArray())
                    .ContinueWith(task =>
                    {
                        stopWatch.StopRecord(PutTag,
                                                SystemTag,
                                                key.setName,
                                                nameof(UpdateChangedCurrentPlayer),
                                                key.userKey.Object);

                        if (Logger.Instance.IsDebugEnabled)
                        {
                            Logger.Instance.DebugFormat("DBConnection.UpdateChangedCurrentPlayer Run End Put {0} Elapsed Time (ms): {1}",
                                                        key,
                                                        stopWatch.ElapsedMilliseconds);
                        }

                        if (stopWatch.ElapsedMilliseconds > Settings.Instance.WarnMaxMSLatencyDBExceeded)
                            Logger.Instance.WarnFormat("DBConnection.UpdateChangedCurrentPlayer Run Exceeded Latency Threshold for Put {1}. Latency: {0}",
                                                        stopWatch.ElapsedMilliseconds,
                                                        key);
                        
                        if (task.IsFaulted || task.IsCanceled)
                        {
                            Program.CanceledFaultProcessing($"DBConnection.UpdateChangedCurrentPlayer Put {player.PlayerId}", task.Exception, Settings.Instance.IgnoreFaults);
                            if (Settings.Instance.IgnoreFaults && !task.IsCanceled)
                            {
                                task.Exception?.Handle(e => true);
                            }
                            else
                                return false;
                        }

                        return true;
                    },
                    cancellationToken,
                    TaskContinuationOptions.AttachedToParent | TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
            }

            async Task CallDBInsertList(Key key,
                                    string binName,
                                    IEnumerable<object> updateList,
                                    int keepSize = 0)
            {
                var stopWatch = Stopwatch.StartNew();

                var record = await this.Connection
                                        .Operate(this.WritePolicy,
                                                    cancellationToken,
                                                    key,
                                                    ListOperation.InsertItems(this.ListPolicy,
                                                                                binName,
                                                                                0,
                                                                                updateList.Reverse().ToList()))
                                        .ContinueWith(task =>
                                        {
                                            stopWatch.StopRecord(OperationTag,
                                                                    SystemTag,
                                                                    key.setName,
                                                                    nameof(UpdateChangedCurrentPlayer),
                                                                    key.userKey.Object);

                                            if (Logger.Instance.IsDebugEnabled)
                                            {
                                                Logger.Instance.DebugFormat("DBConnection.UpdateChangedCurrentPlayer Run End List {0} Elapsed Time (ms): {1}",
                                                                            key,
                                                                            stopWatch.ElapsedMilliseconds);
                                            }

                                            if (stopWatch.ElapsedMilliseconds > Settings.Instance.WarnMaxMSLatencyDBExceeded)
                                                Logger.Instance.WarnFormat("DBConnection.UpdateChangedCurrentPlayer Run Exceeded Latency Threshold for List {1}. Latency: {0}",
                                                                            stopWatch.ElapsedMilliseconds,
                                                                            key);
                                            
                                            if (task.IsFaulted || task.IsCanceled)
                                            {
                                                Program.CanceledFaultProcessing($"DBConnection.UpdateChangedCurrentPlayer List {player.PlayerId}", task.Exception, Settings.Instance.IgnoreFaults);
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

                if (keepSize > 0 && record.bins?.ContainsKey(binName) == true)
                {
                    var lstCnt = (long)record.bins[binName];

                    if (lstCnt > keepSize)
                    {
                        var remove = (int)(lstCnt - keepSize);

                        await this.Connection.Operate(this.WritePolicy,
                                                        cancellationToken,
                                                        key,
                                                        ListOperation.RemoveByIndexRange(binName,
                                                                                            remove * -1,
                                                                                            remove,
                                                                                            ListReturnType.NONE))
                            .ContinueWith(task =>
                            {
                                stopWatch.StopRecord(OperationTag,
                                                        SystemTag,
                                                        key.setName,
                                                        nameof(UpdateChangedCurrentPlayer),
                                                        key.userKey.Object);

                                if (Logger.Instance.IsDebugEnabled)
                                {
                                    Logger.Instance.DebugFormat("DBConnection.UpdateChangedCurrentPlayer Remove List {0} Elapsed Time (ms): {1}",
                                                                key,
                                                                stopWatch.ElapsedMilliseconds);
                                }

                                if (stopWatch.ElapsedMilliseconds > Settings.Instance.WarnMaxMSLatencyDBExceeded)
                                    Logger.Instance.WarnFormat("DBConnection.UpdateChangedCurrentPlayer Run Exceeded Latency Threshold for Remove List {1}. Latency: {0}",
                                                                stopWatch.ElapsedMilliseconds,
                                                                key);

                                if (task.IsFaulted || task.IsCanceled)
                                {
                                    Program.CanceledFaultProcessing($"DBConnection.UpdateChangedCurrentPlayer Remove List {player.PlayerId}", task.Exception, Settings.Instance.IgnoreFaults);
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
                    }
                }
            }

            #region Player Bins Update
            var binstoUpdate = new List<Bin>();

            if (updateSession)
            {
                if (Logger.Instance.IsDebugEnabled)
                    Logger.Instance.DebugFormat("DBConnection.UpdateChangedCurrentPlayer Transform Session Player: {0}",
                                                    player.PlayerId);

                var stopWatch = Stopwatch.StartNew();
                var sessionTransformed = DBHelpers.TransForm(player.Session);

                binstoUpdate.Add(new Bin("Session", sessionTransformed));

                stopWatch.StopRecord(TransformTag,
                                        SystemTag,
                                        this.CurrentPlayersSet.SetName,
                                        nameof(UpdateChangedCurrentPlayer),
                                        player.PlayerId);
            }

            if (updateFin < 0 && player.FinTransactions.Any())
            {
                if (Logger.Instance.IsDebugEnabled)
                    Logger.Instance.DebugFormat("DBConnection.UpdateChangedCurrentPlayer Transform Fin Player: {0}",
                                                    player.PlayerId);

                var stopWatch = Stopwatch.StartNew();
                var finTransformed = player.FinTransactions
                                            .Select(x => DBHelpers.TransForm(x)).ToList();
                stopWatch.StopRecord(TransformTag,
                                        SystemTag,
                                        this.CurrentPlayersSet.SetName,
                                        nameof(UpdateChangedCurrentPlayer),
                                        player.PlayerId);

                binstoUpdate.Add(new Bin("FinTransactions", new Value.ListValue(finTransformed)));
            }

            if (updateGame)
            {
                if (Logger.Instance.IsDebugEnabled)
                    Logger.Instance.DebugFormat("DBConnection.UpdateChangedCurrentPlayer Transform Game Player: {0}",
                                                    player.PlayerId);

                var stopWatch = Stopwatch.StartNew();
                var gameTransformed = DBHelpers.TransForm(player.Game);

                binstoUpdate.Add(new Bin("Game", gameTransformed));

                stopWatch.StopRecord(TransformTag,
                                        SystemTag,
                                        this.CurrentPlayersSet.SetName,
                                        nameof(UpdateChangedCurrentPlayer),
                                        player.PlayerId);
            }

            if (updateWagerResult < 0 && player.WagersResults.Any())
            {
                /*if (Logger.Instance.IsDebugEnabled)
                    Logger.Instance.DebugFormat("DBConnection.UpdateChangedCurrentPlayer Transform WagersResults-List Player: {0}",
                                                    player.PlayerId);

                var wagerresultsTransformed = player.WagersResults
                                                .Select(x => DBHelpers.TransForm(x)).ToList();

                wagerresultsTransformed.Reverse();

                binstoUpdate.Add(new Bin("WagersResults", new Value.ListValue(wagerresultsTransformed)));
                */
                throw new NotSupportedException("Cannot support dumping of all player trans");
            }

            {
                if (Logger.Instance.IsDebugEnabled)
                    Logger.Instance.DebugFormat("DBConnection.UpdateChangedCurrentPlayer Transform Metrics Player: {0}",
                                                    player.PlayerId);

                var stopWatch = Stopwatch.StartNew();
                var metricsTransformed = DBHelpers.TransForm(player.Metrics);
                binstoUpdate.Add(new Bin("Metrics", metricsTransformed));

                binstoUpdate.Add(new Bin("ActiveSession", player.ActiveSession));
                binstoUpdate.Add(new Bin("BingeFlag", player.BingeFlag));
                binstoUpdate.Add(new Bin("Archived", player.Archived));

                stopWatch.StopRecord(TransformTag,
                                        SystemTag,
                                        this.CurrentPlayersSet.SetName,
                                        nameof(UpdateChangedCurrentPlayer),
                                        player.PlayerId);
            }

            if (!this.CurrentPlayersSet.IsEmpty())
                await CallDBPut(new Key(this.CurrentPlayersSet.Namespace, this.CurrentPlayersSet.SetName, player.PlayerId),
                                    binstoUpdate);

            #endregion

            #region List Updates
            if (updateFin > 0 && player.FinTransactions.Any() && !this.CurrentPlayersSet.IsEmpty())
            {
                if (Logger.Instance.IsDebugEnabled)
                    Logger.Instance.DebugFormat("DBConnection.UpdateChangedCurrentPlayer Transform Fin-List Player: {0}",
                                                    player.PlayerId);

                var stopWatch = Stopwatch.StartNew();
                var finTransformed = player.FinTransactions.Take(updateFin)
                                        .Select(x => DBHelpers.TransForm(x));

                stopWatch.StopRecord(TransformTag,
                                        SystemTag,
                                        this.CurrentPlayersSet.SetName,
                                        nameof(UpdateChangedCurrentPlayer),
                                        player.PlayerId);

                await CallDBInsertList(new Key(this.CurrentPlayersSet.Namespace, this.CurrentPlayersSet.SetName, player.PlayerId),
                                            "FinTransactions",
                                            finTransformed,
                                            SettingsSim.Instance.Config.KeepNbrFinTransActions);
            }

            if (updateWagerResult > 0 && player.WagersResults.Any())
            {
                if (Logger.Instance.IsDebugEnabled)
                    Logger.Instance.DebugFormat("DBConnection.UpdateChangedCurrentPlayer Transform WagerResults-List Player: {0}",
                                                    player.PlayerId);

                var stopWatch = Stopwatch.StartNew();
                var wagers = player.WagersResults.TakeLast(updateWagerResult);
                var wagerresultTransformed = wagers.Select(x => DBHelpers.TransForm(x));

                stopWatch.StopRecord(TransformTag,
                                        SystemTag,
                                        this.CurrentPlayersSet.SetName,
                                        nameof(UpdateChangedCurrentPlayer),
                                        player.PlayerId);

                if (!this.CurrentPlayersSet.IsEmpty())
                {
                    await CallDBInsertList(new Key(this.CurrentPlayersSet.Namespace, this.CurrentPlayersSet.SetName, player.PlayerId),
                                                "WagersResults",
                                                wagerresultTransformed,
                                                SettingsSim.Instance.Config.KeepNbrWagerResultTransActions);
                }

                if (wagers.Last().Type != WagerResultTransaction.Types.Wager)
                {
                    var pkey = wagers.Last().Id;

                    if (Logger.Instance.IsDebugEnabled)
                        Logger.Instance.DebugFormat("DBConnection.UpdateChangedCurrentPlayer Transform WagerResults-History-List Player: {0} TransId: {1}",
                                                        player.PlayerId,
                                                        pkey);

                    {
                        var playerSnapshot = new Player(player);

                        stopWatch.Restart();
                        var transform = DBHelpers.TransForm(playerSnapshot);

                        stopWatch.StopRecord(TransformTag,
                                        SystemTag,
                                        this.PlayersHistorySet.SetName,
                                        nameof(UpdateChangedCurrentPlayer),
                                        player.PlayerId);

                        if (!this.PlayersTransHistorySet.IsEmpty())
                        {
                            await CallDBPut(new Key(this.PlayersTransHistorySet.Namespace, this.PlayersTransHistorySet.SetName, pkey),
                                            new Bin[]
                                            {
                                            new Bin("Transaction", transform),
                                            new Bin("PlayerId", player.PlayerId),
                                            new Bin("SessionId", player.Session.Id)
                                            });
                        }
                    }
                    if (!this.PlayersHistorySet.IsEmpty())
                    {
                        await CallDBInsertList(new Key(this.PlayersHistorySet.Namespace, this.PlayersHistorySet.SetName, player.PlayerId),
                                                "PlayerTrans",
                                                new object[] { pkey },
                                                SettingsSim.Instance.Config.PlayerHistoryLastNbrTrans);
                    }
                }

            }

            if (Logger.Instance.IsDebugEnabled)
                Logger.Instance.DebugFormat("DBConnection.UpdateChangedCurrentPlayer Run Start Transform Player: {0}",
                                                player.PlayerId);

            #endregion

            this.PlayerProgression.Decrement("Player");

            if (Logger.Instance.IsDebugEnabled)
                Logger.Instance.DebugFormat("DBConnection.UpdateChangedCurrentPlayer Run End Player: {0}",
                                                player.PlayerId);
            return;
        }

        public volatile bool skipLogMsgBypassingHistoryTrans;

        public async Task UpdateHistory(Player forPlayer,
                                            IEnumerable<Player> history,
                                            CancellationToken cancellationToken)
        {
            var forPlayerId = forPlayer.PlayerId;

            if (Logger.Instance.IsDebugEnabled)
            {
                Logger.Instance.Debug("DBConnection.UpdateHistory(Trans) Start");
                Logger.Instance.DebugFormat("\tPlayer: {0} History: {1}",
                                            forPlayerId,
                                            history.Count());
                Logger.Instance.DebugFormat("\tUsed Threads: {0} Completion Ports: {1}",
                                                Connection.GetClusterStats().threadsInUse,
                                                Connection.GetClusterStats().completionPortsInUse);
            }

            Guid currentPlayerId = Guid.Empty;

            var transactionsForPlayer = new List<long>();

            foreach (var player in history)
            {
                cancellationToken.ThrowIfCancellationRequested();

                this.HistoryProgression.Increment("HistoryTrans", $"Putting Player History Tran {player.PlayerId}...");

                if (Logger.Instance.IsDebugEnabled)
                    Logger.Instance.Debug("DBConnection.UpdateHistory(Trans) Run ForEach Start Transform");

                var transId = player.WagersResults.Last().Id;

                var stopWatch = Stopwatch.StartNew();
                var expando = DBHelpers.TransForm(player);

                stopWatch.StopRecord(TransformTag,
                                        SystemTag,
                                        this.PlayersTransHistorySet.SetName,
                                        nameof(UpdateHistory),
                                        player.PlayerId);

                if (Logger.Instance.IsDebugEnabled)
                    Logger.Instance.DebugFormat("DBConnection.UpdateHistory(Trans) Run ForEach End Transform TransId: {0}",
                                                transId);

                //Update History Transactions
                if (!PlayersTransHistorySet.IsEmpty())
                {
                    stopWatch.Restart();

                    await this.Connection.Put(this.WritePolicy,
                                                    cancellationToken,
                                                    new Key(this.PlayersTransHistorySet.Namespace,
                                                                this.PlayersTransHistorySet.SetName,
                                                                transId),
                                                    new Bin("Transaction", expando),
                                                    new Bin("PlayerId", player.PlayerId),
                                                    new Bin("SessionId", player.Session.Id))
                        .ContinueWith(task =>
                        {
                            stopWatch.StopRecord(PutTag,
                                                    SystemTag,
                                                    this.PlayersTransHistorySet.SetName,
                                                    nameof(UpdateHistory),
                                                    player.PlayerId);

                            if (Logger.Instance.IsDebugEnabled)
                            {
                                Logger.Instance.DebugFormat("DBConnection.UpdateHistory(Player) Run End Put {0} Elapsed Time (ms): {1}",
                                                            transId,
                                                            stopWatch.ElapsedMilliseconds);
                            }

                            if (stopWatch.ElapsedMilliseconds > Settings.Instance.WarnMaxMSLatencyDBExceeded)
                                Logger.Instance.WarnFormat("DBConnection.UpdateHistory(Player) Run Exceeded Latency Threshold for Put {2}-{1}. Latency: {0}",
                                                            stopWatch.ElapsedMilliseconds,
                                                            transId,
                                                            forPlayerId);
                            
                            if (task.IsFaulted || task.IsCanceled)
                            {
                                Program.CanceledFaultProcessing($"DBConnection.UpdateHistory(Player) Put {transId}", task.Exception, Settings.Instance.IgnoreFaults);
                                if (Settings.Instance.IgnoreFaults && !task.IsCanceled)
                                {
                                    task.Exception?.Handle(e => true);
                                }
                                else
                                    return false;
                            }

                            return true;
                        },
                        cancellationToken,
                        TaskContinuationOptions.AttachedToParent | TaskContinuationOptions.ExecuteSynchronously,
                        TaskScheduler.Default);
                }

                if (!transactionsForPlayer.Contains(transId))
                {
                    if (Logger.Instance.IsDebugEnabled)
                        Logger.Instance.DebugFormat("DBConnection.UpdateHistory(Trans) Run ForEach Add TransId {0}",
                                                        transId);

                    transactionsForPlayer.Add(transId);
                }

                this.HistoryProgression.Decrement("HistoryTrans");

                Logger.Instance.Debug("DBConnection.UpdateHistory(Trans) Run End ForEach");

                this.HistoryProgression.Decrement("HistoryTrans");
            }

            this.HistoryProgression.Increment("HistoryTrans", $"Putting History {forPlayerId}...");

            if (SettingsSim.Instance.Config.PlayerHistoryLastNbrTrans != 0)
            {
                Logger.Instance.Debug("DBConnection.UpdateHistory(Player) Start");

                transactionsForPlayer.Reverse();

                if (SettingsSim.Instance.Config.PlayerHistoryLastNbrTrans > 0
                        && transactionsForPlayer.Count > SettingsSim.Instance.Config.PlayerHistoryLastNbrTrans)
                    transactionsForPlayer = transactionsForPlayer.GetRange(0, SettingsSim.Instance.Config.PlayerHistoryLastNbrTrans);

                //Update Player history
                if (!PlayersHistorySet.IsEmpty())
                {
                    var stopWatch = Stopwatch.StartNew();

                    await this.Connection.Put(this.WritePolicy,
                                                    cancellationToken,
                                                    new Key(this.PlayersHistorySet.Namespace,
                                                                this.PlayersHistorySet.SetName,
                                                                forPlayerId),
                                                    new Bin("PlayerTrans", transactionsForPlayer),
                                                    new Bin("State", forPlayer.State),
                                                    new Bin("County", forPlayer.County))
                        .ContinueWith(task =>
                        {
                            stopWatch.StopRecord(PutTag,
                                                    SystemTag,
                                                    this.PlayersTransHistorySet.SetName,
                                                    nameof(UpdateHistory),
                                                    forPlayerId);

                            if (Logger.Instance.IsDebugEnabled)
                            {
                                Logger.Instance.DebugFormat("DBConnection.UpdateHistory(Player) Run End Put {0} Elapsed Time (ms): {1}",
                                                            forPlayerId,
                                                            stopWatch.ElapsedMilliseconds);
                            }

                            if (stopWatch.ElapsedMilliseconds > Settings.Instance.WarnMaxMSLatencyDBExceeded)
                                Logger.Instance.WarnFormat("DBConnection.UpdateHistory(Player) Run Exceeded Latency Threshold for Put {1}. Latency: {0}",
                                                            stopWatch.ElapsedMilliseconds,
                                                            forPlayerId);                            

                            if (task.IsFaulted || task.IsCanceled)
                            {
                                Program.CanceledFaultProcessing($"DBConnection.UpdateHistory(Player) Put {forPlayerId}", task.Exception, Settings.Instance.IgnoreFaults);
                                if (Settings.Instance.IgnoreFaults && !task.IsCanceled)
                                {
                                    task.Exception?.Handle(e => true);
                                }
                                else
                                    return false;
                            }

                            return true;
                        },
                        cancellationToken,
                        TaskContinuationOptions.AttachedToParent | TaskContinuationOptions.ExecuteSynchronously,
                        TaskScheduler.Default);
                }

                if (Logger.Instance.IsDebugEnabled)
                {
                    Logger.Instance.DebugFormat("DBConnection.UpdateHistory(Player) End Trans Ids: {0}", transactionsForPlayer.Count);
                }
            }

            if (Logger.Instance.IsDebugEnabled)
            {
                Logger.Instance.Info("DBConnection.UpdateHistory(Trans) End");
                Logger.Instance.DebugFormat("\tUsed Threads: {0} Completion Ports: {1}",
                                                       Connection.GetClusterStats().threadsInUse,
                                                       Connection.GetClusterStats().completionPortsInUse);
            }

            this.HistoryProgression.Decrement("HistoryTrans");

            return;
        }

        readonly Expression incrEmailCnt = Exp.Build(Exp.Cond(Exp.BinExists("count"), Exp.Add(Exp.IntBin("count"), Exp.Val(1)),
                                                                Exp.Val(1)));

        public async Task<string> DeterineEmail(string firstName, string lastName, string domain, CancellationToken token)
        {
            var email = $"{firstName}.{lastName}@{domain}";

            if (UsedEmailCntSet.IsEmpty()) return email;

            if (Logger.Instance.IsDebugEnabled)
                Logger.Instance.DebugFormat("DBConnection.DeterineEmail Start {0}", email);

            var emailProg = new Progression(this.ConsoleProgression, email);

            var key = new Key(this.UsedEmailCntSet.Namespace, this.UsedEmailCntSet.SetName, email);

            var stopWatch = Stopwatch.StartNew();

            var record = await this.Connection.Operate(this.WritePolicy,
                                                        token,
                                                        key,
                                                        ExpOperation.Write("count", incrEmailCnt, ExpWriteFlags.DEFAULT),
                                                        Operation.Get("count"))
                                .ContinueWith(task =>
                                {
                                    stopWatch.StopRecord(OperationTag,
                                                            SystemTag,
                                                            key.setName,
                                                            nameof(DeterineEmail),
                                                            email);

                                    if (Logger.Instance.IsDebugEnabled)
                                    {
                                        Logger.Instance.DebugFormat("DBConnection.DeterineEmail Run End Operation {0} Elapsed Time (ms): {1}",
                                                                    email,
                                                                    stopWatch.ElapsedMilliseconds);
                                    }

                                    if (stopWatch.ElapsedMilliseconds > Settings.Instance.WarnMaxMSLatencyDBExceeded)
                                        Logger.Instance.WarnFormat("DBConnection.DeterineEmail Run Exceeded Latency Threshold for Operation {0}. Latency: {1}",
                                                                    email,
                                                                    stopWatch.ElapsedMilliseconds);


                                    if (task.IsFaulted || task.IsCanceled)
                                    {
                                        Program.CanceledFaultProcessing($"DBConnection.DeterineEmail Operation {email}", task.Exception, Settings.Instance.IgnoreFaults);
                                        if (Settings.Instance.IgnoreFaults && !task.IsCanceled)
                                        {
                                            task.Exception?.Handle(e => true);
                                            return null;
                                        }
                                    }

                                    return task.Result;
                                },
                                    token,
                                    TaskContinuationOptions.AttachedToParent | TaskContinuationOptions.ExecuteSynchronously,
                                    TaskScheduler.Default);

            var count = ((IEnumerable<Object>)record?.bins["count"]).ElementAt(1) ?? -1;

            email = $"{firstName}.{lastName}{count}@{domain}";

            if (Logger.Instance.IsDebugEnabled)
                Logger.Instance.DebugFormat("DBConnection.DeterineEmail End Exists {0}", email);

            emailProg.Decrement();

            return email;
        }

        public async Task IncrementGlobalSet(GlobalIncrement glbIncr,
                                                CancellationToken token)
        {
            if (GlobalIncrementSet.IsEmpty()) return;

            if (Logger.Instance.IsDebugEnabled)
                Logger.Instance.DebugFormat("DBConnection.IncrementGlobalSet Start {0}", glbIncr.Key);

            var incrProg = new Progression(this.ConsoleProgression, $"Incrementing Global Set {glbIncr.Key}");

            var stopWatch = Stopwatch.StartNew();

            var key = new Key(this.GlobalIncrementSet.Namespace, this.GlobalIncrementSet.SetName, glbIncr.Key);

            var ggr_amount = (double)decimal.Round(glbIncr.GGR, 2);
            var ggr_amountExp = Exp.Build(Exp.Cond(Exp.BinExists("ggr_amount"),
                                                            Exp.Add(Exp.FloatBin("ggr_amount"),
                                                                    Exp.Val(ggr_amount)),
                                                    Exp.Val(ggr_amount)));

            var interventionsExp = Exp.Build(Exp.Cond(Exp.BinExists("interventions"),
                                                            Exp.Add(Exp.IntBin("interventions"),
                                                                        Exp.Val(glbIncr.Interventions)),
                                                            Exp.Val(glbIncr.Interventions)));

            var trn_countExp = Exp.Build(Exp.Cond(Exp.BinExists("trn_count"),
                                                                    Exp.Add(Exp.IntBin("trn_count"),
                                                                                Exp.Val(glbIncr.Transactions)),
                                                                    Exp.Val(glbIncr.Transactions)));

            await this.Connection.Operate(this.WritePolicy,
                                            token,
                                            key,
                                            ExpOperation.Write("ggr_amount", ggr_amountExp, ExpWriteFlags.DEFAULT),
                                            ExpOperation.Write("interventions", interventionsExp, ExpWriteFlags.DEFAULT),
                                            ExpOperation.Write("trn_count", trn_countExp, ExpWriteFlags.DEFAULT),
                                            ExpOperation.Write("process_time", Exp.Build(Exp.Val(glbIncr.IntervalTimeStamp.ToString(Settings.Instance.TimeStampFormatString))), ExpWriteFlags.DEFAULT),
                                            ExpOperation.Write("process_unixts", Exp.Build(Exp.Val(glbIncr.IntervalUnixSecs)), ExpWriteFlags.DEFAULT),
                                            ExpOperation.Write("state_code", Exp.Build(Exp.Val(glbIncr.State)), ExpWriteFlags.DEFAULT),
                                            ExpOperation.Write("county_code", Exp.Build(Exp.Val(glbIncr.CountyCode)), ExpWriteFlags.DEFAULT),
                                            ExpOperation.Write("county_name", Exp.Build(Exp.Val(glbIncr.County)), ExpWriteFlags.DEFAULT),
                                            ExpOperation.Write("state_name", Exp.Build(Exp.Val(glbIncr.StateName)), ExpWriteFlags.DEFAULT))
                .ContinueWith(task =>
                {
                    stopWatch.StopRecord(OperationTag,
                                            SystemTag,
                                            key.setName,
                                            nameof(IncrementGlobalSet),
                                            glbIncr.Key);

                    if (Logger.Instance.IsDebugEnabled)
                    {
                        Logger.Instance.DebugFormat("DBConnection.IncrementGlobalSet Run End Operation {0} Elapsed Time (ms): {1}",
                                                    glbIncr.Key,
                                                    stopWatch.ElapsedMilliseconds);
                    }

                    if (stopWatch.ElapsedMilliseconds > Settings.Instance.WarnMaxMSLatencyDBExceeded)
                        Logger.Instance.WarnFormat("DBConnection.IncrementGlobalSet Run Exceeded Latency Threshold for Operation {0}. Latency: {1}",
                                                    glbIncr.Key,
                                                    stopWatch.ElapsedMilliseconds);


                    if (task.IsFaulted || task.IsCanceled)
                    {
                        Program.CanceledFaultProcessing($"DBConnection.IncrementGlobalSet Operation {glbIncr.Key}", task.Exception, Settings.Instance.IgnoreFaults);
                        if (Settings.Instance.IgnoreFaults && !task.IsCanceled)
                        {
                            task.Exception?.Handle(e => true);
                        }
                        else
                            return false;
                    }

                    return true;
                },
                    token,
                    TaskContinuationOptions.AttachedToParent | TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);

            if (Logger.Instance.IsDebugEnabled)
                Logger.Instance.DebugFormat("DBConnection.IncrementGlobalSet End Exists {0}", glbIncr.Key);
        }

        public async Task UpdateIntervention(Intervention intervention,
                                                CancellationToken cancellationToken)
        {
            if (Logger.Instance.IsDebugEnabled)
                Logger.Instance.DebugFormat("DBConnection.UpdateIntervention Run Start Player: {0}",
                                                intervention.PlayerId);

            cancellationToken.ThrowIfCancellationRequested();

            this.PlayerProgression.Increment("Intervention", $"Transforming/Putting...");

            if (Logger.Instance.IsDebugEnabled)
                Logger.Instance.DebugFormat("DBConnection.UpdateIntervention Run Start Transform Player: {0}",
                                                intervention.PlayerId);

            var stopWatch = Stopwatch.StartNew();
            var propDict = DBHelpers.TransForm(intervention);

            stopWatch.StopRecord(TransformTag,
                                    SystemTag,
                                    this.InterventionSet.SetName,
                                    nameof(UpdateIntervention),
                                    intervention.PlayerId);

            if (Logger.Instance.IsDebugEnabled)
                Logger.Instance.DebugFormat("DBConnection.UpdateIntervention Run End Transform Player: {0} Dict Cnt: {1}",
                                                intervention.PlayerId,
                                                propDict.Count);

            if (!this.InterventionSet.IsEmpty())
            {
                stopWatch.Restart();

                await this.Connection.Put(WritePolicy,
                                                    cancellationToken,
                                                    new Key(this.InterventionSet.Namespace,
                                                                this.InterventionSet.SetName,
                                                                intervention.PrimaryKey),
                                                    DBHelpers.CreateBinRecord(propDict))
                    .ContinueWith(task =>
                    {
                        stopWatch.StopRecord(PutTag,
                                                SystemTag,
                                                this.InterventionSet.SetName,
                                                nameof(UpdateIntervention),
                                                intervention.PlayerId);

                        if (Logger.Instance.IsDebugEnabled)
                        {
                            Logger.Instance.DebugFormat("DBConnection.UpdateIntervention Run End Put {0} Elapsed Time (ms): {1}",
                                                        intervention.PlayerId,
                                                        stopWatch.ElapsedMilliseconds);
                        }

                        if (stopWatch.ElapsedMilliseconds > Settings.Instance.WarnMaxMSLatencyDBExceeded)
                            Logger.Instance.WarnFormat("DBConnection.UpdateIntervention Run Exceeded Latency Threshold for Put {1}. Latency: {0}",
                                                        stopWatch.ElapsedMilliseconds,
                                                        intervention.PlayerId);

                        if (task.IsFaulted || task.IsCanceled)
                        {
                            Program.CanceledFaultProcessing($"DBConnection.UpdateIntervention Put {intervention.PlayerId}", task.Exception, Settings.Instance.IgnoreFaults);
                            if (Settings.Instance.IgnoreFaults && !task.IsCanceled)
                            {
                                task.Exception?.Handle(e => true);
                            }
                            else
                                return false;
                        }

                        return true;
                    },
                    cancellationToken,
                    TaskContinuationOptions.AttachedToParent | TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
            }

            this.PlayerProgression.Decrement("Intervention");

            Logger.Instance.Debug("DBConnection.UpdateIntervention Run End");

            return;
        }
        
        public async Task UpdateLiveWager(Player player,
                                            WagerResultTransaction wagerResult,
                                            WagerResultTransaction wager,
                                            CancellationToken cancellationToken)
        {
            if (Logger.Instance.IsDebugEnabled)
                Logger.Instance.DebugFormat("DBConnection.UpdateLiveWager Run Start Player: {0}",
                                                player.PlayerId);

            cancellationToken.ThrowIfCancellationRequested();

            this.PlayerProgression.Increment("UpdateLiveWager", $"Transforming/Putting...");

            if (!this.LiverWagerSet.IsEmpty())
            {
                var stopWatch = Stopwatch.StartNew();

                var ts = wagerResult.Timestamp.ToString(Settings.Instance.TimeStampFormatString);

                var tsWoZone = wagerResult.Timestamp
                                            .Round(SettingsSim.Instance.Config.GlobalIncremenIntervals, MidpointRounding.ToZero)
                                            .UtcDateTime
                                            .ToString(Settings.Instance.TimeZoneFormatWoZone);
                var pk = Helpers.GetLongHash(Environment.CurrentManagedThreadId);

                await this.Connection.Put(WritePolicy,
                                                    cancellationToken,
                                                    new Key(this.LiverWagerSet.Namespace,
                                                                this.LiverWagerSet.SetName,
                                                                pk),
                                                    new Bin("Aggkey", $"{player.PlayerId}:{tsWoZone}:{wager.Amount}"),
                                                    new Bin("bet_type", wagerResult.BetType),
                                                    new Bin("PlayerId", player.PlayerId),
                                                    new Bin("result_type", wagerResult.Type.ToString()),
                                                    new Bin("risk_score", wagerResult.RiskScore),
                                                    new Bin("stake_amount", (double)wager.Amount),
                                                    new Bin("txn_ts", ts),
                                                    new Bin("txn_unixts", wagerResult.Timestamp.ToUnixTimeSeconds()),
                                                    new Bin("win_amount",
                                                                wagerResult.Type == WagerResultTransaction.Types.Win
                                                                    ? (double)wagerResult.Amount
                                                                    : 0d),
                                                    new Bin("TransId", wagerResult.Id))
                    .ContinueWith(task =>
                    {
                        stopWatch.StopRecord(PutTag,
                                                SystemTag,
                                                this.LiverWagerSet.SetName,
                                                nameof(UpdateLiveWager),
                                                pk.ToString());

                        if (Logger.Instance.IsDebugEnabled)
                        {
                            Logger.Instance.DebugFormat("DBConnection.UpdateLiveWager Run End Put {0} Elapsed Time (ms): {1}",
                                                        player.PlayerId,
                                                        stopWatch.ElapsedMilliseconds);
                        }

                        if (stopWatch.ElapsedMilliseconds > Settings.Instance.WarnMaxMSLatencyDBExceeded)
                            Logger.Instance.WarnFormat("DBConnection.UpdateLiveWager Run Exceeded Latency Threshold for Put {1}. Latency: {0}",
                                                        stopWatch.ElapsedMilliseconds,
                                                        player.PlayerId);

                        if (task.IsFaulted || task.IsCanceled)
                        {
                            Program.CanceledFaultProcessing($"DBConnection.UpdateLiveWager Put {player.PlayerId}", task.Exception, Settings.Instance.IgnoreFaults);
                            if (Settings.Instance.IgnoreFaults && !task.IsCanceled)
                            {
                                task.Exception?.Handle(e => true);
                            }
                            else
                                return false;
                        }

                        return true;
                    },
                    cancellationToken,
                    TaskContinuationOptions.AttachedToParent | TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
            }

            this.PlayerProgression.Decrement("UpdateLiveWager");

            Logger.Instance.Debug("DBConnection.UpdateLiveWager Run End");

            return;
        }


        public async Task<InterventionThresholds> ReFreshInterventionThresholds(InterventionThresholds interventionThresholds,
                                                                                CancellationToken cancellationToken)
        {
            Record record = null;

            if (Logger.Instance.IsDebugEnabled)
                Logger.Instance.DebugFormat("DBConnection.ReFreshInterventionThresholds Run Start Current Version: {0}, Last Refresh Time: {1:HH:mm:ss.ffff}",
                                                interventionThresholds?.Version ?? -1,
                                                interventionThresholds?.RefreshedTime ?? DateTime.MinValue);

            cancellationToken.ThrowIfCancellationRequested();

            this.PlayerProgression.Increment("ReFresh InterventionThresholds", $"Checking...");

            if (!this.InterventionThresholdsSet.IsEmpty())
            {
                var stopWatch = Stopwatch.StartNew();

                record = await this.Connection.Get(this.ReadPolicy,
                                                    cancellationToken,
                                                    new Key(this.InterventionThresholdsSet.Namespace,
                                                                this.InterventionThresholdsSet.SetName,
                                                                interventionThresholds?.Version ?? 0))
                                .ContinueWith(task =>
                                {
                                    stopWatch.StopRecord(GetTag,
                                                            SystemTag,
                                                            this.InterventionThresholdsSet.SetName,
                                                            nameof(ReFreshInterventionThresholds),
                                                            interventionThresholds?.Version ?? 0);

                                    if (Logger.Instance.IsDebugEnabled)
                                    {
                                        Logger.Instance.DebugFormat("DBConnection.ReFreshInterventionThresholds Run End Get {0} Elapsed Time (ms): {1}",
                                                                    interventionThresholds?.Version ?? -1,
                                                                    stopWatch.ElapsedMilliseconds);
                                    }

                                    if (stopWatch.ElapsedMilliseconds > Settings.Instance.WarnMaxMSLatencyDBExceeded)
                                        Logger.Instance.WarnFormat("DBConnection.ReFreshInterventionThresholds Run Exceeded Latency Threshold for Get {1}. Latency: {0}",
                                                                    stopWatch.ElapsedMilliseconds,
                                                                    interventionThresholds?.Version ?? 0);

                                    if (task.IsFaulted || task.IsCanceled)
                                    {
                                        Program.CanceledFaultProcessing($"DBConnection.interventionThresholds Get {interventionThresholds?.Version ?? -1}", task.Exception, Settings.Instance.IgnoreFaults);
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
            }

            InterventionThresholds instance;

            if (record is null || record.bins.Count == 0)
                instance = null;
            else
                instance = new InterventionThresholds(record.bins);

            this.PlayerProgression.Decrement("ReFresh InterventionThresholds");

            if (Logger.Instance.IsDebugEnabled)
                Logger.Instance.DebugFormat("DBConnection.ReFreshInterventionThresholds Run End with returned version: {0}; Bins: {1}",
                                                record == null ? -2 : interventionThresholds?.Version ?? -1,
                                                record?.bins.Count);
            return instance;
        }


        private static int ITUpdating = 0;
        private static long ITUpdateCnt = 0;

        public async Task<bool> InterventionThresholdsRefreshCheck(InterventionThresholds current,
                                                                    CancellationToken token,
                                                                    bool forceRefresh = false)
        {
            if (this.InterventionThresholdsSet.IsEmpty()) return false;

            bool result = false;
            
            if (ITUpdating == 0
                    && (forceRefresh
                            || current == null
                            || current.NextRefreshTime <= DateTime.Now))
            {
                Interlocked.MemoryBarrier();

                if (Interlocked.Exchange(ref ITUpdating, 1) == 1) return false;

                try
                {
                    var newInstance = await this.ReFreshInterventionThresholds(current, token);

                    if (newInstance is not null)
                    {
                        Interlocked.Exchange(ref InterventionThresholds.Instance, newInstance);
                        var incCnt = Interlocked.Increment(ref ITUpdateCnt);

                        if (Logger.Instance.IsDebugEnabled)
                            Logger.Instance.DebugFormat("InterventionThresholds.RefreshCheck updated {6} from Version: {0} ({1:HH\\:mm\\:ss.ffff} - {2:HH\\:mm\\:ss.ffff}) to {3} ({4:HH\\:mm\\:ss.ffff} - {5:HH\\:mm\\:ss.ffff})",
                                                            current?.Version ?? -1,
                                                            current?.RefreshedTime ?? DateTime.MinValue,
                                                            current?.NextRefreshTime ?? DateTime.MinValue,
                                                            InterventionThresholds.Instance.Version,
                                                            InterventionThresholds.Instance.RefreshedTime,
                                                            InterventionThresholds.Instance.NextRefreshTime,
                                                            incCnt);
                    }
                }
                finally
                {
                    Interlocked.Exchange(ref ITUpdating, 0);
                }
            }

            return result;
        }

    }
}
