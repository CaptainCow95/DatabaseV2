using Library.Data;
using Library.Networking;
using System;
using System.Collections.Generic;
using System.Threading;

namespace DatabaseV2
{
    /// <summary>
    /// Represents a database network, based on a chord network.
    /// </summary>
    public class DatabaseNetwork : Network
    {
        /// <summary>
        /// The object to lock on when editing the finger table or the predecessor.
        /// </summary>
        private readonly ReaderWriterLockSlim _chordLock = new ReaderWriterLockSlim();

        /// <summary>
        /// The current chord node.
        /// </summary>
        private readonly ChordNode _self;

        /// <summary>
        /// The thread running the stabilization runtime.
        /// </summary>
        private readonly Thread _stabilizationThread;

        /// <summary>
        /// A value indicating whether the object has already been _disposed.
        /// </summary>
        private bool _disposed = false;

        /// <summary>
        /// The predecessor chord node.
        /// </summary>
        private ChordNode _predecessor;

        /// <summary>
        /// The successor chord node.
        /// </summary>
        private ChordNode _successor;

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseNetwork"/> class.
        /// </summary>
        /// <param name="port">The port to listen for connections on.</param>
        /// <param name="nodes">The nodes to try to connect to the network of.</param>
        public DatabaseNetwork(int port, List<NodeDefinition> nodes)
            : base(port)
        {
            byte[] randomBytes = new byte[4];
            new Random().NextBytes(randomBytes);
            uint chordId = BitConverter.ToUInt32(randomBytes, 0);

            _self = new ChordNode(new NodeDefinition("localhost", Port), chordId);
            _successor = _self;

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
                        _successor = new ChordNode(new NodeDefinition(data["Successor"].ValueAsString()), (uint)data["ChordId"].ValueAsInt64());
                        if (!Equals(_successor, _self) && !Connect(_successor.Node))
                        {
                            _successor = _self;
                        }
                    }
                }
            }

            _stabilizationThread = new Thread(Stabilize);
            _stabilizationThread.Start();
        }

        /// <summary>
        /// Gets the node's predecessor in the chord ring.
        /// </summary>
        public NodeDefinition Predecessor
        {
            get { return _predecessor == null ? null : _predecessor.Node; }
        }

        /// <summary>
        /// Gets the node's successor in the chord ring.
        /// </summary>
        public NodeDefinition Successor
        {
            get { return _successor == null ? null : _successor.Node; }
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

        /// <summary>
        /// Releases all resources used by the current instance of the <see cref="DatabaseNetwork"/> class.
        /// </summary>
        /// <param name="disposing">Whether to dispose of managed resources or not.</param>
        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _chordLock.Dispose();
                    _disposed = true;
                }

                base.Dispose(disposing);
            }
        }

        /// <inheritdoc />
        protected override void HandleDisconnection(NodeDefinition node)
        {
            _chordLock.EnterWriteLock();

            if (_predecessor != null && Equals(_predecessor.Node, node))
            {
                _predecessor = null;
            }

            if (_successor != null && Equals(_successor.Node, node))
            {
                _successor = _self;
            }

            _chordLock.ExitWriteLock();
        }

        /// <inheritdoc />
        protected override bool HandleMessage(Message message)
        {
            if (message.MessageType == "ChordSuccessorRequest")
            {
                _chordLock.EnterReadLock();
                Document responseData = new Document();
                responseData["Successor"] = new DocumentEntry("Successor", _successor.Node.ConnectionName, DocumentEntryType.String);
                responseData["ChordId"] = new DocumentEntry("ChordId", _successor.ChordId, DocumentEntryType.Int64);
                Message response = new Message(message, "ChordSuccessorResponse", responseData, false);
                SendMessage(response);
                _chordLock.ExitReadLock();
                return true;
            }

            if (message.MessageType == "ChordPredecessorRequest")
            {
                _chordLock.EnterReadLock();
                Document responseData = new Document();
                responseData["Predecessor"] = new DocumentEntry("Predecessor", _predecessor == null ? ":0" : _predecessor.Node.ConnectionName, DocumentEntryType.String);
                responseData["ChordId"] = new DocumentEntry("ChordId", _predecessor == null ? 0 : _predecessor.ChordId, DocumentEntryType.Int64);
                Message response = new Message(message, "ChordPredecessorResponse", responseData, false);
                SendMessage(response);
                _chordLock.ExitReadLock();
                return true;
            }

            if (message.MessageType == "ChordNotify")
            {
                _chordLock.EnterWriteLock();

                var data = message.Data;
                if (_predecessor == null || IsBetween((uint)data["ChordId"].ValueAsInt64(), _predecessor.ChordId, _self.ChordId))
                {
                    _predecessor = new ChordNode(new NodeDefinition(data["Node"].ValueAsString()), (uint)data["ChordId"].ValueAsInt64());
                    if (!Connect(_predecessor.Node))
                    {
                        _predecessor = null;
                    }
                }

                _chordLock.ExitWriteLock();
                return true;
            }

            return base.HandleMessage(message);
        }

        /// <summary>
        /// Finds the closest preceding node of the ID.
        /// </summary>
        /// <param name="id">The ID to look for the closest preceding node of.</param>
        /// <returns>The node that is the closest preceding node containing ID.</returns>
        private ChordNode ClosestPrecedingNode(uint id)
        {
            _chordLock.EnterReadLock();

            // TODO: Find the closest node to the specified id among all nodes, not just the successor.
            ChordNode ret = _successor;
            /*
            for (int i = 31; i >= 1; --i)
            {
                if (_fingerTable[i] != null && IsBetween(_fingerTable[i].ChordId, _self.ChordId, id))
                {
                    ret = _fingerTable[i];
                    break;
                }
            }*/

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

            if (_successor != null && (IsBetween(id, _self.ChordId, _successor.ChordId) || id == _successor.ChordId))
            {
                ChordNode ret = _successor;
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

            return new ChordNode(new NodeDefinition(data["Successor"].ValueAsString()), (uint)data["ChordId"].ValueAsInt64());
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
                if (!Equals(_successor, _self))
                {
                    message = new Message(_successor.Node, "ChordPredecessorRequest", new Document(), true);
                    _chordLock.ExitReadLock();
                    SendMessage(message);
                    message.BlockUntilDone();
                    if (!message.Success)
                    {
                        continue;
                    }

                    var data = message.Response.Data;
                    predecessor = new ChordNode(new NodeDefinition(data["Predecessor"].ValueAsString()), (uint)data["ChordId"].ValueAsInt64());
                }
                else
                {
                    _chordLock.ExitReadLock();
                    predecessor = _predecessor ?? new ChordNode(new NodeDefinition(string.Empty, 0), 0);
                }

                _chordLock.EnterWriteLock();

                if (predecessor.Node.Hostname != string.Empty && IsBetween(predecessor.ChordId, _self.ChordId, _successor.ChordId))
                {
                    _successor = predecessor;
                    if (!Equals(_successor, _self) && !Connect(_successor.Node))
                    {
                        _successor = _self;
                    }
                }

                _chordLock.ExitWriteLock();

                _chordLock.EnterReadLock();

                if (!Equals(_successor, _self))
                {
                    Document responseData = new Document();
                    responseData["Node"] = new DocumentEntry("Node", _self.Node.ConnectionName, DocumentEntryType.String);
                    responseData["ChordId"] = new DocumentEntry("ChordId", _self.ChordId, DocumentEntryType.Int64);
                    message = new Message(_successor.Node, "ChordNotify", responseData, false);
                    SendMessage(message);
                }

                _chordLock.ExitReadLock();
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