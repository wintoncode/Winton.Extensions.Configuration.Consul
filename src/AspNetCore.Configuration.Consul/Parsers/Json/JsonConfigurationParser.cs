using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace Chocolate.AspNetCore.Configuration.Consul.Parsers.Json
{
    /// <summary>
    /// Implemenation of <see cref="IConfigurationParser"/> for parsing JSON Configuration
    /// </summary>
    public sealed class JsonConfigurationParser : IConfigurationParser
    {
        /// <inheritdoc/>
        public IDictionary<string, string> Parse(Stream stream)
        {
            using (var streamReader = new StreamReader(stream))
            using (var reader = new JsonTextReader(new StreamReader(stream)))
            {
                var flattener = new JsonFlattener(reader);
                return flattener.Flatten();
            }
        }
    }
}