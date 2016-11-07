using Consul;

namespace Chocolate.AspNetCore.Configuration.Consul
{
    internal interface IConsulClientFactory
    {
        IConsulClient Create();
    }
}