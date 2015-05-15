using Library.Data;
using NUnit.Framework;
using System.Collections.Generic;

namespace UnitTests.Data
{
    /// <summary>
    /// Tests the <see cref="Document"/> class.
    /// </summary>
    [TestFixture]
    public class DocumentTest
    {
        /// <summary>
        /// Tests the insertion of an array.
        /// </summary>
        [Test]
        public void Array()
        {
            string key = "test";
            List<DocumentEntry> value = new List<DocumentEntry>() { "1", 2.0, false };
            Document doc = new Document()
            {
                { key, value }
            };

            Assert.AreEqual(DocumentEntryType.Array, doc[key].ValueType, "The type was not an array.");
            Assert.AreEqual(value, doc[key].ValueAsArray());
            Assert.AreEqual(1, doc.Count);
            Assert.IsTrue(doc.Valid);
        }

        /// <summary>
        /// Tests the insertion of a boolean.
        /// </summary>
        [Test]
        public void Bool()
        {
            string key = "test";
            bool value = true;
            Document doc = new Document()
            {
                { key, value }
            };

            Assert.AreEqual(DocumentEntryType.Boolean, doc[key].ValueType, "The type was not a boolean.");
            Assert.AreEqual(value, doc[key].ValueAsBoolean());
            Assert.AreEqual(1, doc.Count);
            Assert.IsTrue(doc.Valid);
        }

        /// <summary>
        /// Tests the insertion of a document.
        /// </summary>
        [Test]
        public void Document()
        {
            string key = "test";
            Document value = new Document()
            {
                { "key", "value" }
            };
            Document doc = new Document()
            {
                { key, value }
            };

            Assert.AreEqual(DocumentEntryType.Document, doc[key].ValueType, "The type was not a document.");
            Assert.AreEqual(value, doc[key].ValueAsDocument());
            Assert.AreEqual(1, doc.Count);
            Assert.IsTrue(doc.Valid);
        }

        /// <summary>
        /// Tests the insertion of a double.
        /// </summary>
        [Test]
        public void Double()
        {
            string key = "test";
            double value = 5.64;
            Document doc = new Document()
            {
                { key, value }
            };

            Assert.AreEqual(DocumentEntryType.Double, doc[key].ValueType, "The type was not a double.");
            Assert.AreEqual(value, doc[key].ValueAsDouble());
            Assert.AreEqual(1, doc.Count);
            Assert.IsTrue(doc.Valid);
        }

        /// <summary>
        /// Tests an empty document.
        /// </summary>
        [Test]
        public void Empty()
        {
            Document doc = new Document();
            Assert.AreEqual("{}", doc.ToJson());
            Assert.AreEqual(0, doc.Count);
            Assert.IsTrue(doc.Valid);
        }

        /// <summary>
        /// Tests the insertion of a 64-bit integer.
        /// </summary>
        [Test]
        public void Int64()
        {
            string key = "test";
            long value = 9876543210;
            Document doc = new Document()
            {
                { key, value }
            };

            Assert.AreEqual(DocumentEntryType.Int64, doc[key].ValueType, "The type was not a 64-bit integer.");
            Assert.AreEqual(value, doc[key].ValueAsInt64());
            Assert.AreEqual(1, doc.Count);
            Assert.IsTrue(doc.Valid);
        }

        /// <summary>
        /// Tests converting a document to json.
        /// </summary>
        [Test]
        public void JsonConversion()
        {
            string json = "{\"array\":[\"value\",true,5],\"bool\":true,\"document\":{\"key\":\"value\"},\"double\":5.64,\"int64\":9876543210,\"string\":\"value\"}";
            Document doc = new Document()
            {
                { "array", new List<DocumentEntry> { "value", true, 5 } },
                { "bool", true },
                { "document", new Document { { "key", "value" } } },
                { "double", 5.64 },
                { "int64", 9876543210 },
                { "string", "value" }
            };

            Assert.IsTrue(doc.Valid);
            Assert.AreEqual(6, doc.Count);
            Assert.AreEqual(json, doc.ToJson());
        }

        /// <summary>
        /// Tests initializing from json.
        /// </summary>
        [Test]
        public void JsonInit()
        {
            string json = "{\"array\":[\"value\",true,5],\"bool\":true,\"document\":{\"key\":\"value\"},\"double\":5.64,\"int64\":9876543210,\"string\":\"value\"}";
            Document doc = new Document(json);

            Assert.IsTrue(doc.Valid);
            Assert.AreEqual(6, doc.Count);
            Assert.AreEqual(new List<DocumentEntry> { "value", true, 5 }, doc["array"].ValueAsArray());
            Assert.AreEqual(true, doc["bool"].ValueAsBoolean());
            Assert.AreEqual(new Document { { "key", "value" } }, doc["document"].ValueAsDocument());
            Assert.That(doc["double"].ValueAsDouble(), Is.EqualTo(5.64).Within(double.Epsilon));
            Assert.AreEqual(9876543210, doc["int64"].ValueAsInt64());
            Assert.AreEqual("value", doc["string"].ValueAsString());
            Assert.AreEqual(json, doc.ToJson());
        }

        /// <summary>
        /// Tests the insertion of a string.
        /// </summary>
        [Test]
        public void String()
        {
            string key = "test";
            string value = "string";
            Document doc = new Document()
            {
                { key, value }
            };

            Assert.AreEqual(DocumentEntryType.String, doc[key].ValueType, "The type was not a string.");
            Assert.AreEqual(value, doc[key].ValueAsString());
            Assert.AreEqual(1, doc.Count);
            Assert.IsTrue(doc.Valid);
        }
    }
}