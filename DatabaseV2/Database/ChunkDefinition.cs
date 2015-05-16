using Library.Networking;

namespace DatabaseV2.Database
{
    /// <summary>
    /// A class representing the definition of a database chunk.
    /// </summary>
    public class ChunkDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ChunkDefinition"/> class.
        /// </summary>
        /// <param name="start">The start of the chunk.</param>
        /// <param name="end">The end of the chunk.</param>
        /// <param name="owner">The owner of the chunk.</param>
        public ChunkDefinition(ChunkMarker start, ChunkMarker end, NodeDefinition owner)
        {
            Start = start;
            End = end;
            Owner = owner;
        }

        /// <summary>
        /// Gets the end marker of the chunk.
        /// </summary>
        public ChunkMarker End { get; private set; }

        /// <summary>
        /// Gets the owner of the chunk.
        /// </summary>
        public NodeDefinition Owner { get; private set; }

        /// <summary>
        /// Gets the start marker of the chunk.
        /// </summary>
        public ChunkMarker Start { get; private set; }
    }
}