using Amib.Threading;
using DatabaseV2.Networking.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace DatabaseV2.Networking
{
    /// <summary>
    /// Handles network connections.
    /// </summary>
    public abstract class Network
    {
        /// <summary>
        /// The connection cleaner thread.
        /// </summary>
        private readonly Thread _cleanerThread;

        /// <summary>
        /// The incoming connection.
        /// </summary>
        private readonly Dictionary<NodeDefinition, Connection> _incomingConnections = new Dictionary<NodeDefinition, Connection>();

        /// <summary>
        /// The incoming connections lock object.
        /// </summary>
        private readonly ReaderWriterLockSlim _incomingConnectionsLock = new ReaderWriterLockSlim();

        /// <summary>
        /// The incoming connection listener.
        /// </summary>
        private readonly TcpListener _listener;

        /// <summary>
        /// The thread pool used to receive messages.
        /// </summary>
        private readonly SmartThreadPool _messageReceivedPool = new SmartThreadPool(SmartThreadPool.DefaultIdleTimeout, 10, 5);

        /// <summary>
        /// The thread that receives messages.
        /// </summary>
        private readonly Thread _messageReceivingThread;

        /// <summary>
        /// The thread pool used to send messages.
        /// </summary>
        private readonly SmartThreadPool _messageSendPool = new SmartThreadPool(SmartThreadPool.DefaultIdleTimeout, 10, 5);

        /// <summary>
        /// The data received so far for incoming messages.
        /// </summary>
        private readonly Dictionary<Tuple<NodeDefinition, ConnectionType>, List<byte>> _messagesReceived = new Dictionary<Tuple<NodeDefinition, ConnectionType>, List<byte>>();

        /// <summary>
        /// The outgoing connections.
        /// </summary>
        private readonly Dictionary<NodeDefinition, Connection> _outgoingConnections = new Dictionary<NodeDefinition, Connection>();

        /// <summary>
        /// The outgoing connections lock object.
        /// </summary>
        private readonly ReaderWriterLockSlim _outgoingConnectionsLock = new ReaderWriterLockSlim();

        /// <summary>
        /// The port to listen for connections on.
        /// </summary>
        private readonly int _port;

        /// <summary>
        /// The messages waiting for responses.
        /// </summary>
        private readonly Dictionary<uint, Message> _waitingForResponses = new Dictionary<uint, Message>();

        /// <summary>
        /// Initializes a new instance of the <see cref="Network"/> class.
        /// </summary>
        /// <param name="port">The port to listen for connections on.</param>
        protected Network(int port)
        {
            Running = true;

            _port = port;

            _messageSendPool.Start();
            _messageReceivedPool.Start();

            _messageReceivingThread = new Thread(ReceiveMessages);
            _messageReceivingThread.Start();

            _cleanerThread = new Thread(RunCleaner);
            _cleanerThread.Start();

            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();
            _listener.BeginAcceptTcpClient(ProcessRequest, null);
        }

        /// <summary>
        /// Gets the port to listen for connections on.
        /// </summary>
        protected int Port
        {
            get { return _port; }
        }

        /// <summary>
        /// Gets a value indicating whether the network is running.
        /// </summary>
        protected bool Running { get; private set; }

        /// <summary>
        /// Gets a list of the connected nodes.
        /// </summary>
        /// <returns>A list of the connected nodes.</returns>
        public IEnumerable<NodeDefinition> GetConnectedNodes()
        {
            return _outgoingConnections.Select(e => e.Key);
        }

        /// <summary>
        /// Shuts down the network.
        /// </summary>
        public virtual void Shutdown()
        {
            Running = false;
            _listener.Stop();
            _messageReceivingThread.Join();
            _cleanerThread.Join();
            _messageReceivedPool.Shutdown();
            _messageSendPool.Shutdown();
        }

        /// <summary>
        /// Connects to the specified node.
        /// </summary>
        /// <param name="definition">The node to connect to.</param>
        /// <returns>True if the connection was established, otherwise false.</returns>
        protected bool Connect(NodeDefinition definition)
        {
            _outgoingConnectionsLock.EnterReadLock();
            if (_outgoingConnections.ContainsKey(definition) && _outgoingConnections[definition].Status != ConnectionStatus.Disconnected)
            {
                _outgoingConnectionsLock.ExitReadLock();
                return true;
            }

            _outgoingConnectionsLock.ExitReadLock();
            var message = new Message(definition, new JoinRequest(_port), true)
            {
                RequireSecureConnection = false
            };

            SendMessage(message);
            message.BlockUntilDone();
            if (!message.Success)
            {
                return false;
            }

            _outgoingConnectionsLock.EnterReadLock();
            _outgoingConnections[definition].ConnectionEstablished();
            _outgoingConnectionsLock.ExitReadLock();
            return true;
        }

        /// <summary>
        /// Called when a node is disconnected.
        /// </summary>
        /// <param name="node">The node that got disconnected.</param>
        protected abstract void Disconnection(NodeDefinition node);

        /// <summary>
        /// Handles a message.
        /// </summary>
        /// <param name="message">The message that was received.</param>
        protected abstract void HandleMessage(Message message);

        /// <summary>
        /// Sends a message.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <remarks>Queues the sending function onto the thread pool.</remarks>
        protected void SendMessage(Message message)
        {
            message.Status = MessageStatus.Sending;
            _messageSendPool.QueueWorkItem(SendMessageWorkItem, message);
        }

        /// <summary>
        /// Cleans up disconnected connections.
        /// </summary>
        /// <param name="connectionsLock">The connections lock.</param>
        /// <param name="connections">The connection list.</param>
        private void CleanConnections(ReaderWriterLockSlim connectionsLock, Dictionary<NodeDefinition, Connection> connections)
        {
            connectionsLock.EnterWriteLock();
            List<NodeDefinition> removedConnections = new List<NodeDefinition>();
            removedConnections.Clear();
            removedConnections.AddRange(connections.Where(e => e.Value.Status == ConnectionStatus.Disconnected).Select(e => e.Key));
            removedConnections.ForEach(e => connections.Remove(e));
            connectionsLock.ExitWriteLock();

            removedConnections.ForEach(e => _messagesReceived.Remove(new Tuple<NodeDefinition, ConnectionType>(e, ConnectionType.Outgoing)));
            removedConnections.ForEach(Disconnection);

            Monitor.Enter(_waitingForResponses);
            List<uint> removedMessages = new List<uint>();
            foreach (var connection in removedConnections)
            {
                foreach (var message in _waitingForResponses)
                {
                    if (message.Value.Address.Equals(connection))
                    {
                        message.Value.Status = MessageStatus.ResponseFailure;
                        removedMessages.Add(message.Key);
                    }
                }
            }

            removedMessages.ForEach(e => _waitingForResponses.Remove(e));
            Monitor.Exit(_waitingForResponses);
        }

        /// <summary>
        /// Handles processing a completed message.
        /// </summary>
        /// <param name="message">The message to process.</param>
        private void MessageReceivedHandler(Message message)
        {
            if (message.Data is JoinRequest)
            {
                JoinRequest request = (JoinRequest)message.Data;
                RenameConnection(message.Address, request.Address);
                _incomingConnectionsLock.EnterReadLock();
                _incomingConnections[request.Address].ConnectionEstablished();
                _incomingConnectionsLock.ExitReadLock();
                Message response = new Message(message, new JoinResult(), false)
                {
                    Address = request.Address
                };

                SendMessage(response);
            }
            else
            {
                HandleMessage(message);
            }
        }

        /// <summary>
        /// Processes a message.
        /// </summary>
        /// <param name="address">The address the message came from.</param>
        /// <param name="type">The type of connection the message came from.</param>
        /// <param name="data">The data of the message.</param>
        private void ProcessMessage(NodeDefinition address, ConnectionType type, byte[] data)
        {
            Message message = new Message(address, data, type);

            if (message.InResponseTo != 0)
            {
                Monitor.Enter(_waitingForResponses);

                if (_waitingForResponses.ContainsKey(message.InResponseTo))
                {
                    Message waiting = _waitingForResponses[message.InResponseTo];
                    waiting.Response = message;
                    waiting.Status = MessageStatus.ResponseReceived;
                    if (waiting.ResponseCallback != null)
                    {
                        _messageReceivedPool.QueueWorkItem(waiting.ResponseCallback, waiting);
                    }

                    _waitingForResponses.Remove(message.InResponseTo);
                }

                Monitor.Exit(_waitingForResponses);
            }
            else
            {
                _messageReceivedPool.QueueWorkItem(MessageReceivedHandler, message);
            }
        }

        /// <summary>
        /// Processes an incoming connection request.
        /// </summary>
        /// <param name="result">The result of the async call.</param>
        private void ProcessRequest(IAsyncResult result)
        {
            var incoming = _listener.EndAcceptTcpClient(result);
            _listener.BeginAcceptTcpClient(ProcessRequest, null);

            Connection connection = new Connection(incoming);

            _incomingConnectionsLock.EnterWriteLock();

            NodeDefinition def = new NodeDefinition(((IPEndPoint)incoming.Client.RemoteEndPoint).Address.ToString(), ((IPEndPoint)incoming.Client.RemoteEndPoint).Port);
            _incomingConnections.Add(def, connection);

            _incomingConnectionsLock.ExitWriteLock();
        }

        /// <summary>
        /// Scans the active connections for new messages.
        /// </summary>
        private void ReceiveMessages()
        {
            var messageBuffer = new byte[1024];
            while (Running)
            {
                List<Tuple<NodeDefinition, ConnectionType, byte[]>> messages = new List<Tuple<NodeDefinition, ConnectionType, byte[]>>();
                Monitor.Enter(_messagesReceived);
                _outgoingConnectionsLock.EnterReadLock();

                foreach (var connection in _outgoingConnections)
                {
                    if (!connection.Value.Client.Connected)
                    {
                        continue;
                    }

                    var key = new Tuple<NodeDefinition, ConnectionType>(connection.Key, ConnectionType.Outgoing);
                    if (!_messagesReceived.ContainsKey(key))
                    {
                        _messagesReceived.Add(key, new List<byte>(1024));
                    }

                    try
                    {
                        NetworkStream stream = connection.Value.Client.GetStream();
                        while (stream.DataAvailable)
                        {
                            int bytesRead = stream.Read(messageBuffer, 0, 1024);
                            _messagesReceived[key].AddRange(messageBuffer.Take(bytesRead));
                        }
                    }
                    catch
                    {
                        // The stream was closed, do nothing.
                    }
                }

                _outgoingConnectionsLock.ExitReadLock();

                _incomingConnectionsLock.EnterReadLock();

                foreach (var connection in _incomingConnections)
                {
                    if (!connection.Value.Client.Connected)
                    {
                        continue;
                    }

                    var key = new Tuple<NodeDefinition, ConnectionType>(connection.Key, ConnectionType.Incoming);
                    if (!_messagesReceived.ContainsKey(key))
                    {
                        _messagesReceived.Add(key, new List<byte>(1024));
                    }

                    try
                    {
                        NetworkStream stream = connection.Value.Client.GetStream();
                        while (stream.DataAvailable)
                        {
                            int bytesRead = stream.Read(messageBuffer, 0, 1024);
                            _messagesReceived[key].AddRange(messageBuffer.Take(bytesRead));
                        }
                    }
                    catch
                    {
                        // The stream was closed, do nothing.
                    }
                }

                _incomingConnectionsLock.ExitReadLock();

                foreach (var message in _messagesReceived)
                {
                    while (message.Value.Count >= 4)
                    {
                        int length = BitConverter.ToInt32(message.Value.Take(4).ToArray(), 0);
                        if (message.Value.Count >= length + 4)
                        {
                            messages.Add(new Tuple<NodeDefinition, ConnectionType, byte[]>(message.Key.Item1, message.Key.Item2, message.Value.Skip(4).Take(length).ToArray()));
                            message.Value.RemoveRange(0, length + 4);
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                Monitor.Exit(_messagesReceived);

                foreach (var message in messages)
                {
                    ProcessMessage(message.Item1, message.Item2, message.Item3);
                }

                Thread.Sleep(1);
            }
        }

        /// <summary>
        /// Renames a connection once the name is known.
        /// </summary>
        /// <param name="currentName">The current name of the connection.</param>
        /// <param name="newName">The new name of the connection.</param>
        private void RenameConnection(NodeDefinition currentName, NodeDefinition newName)
        {
            if (Equals(currentName, newName))
            {
                return;
            }

            Monitor.Enter(_messagesReceived);
            _incomingConnectionsLock.EnterWriteLock();

            if (_incomingConnections.ContainsKey(currentName))
            {
                if (_incomingConnections.ContainsKey(newName))
                {
                    _incomingConnections.Remove(newName);
                }

                var messagesKey = new Tuple<NodeDefinition, ConnectionType>(newName, ConnectionType.Incoming);
                if (_messagesReceived.ContainsKey(messagesKey))
                {
                    _messagesReceived.Remove(messagesKey);
                }

                var connection = _incomingConnections[currentName];
                _incomingConnections.Remove(currentName);
                _incomingConnections.Add(newName, connection);

                messagesKey = new Tuple<NodeDefinition, ConnectionType>(currentName, ConnectionType.Incoming);
                if (_messagesReceived.ContainsKey(messagesKey))
                {
                    var messageList = _messagesReceived[messagesKey];
                    _messagesReceived.Remove(messagesKey);
                    _messagesReceived.Add(new Tuple<NodeDefinition, ConnectionType>(newName, ConnectionType.Incoming), messageList);
                }
            }

            _incomingConnectionsLock.ExitWriteLock();
            Monitor.Exit(_messagesReceived);
        }

        /// <summary>
        /// Cleans up any disconnected connections and expired messages.
        /// </summary>
        private void RunCleaner()
        {
            while (Running)
            {
                Thread.Sleep(5000);

                Monitor.Enter(_messagesReceived);

                CleanConnections(_outgoingConnectionsLock, _outgoingConnections);
                CleanConnections(_incomingConnectionsLock, _incomingConnections);

                Monitor.Exit(_messagesReceived);

                Monitor.Enter(_waitingForResponses);

                List<uint> removedMessages = new List<uint>();
                foreach (var message in _waitingForResponses)
                {
                    if (message.Value.ExpireTime < DateTime.UtcNow)
                    {
                        message.Value.Status = MessageStatus.ResponseTimeout;
                        removedMessages.Add(message.Key);
                    }
                }

                removedMessages.ForEach(e => _waitingForResponses.Remove(e));

                Monitor.Exit(_waitingForResponses);
            }
        }

        /// <summary>
        /// Sends a message to a node.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="lockObject">The object to lock on.</param>
        /// <param name="connections">The list of connections to search.</param>
        /// <remarks>Called by SendMessageWorkItem to send the message to the correct connections list.</remarks>
        private void SendMessageToNode(Message message, ReaderWriterLockSlim lockObject, Dictionary<NodeDefinition, Connection> connections)
        {
            lockObject.EnterReadLock();

            if (connections.ContainsKey(message.Address) &&
                (connections[message.Address].Status == ConnectionStatus.Connected || !message.RequireSecureConnection))
            {
                try
                {
                    if (message.WaitingForResponse)
                    {
                        Monitor.Enter(_waitingForResponses);
                        _waitingForResponses.Add(message.Id, message);
                        Monitor.Exit(_waitingForResponses);
                    }

                    byte[] dataToSend = message.EncodeMessage();
                    var stream = connections[message.Address].Client.GetStream();
                    stream.Write(dataToSend, 0, dataToSend.Length);

                    message.Status = message.WaitingForResponse ? MessageStatus.WaitingForResponse : MessageStatus.Sent;
                }
                catch
                {
                    message.Status = MessageStatus.SendingFailure;
                    connections[message.Address].Disconnected();

                    Monitor.Enter(_waitingForResponses);
                    _waitingForResponses.Remove(message.Id);
                    Monitor.Exit(_waitingForResponses);
                }
            }
            else if (!connections.ContainsKey(message.Address))
            {
                message.Status = MessageStatus.SendingFailure;

                if (connections.ContainsKey(message.Address))
                {
                    connections[message.Address].Disconnected();
                }
            }

            lockObject.ExitReadLock();
        }

        /// <summary>
        /// Sends a message.
        /// </summary>
        /// <param name="message">The message to send.</param>
        private void SendMessageWorkItem(Message message)
        {
            if (message.Address == null)
            {
                message.Status = MessageStatus.SendingFailure;
                return;
            }

            bool createConnection = false;
            if (message.Type == ConnectionType.Outgoing)
            {
                _outgoingConnectionsLock.EnterReadLock();

                if (!message.RequireSecureConnection && !_outgoingConnections.ContainsKey(message.Address))
                {
                    createConnection = true;
                }

                _outgoingConnectionsLock.ExitReadLock();
            }

            if (createConnection)
            {
                try
                {
                    TcpClient client = new TcpClient(message.Address.Hostname, message.Address.Port);

                    _outgoingConnectionsLock.EnterWriteLock();

                    if (!_outgoingConnections.ContainsKey(message.Address))
                    {
                        _outgoingConnections.Add(message.Address, new Connection(client));
                    }

                    _outgoingConnectionsLock.ExitWriteLock();
                }
                catch
                {
                    message.Status = MessageStatus.SendingFailure;
                }
            }

            if (message.Type == ConnectionType.Outgoing)
            {
                SendMessageToNode(message, _outgoingConnectionsLock, _outgoingConnections);
            }
            else
            {
                SendMessageToNode(message, _incomingConnectionsLock, _incomingConnections);
            }
        }
    }
}