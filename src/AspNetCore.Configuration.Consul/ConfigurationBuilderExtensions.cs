using Microsoft.Extensions.Configuration;

namespace Chocolate.AspNetCore.Configuration.Consul
{
    public static class ConfigurationBuilderExtensions
    {
        /// <\inheritdoc>
        public static IConfigurationBuilder AddConsul(this IConfigurationBuilder builder)
        {
            return builder.Add(new ConsulConfigurationSource());
        }
    }
}