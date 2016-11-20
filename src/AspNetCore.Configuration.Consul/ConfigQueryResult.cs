using System.Net;
using Consul;

namespace Chocolate.AspNetCore.Configuration.Consul
{
    internal sealed class ConfigQueryResult : IConfigQueryResult
    {
        public ConfigQueryResult(QueryResult<KVPair> kvPairQueryResult)
        {
            Exists = kvPairQueryResult?.StatusCode != HttpStatusCode.NotFound
                && kvPairQueryResult?.Response?.Value != null
                && kvPairQueryResult?.Response?.Value.Length != 0;
            Value = kvPairQueryResult?.Response?.Value;
        }

        public bool Exists { get; }

        public byte[] Value { get; }
    }
}