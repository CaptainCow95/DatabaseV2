namespace Library.Logging
{
    /// <summary>
    /// Represents the different logging levels.
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// Only error messages will be logged.
        /// </summary>
        Error,

        /// <summary>
        /// Error and warning messages will be logged.
        /// </summary>
        Warning,

        /// <summary>
        /// Error, warning, and info messages will be logged.
        /// </summary>
        Info,

        /// <summary>
        /// Error, warning, info, and debug messages will be logged.
        /// </summary>
        Debug
    }
}