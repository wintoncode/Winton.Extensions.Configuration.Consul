using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace Chocolate.AspNetCore.Configuration.Consul
{
    internal sealed class ConsulConfigurationProvider : ConfigurationProvider
    {
        private readonly IConsulConfigurationClient _consulConfigClient;
        private readonly IConsulConfigurationSource _source;

        public ConsulConfigurationProvider(IConsulConfigurationSource source, IConsulConfigurationClient consulConfigClient)
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
                    async () => {
                        await DoLoad(reloading: true);
                        OnReload();
                    }
                );
            }
        }

        public override void Load()
        {
            try
            {
                DoLoad(reloading: false).Wait();
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
                bool optional = _source.Optional || reloading;
                byte[] configBytes = await _consulConfigClient.GetConfig(optional);
                LoadIntoMemory(configBytes);
            }
            catch(Exception exception)
            {
                var exceptionContext = new ConsulLoadExceptionContext(_source, exception);
                _source.OnLoadException?.Invoke(exceptionContext);
                if (!exceptionContext.Ignore)
                {
                    throw exception;
                }
            }
        }

        private void LoadIntoMemory(byte[] configBytes)
        {
            if (configBytes == null)
            {
                Data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                return;
            }
            using (var configStream = new MemoryStream(configBytes))
            {
                IDictionary<string, string> parsedData = _source.Parser.Parse(configStream);
                Data = new Dictionary<string, string>(parsedData, StringComparer.OrdinalIgnoreCase);
            }
        }
    }
}