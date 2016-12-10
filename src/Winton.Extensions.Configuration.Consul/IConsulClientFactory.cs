using Consul;

namespace Winton.Extensions.Configuration.Consul
{
    internal interface IConsulClientFactory
    {
        IConsulClient Create();
    }
}