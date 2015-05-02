namespace DatabaseV2.Networking
{
    /// <summary>
    /// Represents the status of a connection.
    /// </summary>
    public enum ConnectionStatus
    {
        /// <summary>
        /// The connection is active.
        /// </summary>
        Connected,

        /// <summary>
        /// The connection is being identified.
        /// </summary>
        Identifying,

        /// <summary>
        /// The connection has been disconnected.
        /// </summary>
        Disconnected
    }
}