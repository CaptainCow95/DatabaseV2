using DatabaseV2.Networking;
using System.Collections.Generic;

namespace DatabaseV2
{
    /// <summary>
    /// Represents settings for the database to use.
    /// </summary>
    public class Settings
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Settings"/> class.
        /// </summary>
        /// <param name="port">The port to run the database on.</param>
        /// <param name="nodes">The nodes to initially connect to.</param>
        /// <param name="enableWebInterface">A value indicating whether the web interface should be enabled.</param>
        public Settings(int port, List<NodeDefinition> nodes, bool enableWebInterface)
        {
            EnableWebInterface = enableWebInterface;
            Port = port;
            Nodes = nodes;
        }

        /// <summary>
        /// Gets a value indicating whether the web interface should be enabled.
        /// </summary>
        public bool EnableWebInterface { get; private set; }

        /// <summary>
        /// Gets the nodes to initially connect to.
        /// </summary>
        public List<NodeDefinition> Nodes { get; private set; }

        /// <summary>
        /// Gets the port to run the database on.
        /// </summary>
        public int Port { get; private set; }
    }
}