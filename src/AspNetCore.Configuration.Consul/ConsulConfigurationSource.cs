using System;
using System.Net.Http;
using System.Threading;
using Chocolate.AspNetCore.Configuration.Consul.Parsers;
using Chocolate.AspNetCore.Configuration.Consul.Parsers.Json;
using Consul;
using Microsoft.Extensions.Configuration;

namespace Chocolate.AspNetCore.Configuration.Consul
{
    internal sealed class ConsulConfigurationSource : IConsulConfigurationSource
    {
        public ConsulConfigurationSource(string key, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            Key = key;
            CancellationToken = cancellationToken;
            Parser = new JsonConfigurationParser();
        }

        public CancellationToken CancellationToken { get; set; }

        public Action<ConsulClientConfiguration> ConsulConfigurationOptions { get; set; }

        public Action<HttpClient> ConsulHttpClientOptions { get; set; }

        public Action<HttpClientHandler> ConsulHttpClientHandlerOptions { get; set; }

        public string Key { get; }

        public Action<ConsulLoadExceptionContext> OnLoadException { get; set; }

        public Action<ConsulWatchExceptionContext> OnWatchException { get; set; }

        public bool Optional { get; set; } = false;

        public IConfigurationParser Parser { get; set; }

        public bool ReloadOnChange { get; set; } = false;

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            var consulClientFactory = new ConsulClientFactory(this);
            var consulConfigClient = new ConsulConfigurationClient(consulClientFactory, this);
            return new ConsulConfigurationProvider(this, consulConfigClient);
        }
    }
}