using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Scopely.Elasticsearch
{
    public abstract class BulkOperation
    {
        public string? Index { get; set; }

        public string? Type { get; set; }

        public string? Id { get; set; }

        public abstract void Write(BulkWriter writer);

        protected internal class Header
        {
            public string? _index;
            public string? _type;
            public string? _id;

            public Header(BulkOperation op)
            {
                _index = op.Index;
                _type = op.Type;
                _id = op.Id;
            }
        }
    }

    public abstract class BulkDocumentOperation : BulkOperation
    {
        private static readonly object _defaultDoc = new();
        public object Doc { get; set; } = _defaultDoc;
    }

    public sealed class BulkUpdateOperation : BulkDocumentOperation
    {
        public int? RetryOnConflict { get; set; }

        public bool DocAsUpsert { get; set; }

        public override void Write(BulkWriter writer) => writer
            .Append(new { update = new UpdateHeader(this) })
            .Append(new DocPayload(this));

        private class DocPayload
        {
            public DocPayload(BulkUpdateOperation op)
            {
                doc = op.Doc;
                doc_as_upsert = op.DocAsUpsert;
            }

            public object doc { get; set; }
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public bool doc_as_upsert { get; set; }
        }

        private class UpdateHeader : Header
        {
            public UpdateHeader(BulkUpdateOperation op)
                : base(op)
            {
                _retry_on_conflict = op.RetryOnConflict;
            }

            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public int? _retry_on_conflict;
        }
    }

    // replaces the doc
    public sealed class BulkIndexOperation : BulkDocumentOperation
    {
        public override void Write(BulkWriter writer) => writer
            .Append(new { index = new Header(this) })
            .Append(Doc);
    }

    // creates new if it doesn't exist
    public sealed class BulkCreateOperation : BulkDocumentOperation
    {
        public override void Write(BulkWriter writer) => writer
            .Append(new { create = new Header(this) })
            .Append(Doc);
    }

    // deletes the doc
    public sealed class BulkDeleteOperation : BulkOperation
    {
        public override void Write(BulkWriter writer) => writer
            .Append(new { delete = new Header(this) });
    }
}
