﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DatabaseV2
{
    /// <summary>
    /// Represents an entry in a document.
    /// </summary>
    public class DocumentEntry
    {
        /// <summary>
        /// The key of the entry.
        /// </summary>
        private readonly string _key;

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
        /// <param name="key">The key for the entry.</param>
        /// <param name="value">The value for the entry.</param>
        /// <param name="type">The type for the entry.</param>
        public DocumentEntry(string key, object value, DocumentEntryType type)
        {
            _key = key;
            _value = value;
            _valueType = type;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentEntry"/> class.
        /// </summary>
        /// <param name="reader">The <see cref="JsonTextReader"/> to read from.</param>
        /// <param name="arrayEntry">A value indicating whether this is an array entry.</param>
        public DocumentEntry(JsonTextReader reader, bool arrayEntry)
        {
            if (!arrayEntry)
            {
                _key = (string)reader.Value;
                reader.Read();
            }

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
        /// Gets the key.
        /// </summary>
        public string Key
        {
            get { return _key; }
        }

        /// <summary>
        /// Gets the value as an object.
        /// </summary>
        public object Value
        {
            get { return _value; }
        }

        /// <summary>
        /// Gets the value as an array.
        /// </summary>
        public List<DocumentEntry> ValueAsArray
        {
            get
            {
                if (_valueType == DocumentEntryType.Array)
                {
                    return (List<DocumentEntry>)_value;
                }

                throw new DataTypeException(_value.GetType(), typeof(List<DocumentEntry>));
            }
        }

        /// <summary>
        /// Gets a value indicating whether the value is true or false.
        /// </summary>
        public bool ValueAsBoolean
        {
            get
            {
                if (_valueType == DocumentEntryType.Boolean)
                {
                    return Convert.ToBoolean(_value);
                }

                throw new DataTypeException(_value.GetType(), typeof(bool));
            }
        }

        /// <summary>
        /// Gets the value as a document.
        /// </summary>
        public Document ValueAsDocument
        {
            get
            {
                if (_valueType == DocumentEntryType.Document)
                {
                    return (Document)_value;
                }

                throw new DataTypeException(_value.GetType(), typeof(Document));
            }
        }

        /// <summary>
        /// Gets the value as a float.
        /// </summary>
        public float ValueAsFloat
        {
            get
            {
                if (_valueType == DocumentEntryType.Float)
                {
                    return Convert.ToSingle(_value);
                }

                throw new DataTypeException(_value.GetType(), typeof(float));
            }
        }

        /// <summary>
        /// Gets the value as an integer.
        /// </summary>
        public int ValueAsInteger
        {
            get
            {
                if (_valueType == DocumentEntryType.Integer)
                {
                    return Convert.ToInt32(_value);
                }

                throw new DataTypeException(_value.GetType(), typeof(int));
            }
        }

        /// <summary>
        /// Gets the value as a string.
        /// </summary>
        public string ValueAsString
        {
            get
            {
                if (_valueType == DocumentEntryType.String)
                {
                    return Convert.ToString(_value);
                }

                throw new DataTypeException(_value.GetType(), typeof(string));
            }
        }

        /// <summary>
        /// Gets the type of the value.
        /// </summary>
        public DocumentEntryType ValueType
        {
            get { return _valueType; }
        }

        /// <summary>
        /// Writes the entry to a <see cref="JsonTextWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="JsonTextWriter"/> to write to.</param>
        public void Write(JsonTextWriter writer)
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
                entries.Add(new DocumentEntry(reader, true));
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
                    type = DocumentEntryType.Float;
                    break;

                case JsonToken.Integer:
                    type = DocumentEntryType.Integer;
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