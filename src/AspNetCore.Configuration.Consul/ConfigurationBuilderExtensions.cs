using System;
using Microsoft.Extensions.Configuration;

namespace Chocolate.AspNetCore.Configuration.Consul
{
    /// <summary>
    /// Extensions for the <see cref="IConfigurationBuilder"/> that provide syntactic sugar for 
    /// using the <see cref="IConsulConfigurationSource"/>.
    /// </summary>
    public static class ConfigurationBuilderExtensions
    {
        /// <summary>
        /// Adds Consul as a configuration source to the <see cref="IConfigurationBuilder"/>
        /// using the default settings in <see cref="IConsulConfigurationSource"/>.
        /// </summary>
        public static IConfigurationBuilder AddConsul(this IConfigurationBuilder builder)
        {
            return builder.AddConsul(options => {});
        }

        /// <summary>
        /// Adds Consul as a configuration source to the <see cref="IConfigurationBuilder"/>
        /// and applies the given overrides to the <see cref="IConsulConfigurationSource"/>.
        /// </summary>
        public static IConfigurationBuilder AddConsul(this IConfigurationBuilder builder, Action<IConsulConfigurationSource> options)
        {
            var consulConfigSource = new ConsulConfigurationSource();
            options(consulConfigSource);
            return builder.Add(consulConfigSource);
        }
    }
}