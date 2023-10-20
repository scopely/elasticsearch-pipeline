using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Scopely.Elasticsearch
{
    class ElasticsearchUrl
    {
        static readonly Regex _awsRegionRegex = new Regex($@"\.(<region>[^\.]+)\.es\.amazonaws\.com$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        readonly string _url;

        public ElasticsearchUrl(string url)
        {
            _url = url;
            var builder = new UriBuilder(url);
            var match = _awsRegionRegex.Match(builder.Host);
            if (match.Success)
            {
                AwsRegion = match.Groups["region"].Value;
            }
        }

        public string? AwsRegion { get; private set; }

        public override string ToString() => _url;
    }
}
