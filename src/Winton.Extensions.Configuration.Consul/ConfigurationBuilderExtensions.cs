using System;
using System.Threading;
using Microsoft.Extensions.Configuration;

namespace Winton.Extensions.Configuration.Consul
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
        /// <param name="builder">The builder to add consul to.</param>
        /// <param name="key">The key in consul where the configuration is located.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to cancel any open Consul connections or watchers.</param>
        /// <returns>The builder</returns>
        public static IConfigurationBuilder AddConsul(this IConfigurationBuilder builder, string key, CancellationToken cancellationToken)
        {
            return builder.AddConsul(key, cancellationToken, options => {});
        }

        /// <summary>
        /// Adds Consul as a configuration source to the <see cref="IConfigurationBuilder"/>
        /// and applies the given overrides to the <see cref="IConsulConfigurationSource"/>.
        /// </summary>
        /// <param name="builder">The builder to add consul to</param>
        /// <param name="key">The key in consul where the configuration is located</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to cancel any open Consul connections or watchers.</param>
        /// <param name="options">An action used to configure the options of the <see cref="IConsulConfigurationSource"/></param>
        /// <returns>The builder</returns>
        public static IConfigurationBuilder AddConsul(this IConfigurationBuilder builder, string key, CancellationToken cancellationToken, Action<IConsulConfigurationSource> options)
        {
            var consulConfigSource = new ConsulConfigurationSource(key, cancellationToken);
            options(consulConfigSource);
            return builder.Add(consulConfigSource);
        }
    }
}