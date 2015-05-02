using System;
using System.Collections.Generic;
using System.Linq;

namespace DatabaseV2
{
    /// <summary>
    /// Represents settings for the database to use.
    /// </summary>
    public class Settings
    {
        /// <summary>
        /// Gets or sets the nodes to initially connect to.
        /// </summary>
        public List<Node> Nodes { get; set; }

        /// <summary>
        /// Gets or sets the port to run the database on.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Parses a string into a list of nodes.
        /// </summary>
        /// <param name="nodeList">The string to parse.</param>
        public void ParseNodeList(string nodeList)
        {
            List<Node> nodes = new List<Node>();
            var parts = nodeList.Split(',');
            try
            {
                nodes.AddRange(parts.Select(t => new Node(t)));
                Nodes = nodes;
            }
            catch (ArgumentException)
            {
                Console.WriteLine("Connection string is not in the right format.");
            }
        }
    }
}