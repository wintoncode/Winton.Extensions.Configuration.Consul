// Copyright (c) Winton. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENCE in the project root for license information.

using System.Net;
using Consul;

namespace Winton.Extensions.Configuration.Consul.Extensions
{
    internal static class KvPairQueryResultExtensions
    {
        internal static bool HasValue(this QueryResult<KVPair> queryResult)
        {
            return queryResult?.StatusCode != HttpStatusCode.NotFound
                   && queryResult.Value()?.Length > 0;
        }

        internal static byte[] Value(this QueryResult<KVPair> queryResult)
        {
            return queryResult?.Response?.Value;
        }
    }
}