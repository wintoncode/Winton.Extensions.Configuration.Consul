// Copyright (c) Winton. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENCE in the project root for license information.

using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Winton.Extensions.Configuration.Consul.Parsers.Json
{
    /// <inheritdoc />
    /// <summary>
    ///     Implemenation of <see cref="IConfigurationParser" /> for parsing JSON Configuration
    /// </summary>
    public sealed class JsonConfigurationParser : IConfigurationParser
    {
        /// <inheritdoc />
        public IDictionary<string, string> Parse(Stream stream)
        {
            using (var streamReader = new StreamReader(stream))
            using (var jsonReader = new JsonTextReader(streamReader))
            {
                jsonReader.DateParseHandling = DateParseHandling.None;
                JObject jsonConfig = JObject.Load(jsonReader);
                return jsonConfig.Flatten();
            }
        }
    }
}