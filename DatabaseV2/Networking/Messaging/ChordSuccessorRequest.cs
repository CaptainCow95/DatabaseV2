namespace DatabaseV2.Networking.Messaging
{
    /// <summary>
    /// Represents a request for a node's successor.
    /// </summary>
    public class ChordSuccessorRequest : MessageData
    {
        /// <inheritdoc />
        protected override byte[] EncodeData()
        {
            return new byte[0];
        }

        /// <inheritdoc />
        protected override MessageType GetMessageTypeId()
        {
            return MessageType.ChordSuccessorRequest;
        }
    }
}