using Library.Networking;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Driver
{
    /// <summary>
    /// Represents the main interface to the database.
    /// </summary>
    public class Driver
    {
        /// <summary>
        /// The network to connect through.
        /// </summary>
        private Network _network;

        /// <summary>
        /// A list of the nodes in the connection string.
        /// </summary>
        private List<NodeDefinition> _nodes;

        /// <summary>
        /// The reconnection thread.
        /// </summary>
        private Thread _reconnectionThread;

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