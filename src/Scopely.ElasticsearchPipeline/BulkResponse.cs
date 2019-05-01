using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Scopely.Elasticsearch
{
    class BulkResponse
    {
        [JsonProperty("errors")]
        public bool Errors { get; set; }
    }
}
