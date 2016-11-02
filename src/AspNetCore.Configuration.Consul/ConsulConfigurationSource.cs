using Microsoft.Extensions.Configuration;

namespace Chocolate.AspNetCore.Configuration.Consul
{
    public sealed class ConsulConfigurationSource : IConfigurationSource
    {
        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new ConsulConfigurationProvider();
        }
    }
}