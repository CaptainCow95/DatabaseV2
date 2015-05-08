namespace Library.Networking
{
    /// <summary>
    /// Represents the status of a message.
    /// </summary>
    public enum MessageStatus
    {
        /// <summary>
        /// The message has been created.
        /// </summary>
        Created,

        /// <summary>
        /// The message is in the process of being sent.
        /// </summary>
        Sending,

        /// <summary>
        /// The message has successfully been sent.
        /// </summary>
        Sent,

        /// <summary>
        /// The message encountered an error during sending.
        /// </summary>
        SendingFailure,

        /// <summary>
        /// The message is waiting for a response.
        /// </summary>
        WaitingForResponse,

        /// <summary>
        /// The message has received a response.
        /// </summary>
        ResponseReceived,

        /// <summary>
        /// The message was received from another node.
        /// </summary>
        Received,

        /// <summary>
        /// The message failed to get a response before the connection was severed.
        /// </summary>
        ResponseFailure,

        /// <summary>
        /// The message timed out while waiting for a response.
        /// </summary>
        ResponseTimeout
    }
}