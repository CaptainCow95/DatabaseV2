namespace DatabaseLibrary.Networking.Messaging
{
    /// <summary>
    /// Represents a request to join a network.
    /// </summary>
    internal class JoinRequest : MessageData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JoinRequest"/> class.
        /// </summary>
        /// <param name="port">The port the node is running on.</param>
        public JoinRequest(int port)
        {
            Address = new NodeDefinition("localhost", port);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JoinRequest"/> class.
        /// </summary>
        /// <param name="data">The data to decode.</param>
        /// <param name="index">The index in the data.</param>
        internal JoinRequest(byte[] data, int index)
        {
            Address = new NodeDefinition(ByteArrayHelper.ToString(data, ref index), ByteArrayHelper.ToInt32(data, ref index));
        }

        /// <summary>
        /// Gets the address the request came from.
        /// </summary>
        public NodeDefinition Address { get; private set; }

        /// <inheritdoc />
        protected override byte[] EncodeData()
        {
            return ByteArrayHelper.Combine(ByteArrayHelper.ToBytes(Address.Hostname), ByteArrayHelper.ToBytes(Address.Port));
        }

        /// <inheritdoc />
        protected override MessageType GetMessageTypeId()
        {
            return MessageType.JoinRequest;
        }
    }
}