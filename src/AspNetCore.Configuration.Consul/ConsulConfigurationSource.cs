using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;

namespace Chocolate.AspNetCore.Configuration.Consul
{
    /// <summary>
    /// An IConfigurationSource for the ConsulConfigurationProvider
    /// </summary>
    public sealed class ConsulConfigurationSource : IConfigurationSource
    {
        public ConsulConfigurationSource()
        {
            FileConfigurationProvider = new JsonConfigurationProvider
        }

        /// <summary>
        /// The name of the application in consul.
        /// Used as the top level key when querying Consul.
        /// </summary>
        public string ApplicationName { get; set; }

        /// <summary>
        /// The environment for which the configuration should be loaded.
        /// Defaults to IHostingEnvironment.Environment.
        /// </summary>
        public string EnvironmentName { get; set; }

        /// <summary>
        /// The FileConfigurationProvider to use when parsing the config.
        /// Allows different data formats to be stored in consul under the given key.
        /// Defaults to JsonConfigurationProvider
        /// </summary>
        public FileConfigurationProvider ConfigurationProvider { get; set; }

        /// <summary>
        /// Determines if loading the config is optional.
        /// </summary>
        public bool Optional { get; set; }

        /// <summary>
        /// Determines whether the source will be loaded if the data in consul changes.
        /// </summary>
        public bool ReloadOnChange { get; set; }

        /// <inheritdoc/>
        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            JsonConfigurationSource source = new JsonConfigurationSource();
            JsonConfigurationProvider provider = new JsonConfigurationProvider(source);
            return new ConsulConfigurationProvider(this);
        }
    }
}