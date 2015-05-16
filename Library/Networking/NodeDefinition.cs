using System;
using System.Collections.Generic;
using System.Net;

namespace Library.Networking
{
    /// <summary>
    /// Represents the definition of a node.
    /// </summary>
    public class NodeDefinition : IComparable, IComparer<NodeDefinition>
    {
        /// <summary>
        /// The full connection name of the node.
        /// </summary>
        private readonly string _connectionName;

        /// <summary>
        /// The hostname of the node.
        /// </summary>
        private readonly string _hostname;

        /// <summary>
        /// The port of the node.
        /// </summary>
        private readonly int _port;

        /// <summary>
        /// Initializes a new instance of the <see cref="NodeDefinition"/> class.
        /// </summary>
        /// <param name="hostname">The hostname of the node.</param>
        /// <param name="port">The port of the node.</param>
        public NodeDefinition(string hostname, int port)
        {
            _hostname = hostname.Equals("localhost", StringComparison.InvariantCultureIgnoreCase) ? Dns.GetHostName() : hostname;
            _port = port;
            _connectionName = Hostname + ":" + Port;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NodeDefinition"/> class. Parses the connection name into the hostname and port parts.
        /// </summary>
        /// <param name="connectionName">The connection name of the node.</param>
        /// <exception cref="ArgumentException">Thrown when the connection name cannot be split into a hostname and a port.</exception>
        public NodeDefinition(string connectionName)
        {
            string[] parts = connectionName.Split(':');
            if (parts.Length != 2)
            {
                throw new ArgumentException("Connection name is not in the correct format.", connectionName);
            }

            _hostname = parts[0].Equals("localhost", StringComparison.InvariantCultureIgnoreCase) ? Dns.GetHostName() : parts[0];
            int tempPort;
            if (int.TryParse(parts[1], out tempPort))
            {
                _port = tempPort;
            }
            else
            {
                throw new ArgumentException("Connection name is not in the correct format.", connectionName);
            }

            _connectionName = Hostname + ":" + Port;
        }

        /// <summary>
        /// Gets the full connection name of the node.
        /// </summary>
        public string ConnectionName
        {
            get { return _connectionName; }
        }

        /// <summary>
        /// Gets the hostname of the node.
        /// </summary>
        public string Hostname
        {
            get { return _hostname; }
        }

        /// <summary>
        /// Gets the port of the node.
        /// </summary>
        public int Port
        {
            get { return _port; }
        }

        /// <inheritdoc />
        public int Compare(NodeDefinition x, NodeDefinition y)
        {
            return string.Compare(x._connectionName, y._connectionName, StringComparison.Ordinal);
        }

        /// <inheritdoc />
        public int CompareTo(object obj)
        {
            if (obj == null)
            {
                return 1;
            }

            NodeDefinition node = obj as NodeDefinition;
            if (node != null)
            {
                return string.Compare(_connectionName, node._connectionName, StringComparison.Ordinal);
            }

            throw new ArgumentException("Object is not a NodeDefinition");
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            NodeDefinition node = obj as NodeDefinition;
            return node != null && Equals(node._connectionName, _connectionName);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return _connectionName.GetHashCode();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return _connectionName;
        }
    }
}