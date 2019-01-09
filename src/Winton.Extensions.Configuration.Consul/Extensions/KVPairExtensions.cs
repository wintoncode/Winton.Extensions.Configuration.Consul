// Copyright (c) Winton. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENCE in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Consul;
using Winton.Extensions.Configuration.Consul.Parsers;

namespace Winton.Extensions.Configuration.Consul.Extensions
{
    internal static class KVPairExtensions
    {
        internal static IEnumerable<KeyValuePair<string, string>> ConvertToConfig(
            this KVPair kvPair,
            string keyToRemove,
            IConfigurationParser parser)
        {
            using (Stream stream = new MemoryStream(kvPair.Value))
            {
                return parser
                    .Parse(stream)
                    .Select(
                        pair =>
                        {
                            string key = $"{kvPair.Key.RemoveStart(keyToRemove).TrimEnd('/')}:{pair.Key}"
                                .Replace('/', ':')
                                .Trim(':');
                            if (string.IsNullOrEmpty(key))
                            {
                                throw new InvalidKeyPairException(
                                    "The key must not be null or empty. Ensure that there is at least one key under the root of the config or that the data there contains more than just a single value.");
                            }

                            return new KeyValuePair<string, string>(key, pair.Value);
                        });
            }
        }

        internal static bool HasValue(this KVPair kvPair)
        {
            return kvPair.IsLeafNode() && kvPair.Value != null && kvPair.Value.Any();
        }

        internal static bool IsLeafNode(this KVPair kvPair)
        {
            return !kvPair.Key.EndsWith("/");
        }

        private static string RemoveStart(this string s, string toRemove)
        {
            return s.StartsWith(toRemove) ? s.Remove(0, toRemove.Length) : s;
        }
    }
}