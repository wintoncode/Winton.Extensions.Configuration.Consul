// Copyright (c) Winton. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

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
            using Stream stream = new MemoryStream(kvPair.Value);
            var baseKey = kvPair.Key;
            return parser
                .Parse(stream)
                .Select(pair => pair.NormalizeKey(kvPair.Key))
                .Select(pair => pair.RewriteIfNeeded(parser))
                .Select(pair => pair.RemoveKeyPrefix(keyToRemove));
        }

        internal static string NormalizeAsConfigKey(this string rawKey)
        {
            return rawKey.Replace('/', ':').Trim(':');
        }

        internal static KeyValuePair<string, string> NormalizeKey(this KeyValuePair<string, string> pair, string baseKey = "")
        {
            var normalizedKey = $"{baseKey.TrimEnd('/')}/{pair.Key}".NormalizeAsConfigKey();

            return new KeyValuePair<string, string>(normalizedKey, pair.Value);
        }

        internal static KeyValuePair<string, string> RewriteIfNeeded(this KeyValuePair<string, string> pair, IConfigurationParser parser)
        {
            return parser is IConfigurationKeyValueRewriter rewriter ? rewriter.Rewrite(pair) : pair;
        }

        internal static KeyValuePair<string, string> RemoveKeyPrefix(this KeyValuePair<string, string> pair, string keyToRemove = "")
        {
            string cleanedKey = pair.Key.RemoveStart(keyToRemove.NormalizeAsConfigKey()).Trim(':');

            if (string.IsNullOrEmpty(cleanedKey))
            {
                throw new InvalidKeyPairException(
                    "The key must not be null or empty. Ensure that there is at least one key under the root of the config or that the data there contains more than just a single value.");
            }

            return new KeyValuePair<string, string>(cleanedKey, pair.Value);
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