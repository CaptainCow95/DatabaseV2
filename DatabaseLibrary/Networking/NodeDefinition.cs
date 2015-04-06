using System;
using System.Net;

namespace DatabaseLibrary.Networking
{
    /// <summary>
    /// Represents the definition of a node.
    /// </summary>
    public class NodeDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NodeDefinition"/> class.
        /// </summary>
        /// <param name="hostname">The hostname of the node.</param>
        /// <param name="port">The port of the node.</param>
        public NodeDefinition(string hostname, int port)
        {
            Hostname = hostname.Equals("localhost", StringComparison.InvariantCultureIgnoreCase) ? Dns.GetHostName() : hostname;
            Port = port;
            ConnectionName = Hostname + ":" + Port;
        }

        /// <summary>
        /// Gets the full connection name of the node.
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
            NodeDefinition node = obj as NodeDefinition;
            return node != null && Equals(node.ConnectionName, ConnectionName);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return ConnectionName.GetHashCode();
        }
    }
}