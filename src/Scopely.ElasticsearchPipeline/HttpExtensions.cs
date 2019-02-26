using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Scopely.Elasticsearch
{
    static class HttpExtensions
    {
        public static async Task<string> GetDumpAsync(this HttpResponseMessage response)
        {
            var sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
            {
                await response.DumpToAsync(writer);
                writer.Flush();
            }
            return sb.ToString();
        }

        public static async Task DumpToAsync(this HttpResponseMessage response, TextWriter writer)
        {
            writer.Write("HTTP ");
            writer.WriteLine((int)response.StatusCode);
            foreach (var header in response.Headers)
            {
                foreach (var val in header.Value)
                {
                    writer.Write(header.Key);
                    writer.Write(": ");
                }
            }
            if (response.Content != null)
            {
                using (var s = await response.Content.ReadAsStreamAsync())
                using (var reader = new StreamReader(s))
                {
                    var buff = new char[1024];
                    int len = 0;
                    for (int i = 0; i < 4; ++i)
                    {
                        len = await reader.ReadBlockAsync(buff, 0, buff.Length);
                        if (len <= 0) break;
                        await writer.WriteAsync(buff, 0, len);
                    }
                    if (len > 0)
                    {
                        writer.WriteLine("\n<==== Response Truncated Due to Length ====>");
                    }
                }
            }
        }
    }
}
