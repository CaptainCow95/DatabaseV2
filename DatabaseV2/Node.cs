using System;
using System.Net;

namespace DatabaseV2
{
    /// <summary>
    /// Represents a node's connection information.
    /// </summary>
    public class Node
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Node"/> class.
        /// </summary>
        /// <param name="hostname">The hostname of the node.</param>
        /// <param name="port">The port of the node.</param>
        public Node(string hostname, int port)
        {
            Hostname = hostname;
            Port = port;
            ConnectionName = Hostname + ':' + Port;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Node"/> class. Parses the connection name into the hostname and port parts.
        /// </summary>
        /// <param name="connectionName">The connection name of the node.</param>
        /// <exception cref="ArgumentException">Thrown when the connection name cannot be split into a hostname and a port.</exception>
        public Node(string connectionName)
        {
            string[] parts = connectionName.Split(':');
            if (parts.Length != 2)
            {
                throw new ArgumentException("Connection name is not in the correct format.", connectionName);
            }

            Hostname = parts[0];
            int tempPort;
            if (int.TryParse(parts[1], out tempPort))
            {
                Port = tempPort;
            }
            else
            {
                throw new ArgumentException("Connection name is not in the correct format.", connectionName);
            }

            ConnectionName = connectionName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Node"/> class. Assumes that the node is being created to refer to the localhost.
        /// </summary>
        /// <param name="port">The port of the node.</param>
        public Node(int port)
        {
            Hostname = Dns.GetHostName();
            Port = port;
        }

        /// <summary>
        /// Gets the connection name of the node.
        /// </summary>
        public string ConnectionName { get; private set; }

        /// <summary>
        /// Gets the hostname of the node.
        /// </summary>
        public string Hostname { get; private set; }

        /// <summary>
        /// Gets the port of the node.
        /// </summary>
        public int Port { get; private set; }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (obj is Node)
            {
                return ConnectionName.Equals(((Node)obj).ConnectionName);
            }

            return false;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return ConnectionName.GetHashCode();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return ConnectionName;
        }
    }
}