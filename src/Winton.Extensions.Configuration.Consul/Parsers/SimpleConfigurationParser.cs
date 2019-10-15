// Copyright (c) Winton. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.Collections.Generic;
using System.IO;

namespace Winton.Extensions.Configuration.Consul.Parsers
{
    /// <inheritdoc />
    /// <summary>
    ///     Implementation of <see cref="IConfigurationParser" /> for parsing simple values.
    /// </summary>
    public sealed class SimpleConfigurationParser : IConfigurationParser
    {
        /// <inheritdoc />
        public IDictionary<string, string> Parse(Stream stream)
        {
            using var streamReader = new StreamReader(stream);
            return new Dictionary<string, string> { { string.Empty, streamReader.ReadToEnd() } };
        }
    }
}