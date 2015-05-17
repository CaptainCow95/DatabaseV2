using DatabaseV2.Database;
using Library;
using Library.Data;
using Library.Logging;
using Library.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace DatabaseV2
{
    /// <summary>
    /// Represents a controller node in the database.
    /// </summary>
    public class ControllerNode : IDisposable
    {
        /// <summary>
        /// The amount of time to add for each additional controller node.
        /// </summary>
        private const int CandidateTimePerControllerNode = 5;

        /// <summary>
        /// The lock to use when handling leader data.
        /// </summary>
        private readonly ReaderWriterLockSlim _leaderLock = new ReaderWriterLockSlim();

        /// <summary>
        /// The thread running the maintenance jobs.
        /// </summary>
        private readonly Thread _maintenanceThread;

        /// <summary>
        /// The network to use as a backend.
        /// </summary>
        private readonly Network _network;

        /// <summary>
        /// A random number generator.
        /// </summary>
        private readonly Random _random = new Random();

        /// <summary>
        /// The node definition of the current node.
        /// </summary>
        private readonly NodeDefinition _self;

        /// <summary>
        /// The settings of the database.
        /// </summary>
        private readonly Settings _settings;

        /// <summary>
        /// The web interface.
        /// </summary>
        private readonly WebInterface _webInterface;

        /// <summary>
        /// The current election term.
        /// </summary>
        private long _currentTerm;

        /// <summary>
        /// The controller's database lookup table.
        /// </summary>
        private DatabaseLookup _databaseLookupTable;

        /// <summary>
        /// A value indicating whether the object has already been _disposed.
        /// </summary>
        private bool _disposed = false;

        /// <summary>
        /// A value indicating whether the current node is the leader.
        /// </summary>
        private bool _isLeader = false;

        /// <summary>
        /// The current leader.
        /// </summary>
        private NodeDefinition _leader = null;

        /// <summary>
        /// The next time the node can try to elect itself leader.
        /// </summary>
        private DateTime _leaderCandidateTime = DateTime.UtcNow;

        /// <summary>
        /// Whether the node has voted this term.
        /// </summary>
        private bool _votedThisTerm = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="ControllerNode"/> class.
        /// </summary>
        /// <param name="settings">The settings to use.</param>
        public ControllerNode(Settings settings)
        {
            Logger.Log("Starting up as a controller node.", LogLevel.Info);
            _settings = settings;
            _self = new NodeDefinition("localhost", _settings.Port);
            _databaseLookupTable = new DatabaseLookup(_self);

            Logger.Init(_settings.LogLocation, _settings.LogLevel);
            _network = new Network(_settings.Port);
            _network.Disconnection += Network_Disconnection;
            _network.MessageReceived += Network_MessageReceived;
            _webInterface = new WebInterface(_network);

            if (_settings.EnableWebInterface)
            {
                _webInterface.Enable(_settings.Port + 1);
            }

            foreach (var n in _settings.Nodes.Except(new[] { _self }))
            {
                _network.Connect(n);
            }

            _maintenanceThread = new Thread(RunMaintenance);
            _maintenanceThread.Start();
        }

        /// <summary>
        /// Gets the current leader, null if there is none.
        /// </summary>
        public NodeDefinition Leader
        {
            get { return _leader; }
        }

        /// <summary>
        /// Releases all resources used by the current instance of the <see cref="DatabaseNode"/> class.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Enters a run loop.
        /// </summary>
        public void Run()
        {
            while (Console.ReadLine() != "exit")
            {
            }

            Shutdown();
        }

        /// <summary>
        /// Shuts down the node.
        /// </summary>
        public void Shutdown()
        {
            _network.Shutdown();
            _webInterface.Disable();

            if (!_maintenanceThread.Join(5000))
            {
                _maintenanceThread.Abort();
            }

            Logger.Shutdown();
        }

        /// <summary>
        /// Releases all resources used by the current instance of the <see cref="DatabaseNode"/> class.
        /// </summary>
        /// <param name="disposing">Whether to dispose of managed resources or not.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _network.Dispose();
                    _webInterface.Dispose();
                    _leaderLock.Dispose();
                    _databaseLookupTable.Dispose();
                    _disposed = true;
                }
            }
        }

        /// <summary>
        /// Tries to elect itself as leader.
        /// </summary>
        private void InitiateLeaderVote()
        {
            Logger.Log("Attempting to become leader.", LogLevel.Debug);
            _leaderLock.EnterWriteLock();
            _currentTerm += 1;
            _votedThisTerm = true;
            long term = _currentTerm;
            _leaderLock.ExitWriteLock();

            Document initiateVoteData = new Document
            {
                { "CurrentTerm", term }
            };
            List<Message> messages = new List<Message>();
            foreach (var n in _settings.Nodes.Except(new[] { _self }))
            {
                Message m = new Message(n, "InitiateLeaderVote", initiateVoteData, true);
                _network.SendMessage(m);
                messages.Add(m);
            }

            messages.ForEach(e => e.BlockUntilDone());

            int yesVotes = 1;
            foreach (var m in messages)
            {
                if (m.Success)
                {
                    if (m.Response.Data["Vote"].ValueAsString() == "Yes")
                    {
                        ++yesVotes;
                    }
                    else if (m.Response.Data["Vote"].ValueAsString() == "No")
                    {
                        _leaderLock.EnterWriteLock();

                        if (_currentTerm < m.Response.Data["CurrentTerm"].ValueAsInt64())
                        {
                            _currentTerm = m.Response.Data["CurrentTerm"].ValueAsInt64();
                            _votedThisTerm = false;
                        }

                        ResetLeaderCandidateTime();

                        _leaderLock.ExitWriteLock();

                        Logger.Log("Attempt to become leader failed, newer term exists.", LogLevel.Debug);
                        return;
                    }
                }
            }

            _leaderLock.EnterReadLock();

            if (term == _currentTerm && yesVotes >= (_settings.Nodes.Count / 2) + 1)
            {
                _isLeader = true;
                _leader = _self;
                Document newLeaderData = new Document
                {
                    { "Leader", _self.ConnectionName },
                    { "CurrentTerm", term }
                };
                foreach (var n in _network.GetConnectedNodes())
                {
                    _network.SendMessage(new Message(n, "NewLeader", newLeaderData, false));
                }

                Logger.Log("Electing self as leader.", LogLevel.Info);
            }
            else
            {
                ResetLeaderCandidateTime();
                Logger.Log("Attempt to become leader failed, a majority was not achieved.", LogLevel.Debug);
            }

            _leaderLock.ExitReadLock();
        }

        /// <summary>
        /// Handles any disconnections from the network.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event arguments.</param>
        private void Network_Disconnection(object sender, DisconnectionEventArgs e)
        {
            _leaderLock.EnterWriteLock();

            if (Equals(_leader, e.Node))
            {
                _leader = null;
                ResetLeaderCandidateTime();
                Logger.Log("Lost connection to leader.", LogLevel.Info);
            }

            // Less than half of the nodes are up, there is no longer a leader
            if (_isLeader && _network.GetConnectedNodes().Intersect(_settings.Nodes).Count() < (_settings.Nodes.Count / 2))
            {
                _isLeader = false;
                _leader = null;
                ResetLeaderCandidateTime();
                Logger.Log("Too few nodes to maintain leadership, resetting.", LogLevel.Info);

                Document newLeaderData = new Document
                {
                    { "Leader", string.Empty }
                };
                foreach (var n in _network.GetConnectedNodes())
                {
                    _network.SendMessage(new Message(n, "NewLeader", newLeaderData, false));
                }
            }

            _leaderLock.ExitWriteLock();
        }

        /// <summary>
        /// Handles any messages received by the network.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event arguments.</param>
        private void Network_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            if (e.Message.MessageType == "InitiateLeaderVote")
            {
                _leaderLock.EnterWriteLock();

                Document responseData;
                if (_currentTerm < e.Message.Data["CurrentTerm"].ValueAsInt64() || (_currentTerm == e.Message.Data["CurrentTerm"].ValueAsInt64() && !_votedThisTerm))
                {
                    responseData = new Document
                        {
                            { "Vote", "Yes" }
                        };
                    _votedThisTerm = true;
                    _currentTerm = e.Message.Data["CurrentTerm"].ValueAsInt64();
                    Logger.Log("Voting yes to the leader attempt from " + e.Message.Address + ".", LogLevel.Debug);
                }
                else if (_currentTerm == e.Message.Data["CurrentTerm"].ValueAsInt64())
                {
                    responseData = new Document
                        {
                            { "Vote", "No" },
                            { "CurrentTerm", _currentTerm }
                        };
                    Logger.Log("Voting no to leader attempt from " + e.Message.Address + " because I have already voted this term.", LogLevel.Debug);
                }
                else
                {
                    responseData = new Document
                        {
                            { "Vote", "No" },
                            { "CurrentTerm", _currentTerm }
                        };
                    Logger.Log("Voting no to leader attempt from " + e.Message.Address + " because I have already voted for a future term.", LogLevel.Debug);
                }

                _network.SendMessage(new Message(e.Message, "LeaderVoteResponse", responseData, false));

                _leaderLock.ExitWriteLock();
            }
            else if (e.Message.MessageType == "NewLeader")
            {
                _leaderLock.EnterWriteLock();

                if (e.Message.Data["Leader"].ValueAsString() == string.Empty)
                {
                    _leader = null;
                    _isLeader = false;
                    Logger.Log("The previous leader has stepped down.", LogLevel.Info);
                }
                else if (_currentTerm == e.Message.Data["CurrentTerm"].ValueAsInt64())
                {
                    _leader = new NodeDefinition(e.Message.Data["Leader"].ValueAsString());
                    _isLeader = false;
                    Logger.Log(_leader.ConnectionName + " elected as leader.", LogLevel.Info);
                }

                _leaderLock.ExitWriteLock();
            }
            else if (e.Message.MessageType == "LeaderRequest")
            {
                _leaderLock.EnterReadLock();
                Document leaderData = new Document
                {
                    { "Leader", _leader == null ? string.Empty : _leader.ConnectionName },
                    { "CurrentTerm", _currentTerm }
                };
                _leaderLock.ExitReadLock();

                _network.SendMessage(new Message(e.Message, "LeaderResponse", leaderData, false));
            }
        }

        /// <summary>
        /// Resets the time until this node can try to elect itself leader.
        /// </summary>
        private void ResetLeaderCandidateTime()
        {
            _leaderCandidateTime = DateTime.UtcNow + TimeSpan.FromSeconds(_random.Next(CandidateTimePerControllerNode, CandidateTimePerControllerNode * _settings.Nodes.Count));
        }

        /// <summary>
        /// Runs the node's maintenance jobs.
        /// </summary>
        private void RunMaintenance()
        {
            ThreadHelper.ResponsiveSleep(5000, () => _network.Running);
            while (_network.Running)
            {
                _leaderLock.EnterReadLock();
                if (_leader == null && DateTime.UtcNow > _leaderCandidateTime)
                {
                    _leaderLock.ExitReadLock();
                    InitiateLeaderVote();
                }
                else
                {
                    _leaderLock.ExitReadLock();
                }

                ThreadHelper.ResponsiveSleep(1000, () => _network.Running);
            }
        }
    }
}