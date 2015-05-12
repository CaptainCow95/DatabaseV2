using Library;
using Library.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace DatabaseV2
{
    public class DatabaseNetworkv2 : Network
    {
        private readonly List<NodeDefinition> _nodes;
        private readonly Thread _reconnectionThread;
        private readonly DatabaseNetworkNode _self;

        public DatabaseNetworkv2(int port, List<NodeDefinition> nodes)
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

        public override void Shutdown()
        {
            base.Shutdown();
            if (!_reconnectionThread.Join(5000))
            {
                _reconnectionThread.Abort();
            }
        }

        private void AttemptConnect(List<NodeDefinition> nodes)
        {
            foreach (var n in nodes.Except(GetOutgoingConnectedNodes()))
            {
                Connect(n);
            }
        }

        private void RunReconnection()
        {
            ThreadHelper.ResponsiveSleep(5000, () => Running);
            while (Running)
            {
                AttemptConnect(_nodes);

                ThreadHelper.ResponsiveSleep(5000, () => Running);
            }
        }

        private class DatabaseNetworkNode
        {
            private readonly uint _id;
            private readonly NodeDefinition _node;

            public DatabaseNetworkNode(NodeDefinition node, uint id)
            {
                _node = node;
                _id = id;
            }

            public uint Id
            {
                get { return _id; }
            }

            public NodeDefinition Node
            {
                get { return _node; }
            }

            public override bool Equals(object obj)
            {
                DatabaseNetworkNode node = obj as DatabaseNetworkNode;
                return node != null && Equals(node.Node, _node) && Equals(node.Id, _id);
            }

            public override int GetHashCode()
            {
                return Node.GetHashCode() * _id.GetHashCode();
            }

            public override string ToString()
            {
                return _node + " " + _id;
            }
        }
    }
}