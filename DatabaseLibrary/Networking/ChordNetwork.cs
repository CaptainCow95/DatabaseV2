﻿using DatabaseLibrary.Networking.Messaging;
using System;
using System.Threading;

namespace DatabaseLibrary.Networking
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
        /// The thread running the stabilization runtime.
        /// </summary>
        private readonly Thread _stabilizationThread;

        /// <summary>
        /// The chord finger table.
        /// </summary>
        private ChordNode[] _fingerTable = new ChordNode[32];

        /// <summary>
        /// The next finger to update during stabilization.
        /// </summary>
        private int _nextFingerNode = 1;

        /// <summary>
        /// The predecessor chord node.
        /// </summary>
        private ChordNode _predecessor;

        /// <summary>
        /// The current chord node.
        /// </summary>
        private ChordNode _self;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChordNetwork"/> class.
        /// </summary>
        /// <param name="port">The port to listen for connections on.</param>
        /// <param name="node">The node to try to connect to the network of.</param>
        public ChordNetwork(int port, NodeDefinition node)
            : base(port)
        {
            byte[] randomBytes = new byte[4];
            new Random().NextBytes(randomBytes);
            uint chordId = BitConverter.ToUInt32(randomBytes, 0);

            _self = new ChordNode(new NodeDefinition("localhost", Port), chordId);
            _fingerTable[0] = _self;

            Connect(node);
            Message message = new Message(node, new ChordSuccessorRequest(), true);
            SendMessage(message);
            message.BlockUntilDone();
            if (message.Success)
            {
                var response = (ChordSuccessorResponse)message.Response.Data;
                _fingerTable[0] = new ChordNode(response.Successor, response.ChordID);
                if (!Equals(_fingerTable[0], _self) && !Connect(_fingerTable[0].Node))
                {
                    _fingerTable[0] = _self;
                }
            }

            _stabilizationThread = new Thread(Stabilize);
            _stabilizationThread.Start();
        }

        /// <summary>
        /// Prints out the status of the chord network.
        /// </summary>
        public void PrintStatus()
        {
            Console.WriteLine("Predecessor: " + (_predecessor == null ? "null" : _predecessor.Node.ConnectionName));
            Console.WriteLine("Finger Table:");
            for (int i = 0; i < 32; ++i)
            {
                if (_fingerTable[i] == null)
                {
                    Console.WriteLine("null");
                }
                else
                {
                    Console.WriteLine(_fingerTable[i].Node.ConnectionName);
                }
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
            if (message.Data is ChordSuccessorRequest)
            {
                _chordLock.EnterReadLock();
                Message response = new Message(message, new ChordSuccessorResponse(_fingerTable[0].Node, _fingerTable[0].ChordID), false);
                SendMessage(response);
                _chordLock.ExitReadLock();
            }
            else if (message.Data is ChordPredecessorRequest)
            {
                _chordLock.EnterReadLock();
                var responseData = _predecessor == null
                    ? new ChordPredecessorResponse(new NodeDefinition(string.Empty, 0), 0)
                    : new ChordPredecessorResponse(_predecessor.Node, _predecessor.ChordID);
                Message response = new Message(message, responseData, false);
                SendMessage(response);
                _chordLock.ExitReadLock();
            }
            else if (message.Data is ChordNotify)
            {
                _chordLock.EnterWriteLock();

                var data = (ChordNotify)message.Data;
                if (_predecessor == null || IsBetween(data.ChordID, _predecessor.ChordID, _self.ChordID))
                {
                    _predecessor = new ChordNode(data.Node, data.ChordID);
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
                if (_fingerTable[i] != null && IsBetween(_fingerTable[i].ChordID, _self.ChordID, id))
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

            if (_fingerTable[0] != null && (IsBetween(id, _self.ChordID, _fingerTable[0].ChordID) || id == _fingerTable[0].ChordID))
            {
                ChordNode ret = _fingerTable[0];
                _chordLock.ExitReadLock();
                return ret;
            }

            _chordLock.ExitReadLock();

            var node = ClosestPrecedingNode(id);

            Message message = new Message(node.Node, new ChordSuccessorRequest(), true);
            SendMessage(message);
            message.BlockUntilDone();
            if (!message.Success)
            {
                return null;
            }

            var response = (ChordSuccessorResponse)message.Response.Data;
            return new ChordNode(response.Successor, response.ChordID);
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
            while (true)
            {
                Thread.Sleep(500);

                _chordLock.EnterReadLock();

                ChordNode predecessor;
                Message message;
                if (!Equals(_fingerTable[0], _self))
                {
                    message = new Message(_fingerTable[0].Node, new ChordPredecessorRequest(), true);
                    _chordLock.ExitReadLock();
                    SendMessage(message);
                    message.BlockUntilDone();
                    if (!message.Success)
                    {
                        continue;
                    }

                    var response = (ChordPredecessorResponse)message.Response.Data;
                    predecessor = new ChordNode(response.Predecessor, response.ChordID);
                }
                else
                {
                    _chordLock.ExitReadLock();
                    predecessor = _predecessor ?? new ChordNode(new NodeDefinition(string.Empty, 0), 0);
                }

                _chordLock.EnterWriteLock();

                if (predecessor.Node.Hostname != string.Empty && IsBetween(predecessor.ChordID, _self.ChordID, _fingerTable[0].ChordID))
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
                    message = new Message(_fingerTable[0].Node, new ChordNotify(_self.Node, _self.ChordID), false);
                    SendMessage(message);
                }

                _chordLock.ExitReadLock();

                // Fix the finger table
                _nextFingerNode++;
                if (_nextFingerNode >= 32)
                {
                    _nextFingerNode = 1;
                }

                var node = FindSuccessor(_self.ChordID + ((uint)1 << (_nextFingerNode - 1)));
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
            /// Initializes a new instance of the <see cref="ChordNode"/> class.
            /// </summary>
            /// <param name="node">The node specified in this <see cref="ChordNode"/>.</param>
            /// <param name="chordId">The chord ID.</param>
            public ChordNode(NodeDefinition node, uint chordId)
            {
                Node = node;
                ChordID = chordId;
            }

            /// <summary>
            /// Gets or sets the chord ID.
            /// </summary>
            public uint ChordID { get; set; }

            /// <summary>
            /// Gets or sets the node.
            /// </summary>
            public NodeDefinition Node { get; set; }

            /// <inheritdoc />
            public override bool Equals(object obj)
            {
                ChordNode node = obj as ChordNode;
                return node != null && Equals(node.Node, Node) && Equals(node.ChordID, ChordID);
            }

            /// <inheritdoc />
            public override int GetHashCode()
            {
                return Node.GetHashCode() + ChordID.GetHashCode();
            }
        }
    }
}