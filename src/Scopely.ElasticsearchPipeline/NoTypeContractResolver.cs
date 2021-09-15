using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Scopely.Elasticsearch
{
    internal class NoTypeContractResolver : DefaultContractResolver
    {
        public static NoTypeContractResolver Instance { get; } = new NoTypeContractResolver();

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty property = base.CreateProperty(member, memberSerialization);

            if (typeof(BulkOperation.Header).IsAssignableFrom(property.DeclaringType) && property.PropertyName == "_type")
            {
                property.ShouldSerialize = instance => false;
            }

            return property;
        }
    }
}
