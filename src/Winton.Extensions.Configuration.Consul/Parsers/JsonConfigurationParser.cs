// Copyright (c) Winton. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration.Json;

namespace Winton.Extensions.Configuration.Consul.Parsers
{
    /// <inheritdoc />
    /// <summary>
    ///     Implementation of <see cref="IConfigurationParser" /> for parsing JSON Configuration.
    /// </summary>
    public sealed class JsonConfigurationParser : IConfigurationParser
    {
        /// <summary>
        /// Parse the <see cref="Stream" /> into a dictionary.
        /// </summary>
        /// <param name="stream">The stream to parse.</param>
        /// <returns>A dictionary representing the configuration in a flattened form.</returns>
        public IDictionary<string, string> Parse(Stream stream)
        {
            return JsonStreamParser.Parse(stream);
        }

        private sealed class JsonStreamParser : JsonStreamConfigurationProvider
        {
            private JsonStreamParser(JsonStreamConfigurationSource source)
                : base(source)
            {
            }

            internal static IDictionary<string, string> Parse(Stream stream)
            {
                var provider = new JsonStreamParser(new JsonStreamConfigurationSource { Stream = stream });
                provider.Load();
                return provider.Data;
            }
        }
    }
}