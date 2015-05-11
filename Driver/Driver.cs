using Library.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Driver
{
    /// <summary>
    /// Represents the main interface to the database.
    /// </summary>
    public class Driver : IDisposable
    {
        /// <summary>
        /// The network to connect through.
        /// </summary>
        private readonly Network _network;

        /// <summary>
        /// A list of the nodes in the connection string.
        /// </summary>
        private readonly List<NodeDefinition> _nodes;

        /// <summary>
        /// The reconnection thread.
        /// </summary>
        private readonly Thread _reconnectionThread;

        /// <summary>
        /// A value indicating whether the object has already been _disposed.
        /// </summary>
        private bool _disposed = false;

        /// <summary>
        /// A value indicating whether the driver is running.
        /// </summary>
        private bool _running;

        /// <summary>
        /// Initializes a new instance of the <see cref="Driver"/> class.
        /// </summary>
        /// <param name="connectionString">The initial connection string.</param>
        public Driver(string connectionString)
        {
            _nodes = connectionString.Split(',').Select(e => new NodeDefinition(e)).ToList();

            _running = true;
            _network = new Network();

            foreach (var item in _nodes)
            {
                _network.Connect(item);
            }

            _reconnectionThread = new Thread(RunReconnection);
            _reconnectionThread.Start();
        }

        /// <summary>
        /// Releases all resources used by the current instance of the <see cref="Network"/> class.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Releases all resources used by the current instance of the <see cref="Network"/> class.
        /// </summary>
        /// <param name="disposing">Whether to dispose of managed resources or not.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _network.Dispose();
                    _disposed = true;
                }
            }
        }

        /// <summary>
        /// The reconnection thread run method.
        /// </summary>
        private void RunReconnection()
        {
            while (_running)
            {
                Thread.Sleep(5000);

                foreach (var item in _nodes)
                {
                    if (!_network.IsConnected(item))
                    {
                        _network.Connect(item);
                    }
                }
            }
        }

        /// <summary>
        /// Shuts down the driver.
        /// </summary>
        private void Shutdown()
        {
            _running = false;
            _reconnectionThread.Join();
            _network.Shutdown();
        }
    }
}