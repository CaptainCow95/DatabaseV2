using Library;
using Library.Data;
using Library.Logging;
using Library.Networking;
using System;
using System.Threading;

namespace DatabaseV2
{
    /// <summary>
    /// Represents a node in the database.
    /// </summary>
    public class DatabaseNode : IDisposable
    {
        /// <summary>
        /// The lock when dealing with leader data.
        /// </summary>
        private readonly ReaderWriterLockSlim _leaderLock = new ReaderWriterLockSlim();

        /// <summary>
        /// The network to use as a backend.
        /// </summary>
        private readonly Network _network;

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
        /// A value indicating whether the object has already been _disposed.
        /// </summary>
        private bool _disposed = false;

        /// <summary>
        /// The current leader.
        /// </summary>
        private NodeDefinition _leader = null;

        /// <summary>
        /// The maintenance thread.
        /// </summary>
        private Thread _maintenanceThread;

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseNode"/> class.
        /// </summary>
        /// <param name="settings">The settings to use.</param>
        public DatabaseNode(Settings settings)
        {
            Logger.Log("Starting up as a database node.", LogLevel.Info);
            _settings = settings;

            Logger.Init(_settings.LogLocation, _settings.LogLevel);
            _network = new Network(_settings.Port);
            _network.Disconnection += Network_Disconnection;
            _network.MessageReceived += Network_MessageReceived;
            _webInterface = new WebInterface(_network);

            if (_settings.EnableWebInterface)
            {
                _webInterface.Enable(_settings.Port + 1);
            }

            foreach (var n in _settings.Nodes)
            {
                _network.Connect(n);
            }

            _maintenanceThread = new Thread(RunMaintenance);
            _maintenanceThread.Start();
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
                    _disposed = true;
                }
            }
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
                Logger.Log("Lost connection to leader.", LogLevel.Info);
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
            if (e.Message.MessageType == "NewLeader")
            {
                _leaderLock.EnterWriteLock();

                if (e.Message.Data["Leader"].ValueAsString() == string.Empty)
                {
                    _leader = null;
                    Logger.Log("The previous leader has stepped down.", LogLevel.Info);
                }
                else if (_currentTerm <= e.Message.Data["CurrentTerm"].ValueAsInt64())
                {
                    _leader = new NodeDefinition(e.Message.Data["Leader"].ValueAsString());
                    _currentTerm = e.Message.Data["CurrentTerm"].ValueAsInt64();
                    Logger.Log(_leader.ConnectionName + " elected as leader.", LogLevel.Info);
                }

                _leaderLock.ExitWriteLock();
            }
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
                bool haveLeader = _leader != null;
                _leaderLock.ExitReadLock();

                if (!haveLeader)
                {
                    foreach (var n in _settings.Nodes)
                    {
                        Message m = new Message(n, "LeaderRequest", new Document(), true);
                        _network.SendMessage(m);
                        m.BlockUntilDone();
                        if (m.Success && m.Response.Data["Leader"].ValueAsString() != string.Empty)
                        {
                            _leaderLock.EnterWriteLock();

                            if (_leader == null)
                            {
                                _leader = new NodeDefinition(m.Response.Data["Leader"].ValueAsString());
                                _currentTerm = m.Response.Data["CurrentTerm"].ValueAsInt64();
                                Logger.Log(_leader.ConnectionName + " elected as leader.", LogLevel.Info);
                            }

                            _leaderLock.ExitWriteLock();
                            break;
                        }
                    }
                }

                ThreadHelper.ResponsiveSleep(5000, () => _network.Running);
            }
        }
    }
}