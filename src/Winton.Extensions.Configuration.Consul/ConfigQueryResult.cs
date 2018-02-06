// Copyright (c) Winton. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENCE in the project root for license information.

using System.Net;
using Consul;

namespace Winton.Extensions.Configuration.Consul
{
    internal sealed class ConfigQueryResult : IConfigQueryResult
    {
        public ConfigQueryResult(QueryResult<KVPair> kvPairQueryResult)
        {
            Exists = kvPairQueryResult?.StatusCode != HttpStatusCode.NotFound
                     && kvPairQueryResult?.Response?.Value != null
                     && kvPairQueryResult.Response?.Value.Length != 0;
            Value = kvPairQueryResult?.Response?.Value;
        }

        public bool Exists { get; }

        public byte[] Value { get; }
    }
}