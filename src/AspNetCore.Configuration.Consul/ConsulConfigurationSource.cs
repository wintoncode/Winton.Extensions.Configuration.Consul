using System;
using System.Net.Http;
using Chocolate.AspNetCore.Configuration.Consul.Parsers;
using Consul;
using Microsoft.Extensions.Configuration;

namespace Chocolate.AspNetCore.Configuration.Consul
{
    internal sealed class ConsulConfigurationSource : IConsulConfigurationSource
    {
        public ConsulConfigurationSource()
        {
            Parser = new JsonConfigurationParser();
        }

        public Action<ConsulClientConfiguration> ConsulConfigurationOptions { get; set; }

        public Action<HttpClient> ConsulHttpClientOptions { get; set; }

        public Action<HttpClientHandler> ConsulHttpClientHandlerOptions { get; set; }

        public string Key { get; set; }

        public Action<ConsulLoadExceptionContext> OnLoadException { get; set; }

        public bool Optional { get; set; } = false;

        public IConfigurationParser Parser { get; set; }

        public bool ReloadOnChange { get; set; } = false;

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            var consulClientFactory = new ConsulClientFactory(this);
            var consulConfigClient = new ConsulConfigurationClient(consulClientFactory);
            return new ConsulConfigurationProvider(this, consulConfigClient);
        }
    }
}