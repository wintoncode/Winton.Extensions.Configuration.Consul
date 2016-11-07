using Microsoft.Extensions.Configuration;

namespace Chocolate.AspNetCore.Configuration.Consul
{
    /// <summary>
    /// Provides configuration from Consul
    /// </summary>
    public interface IConsulConfigurationProvider : IConfigurationProvider
    {
        /// <summary>
        /// The source settings for this provider.
        /// </summary>
        IConsulConfigurationSource Source { get; }
    }
}