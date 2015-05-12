using Library;
using Library.Data;
using Library.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace DatabaseV2
{
    /// <summary>
    /// Represents a database network.
    /// </summary>
    public class DatabaseNetwork : Network
    {
        /// <summary>
        /// The list of nodes to try to connect to.
        /// </summary>
        private readonly List<NodeDefinition> _nodes;

        /// <summary>
        /// The reconnection thread.
        /// </summary>
        private readonly Thread _reconnectionThread;

        /// <summary>
        /// The current node.
        /// </summary>
        private readonly DatabaseNetworkNode _self;

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseNetwork"/> class.
        /// </summary>
        /// <param name="port">The port to listen on.</param>
        /// <param name="nodes">The nodes to try to connect to.</param>
        public DatabaseNetwork(int port, List<NodeDefinition> nodes)
            : base(port)
        {
            byte[] randomBytes = new byte[4];
            new Random().NextBytes(randomBytes);
            _self = new DatabaseNetworkNode(new NodeDefinition("localhost", Port), BitConverter.ToUInt32(randomBytes, 0));

            AttemptConnect(nodes);
            _nodes = new List<NodeDefinition>(nodes);

            _reconnectionThread = new Thread(RunReconnection);
            _reconnectionThread.Start();
        }

        /// <inheritdoc />
        public override void Shutdown()
        {
            base.Shutdown();
            if (!_reconnectionThread.Join(5000))
            {
                _reconnectionThread.Abort();
            }
        }

        /// <inheritdoc />
        protected override bool HandleMessage(Message message)
        {
            if (message.MessageType == "NeighborListRequest")
            {
                Document data = new Document
                {
                    { "Neighbors", GetOutgoingConnectedNodes().Select(node => new DocumentEntry(node.ConnectionName)).ToList() }
                };
                SendMessage(new Message(message, "NeighborListResponse", data, false));
            }

            return base.HandleMessage(message);
        }

        /// <summary>
        /// Attempts to connect to a list of nodes.
        /// </summary>
        /// <param name="nodes">The list of nodes to connect to.</param>
        private void AttemptConnect(List<NodeDefinition> nodes)
        {
            foreach (var n in nodes.Except(GetOutgoingConnectedNodes()))
            {
                Connect(n);
            }
        }

        /// <summary>
        /// Runs the reconnection thread.
        /// </summary>
        private void RunReconnection()
        {
            ThreadHelper.ResponsiveSleep(5000, () => Running);
            while (Running)
            {
                AttemptConnect(_nodes);

                foreach (var node in GetOutgoingConnectedNodes())
                {
                    Message message = new Message(node, "NeighborListRequest", new Document(), true);
                    message.BlockUntilDone();
                    if (message.Success)
                    {
                        AttemptConnect(message.Response.Data["Neighbors"].ValueAsArray().Select(item => new NodeDefinition(item.ValueAsString())).ToList());
                    }
                }

                ThreadHelper.ResponsiveSleep(5000, () => Running);
            }
        }

        /// <summary>
        /// A class representing a database network node.
        /// </summary>
        private class DatabaseNetworkNode
        {
            /// <summary>
            /// The id of the node.
            /// </summary>
            private readonly uint _id;

            /// <summary>
            /// The node definition.
            /// </summary>
            private readonly NodeDefinition _node;

            /// <summary>
            /// Initializes a new instance of the <see cref="DatabaseNetworkNode"/> class.
            /// </summary>
            /// <param name="node">The node definition.</param>
            /// <param name="id">The id of the node.</param>
            public DatabaseNetworkNode(NodeDefinition node, uint id)
            {
                _node = node;
                _id = id;
            }

            /// <summary>
            /// Gets the id of the node.
            /// </summary>
            public uint Id
            {
                get { return _id; }
            }

            /// <summary>
            /// Gets the node definition.
            /// </summary>
            public NodeDefinition Node
            {
                get { return _node; }
            }

            /// <inheritdoc />
            public override bool Equals(object obj)
            {
                DatabaseNetworkNode node = obj as DatabaseNetworkNode;
                return node != null && Equals(node.Node, _node) && Equals(node.Id, _id);
            }

            /// <inheritdoc />
            public override int GetHashCode()
            {
                return Node.GetHashCode() * _id.GetHashCode();
            }

            /// <inheritdoc />
            public override string ToString()
            {
                return _node + " " + _id;
            }
        }
    }
}