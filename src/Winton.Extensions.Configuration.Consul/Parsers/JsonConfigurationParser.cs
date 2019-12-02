// Copyright (c) Winton. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace Winton.Extensions.Configuration.Consul.Parsers
{
    /// <inheritdoc />
    /// <summary>
    ///     Implementation of <see cref="IConfigurationParser" /> for parsing JSON Configuration.
    /// </summary>
    public sealed class JsonConfigurationParser : IConfigurationParser
    {
        /// <inheritdoc />
        public IDictionary<string, string> Parse(Stream stream)
        {
            return new ConfigurationBuilder()
                .AddJsonStream(stream)
                .Build()
                .AsEnumerable()
                .ToDictionary(pair => pair.Key, pair => pair.Value);
        }
    }
}