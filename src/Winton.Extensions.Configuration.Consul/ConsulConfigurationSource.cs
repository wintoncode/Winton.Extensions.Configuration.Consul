// Copyright (c) Winton. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENCE in the project root for license information.

using System;
using System.Net.Http;
using System.Threading;
using Consul;
using Microsoft.Extensions.Configuration;
using Winton.Extensions.Configuration.Consul.Parsers;
using Winton.Extensions.Configuration.Consul.Parsers.Json;

namespace Winton.Extensions.Configuration.Consul
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

        public CancellationToken CancellationToken { get; }

        public Action<ConsulClientConfiguration> ConsulConfigurationOptions { get; set; }

        public Action<HttpClientHandler> ConsulHttpClientHandlerOptions { get; set; }

        public Action<HttpClient> ConsulHttpClientOptions { get; set; }

        public string Key { get; }

        public string KeyToRemove { get; set; }

        public Action<ConsulLoadExceptionContext> OnLoadException { get; set; }

        public Func<ConsulWatchExceptionContext, TimeSpan> OnWatchException { get; set; }

        public bool Optional { get; set; } = false;

        public IConfigurationParser Parser { get; set; }

        public bool ReloadOnChange { get; set; } = false;

        public bool PersistenceToLocal { get; set; } = false;

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            var consulClientFactory = new ConsulClientFactory(this);
            var consulConfigClient = new ConsulConfigurationClient(consulClientFactory);
            return new ConsulConfigurationProvider(this, consulConfigClient);
        }
    }
}