// Copyright (c) Winton. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENCE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Consul;
using Winton.Extensions.Configuration.Consul.Parsers;

namespace Winton.Extensions.Configuration.Consul.Extensions
{
    internal static class KVPairQueryResultExtensions
    {
        internal static bool HasValue(this QueryResult<KVPair[]> queryResult)
        {
            return queryResult != null
                && queryResult.StatusCode != HttpStatusCode.NotFound
                && queryResult.Response != null
                && queryResult.Response.Any(kvp => kvp.HasValue());
        }
    }
}