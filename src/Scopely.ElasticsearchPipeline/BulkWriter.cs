using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Scopely.Elasticsearch
{
    public class BulkWriter
    {
        readonly JsonSerializer _serializer;
        readonly TextWriter _writer;

        public BulkWriter(Stream stream, bool omitTypeHeaders)
        {
            _serializer = new JsonSerializer();
            if (omitTypeHeaders)
            {
                _serializer.ContractResolver = NoTypeContractResolver.Instance;
            }
            _writer = new StreamWriter(stream, new UTF8Encoding(false))
            {
                NewLine = "\n",
            };
        }

        public BulkWriter Append(object value)
        {
            _serializer.Serialize(_writer, value);
            _writer.WriteLine();
            return this;
        }

        public void Flush()
        {
            _writer.Flush();
        }
    }
}
