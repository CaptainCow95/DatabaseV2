using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace DatabaseV2.Networking
{
    /// <summary>
    /// Represents a chord network.
    /// </summary>
    public class ChordNetwork : Network
    {
        /// <summary>
        /// The object to lock on when editing the finger table or the predecessor.
        /// </summary>
        private readonly ReaderWriterLockSlim _chordLock = new ReaderWriterLockSlim();

        /// <summary>
        /// The chord finger table.
        /// </summary>
        private readonly ChordNode[] _fingerTable = new ChordNode[32];

        /// <summary>
        /// The current chord node.
        /// </summary>
        private readonly ChordNode _self;

        /// <summary>
        /// The thread running the stabilization runtime.
        /// </summary>
        private readonly Thread _stabilizationThread;

        /// <summary>
        /// The next finger to update during stabilization.
        /// </summary>
        private int _nextFingerNode = 1;

        /// <summary>
        /// The predecessor chord node.
        /// </summary>
        private ChordNode _predecessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChordNetwork"/> class.
        /// </summary>
        /// <param name="port">The port to listen for connections on.</param>
        /// <param name="nodes">The nodes to try to connect to the network of.</param>
        public ChordNetwork(int port, List<NodeDefinition> nodes)
            : base(port)
        {
            byte[] randomBytes = new byte[4];
            new Random().NextBytes(randomBytes);
            uint chordId = BitConverter.ToUInt32(randomBytes, 0);

            _self = new ChordNode(new NodeDefinition("localhost", Port), chordId);
            _fingerTable[0] = _self;

            foreach (var node in nodes)
            {
                if (Connect(node))
                {
                    Message message = new Message(node, "ChordSuccessorRequest", new Document(), true);
                    SendMessage(message);
                    message.BlockUntilDone();
                    if (message.Success && message.Response.MessageType == "ChordSuccessorResponse")
                    {
                        var data = message.Response.Data;

                        // TODO: Find a way to make this an actual uint.
                        _fingerTable[0] = new ChordNode(new NodeDefinition(data["Successor"].ValueAsString), (uint)data["ChordId"].ValueAsInteger);
                        if (!Equals(_fingerTable[0], _self) && !Connect(_fingerTable[0].Node))
                        {
                            _fingerTable[0] = _self;
                        }
                    }
                }
            }

            _stabilizationThread = new Thread(Stabilize);
            _stabilizationThread.Start();
        }

        /// <summary>
        /// Gets the node's finger table.
        /// </summary>
        public IEnumerable<NodeDefinition> FingerTable
        {
            get { return _fingerTable.Select(e => e == null ? null : e.Node); }
        }

        /// <summary>
        /// Gets the node's predecessor in the chord ring.
        /// </summary>
        public NodeDefinition Predecessor
        {
            get { return _predecessor == null ? null : _predecessor.Node; }
        }

        /// <inheritdoc />
        public override void Shutdown()
        {
            base.Shutdown();

            if (!_stabilizationThread.Join(5000))
            {
                _stabilizationThread.Abort();
            }
        }

        /// <inheritdoc />
        protected override void Disconnection(NodeDefinition node)
        {
            _chordLock.EnterWriteLock();

            if (Equals(_predecessor.Node, node))
            {
                _predecessor = null;
            }

            if (Equals(_fingerTable[0].Node, node))
            {
                _fingerTable[0] = _self;
            }

            for (int i = 1; i < 32; ++i)
            {
                if (Equals(_fingerTable[i].Node, node))
                {
                    _fingerTable[i] = null;
                }
            }

            _chordLock.ExitWriteLock();
        }

        /// <inheritdoc />
        protected override void HandleMessage(Message message)
        {
            if (message.MessageType == "ChordSuccessorRequest")
            {
                _chordLock.EnterReadLock();
                Document responseData = new Document();
                responseData["Successor"] = new DocumentEntry("Successor", _fingerTable[0].Node.ConnectionName, DocumentEntryType.String);

                // TODO: Find a way to make this an actual uint.
                responseData["ChordId"] = new DocumentEntry("ChordId", _fingerTable[0].ChordId, DocumentEntryType.Integer);
                Message response = new Message(message, "ChordSuccessorResponse", responseData, false);
                SendMessage(response);
                _chordLock.ExitReadLock();
            }
            else if (message.MessageType == "ChordPredecessorRequest")
            {
                _chordLock.EnterReadLock();
                Document responseData = new Document();
                responseData["Predecessor"] = new DocumentEntry("Predecessor", _predecessor == null ? ":0" : _predecessor.Node.ConnectionName, DocumentEntryType.String);

                // TODO: Find a way to make this an actual uint.
                responseData["ChordId"] = new DocumentEntry("ChordId", _predecessor == null ? 0 : _predecessor.ChordId, DocumentEntryType.Integer);
                Message response = new Message(message, "ChordPredecessorResponse", responseData, false);
                SendMessage(response);
                _chordLock.ExitReadLock();
            }
            else if (message.MessageType == "ChordNotify")
            {
                _chordLock.EnterWriteLock();

                var data = message.Data;

                // TODO: Find a way to make this an actual uint.
                if (_predecessor == null || IsBetween((uint)data["ChordId"].ValueAsInteger, _predecessor.ChordId, _self.ChordId))
                {
                    // TODO: Find a way to make this an actual uint.
                    _predecessor = new ChordNode(new NodeDefinition(data["Node"].ValueAsString), (uint)data["ChordId"].ValueAsInteger);
                    if (!Connect(_predecessor.Node))
                    {
                        _predecessor = null;
                    }
                }

                _chordLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Finds the closest preceding node of the ID.
        /// </summary>
        /// <param name="id">The ID to look for the closest preceding node of.</param>
        /// <returns>The node that is the closest preceding node containing ID.</returns>
        private ChordNode ClosestPrecedingNode(uint id)
        {
            _chordLock.EnterReadLock();

            ChordNode ret = null;
            for (int i = 31; i >= 1; --i)
            {
                if (_fingerTable[i] != null && IsBetween(_fingerTable[i].ChordId, _self.ChordId, id))
                {
                    ret = _fingerTable[i];
                    break;
                }
            }

            if (ret == null)
            {
                ret = _fingerTable[0];
            }

            _chordLock.ExitReadLock();
            return ret;
        }

        /// <summary>
        /// Finds the successor node of the ID.
        /// </summary>
        /// <param name="id">The ID to look for the successor node of.</param>
        /// <returns>The node that is the successor of the node containing ID.</returns>
        private ChordNode FindSuccessor(uint id)
        {
            _chordLock.EnterReadLock();

            if (_fingerTable[0] != null && (IsBetween(id, _self.ChordId, _fingerTable[0].ChordId) || id == _fingerTable[0].ChordId))
            {
                ChordNode ret = _fingerTable[0];
                _chordLock.ExitReadLock();
                return ret;
            }

            _chordLock.ExitReadLock();

            var node = ClosestPrecedingNode(id);

            Message message = new Message(node.Node, "ChordSucessorRequest", new Document(), true);
            SendMessage(message);
            message.BlockUntilDone();
            if (!message.Success)
            {
                return null;
            }

            var data = message.Response.Data;

            // TODO: Find a way to make this an actual uint.
            return new ChordNode(new NodeDefinition(data["Successor"].ValueAsString), (uint)data["ChordId"].ValueAsInteger);
        }

        /// <summary>
        /// Checks if a value is between two other values.
        /// </summary>
        /// <param name="num">The number to check.</param>
        /// <param name="min">The exclusive minimum value.</param>
        /// <param name="max">The exclusive maximum value.</param>
        /// <returns>True if the value is between the min and the max, otherwise false.</returns>
        private bool IsBetween(uint num, uint min, uint max)
        {
            if (min < max)
            {
                return num > min && num < max;
            }

            return num > min || num < max;
        }

        /// <summary>
        /// Stabilizes the chord network.
        /// </summary>
        private void Stabilize()
        {
            while (Running)
            {
                Thread.Sleep(500);

                _chordLock.EnterReadLock();

                ChordNode predecessor;
                Message message;
                if (!Equals(_fingerTable[0], _self))
                {
                    message = new Message(_fingerTable[0].Node, "ChordPredecessorRequest", new Document(), true);
                    _chordLock.ExitReadLock();
                    SendMessage(message);
                    message.BlockUntilDone();
                    if (!message.Success)
                    {
                        continue;
                    }

                    var data = message.Response.Data;

                    // TODO: Find a way to make this an actual uint.
                    predecessor = new ChordNode(new NodeDefinition(data["Predecessor"].ValueAsString), (uint)data["ChordId"].ValueAsInteger);
                }
                else
                {
                    _chordLock.ExitReadLock();
                    predecessor = _predecessor ?? new ChordNode(new NodeDefinition(string.Empty, 0), 0);
                }

                _chordLock.EnterWriteLock();

                if (predecessor.Node.Hostname != string.Empty && IsBetween(predecessor.ChordId, _self.ChordId, _fingerTable[0].ChordId))
                {
                    _fingerTable[0] = predecessor;
                    if (!Equals(_fingerTable[0], _self) && !Connect(_fingerTable[0].Node))
                    {
                        _fingerTable[0] = _self;
                    }
                }

                _chordLock.ExitWriteLock();

                _chordLock.EnterReadLock();

                if (!Equals(_fingerTable[0], _self))
                {
                    Document responseData = new Document();
                    responseData["Node"] = new DocumentEntry("Node", _self.Node.ConnectionName, DocumentEntryType.String);

                    // TODO: Find a way to make this an actual uint.
                    responseData["ChordId"] = new DocumentEntry("ChordId", _self.ChordId, DocumentEntryType.Integer);
                    message = new Message(_fingerTable[0].Node, "ChordNotify", responseData, false);
                    SendMessage(message);
                }

                _chordLock.ExitReadLock();

                // Fix the finger table
                _nextFingerNode++;
                if (_nextFingerNode >= 32)
                {
                    _nextFingerNode = 1;
                }

                var node = FindSuccessor(_self.ChordId + ((uint)1 << (_nextFingerNode - 1)));
                if (node != null)
                {
                    _chordLock.EnterWriteLock();

                    _fingerTable[_nextFingerNode] = node;
                    if (!Connect(node.Node))
                    {
                        _fingerTable[_nextFingerNode] = null;
                    }

                    _chordLock.ExitWriteLock();
                }
            }
        }

        /// <summary>
        /// Represents a node in the chord network.
        /// </summary>
        private class ChordNode
        {
            /// <summary>
            /// The chord id.
            /// </summary>
            private readonly uint _chordId;

            /// <summary>
            /// The node definition.
            /// </summary>
            private readonly NodeDefinition _node;

            /// <summary>
            /// Initializes a new instance of the <see cref="ChordNode"/> class.
            /// </summary>
            /// <param name="node">The node specified in this <see cref="ChordNode"/>.</param>
            /// <param name="chordId">The chord ID.</param>
            public ChordNode(NodeDefinition node, uint chordId)
            {
                _node = node;
                _chordId = chordId;
            }

            /// <summary>
            /// Gets the chord ID.
            /// </summary>
            public uint ChordId
            {
                get { return _chordId; }
            }

            /// <summary>
            /// Gets the node.
            /// </summary>
            public NodeDefinition Node
            {
                get { return _node; }
            }

            /// <inheritdoc />
            public override bool Equals(object obj)
            {
                ChordNode node = obj as ChordNode;
                return node != null && Equals(node.Node, Node) && Equals(node.ChordId, ChordId);
            }

            /// <inheritdoc />
            public override int GetHashCode()
            {
                return Node.GetHashCode() + ChordId.GetHashCode();
            }
        }
    }
}