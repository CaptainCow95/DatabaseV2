namespace DatabaseLibrary.Networking.Messaging
{
    /// <summary>
    /// Represents a request for a node's predecessor.
    /// </summary>
    internal class ChordPredecessorRequest : MessageData
    {
        /// <inheritdoc />
        protected override byte[] EncodeData()
        {
            return new byte[0];
        }

        /// <inheritdoc />
        protected override MessageData.MessageType GetMessageTypeId()
        {
            return MessageType.ChordPredecessorRequest;
        }
    }
}