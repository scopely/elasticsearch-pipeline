using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Net.Http;

namespace Scopely.Elasticsearch
{
    public class ElasticsearchPipelineOptions
    {
        public int TargetBulkSizeInBytes { get; set; } = 1 << 20;
        public ILogger Logger { get; set; } = NullLogger.Instance;
        /// <summary>
        /// False by default. True to omit _type in the bulk api.
        /// Note that _type is deprected in newer versions of elasticsearch.
        /// </summary>
        public bool OmitTypeHeaders { get; set; }
        public Func<HttpClient>? HttpClientFactory { get; set; }
    }
}
