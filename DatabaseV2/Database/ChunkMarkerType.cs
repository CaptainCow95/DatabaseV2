namespace DatabaseV2.Database
{
    /// <summary>
    /// The different types of chunk marker types.
    /// </summary>
    public enum ChunkMarkerType
    {
        /// <summary>
        /// The start of the database.
        /// </summary>
        Start,

        /// <summary>
        /// A value in the database.
        /// </summary>
        Value,

        /// <summary>
        /// The end of the database.
        /// </summary>
        End
    }
}