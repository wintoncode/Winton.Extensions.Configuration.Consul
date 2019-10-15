// Copyright (c) Winton. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using Consul;

namespace Winton.Extensions.Configuration.Consul
{
    internal sealed class ConsulClientFactory : IConsulClientFactory
    {
        private readonly IConsulConfigurationSource _consulConfigSource;

        public ConsulClientFactory(IConsulConfigurationSource consulConfigSource)
        {
            _consulConfigSource = consulConfigSource;
        }

        public IConsulClient Create()
        {
            return new ConsulClient(
                _consulConfigSource.ConsulConfigurationOptions,
                _consulConfigSource.ConsulHttpClientOptions,
                _consulConfigSource.ConsulHttpClientHandlerOptions);
        }
    }
}