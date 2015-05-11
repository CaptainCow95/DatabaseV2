using System;

namespace Library.Data
{
    /// <summary>
    /// Thrown when trying to get a data type out of a <see cref="DocumentEntry"/> and the data type requested does not match the actual data type.
    /// </summary>
    [Serializable]
    public class DataTypeException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataTypeException"/> class.
        /// </summary>
        /// <param name="actual">The actual type of the value.</param>
        /// <param name="expected">The expected type of the value.</param>
        public DataTypeException(Type actual, Type expected)
            : base("Asked for a type of " + expected + " but found a type of " + actual + ".")
        {
        }
    }
}