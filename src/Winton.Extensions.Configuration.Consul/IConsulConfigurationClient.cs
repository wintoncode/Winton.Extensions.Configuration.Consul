using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;

namespace Winton.Extensions.Configuration.Consul
{
    internal interface IConsulConfigurationClient
    {
        Task<IConfigQueryResult> GetConfig();

        IChangeToken Watch(Action<ConsulWatchExceptionContext> onException);
    }
}