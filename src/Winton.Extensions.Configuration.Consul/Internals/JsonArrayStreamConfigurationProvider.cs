// Copyright (c) Winton. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;

namespace Winton.Extensions.Configuration.Consul.Internals
{
    internal sealed class JsonArrayStreamConfigurationProvider : StreamConfigurationProvider
    {
        public JsonArrayStreamConfigurationProvider(JsonArrayStreamConfigurationSource source)
            : base(source)
        {
        }

        /// <inheritdoc />
        /// <summary>
        ///     Loads json configuration key/values from a stream into a provider.
        /// </summary>
        /// <param name="stream">The json <see cref="Stream"/> to load configuration data from.</param>
        public override void Load(Stream stream)
        {
            Data = JsonConfigurationFileParser.Parse(stream);
        }
    }
}