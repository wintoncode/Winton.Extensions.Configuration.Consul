// Copyright (c) Winton. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENCE in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Consul;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using Winton.Extensions.Configuration.Consul.Extensions;

namespace Winton.Extensions.Configuration.Consul
{
    internal sealed class ConsulConfigurationProvider : ConfigurationProvider
    {
        private readonly IConsulConfigurationClient _consulConfigClient;
        private readonly IConsulConfigurationSource _source;

        public ConsulConfigurationProvider(
            IConsulConfigurationSource source,
            IConsulConfigurationClient consulConfigClient)
        {
            if (source.Parser == null)
            {
                throw new ArgumentNullException(nameof(source.Parser));
            }

            _consulConfigClient = consulConfigClient;
            _source = source;

            if (source.ReloadOnChange)
            {
                ChangeToken.OnChange(
                    () => _consulConfigClient.Watch(_source.OnWatchException),
                    async () =>
                    {
                        await DoLoad(true).ConfigureAwait(false);
                        OnReload();
                    });
            }
        }

        public override void Load()
        {
            try
            {
                DoLoad(false).Wait();
            }
            catch (AggregateException aggregateException)
            {
                throw aggregateException.InnerException;
            }
        }

        private Dictionary<string, string> ConvertResultToDictionary(QueryResult<KVPair> queryResult)
        {
            if (!queryResult.HasValue())
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            using (var configStream = new MemoryStream(queryResult.Value()))
            {
                return new Dictionary<string, string>(
                    _source.Parser.Parse(configStream),
                    StringComparer.OrdinalIgnoreCase);
            }
        }

        private async Task DoLoad(bool reloading)
        {
            try
            {
                QueryResult<KVPair> queryResult = await _consulConfigClient.GetConfig().ConfigureAwait(false);
                if (!queryResult.HasValue() && !_source.Optional)
                {
                    if (!reloading)
                    {
                        throw new Exception(
                            $"The configuration for key {_source.Key} was not found and is not optional.");
                    }

                    // Don't overwrite mandatory config with empty data if not found when reloading
                    return;
                }

                Data = ConvertResultToDictionary(queryResult);
            }
            catch (Exception exception)
            {
                var exceptionContext = new ConsulLoadExceptionContext(_source, exception);
                _source.OnLoadException?.Invoke(exceptionContext);
                if (!exceptionContext.Ignore)
                {
                    throw;
                }
            }
        }
    }
}