using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Consul;

namespace Chocolate.AspNetCore.Configuration.Consul
{
    internal sealed class ConsulConfigurationClient : IConsulConfigurationClient
    {
        private readonly IConsulClientFactory _consulClientFactory;

        public ConsulConfigurationClient(IConsulClientFactory consulClientFactory)
        {
            _consulClientFactory = consulClientFactory;
        }

        public async Task<Stream> GetConfig(string key, bool optional)
        {
            using (IConsulClient consulClient = _consulClientFactory.Create())
            {
                QueryResult<KVPair> result = await consulClient.KV.Get(key);
                switch (result.StatusCode)
                {
                    case HttpStatusCode.OK:
                        return new MemoryStream(result.Response.Value);
                    case HttpStatusCode.NotFound:
                        if (optional) 
                        {
                            return new MemoryStream();
                        }
                        throw new Exception($"The configuration for key {key} was not found and is not optional.");
                    default:
                        throw new Exception($"Error loading configuration from consul. Status code: {result.StatusCode}.");
                }
            }
        }
    }
}