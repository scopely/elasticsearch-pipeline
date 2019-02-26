using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Logging;

namespace Scopely.Elasticsearch
{
    public class ElasticsearchPipeline
    {
        readonly ITargetBlock<BulkOperation> _target;
        readonly Task _completion;

        public ElasticsearchPipeline(
            string[] targetUrls,
            ElasticsearchPipelineOptions options)
            : this(CreateBroadcastBulkWriterBlock(targetUrls, options), options)
        {
        }

        public ElasticsearchPipeline(
            ITargetBlock<byte[]> finalTarget,
            ElasticsearchPipelineOptions options)
        {
            var target = CreateBulkRequestBodyBlock(options.TargetBulkSizeInBytes);
            target.LinkTo(finalTarget, new DataflowLinkOptions { PropagateCompletion = true });
            _target = target;
            _completion = finalTarget.Completion;
        }

        public Task SendAsync(BulkOperation operation) => _target.SendAsync(operation);

        public Task ShutdownAsync()
        {
            _target.Complete();
            return _completion;
        }

        private static ITargetBlock<byte[]> CreateBroadcastBulkWriterBlock(string[] urls, ElasticsearchPipelineOptions options)
        {
            var target = new BroadcastBlock<byte[]>(b => b, new DataflowBlockOptions { BoundedCapacity = 1 });
            var tasks = new List<Task>();
            foreach (var url in urls)
            {
                var subTarget = CreateBulkWriterBlock(url, options);
                target.LinkTo(subTarget, new DataflowLinkOptions { PropagateCompletion = true });
                tasks.Add(subTarget.Completion);
            }
            return new TargetBlockWithCompletion<byte[]>(target, Task.WhenAll(tasks));
        }

        private static ITargetBlock<byte[]> CreateBulkWriterBlock(string url, ElasticsearchPipelineOptions options)
        {
            var logger = options.Logger;
            var postUrl = new Uri(new Uri(url), "_bulk");
            var client = options.HttpClientFactory?.Invoke() ?? new HttpClient();
            var block = new ActionBlock<byte[]>(async body =>
            {
                var request = new HttpRequestMessage(HttpMethod.Post, postUrl)
                {
                    Content = new ByteArrayContent(body)
                };
                if (logger?.IsEnabled(LogLevel.Debug) ?? false) logger.LogDebug($"Signing {request.Method} {request.RequestUri} ...");
                request = await ElasticsearchSigner.SignAsync(request);
                if (logger?.IsEnabled(LogLevel.Debug) ?? false) logger.LogDebug($"Sending {request.Method} {request.RequestUri} ...");
                var response = await client.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    logger?.LogError(await response.GetDumpAsync());
                    throw new Exception($"Got {response.StatusCode} response from {postUrl}.");
                }
                else
                {
                    logger?.LogInformation($"Wrote {body.Length:n0} bytes /_bulk API");
                }
            });
            block.Completion.ContinueWith(t => client.Dispose());
            return block;
        }

        private static IPropagatorBlock<BulkOperation, byte[]> CreateBulkRequestBodyBlock(int targetSizeInBytes)
        {
            var transformOpts = new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 1 };
            var stream = new MemoryStream();
            var writer = new BulkWriter(stream);
            var source = new BufferBlock<byte[]>();
            var target = new ActionBlock<BulkOperation>(async op =>
            {
                op.Write(writer);
                writer.Flush();
                if (stream.Length >= targetSizeInBytes)
                {
                    stream.Position = 0;
                    await source.SendAsync(stream.ToArray()).ConfigureAwait(false);
                    stream.SetLength(0);
                    writer = new BulkWriter(stream);
                }
            }, transformOpts);
            target.Completion.ContinueWith(async t =>
            {
                if (stream.Length > 0)
                {
                    stream.Position = 0;
                    await source.SendAsync(stream.ToArray()).ConfigureAwait(false);
                    stream.SetLength(0);
                }
                source.Complete();
            });
            return DataflowBlock.Encapsulate(target, source);
        }
    }
}
