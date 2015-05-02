namespace DatabaseV2.Networking.Messaging
{
    /// <summary>
    /// Represents a response to a <see cref="ChordPredecessorRequest"/>.
    /// </summary>
    public class ChordPredecessorResponse : MessageData
    {
        /// <summary>
        /// The chord ID of the node.
        /// </summary>
        private readonly uint _chordId;

        /// <summary>
        /// The predecessor node.
        /// </summary>
        private readonly NodeDefinition _predecessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChordPredecessorResponse"/> class.
        /// </summary>
        /// <param name="predecessor">The predecessor node.</param>
        /// <param name="chordId">The chord ID of the node.</param>
        public ChordPredecessorResponse(NodeDefinition predecessor, uint chordId)
        {
            _predecessor = predecessor;
            _chordId = chordId;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChordPredecessorResponse"/> class.
        /// </summary>
        /// <param name="data">The data to decode.</param>
        /// <param name="index">The index in the data.</param>
        public ChordPredecessorResponse(byte[] data, int index)
        {
            _predecessor = new NodeDefinition(ByteArrayHelper.ToString(data, ref index), ByteArrayHelper.ToInt32(data, ref index));
            _chordId = ByteArrayHelper.ToUInt32(data, ref index);
        }

        /// <summary>
        /// Gets the chord ID of the node.
        /// </summary>
        public uint ChordId
        {
            get { return _chordId; }
        }

        /// <summary>
        /// Gets the predecessor node.
        /// </summary>
        public NodeDefinition Predecessor
        {
            get { return _predecessor; }
        }

        /// <inheritdoc />
        protected override byte[] EncodeData()
        {
            return ByteArrayHelper.Combine(ByteArrayHelper.ToBytes(_predecessor.Hostname), ByteArrayHelper.ToBytes(_predecessor.Port), ByteArrayHelper.ToBytes(_chordId));
        }

        /// <inheritdoc />
        protected override MessageType GetMessageTypeId()
        {
            return MessageType.ChordPredecessorResponse;
        }
    }
}