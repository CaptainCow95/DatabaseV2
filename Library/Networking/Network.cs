using Amib.Threading;
using Library.Data;
using Library.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Library.Networking
{
    /// <summary>
    /// Handles network connections.
    /// </summary>
    public class Network : IDisposable
    {
        /// <summary>
        /// A list of the nodes to try to reconnect to.
        /// </summary>
        private readonly List<NodeDefinition> _connectedNodes = new List<NodeDefinition>();

        /// <summary>
        /// The heartbeat thread.
        /// </summary>
        private readonly Thread _heartbeatThread;

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
        /// The connection maintenance thread.
        /// </summary>
        private readonly Thread _maintenanceThread;

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
        /// A value indicating whether the object has already been _disposed.
        /// </summary>
        private bool _disposed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="Network"/> class.
        /// </summary>
        public Network()
        {
            Running = true;

            _messageSendPool.Start();
            _messageReceivedPool.Start();

            _messageReceivingThread = new Thread(ReceiveMessages);
            _messageReceivingThread.Start();

            _maintenanceThread = new Thread(RunMaintenance);
            _maintenanceThread.Start();

            _heartbeatThread = new Thread(RunHeartbeat);
            _heartbeatThread.Start();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Network"/> class.
        /// </summary>
        /// <param name="port">The port to listen for connections on.</param>
        public Network(int port)
            : this()
        {
            _port = port;

            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();
            _listener.BeginAcceptTcpClient(ProcessRequest, null);
        }

        /// <summary>
        /// An event called when a node is disconnected.
        /// </summary>
        public event EventHandler<DisconnectionEventArgs> Disconnection;

        /// <summary>
        /// An event called when a new message is received.
        /// </summary>
        public event EventHandler<MessageReceivedEventArgs> MessageReceived;

        /// <summary>
        /// Gets a value indicating whether the network is running.
        /// </summary>
        public bool Running { get; private set; }

        /// <summary>
        /// Gets the port to listen for connections on.
        /// </summary>
        protected int Port
        {
            get { return _port; }
        }

        /// <summary>
        /// Connects to the specified node.
        /// </summary>
        /// <param name="definition">The node to connect to.</param>
        /// <returns>True if the connection was established, otherwise false.</returns>
        public bool Connect(NodeDefinition definition)
        {
            lock (_connectedNodes)
            {
                if (!_connectedNodes.Contains(definition))
                {
                    _connectedNodes.Add(definition);
                }
            }

            return ConnectInternal(definition);
        }

        /// <summary>
        /// Disconnects from a specified node.
        /// </summary>
        /// <param name="definition">The node to disconnect from.</param>
        public void Disconnect(NodeDefinition definition)
        {
            lock (_connectedNodes)
            {
                _connectedNodes.Remove(definition);
            }

            DisconnectInternal(definition);
        }

        /// <summary>
        /// Releases all resources used by the current instance of the <see cref="Network"/> class.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Gets a list of the connected nodes.
        /// </summary>
        /// <returns>A list of the connected nodes.</returns>
        public IEnumerable<NodeDefinition> GetConnectedNodes()
        {
            return GetOutgoingConnectedNodes();
        }

        /// <summary>
        /// Checks if the network is connected to a node.
        /// </summary>
        /// <param name="node">The node the check for.</param>
        /// <returns>True if the node was found, otherwise false.</returns>
        public bool IsConnected(NodeDefinition node)
        {
            _outgoingConnectionsLock.EnterReadLock();
            bool found = _outgoingConnections.ContainsKey(node) && _outgoingConnections[node].Status == ConnectionStatus.Connected;
            _outgoingConnectionsLock.ExitReadLock();

            return found;
        }

        /// <summary>
        /// Sends a message.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <remarks>Queues the sending function onto the thread pool.</remarks>
        public void SendMessage(Message message)
        {
            message.Status = MessageStatus.Sending;
            _messageSendPool.QueueWorkItem(SendMessageWorkItem, message);
        }

        /// <summary>
        /// Shuts down the network.
        /// </summary>
        public virtual void Shutdown()
        {
            Logger.Log("Shutting down network.", LogLevel.Info);
            Running = false;
            if (_listener != null)
            {
                _listener.Stop();
            }

            _heartbeatThread.Join();
            _messageReceivingThread.Join();
            _maintenanceThread.Join();
            _messageReceivedPool.Shutdown();
            _messageSendPool.Shutdown();
        }

        /// <summary>
        /// Releases all resources used by the current instance of the <see cref="Network"/> class.
        /// </summary>
        /// <param name="disposing">Whether to dispose of managed resources or not.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _messageReceivedPool.Dispose();
                    _messageSendPool.Dispose();
                    _incomingConnectionsLock.Dispose();
                    _outgoingConnectionsLock.Dispose();
                    _disposed = true;
                }
            }
        }

        /// <summary>
        /// Gets a list of the connected incoming nodes.
        /// </summary>
        /// <returns>A list of the connected incoming nodes.</returns>
        protected IEnumerable<NodeDefinition> GetIncomingConnectedNodes()
        {
            _incomingConnectionsLock.EnterReadLock();
            var ret = _incomingConnections.Select(e => e.Key).ToList();
            _incomingConnectionsLock.ExitReadLock();
            return ret;
        }

        /// <summary>
        /// Gets a list of the connected outgoing nodes.
        /// </summary>
        /// <returns>A list of the connected outgoing nodes.</returns>
        protected IEnumerable<NodeDefinition> GetOutgoingConnectedNodes()
        {
            _outgoingConnectionsLock.EnterReadLock();
            var ret = _outgoingConnections.Select(e => e.Key).ToList();
            _outgoingConnectionsLock.ExitReadLock();
            return ret;
        }

        /// <summary>
        /// Called when a node is disconnected.
        /// </summary>
        /// <param name="node">The node that got disconnected.</param>
        protected virtual void HandleDisconnection(NodeDefinition node)
        {
        }

        /// <summary>
        /// Handles a message.
        /// </summary>
        /// <param name="message">The message that was received.</param>
        /// <returns>A value indicating whether the message has been handled.</returns>
        protected virtual bool HandleMessage(Message message)
        {
            return false;
        }

        /// <summary>
        /// Connects to the specified node.
        /// </summary>
        /// <param name="definition">The node to connect to.</param>
        /// <returns>True if the connection was established, otherwise false.</returns>
        private bool ConnectInternal(NodeDefinition definition)
        {
            _outgoingConnectionsLock.EnterUpgradeableReadLock();
            if (_outgoingConnections.ContainsKey(definition))
            {
                _outgoingConnectionsLock.ExitUpgradeableReadLock();
                return true;
            }

            _outgoingConnectionsLock.EnterWriteLock();

            bool success = true;
            try
            {
                TcpClient client = new TcpClient(definition.Hostname, definition.Port);
                _outgoingConnections.Add(definition, new Connection(client));
            }
            catch
            {
                success = false;
            }

            _outgoingConnectionsLock.ExitWriteLock();
            _outgoingConnectionsLock.ExitUpgradeableReadLock();

            if (!success)
            {
                return false;
            }

            Document messageData = new Document
            {
                { "Address", new NodeDefinition("localhost", _port).ConnectionName }
            };
            messageData["Address"] = new DocumentEntry(new NodeDefinition("localhost", _port).ConnectionName);
            var message = new Message(definition, "JoinRequest", messageData, true)
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

            Logger.Log("Connected to " + definition.ConnectionName + ".", LogLevel.Debug);

            return true;
        }

        /// <summary>
        /// Disconnects from a specified node.
        /// </summary>
        /// <param name="definition">The node to disconnect from.</param>
        private void DisconnectInternal(NodeDefinition definition)
        {
            _outgoingConnectionsLock.EnterWriteLock();
            bool removed = _outgoingConnections.Remove(definition);
            _outgoingConnectionsLock.ExitWriteLock();

            _incomingConnectionsLock.EnterWriteLock();
            removed = removed || _incomingConnections.Remove(definition);
            _incomingConnectionsLock.ExitWriteLock();

            if (removed)
            {
                lock (_messagesReceived)
                {
                    _messagesReceived.Remove(new Tuple<NodeDefinition, ConnectionType>(definition, ConnectionType.Outgoing));
                    _messagesReceived.Remove(new Tuple<NodeDefinition, ConnectionType>(definition, ConnectionType.Incoming));
                }

                Logger.Log("Disconnected from " + definition.ConnectionName + ".", LogLevel.Debug);
                HandleDisconnection(definition);
                if (Disconnection != null)
                {
                    Disconnection(this, new DisconnectionEventArgs(definition));
                }

                lock (_waitingForResponses)
                {
                    List<uint> removedMessages = new List<uint>();
                    foreach (var message in _waitingForResponses)
                    {
                        if (message.Value.Address.Equals(definition))
                        {
                            message.Value.Status = MessageStatus.ResponseFailure;
                            removedMessages.Add(message.Key);
                        }
                    }

                    removedMessages.ForEach(e => _waitingForResponses.Remove(e));
                }
            }
        }

        /// <summary>
        /// Handles processing a completed message.
        /// </summary>
        /// <param name="message">The message to process.</param>
        private void MessageReceivedHandler(Message message)
        {
            if (message.MessageType == "JoinRequest")
            {
                var data = message.Data;
                var dataAddress = new NodeDefinition(data["Address"].ValueAsString());
                RenameConnection(message.Address, dataAddress);
                _incomingConnectionsLock.EnterReadLock();
                _incomingConnections[dataAddress].ConnectionEstablished();
                _incomingConnectionsLock.ExitReadLock();
                Message response = new Message(message, "JoinResult", new Document(), false)
                {
                    Address = dataAddress
                };

                SendMessage(response);
            }
            else
            {
                if (!HandleMessage(message) && MessageReceived != null)
                {
                    MessageReceived(this, new MessageReceivedEventArgs(message));
                }
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
                lock (_waitingForResponses)
                {
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
                }
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
            TcpClient incoming;
            try
            {
                incoming = _listener.EndAcceptTcpClient(result);
                _listener.BeginAcceptTcpClient(ProcessRequest, null);
            }
            catch (ObjectDisposedException)
            {
                // The connection listener was shutdown, probably because we ourselves are shutting down.
                return;
            }

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

                _outgoingConnectionsLock.EnterReadLock();

                lock (_messagesReceived)
                {
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
                }

                _outgoingConnectionsLock.ExitReadLock();

                _incomingConnectionsLock.EnterReadLock();

                lock (_messagesReceived)
                {
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
                }

                _incomingConnectionsLock.ExitReadLock();

                lock (_messagesReceived)
                {
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
                }

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

            _incomingConnectionsLock.EnterWriteLock();

            lock (_messagesReceived)
            {
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
                        _messagesReceived.Add(
                            new Tuple<NodeDefinition, ConnectionType>(newName, ConnectionType.Incoming), messageList);
                    }
                }
            }

            _incomingConnectionsLock.ExitWriteLock();
        }

        /// <summary>
        /// Runs the heartbeat thread.
        /// </summary>
        private void RunHeartbeat()
        {
            ThreadHelper.ResponsiveSleep(5000, () => Running);
            while (Running)
            {
                List<Message> messages = new List<Message>();
                _outgoingConnectionsLock.EnterReadLock();

                foreach (var conn in _outgoingConnections)
                {
                    Message message = new Message(conn.Key, "Heartbeat", new Document(), false);
                    messages.Add(message);
                    SendMessage(message);
                }

                _outgoingConnectionsLock.ExitReadLock();

                _incomingConnectionsLock.EnterReadLock();

                foreach (var conn in _incomingConnections)
                {
                    Message message = new Message(conn.Key, "Heartbeat", new Document(), false)
                    {
                        Type = ConnectionType.Incoming
                    };
                    messages.Add(message);
                    SendMessage(message);
                }

                _incomingConnectionsLock.ExitReadLock();

                messages.ForEach(e => e.BlockUntilDone());

                ThreadHelper.ResponsiveSleep(1000, () => Running);
            }
        }

        /// <summary>
        /// Cleans up any disconnected connections and expired messages.
        /// </summary>
        private void RunMaintenance()
        {
            ThreadHelper.ResponsiveSleep(5000, () => Running);
            while (Running)
            {
                lock (_waitingForResponses)
                {
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
                }

                foreach (var n in _connectedNodes.Except(GetOutgoingConnectedNodes()))
                {
                    ConnectInternal(n);
                }

                ThreadHelper.ResponsiveSleep(1000, () => Running);
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

            if (connections.ContainsKey(message.Address) && (connections[message.Address].Status == ConnectionStatus.Connected || !message.RequireSecureConnection))
            {
                try
                {
                    if (message.WaitingForResponse)
                    {
                        lock (_waitingForResponses)
                        {
                            _waitingForResponses.Add(message.Id, message);
                        }
                    }

                    byte[] dataToSend = message.EncodeMessage();
                    var stream = connections[message.Address].Client.GetStream();
                    stream.Write(dataToSend, 0, dataToSend.Length);

                    message.Status = message.WaitingForResponse ? MessageStatus.WaitingForResponse : MessageStatus.Sent;
                }
                catch
                {
                    message.Status = MessageStatus.SendingFailure;

                    lock (_waitingForResponses)
                    {
                        _waitingForResponses.Remove(message.Id);
                    }
                }
            }
            else
            {
                message.Status = MessageStatus.SendingFailure;
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

            if (message.Type == ConnectionType.Outgoing)
            {
                SendMessageToNode(message, _outgoingConnectionsLock, _outgoingConnections);
            }
            else
            {
                SendMessageToNode(message, _incomingConnectionsLock, _incomingConnections);
            }

            if (message.Status == MessageStatus.SendingFailure)
            {
                DisconnectInternal(message.Address);
            }
        }
    }
}