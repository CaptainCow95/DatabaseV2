namespace DatabaseLibrary.Networking.Messaging
{
    /// <summary>
    /// Represents the result of attempting to join a network.
    /// </summary>
    internal class JoinResult : MessageData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JoinResult"/> class.
        /// </summary>
        public JoinResult()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JoinResult"/> class.
        /// </summary>
        /// <param name="data">The data to decode.</param>
        /// <param name="index">The index in the data.</param>
        public JoinResult(byte[] data, int index)
        {
        }

        /// <inheritdoc />
        protected override byte[] EncodeData()
        {
            return new byte[0];
        }

        /// <inheritdoc />
        protected override MessageType GetMessageTypeId()
        {
            return MessageType.JoinResult;
        }
    }
}