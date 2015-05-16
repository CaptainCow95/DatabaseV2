using System;

namespace DatabaseV2.Database
{
    /// <summary>
    /// Represents a marker between chunks in the database.
    /// </summary>
    public class ChunkMarker
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ChunkMarker"/> class.
        /// </summary>
        /// <param name="type">The marker type.</param>
        public ChunkMarker(ChunkMarkerType type)
        {
            if (type == ChunkMarkerType.Value)
            {
                throw new ArgumentException("Wrong constructor called for initializing to a value.");
            }

            Type = type;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChunkMarker"/> class.
        /// </summary>
        /// <param name="value">The value of the marker.</param>
        public ChunkMarker(string value)
        {
            Value = value;
            Type = ChunkMarkerType.Value;
        }

        /// <summary>
        /// Gets the marker's type.
        /// </summary>
        public ChunkMarkerType Type { get; private set; }

        /// <summary>
        /// Gets the marker's value.
        /// </summary>
        public string Value { get; private set; }

        /// <summary>
        /// Converts a chunk marker from a string.
        /// </summary>
        /// <param name="s">The string to convert from.</param>
        /// <returns>The marker represented by the string.</returns>
        public static ChunkMarker ConvertFromString(string s)
        {
            switch (s)
            {
                case "start":
                    return new ChunkMarker(ChunkMarkerType.Start);

                case "end":
                    return new ChunkMarker(ChunkMarkerType.End);

                default:
                    return new ChunkMarker(s.Substring(6));
            }
        }

        /// <inheritdoc />
        public override string ToString()
        {
            switch (Type)
            {
                case ChunkMarkerType.Start:
                    return "start";

                case ChunkMarkerType.End:
                    return "end";

                default:
                    return "value " + Value;
            }
        }
    }
}