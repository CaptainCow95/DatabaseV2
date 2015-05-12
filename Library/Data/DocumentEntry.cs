using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Library.Data
{
    /// <summary>
    /// Represents an entry in a document.
    /// </summary>
    public class DocumentEntry
    {
        /// <summary>
        /// The value of the entry.
        /// </summary>
        private readonly object _value;

        /// <summary>
        /// The type of the value.
        /// </summary>
        private readonly DocumentEntryType _valueType;

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentEntry"/> class.
        /// </summary>
        /// <param name="value">The value for the entry.</param>
        public DocumentEntry(List<DocumentEntry> value)
            : this(value, DocumentEntryType.Array)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentEntry"/> class.
        /// </summary>
        /// <param name="value">The value for the entry.</param>
        public DocumentEntry(bool value)
            : this(value, DocumentEntryType.Boolean)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentEntry"/> class.
        /// </summary>
        /// <param name="value">The value for the entry.</param>
        public DocumentEntry(double value)
            : this(value, DocumentEntryType.Double)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentEntry"/> class.
        /// </summary>
        /// <param name="value">The value for the entry.</param>
        public DocumentEntry(Document value)
            : this(value, DocumentEntryType.Document)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentEntry"/> class.
        /// </summary>
        /// <param name="value">The value for the entry.</param>
        public DocumentEntry(string value)
            : this(value, DocumentEntryType.String)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentEntry"/> class.
        /// </summary>
        /// <param name="value">The value for the entry.</param>
        public DocumentEntry(long value)
            : this(value, DocumentEntryType.Int64)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentEntry"/> class.
        /// </summary>
        /// <param name="reader">The <see cref="JsonTextReader"/> to read from.</param>
        internal DocumentEntry(JsonTextReader reader)
        {
            switch (reader.TokenType)
            {
                case JsonToken.StartArray:
                    _value = ReadArray(reader);
                    _valueType = DocumentEntryType.Array;
                    break;

                case JsonToken.StartObject:
                    _value = ReadDocument(reader);
                    _valueType = DocumentEntryType.Document;
                    break;

                default:
                    var result = ReadValue(reader);
                    _value = result.Item1;
                    _valueType = result.Item2;
                    break;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentEntry"/> class.
        /// </summary>
        /// <param name="value">The value for the entry.</param>
        /// <param name="type">The type for the entry.</param>
        private DocumentEntry(object value, DocumentEntryType type)
        {
            _value = value;
            _valueType = type;
        }

        /// <summary>
        /// Gets the value as an object.
        /// </summary>
        public object Value
        {
            get { return _value; }
        }

        /// <summary>
        /// Gets the type of the value.
        /// </summary>
        public DocumentEntryType ValueType
        {
            get { return _valueType; }
        }

        /// <summary>
        /// Gets the value as an array.
        /// </summary>
        /// <returns>The value as an array.</returns>
        public List<DocumentEntry> ValueAsArray()
        {
            if (_valueType == DocumentEntryType.Array)
            {
                return (List<DocumentEntry>)_value;
            }

            throw new DataTypeException(_value.GetType(), typeof(List<DocumentEntry>));
        }

        /// <summary>
        /// Gets a value indicating whether the value is true or false.
        /// </summary>
        /// <returns>The value as a boolean.</returns>
        public bool ValueAsBoolean()
        {
            if (_valueType == DocumentEntryType.Boolean)
            {
                return Convert.ToBoolean(_value);
            }

            throw new DataTypeException(_value.GetType(), typeof(bool));
        }

        /// <summary>
        /// Gets the value as a document.
        /// </summary>
        /// <returns>The value as a document.</returns>
        public Document ValueAsDocument()
        {
            if (_valueType == DocumentEntryType.Document)
            {
                return (Document)_value;
            }

            throw new DataTypeException(_value.GetType(), typeof(Document));
        }

        /// <summary>
        /// Gets the value as a double.
        /// </summary>
        /// <returns>The value as a double.</returns>
        public double ValueAsDouble()
        {
            if (_valueType == DocumentEntryType.Double)
            {
                return Convert.ToDouble(_value);
            }

            throw new DataTypeException(_value.GetType(), typeof(float));
        }

        /// <summary>
        /// Gets the value as an 64-bit integer.
        /// </summary>
        /// <returns>The value as a 64-bit integer.</returns>
        public long ValueAsInt64()
        {
            if (_valueType == DocumentEntryType.Int64)
            {
                return Convert.ToInt64(_value);
            }

            throw new DataTypeException(_value.GetType(), typeof(int));
        }

        /// <summary>
        /// Gets the value as a string.
        /// </summary>
        /// <returns>The value as a string.</returns>
        public string ValueAsString()
        {
            if (_valueType == DocumentEntryType.String)
            {
                return Convert.ToString(_value);
            }

            throw new DataTypeException(_value.GetType(), typeof(string));
        }

        /// <summary>
        /// Writes the entry to a <see cref="JsonTextWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="JsonTextWriter"/> to write to.</param>
        internal void Write(JsonTextWriter writer)
        {
            switch (_valueType)
            {
                case DocumentEntryType.Array:
                    writer.WriteStartArray();

                    foreach (var item in (List<DocumentEntry>)_value)
                    {
                        item.Write(writer);
                    }

                    writer.WriteEndArray();
                    break;

                case DocumentEntryType.Document:
                    ((Document)_value).Write(writer);
                    break;

                default:
                    writer.WriteValue(_value);
                    break;
            }
        }

        /// <summary>
        /// Reads an array from a <see cref="JsonTextReader"/>.
        /// </summary>
        /// <param name="reader">The <see cref="JsonTextReader"/> to read from.</param>
        /// <returns>The document that was read.</returns>
        private List<DocumentEntry> ReadArray(JsonTextReader reader)
        {
            List<DocumentEntry> entries = new List<DocumentEntry>();
            while (reader.Read() && reader.TokenType != JsonToken.EndArray)
            {
                entries.Add(new DocumentEntry(reader));
            }

            return entries;
        }

        /// <summary>
        /// Reads a document from a <see cref="JsonTextReader"/>.
        /// </summary>
        /// <param name="reader">The <see cref="JsonTextReader"/> to read from.</param>
        /// <returns>The document that was read.</returns>
        private Document ReadDocument(JsonTextReader reader)
        {
            return new Document(reader);
        }

        /// <summary>
        /// Reads a value from a <see cref="JsonTextReader"/>.
        /// </summary>
        /// <param name="reader">The <see cref="JsonTextReader"/> to read from.</param>
        /// <returns>The object and type that was read.</returns>
        private Tuple<object, DocumentEntryType> ReadValue(JsonTextReader reader)
        {
            DocumentEntryType type;
            switch (reader.TokenType)
            {
                case JsonToken.Boolean:
                    type = DocumentEntryType.Boolean;
                    break;

                case JsonToken.Float:
                    type = DocumentEntryType.Double;
                    break;

                case JsonToken.Integer:
                    type = DocumentEntryType.Int64;
                    break;

                case JsonToken.String:
                    type = DocumentEntryType.String;
                    break;

                default:
                    throw new ArgumentException("Could not read the specified json token type.");
            }

            return new Tuple<object, DocumentEntryType>(reader.Value, type);
        }
    }
}