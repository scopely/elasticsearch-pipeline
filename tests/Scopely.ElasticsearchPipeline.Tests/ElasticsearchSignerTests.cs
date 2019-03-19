using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Scopely.Elasticsearch.Tests
{
    [TestFixture]
    public class ElasticsearchSignerTests
    {
        static HttpClient _client;

        [OneTimeSetUp]
        public static void SetUp()
        {
            _client = new HttpClient();
        }

        [OneTimeTearDown]
        public static void TearDown()
        {
            _client.Dispose();
        }

        // note add integration tests here when needed
        //[TestCase("url here")]
        public async Task Get_url_should_be_successful(string url)
        {
            var message = new HttpRequestMessage(HttpMethod.Get, url);
            await ElasticsearchSigner.SignAsync(message);
            using (var response = await _client.SendAsync(message))
            {
                if (response.IsSuccessStatusCode) return;
                var body = await response.Content.ReadAsStringAsync();
                TestContext.WriteLine($"HTTP {(int)response.StatusCode}");
                TestContext.WriteLine(body);
                Assert.Fail($"Expected 2xx reponse but got {(int)response.StatusCode}");
            }
        }
    }
}
