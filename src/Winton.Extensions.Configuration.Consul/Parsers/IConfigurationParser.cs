using System.Collections.Generic;
using System.IO;

namespace Winton.Extensions.Configuration.Consul.Parsers
{
    /// <summary>
    /// Defines how the configuration loaded from Consul should be parsed.
    /// </summary>
    public interface IConfigurationParser
    {
        /// <summary>
        /// Parse the <see cref="Stream"/> into a dictionary.
        /// </summary>
        IDictionary<string, string> Parse(Stream stream);
    }
}