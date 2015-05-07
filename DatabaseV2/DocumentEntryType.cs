namespace DatabaseV2
{
    /// <summary>
    /// Represents the different types of document entries.
    /// </summary>
    public enum DocumentEntryType
    {
        /// <summary>
        /// An entry that is an array.
        /// </summary>
        Array,

        /// <summary>
        /// An entry that is a boolean.
        /// </summary>
        Boolean,

        /// <summary>
        /// An entry that is a double.
        /// </summary>
        Double,

        /// <summary>
        /// An entry that is a 64-bit integer.
        /// </summary>
        Int64,

        /// <summary>
        /// An entry that is an embedded document.
        /// </summary>
        Document,

        /// <summary>
        /// An entry that is a string.
        /// </summary>
        String
    }
}