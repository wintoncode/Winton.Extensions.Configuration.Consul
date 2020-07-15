// Copyright (c) Winton. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Winton.Extensions.Configuration.Consul.Internals;

namespace Winton.Extensions.Configuration.Consul.Extensions
{
    /// <summary>
    /// Extra Extesntions for Json Array Parsing.
    /// </summary>
    public static class ConfigurationbuilderExtensions
    {
        /// <summary>
        /// Adds a JSON Array configuration source to <paramref name="builder"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="stream">The <see cref="Stream"/> to read the json configuration data from.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        public static IConfigurationBuilder AddJsonArrayStream(this IConfigurationBuilder builder, Stream stream)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.Add<JsonArrayStreamConfigurationSource>(s => s.Stream = stream);
        }
    }
}