using Library.Data;
using System;
using System.Threading;

namespace Library.Networking
{
    /// <summary>
    /// Represents a network message.
    /// </summary>
    public class Message
    {
        /// <summary>
        /// The object to lock on when giving out the next message's ID.
        /// </summary>
        private static readonly object NextIdLockObject = new object();

        /// <summary>
        /// The ID to use for the next message.
        /// </summary>
        private static uint _nextId = 0;

        /// <summary>
        /// The expire time of the message.
        /// </summary>
        private readonly DateTime _expireTime;

        /// <summary>
        /// The ID of the message.
        /// </summary>
        private readonly uint _id;

        /// <summary>
        /// The message ID the message is in response to.
        /// </summary>
        private readonly uint _inResponseTo;

        /// <summary>
        /// The type of the message.
        /// </summary>
        private readonly string _messageType;

        /// <summary>
        /// A value indicating whether the message is waiting for a response.
        /// </summary>
        private readonly bool _waitingForResponse;

        /// <summary>
        /// A value indicating whether an already identified connection to send the message.
        /// </summary>
        private bool _requireSecureConnection = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="Message"/> class.
        /// </summary>
        /// <param name="address">The address to send the message to.</param>
        /// <param name="messageType">The type of the message.</param>
        /// <param name="data">The data contained in the message.</param>
        /// <param name="waitingForResponse">A value indicating whether the message is waiting for a response.</param>
        /// <param name="timeout">The number of seconds before the message times out.</param>
        public Message(NodeDefinition address, string messageType, Document data, bool waitingForResponse, uint timeout = 60)
        {
            Address = address;
            _messageType = messageType;
            Data = data;
            _id = GetNextId();
            _waitingForResponse = waitingForResponse;
            _expireTime = DateTime.UtcNow.AddSeconds(timeout);
            Status = MessageStatus.Created;
            Type = ConnectionType.Outgoing;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Message"/> class.
        /// </summary>
        /// <param name="responseTo">The message that this message is in response to.</param>
        /// <param name="messageType">The type of the message.</param>
        /// <param name="data">The data contained in the message.</param>
        /// <param name="waitingForResponse">A value indicating whether the message is waiting for a response.</param>
        /// <param name="timeout">The number of seconds before the message times out.</param>
        public Message(Message responseTo, string messageType, Document data, bool waitingForResponse, uint timeout = 60)
            : this(responseTo.Address, messageType, data, waitingForResponse, timeout)
        {
            _inResponseTo = responseTo._id;
            Type = responseTo.Type;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Message"/> class.
        /// </summary>
        /// <param name="address">The address the message is from.</param>
        /// <param name="data">The data that represents the message.</param>
        /// <param name="type">The type of connection the message came from.</param>
        public Message(NodeDefinition address, byte[] data, ConnectionType type)
        {
            Address = address;
            Type = type;

            int index = 0;
            _id = ByteArrayHelper.ToUInt32(data, ref index);
            _inResponseTo = ByteArrayHelper.ToUInt32(data, ref index);
            _waitingForResponse = ByteArrayHelper.ToBoolean(data, ref index);
            _messageType = ByteArrayHelper.ToString(data, ref index);
            Data = new Document(ByteArrayHelper.ToString(data, ref index));

            Status = MessageStatus.Received;
        }

        /// <summary>
        /// Gets or sets the location to send the message to.
        /// </summary>
        public NodeDefinition Address { get; set; }

        /// <summary>
        /// Gets the data contained in the message.
        /// </summary>
        public Document Data { get; private set; }

        /// <summary>
        /// Gets the expire time of the message.
        /// </summary>
        public DateTime ExpireTime
        {
            get { return _expireTime; }
        }

        /// <summary>
        /// Gets the ID of the message.
        /// </summary>
        public uint Id
        {
            get { return _id; }
        }

        /// <summary>
        /// Gets the message ID the message is in response to.
        /// </summary>
        public uint InResponseTo
        {
            get { return _inResponseTo; }
        }

        /// <summary>
        /// Gets the message type.
        /// </summary>
        public string MessageType
        {
            get { return _messageType; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether an already identified connection to send the message.
        /// </summary>
        public bool RequireSecureConnection
        {
            get { return _requireSecureConnection; }
            set { _requireSecureConnection = value; }
        }

        /// <summary>
        /// Gets or sets the response to the message.
        /// </summary>
        public Message Response { get; set; }

        /// <summary>
        /// Gets or sets the function to call when a response is received.
        /// </summary>
        public Action<Message> ResponseCallback { get; set; }

        /// <summary>
        /// Gets or sets the status of the message.
        /// </summary>
        public MessageStatus Status { get; set; }

        /// <summary>
        /// Gets a value indicating whether the message was sent successfully if it is not waiting for a response, otherwise it indicates whether the response has been successfully received.
        /// </summary>
        public bool Success
        {
            get { return Status == MessageStatus.Sent || Status == MessageStatus.ResponseReceived; }
        }

        /// <summary>
        /// Gets the type of the connection.
        /// </summary>
        public ConnectionType Type { get; internal set; }

        /// <summary>
        /// Gets a value indicating whether the message is waiting for a response.
        /// </summary>
        public bool WaitingForResponse
        {
            get { return _waitingForResponse; }
        }

        /// <summary>
        /// Blocks until an error occurs, the message is sent successfully if it isn't waiting for a response, or until a response is received if it is waiting for a response.
        /// </summary>
        public void BlockUntilDone()
        {
            while (Status == MessageStatus.Sending || Status == MessageStatus.WaitingForResponse)
            {
                Thread.Sleep(1);
            }
        }

        /// <summary>
        /// Encodes a message for transfer.
        /// </summary>
        /// <returns>The encoded message.</returns>
        public byte[] EncodeMessage()
        {
            byte[] idBytes = ByteArrayHelper.ToBytes(_id);
            byte[] inResponseToBytes = ByteArrayHelper.ToBytes(_inResponseTo);
            byte[] waitingForResponseBytes = ByteArrayHelper.ToBytes(_waitingForResponse);
            byte[] messageTypeBytes = ByteArrayHelper.ToBytes(_messageType);
            byte[] data = ByteArrayHelper.ToBytes(Data.ToJson());
            int length = idBytes.Length + inResponseToBytes.Length + waitingForResponseBytes.Length + messageTypeBytes.Length + data.Length;

            return ByteArrayHelper.Combine(ByteArrayHelper.ToBytes(length), idBytes, inResponseToBytes, waitingForResponseBytes, messageTypeBytes, data);
        }

        /// <summary>
        /// Gets the next message ID.
        /// </summary>
        /// <returns>The next message ID.</returns>
        private static uint GetNextId()
        {
            Monitor.Enter(NextIdLockObject);
            ++_nextId;
            if (_nextId == 0)
            {
                ++_nextId;
            }

            uint temp = _nextId;
            Monitor.Exit(NextIdLockObject);
            return temp;
        }
    }
}