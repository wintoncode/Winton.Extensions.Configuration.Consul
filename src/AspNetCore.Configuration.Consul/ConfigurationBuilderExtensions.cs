using Chocolate.AspNetCore.Configuration.Consul;

namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// Extensions for the IConfigurationBuilder that provide syntactic sugar for using the ConsulConfigurationSource
    /// </summary>
    public static class ConfigurationBuilderExtensions
    {
        /// <inheritdoc/>
        public static IConfigurationBuilder AddConsul(this IConfigurationBuilder builder)
        {
            return builder.Add(new ConsulConfigurationSource());
        }
    }
}