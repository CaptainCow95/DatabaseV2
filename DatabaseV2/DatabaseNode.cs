using Library.Logging;
using Library.Networking;
using System;

namespace DatabaseV2
{
    /// <summary>
    /// Represents a node in the database.
    /// </summary>
    public class DatabaseNode : IDisposable
    {
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
        /// A value indicating whether the object has already been _disposed.
        /// </summary>
        private bool _disposed = false;

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
            _webInterface = new WebInterface(_network);

            if (_settings.EnableWebInterface)
            {
                _webInterface.Enable(_settings.Port + 1);
            }

            foreach (var n in _settings.Nodes)
            {
                _network.Connect(n);
            }
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
                    _disposed = true;
                }
            }
        }
    }
}