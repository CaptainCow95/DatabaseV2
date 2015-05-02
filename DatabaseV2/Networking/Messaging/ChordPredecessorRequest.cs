namespace DatabaseV2.Networking.Messaging
{
    /// <summary>
    /// Represents a request for a node's predecessor.
    /// </summary>
    public class ChordPredecessorRequest : MessageData
    {
        /// <inheritdoc />
        protected override byte[] EncodeData()
        {
            return new byte[0];
        }

        /// <inheritdoc />
        protected override MessageType GetMessageTypeId()
        {
            return MessageType.ChordPredecessorRequest;
        }
    }
}