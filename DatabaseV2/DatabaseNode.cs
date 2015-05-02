namespace DatabaseV2
{
    /// <summary>
    /// Represents a node in the database.
    /// </summary>
    public class DatabaseNode
    {
        /// <summary>
        /// The settings of the database.
        /// </summary>
        private Settings _settings;

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseNode"/> class.
        /// </summary>
        /// <param name="settings">The settings to use.</param>
        public DatabaseNode(Settings settings)
        {
            _settings = settings;
        }
    }
}