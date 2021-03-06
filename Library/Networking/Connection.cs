﻿using System.Net.Sockets;

namespace Library.Networking
{
    /// <summary>
    /// Represents a network connection to another node.
    /// </summary>
    public class Connection
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Connection"/> class.
        /// </summary>
        /// <param name="client">The <see cref="TcpClient"/> representing the connection.</param>
        public Connection(TcpClient client)
        {
            Client = client;
            Status = ConnectionStatus.Verifying;
        }

        /// <summary>
        /// Gets the <see cref="TcpClient"/> representing the connection.
        /// </summary>
        public TcpClient Client { get; private set; }

        /// <summary>
        /// Gets the current status of the connection.
        /// </summary>
        public ConnectionStatus Status { get; private set; }

        /// <summary>
        /// Marks the connection as established.
        /// </summary>
        public void ConnectionEstablished()
        {
            Status = ConnectionStatus.Connected;
        }
    }
}