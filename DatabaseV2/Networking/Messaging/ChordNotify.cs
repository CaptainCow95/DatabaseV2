namespace DatabaseV2.Networking.Messaging
{
    /// <summary>
    /// Represents a notify message.
    /// </summary>
    public class ChordNotify : MessageData
    {
        /// <summary>
        /// The chord ID of the node.
        /// </summary>
        private readonly uint _chordId;

        /// <summary>
        /// The node sending the notify.
        /// </summary>
        private readonly NodeDefinition _node;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChordNotify"/> class.
        /// </summary>
        /// <param name="node">The node sending the notify.</param>
        /// <param name="chordId">The chord ID of the node.</param>
        public ChordNotify(NodeDefinition node, uint chordId)
        {
            _node = node;
            _chordId = chordId;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChordNotify"/> class.
        /// </summary>
        /// <param name="data">The data to decode.</param>
        /// <param name="index">The index in the data.</param>
        public ChordNotify(byte[] data, int index)
        {
            _node = new NodeDefinition(ByteArrayHelper.ToString(data, ref index), ByteArrayHelper.ToInt32(data, ref index));
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
        /// Gets the node sending the notify.
        /// </summary>
        public NodeDefinition Node
        {
            get { return _node; }
        }

        /// <inheritdoc />
        protected override byte[] EncodeData()
        {
            return ByteArrayHelper.Combine(ByteArrayHelper.ToBytes(_node.Hostname), ByteArrayHelper.ToBytes(_node.Port), ByteArrayHelper.ToBytes(_chordId));
        }

        /// <inheritdoc />
        protected override MessageType GetMessageTypeId()
        {
            return MessageType.ChordNotify;
        }
    }
}