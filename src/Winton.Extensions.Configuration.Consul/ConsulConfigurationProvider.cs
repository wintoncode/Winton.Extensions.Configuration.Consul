// Copyright (c) Winton. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENCE in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

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

        private async Task DoLoad(bool reloading)
        {
            try
            {
                IConfigQueryResult configQueryResult = await _consulConfigClient.GetConfig().ConfigureAwait(false);
                if (!configQueryResult.Exists && !_source.Optional)
                {
                    if (!reloading)
                    {
                        throw new Exception(
                            $"The configuration for key {_source.Key} was not found and is not optional.");
                    }

                    // Don't overwrite mandatory config with empty data if not found when reloading
                    return;
                }

                LoadIntoMemory(configQueryResult);
            }
            catch (Exception exception)
            {
                HandleLoadException(exception);
            }
        }

        private void HandleLoadException(Exception exception)
        {
            var exceptionContext = new ConsulLoadExceptionContext(_source, exception);
            _source.OnLoadException?.Invoke(exceptionContext);
            if (!exceptionContext.Ignore)
            {
                throw exception;
            }
        }

        private void LoadIntoMemory(IConfigQueryResult configQueryResult)
        {
            if (!configQueryResult.Exists)
            {
                Data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
            else
            {
                using (var configStream = new MemoryStream(configQueryResult.Value))
                {
                    IDictionary<string, string> parsedData = _source.Parser.Parse(configStream);
                    Data = new Dictionary<string, string>(parsedData, StringComparer.OrdinalIgnoreCase);
                }
            }
        }
    }
}