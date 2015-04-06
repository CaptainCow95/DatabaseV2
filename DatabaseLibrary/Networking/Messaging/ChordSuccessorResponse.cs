namespace DatabaseLibrary.Networking.Messaging
{
    /// <summary>
    /// Represents a response to a <see cref="ChordSuccessorRequest"/>.
    /// </summary>
    internal class ChordSuccessorResponse : MessageData
    {
        /// <summary>
        /// The chord ID of the node.
        /// </summary>
        private readonly uint _chordId;

        /// <summary>
        /// The successor node.
        /// </summary>
        private readonly NodeDefinition _successor;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChordSuccessorResponse"/> class.
        /// </summary>
        /// <param name="successor">The successor node.</param>
        /// <param name="chordId">The chord ID of the node.</param>
        public ChordSuccessorResponse(NodeDefinition successor, uint chordId)
        {
            _successor = successor;
            _chordId = chordId;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChordSuccessorResponse"/> class.
        /// </summary>
        /// <param name="data">The data to decode.</param>
        /// <param name="index">The index in the data.</param>
        internal ChordSuccessorResponse(byte[] data, int index)
        {
            _successor = new NodeDefinition(ByteArrayHelper.ToString(data, ref index), ByteArrayHelper.ToInt32(data, ref index));
            _chordId = ByteArrayHelper.ToUInt32(data, ref index);
        }

        /// <summary>
        /// Gets the chord ID of the node.
        /// </summary>
        public uint ChordID
        {
            get { return _chordId; }
        }

        /// <summary>
        /// Gets the successor node.
        /// </summary>
        public NodeDefinition Successor
        {
            get { return _successor; }
        }

        /// <inheritdoc />
        protected override byte[] EncodeData()
        {
            return ByteArrayHelper.Combine(ByteArrayHelper.ToBytes(_successor.Hostname), ByteArrayHelper.ToBytes(_successor.Port), ByteArrayHelper.ToBytes(_chordId));
        }

        /// <inheritdoc />
        protected override MessageData.MessageType GetMessageTypeId()
        {
            return MessageType.ChordSuccessorResponse;
        }
    }
}