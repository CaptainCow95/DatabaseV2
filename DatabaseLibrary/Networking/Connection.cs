using System;
using System.Net.Sockets;

namespace DatabaseLibrary.Networking
{
    /// <summary>
    /// Represents a network connection to another node.
    /// </summary>
    internal class Connection
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Connection"/> class.
        /// </summary>
        /// <param name="client">The <see cref="TcpClient"/> representing the connection.</param>
        /// <param name="type">The type of connection.</param>
        public Connection(TcpClient client, ConnectionType type)
        {
            Client = client;
            LastActiveTime = DateTime.UtcNow;
            Status = ConnectionStatus.Identifying;
            Type = type;
        }

        /// <summary>
        /// Gets the <see cref="TcpClient"/> representing the connection.
        /// </summary>
        public TcpClient Client { get; private set; }

        /// <summary>
        /// Gets the last time this connection was active.
        /// </summary>
        public DateTime LastActiveTime { get; private set; }

        /// <summary>
        /// Gets the current status of the connection.
        /// </summary>
        public ConnectionStatus Status { get; private set; }

        /// <summary>
        /// Gets the type of the connection.
        /// </summary>
        public ConnectionType Type { get; private set; }

        /// <summary>
        /// Updates the last active time of the connection.
        /// </summary>
        public void Active()
        {
            LastActiveTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Marks the connection as established.
        /// </summary>
        public void ConnectionEstablished()
        {
            Status = ConnectionStatus.Connected;
        }

        /// <summary>
        /// Marks the connection as disconnected.
        /// </summary>
        public void Disconnected()
        {
            Status = ConnectionStatus.Disconnected;
        }
    }
}