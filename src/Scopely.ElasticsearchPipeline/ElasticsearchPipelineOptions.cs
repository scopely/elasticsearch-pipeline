using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;

namespace Scopely.Elasticsearch
{
    public class ElasticsearchPipelineOptions
    {
        public int TargetBulkSizeInBytes { get; set; } = 1 << 20;
        public ILogger Logger { get; set; }
        public Func<HttpClient> HttpClientFactory { get; set; }
    }
}
