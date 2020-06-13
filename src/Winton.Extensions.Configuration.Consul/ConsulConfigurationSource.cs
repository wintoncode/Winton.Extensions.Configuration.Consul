// Copyright (c) Winton. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net.Http;
using Consul;
using Microsoft.Extensions.Configuration;
using Winton.Extensions.Configuration.Consul.Extensions;
using Winton.Extensions.Configuration.Consul.Parsers;

namespace Winton.Extensions.Configuration.Consul
{
    internal sealed class ConsulConfigurationSource : IConsulConfigurationSource
    {
        private string? _keyToRemove;

        public ConsulConfigurationSource(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            Key = key;
            Parser = new JsonConfigurationParser();
            ConvertToConfig = DefaultConvertToConfigStrategy;
        }

        public Action<ConsulClientConfiguration>? ConsulConfigurationOptions { get; set; }

        public Action<HttpClientHandler>? ConsulHttpClientHandlerOptions { get; set; }

        public Action<HttpClient>? ConsulHttpClientOptions { get; set; }

        public string Key { get; }

        public string KeyToRemove
        {
            get => _keyToRemove ?? Key;
            set => _keyToRemove = value;
        }

        public Func<KVPair, IEnumerable<KeyValuePair<string, string>>> ConvertToConfig { get; set; }

        public Action<ConsulLoadExceptionContext>? OnLoadException { get; set; }

        public Func<ConsulWatchExceptionContext, TimeSpan>? OnWatchException { get; set; }

        public bool Optional { get; set; } = false;

        public IConfigurationParser Parser { get; set; }

        public TimeSpan PollWaitTime { get; set; } = TimeSpan.FromMinutes(5);

        public bool ReloadOnChange { get; set; } = false;

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            var consulClientFactory = new ConsulClientFactory(this);
            return new ConsulConfigurationProvider(this, consulClientFactory);
        }

        private IEnumerable<KeyValuePair<string, string>> DefaultConvertToConfigStrategy(KVPair consulKvPair)
        {
            return consulKvPair.ConvertToConfig(this.KeyToRemove, this.Parser);
        }
    }
}