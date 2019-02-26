using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Scopely.Elasticsearch;
using Microsoft.Extensions.Logging;

namespace Scopely.Elasticsearch.Tests
{
    [TestFixture]
    public class ElasticsearchPipelineTests
    {
        public static BulkOperation[] _operations = new BulkOperation[]
        {
            new BulkUpdateOperation
            {
                Id = "123",
                Index = "test-index",
                Type = "test-type",
                Doc = new { foo = "bar", foo_n = 1.1 },
                DocAsUpsert = true,
            },
            new BulkDeleteOperation
            {
                Id = "124",
                Index = "test-index",
                Type = "test-type",
            }
        };

        string _rawText;
        string[] _bulkLines;

        [SetUp]
        public async Task SetUp()
        {
            var output = new MemoryStream();
            var target = new ActionBlock<byte[]>(m => output.Write(m, 0, m.Length));
            var pipeline = new ElasticsearchPipeline(target, new ElasticsearchPipelineOptions
            {
                TargetBulkSizeInBytes = 10 << 10,
            });
            foreach (var op in _operations)
            {
                await pipeline.SendAsync(op);
            }
            await pipeline.ShutdownAsync();
            _rawText = Encoding.UTF8.GetString(output.ToArray());
            Console.Write(_rawText);
            output.Position = 0;
            IEnumerable<string> GetLines()
            {
                using (var reader = new StreamReader(output))
                {
                    while (reader.Peek() > 0)
                    {
                        yield return reader.ReadLine();
                    }
                }
            }
            _bulkLines = GetLines().ToArray();
        }

        [Test]
        public void Text_should_have_a_final_endline()
        {
            Assert.AreEqual('\n', _rawText[_rawText.Length - 1]);
        }

        [Test]
        public void Update_header_should_have_expected_values()
        {
            var updateHeader = JObject.Parse(_bulkLines[0]);
            var update = updateHeader["update"];
            Assert.AreEqual(JTokenType.Object, update.Type);
            Assert.AreEqual(JTokenType.String, update["_id"].Type);
            Assert.AreEqual("123", update["_id"].Value<string>());
            Assert.AreEqual(JTokenType.String, update["_index"].Type);
            Assert.AreEqual("test-index", update["_index"].Value<string>());
            Assert.AreEqual(JTokenType.String, update["_type"].Type);
            Assert.AreEqual("test-type", update["_type"].Value<string>());
        }

        [Test]
        public void Update_doc_should_have_expected_values()
        {
            var updateDoc = JObject.Parse(_bulkLines[1]);
            var doc = updateDoc["doc"];
            Assert.AreEqual(JTokenType.Object, doc.Type);
            Assert.AreEqual(JTokenType.String, doc["foo"].Type);
            Assert.AreEqual("bar", doc["foo"].Value<string>());
            Assert.AreEqual(JTokenType.Float, doc["foo_n"].Type);
            Assert.AreEqual("1.1", doc["foo_n"].Value<string>());
        }

        [Test]
        public void Delete_should_have_expected_values()
        {
            var deleteHeader = JObject.Parse(_bulkLines[2]);
            var delete = deleteHeader["delete"];
            Assert.AreEqual(JTokenType.Object, delete.Type);
            Assert.AreEqual(JTokenType.String, delete["_id"].Type);
            Assert.AreEqual("124", delete["_id"].Value<string>());
            Assert.AreEqual(JTokenType.String, delete["_index"].Type);
            Assert.AreEqual("test-index", delete["_index"].Value<string>());
            Assert.AreEqual(JTokenType.String, delete["_type"].Type);
            Assert.AreEqual("test-type", delete["_type"].Value<string>());
        }

        [Test]
        public void All_lines_should_be_valid_json()
        {
            foreach (var line in _bulkLines)
            {
                var obj = JObject.Parse(line);
                Assert.IsNotNull(obj);
            }
        }
    }
}
