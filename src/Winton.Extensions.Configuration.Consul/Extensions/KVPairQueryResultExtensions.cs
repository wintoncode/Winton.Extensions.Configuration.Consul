// Copyright (c) Winton. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

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
        internal static bool HasValue(this QueryResult<KVPair[]> result)
        {
            return result != null
                   && result.StatusCode != HttpStatusCode.NotFound
                   && result.Response != null
                   && result.Response.Any(kvp => kvp.HasValue());
        }

        internal static Dictionary<string, string> ToConfigDictionary(
            this QueryResult<KVPair[]> result,
            string keyToRemove,
            IConfigurationParser parser)
        {
            return (result.Response ?? new KVPair[0])
                .Where(kvp => kvp.HasValue())
                .SelectMany(kvp => kvp.ConvertToConfig(keyToRemove, parser))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.OrdinalIgnoreCase);
        }
    }
}