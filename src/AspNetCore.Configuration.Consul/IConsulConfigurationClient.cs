using System.IO;
using System.Threading.Tasks;

namespace Chocolate.AspNetCore.Configuration.Consul
{
    internal interface IConsulConfigurationClient
    {
        Task<Stream> GetConfig(string key, bool optional);
    }
}