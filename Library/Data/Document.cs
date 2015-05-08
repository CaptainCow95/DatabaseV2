using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Library.Data
{
    /// <summary>
    /// Represents a json document.
    /// </summary>
    public class Document : IEnumerable<KeyValuePair<string, DocumentEntry>>
    {
        /// <summary>
        /// The entries in this document.
        /// </summary>
        private readonly Dictionary<string, DocumentEntry> _data = new Dictionary<string, DocumentEntry>();

        /// <summary>
        /// A value indicating whether the document is value.
        /// </summary>
        private readonly bool _valid = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="Document"/> class.
        /// </summary>
        public Document()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Document"/> class.
        /// </summary>
        /// <param name="reader">The json reader to initialize the document with.</param>
        public Document(JsonTextReader reader)
        {
            while (reader.Read() && reader.TokenType != JsonToken.EndObject)
            {
                DocumentEntry entry = new DocumentEntry(reader, false);
                _data.Add(entry.Key, entry);
            }

            if (reader.TokenType != JsonToken.EndObject)
            {
                throw new ArgumentException("Invalid json given.");
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Document"/> class.
        /// </summary>
        /// <param name="json">The json to initialize the document with.</param>
        public Document(string json)
        {
            using (StringReader stringReader = new StringReader(json))
            using (JsonTextReader reader = new JsonTextReader(stringReader))
            {
                reader.Read();
                if (reader.TokenType != JsonToken.StartObject)
                {
                    throw new ArgumentException("Invalid json given.");
                }

                while (reader.Read() && reader.TokenType != JsonToken.EndObject)
                {
                    DocumentEntry entry = new DocumentEntry(reader, false);
                    if (entry.ValueType == DocumentEntryType.Document && !entry.ValueAsDocument.Valid)
                    {
                        _valid = false;
                        return;
                    }

                    _data.Add(entry.Key, entry);
                }

                if (reader.TokenType != JsonToken.EndObject)
                {
                    throw new ArgumentException("Invalid json given.");
                }

                if (reader.Read())
                {
                    throw new ArgumentException("Invalid json given.");
                }
            }
        }

        /// <summary>
        /// Gets the count of items in the document.
        /// </summary>
        public int Count
        {
            get { return _data.Count; }
        }

        /// <summary>
        /// Gets a value indicating whether this document is valid.
        /// </summary>
        public bool Valid
        {
            get { return _valid; }
        }

        /// <inheritdoc />
        public DocumentEntry this[string key]
        {
            get
            {
                if (key.Contains("."))
                {
                    string subfield = key.Substring(0, key.IndexOf(".", StringComparison.InvariantCulture));
                    if (_data.ContainsKey(subfield) && _data[subfield].ValueType == DocumentEntryType.Document)
                    {
                        return _data[subfield].ValueAsDocument[key.Substring(subfield.Length + 1)];
                    }
                }
                else
                {
                    return _data[key];
                }

                throw new KeyNotFoundException("Could not find the key \"" + key + "\".");
            }

            set
            {
                if (key.Contains("."))
                {
                    string subfield = key.Substring(0, key.IndexOf(".", StringComparison.InvariantCulture));
                    if (_data.ContainsKey(subfield) && _data[subfield].ValueType == DocumentEntryType.Document)
                    {
                        _data[subfield].ValueAsDocument[key.Substring(subfield.Length + 1)] = value;
                    }
                }
                else
                {
                    _data[key] = value;
                }
            }
        }

        /// <summary>
        /// Checks if the document contains the specified key.
        /// </summary>
        /// <param name="key">The key to search for.</param>
        /// <returns>True if the key was found, otherwise false.</returns>
        public bool ContainsKey(string key)
        {
            if (key.Contains("."))
            {
                string subfield = key.Substring(0, key.IndexOf(".", StringComparison.InvariantCulture));
                if (_data.ContainsKey(subfield) && _data[subfield].ValueType == DocumentEntryType.Document)
                {
                    return _data[subfield].ValueAsDocument.ContainsKey(key.Substring(subfield.Length + 1));
                }
            }
            else
            {
                return _data.ContainsKey(key);
            }

            return false;
        }

        /// <inheritdoc />
        public IEnumerator<KeyValuePair<string, DocumentEntry>> GetEnumerator()
        {
            return _data.GetEnumerator();
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _data.GetEnumerator();
        }

        /// <summary>
        /// Converts a document to json.
        /// </summary>
        /// <returns>The json represents the document.</returns>
        public string ToJson()
        {
            StringBuilder builder = new StringBuilder();

            using (StringWriter stringWriter = new StringWriter(builder))
            using (JsonTextWriter writer = new JsonTextWriter(stringWriter))
            {
                Write(writer);
            }

            return builder.ToString();
        }

        /// <summary>
        /// Writes the entry to a <see cref="JsonTextWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="JsonTextWriter"/> to write to.</param>
        public void Write(JsonTextWriter writer)
        {
            writer.WriteStartObject();

            foreach (var item in _data)
            {
                writer.WritePropertyName(item.Key);
                item.Value.Write(writer);
            }

            writer.WriteEndObject();
        }
    }
}