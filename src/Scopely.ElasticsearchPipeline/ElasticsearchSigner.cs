using Amazon.Runtime;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Scopely.Elasticsearch
{
    static class ElasticsearchSigner
    {
        static readonly Func<AWS4RequestSigner> _getSigner = Cache.Wrap(TimeSpan.FromMinutes(1), () =>
        {
            var creds = FallbackCredentialsFactory.GetCredentials().GetCredentials();
            return new AWS4RequestSigner(
                creds.AccessKey,
                creds.SecretKey,
                creds.UseToken ? creds.Token : null);
        });

        private static AWS4RequestSigner Signer => _getSigner();

        static readonly Regex _hostRegex = new Regex(@"\.([\w-\d]+)\.es\.amazonaws\.com$", RegexOptions.Compiled);

        public static Task<HttpRequestMessage> SignAsync(HttpRequestMessage request)
        {
            var match = _hostRegex.Match(request.RequestUri.Host);
            if (!match.Success) return Task.FromResult(request);
            var region = match.Groups[1].Value;
            return Signer.Sign(request, "es", region);
        }
    }
}
