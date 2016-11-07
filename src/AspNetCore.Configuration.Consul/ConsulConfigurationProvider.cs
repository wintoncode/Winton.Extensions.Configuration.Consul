using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Chocolate.AspNetCore.Configuration.Consul
{
    internal sealed class ConsulConfigurationProvider : ConfigurationProvider, IConsulConfigurationProvider
    {
        private readonly IConsulConfigurationClient _consulConfigClient;

        public ConsulConfigurationProvider(IConsulConfigurationSource source, IConsulConfigurationClient consulConfigClient)
        {
            if (source.Parser == null)
            {
                throw new ArgumentNullException(nameof(source.Parser));
            }
            if (string.IsNullOrWhiteSpace(source.Key))
            {
                throw new ArgumentNullException(nameof(source.Key));
            }
            _consulConfigClient = consulConfigClient;
            Source = source;
        }

        public IConsulConfigurationSource Source { get; }

        public override void Load()
        {
            Load(reloading: false).Wait();
        }

        private async Task Load(bool reloading)
        {
            try 
            {
                // Always optional on reload
                bool optional = Source.Optional || reloading;
                using (Stream configStream = await _consulConfigClient.GetConfig(Source.Key, optional))
                {
                    if (reloading)
                    {
                        // Always create new Data on reload to drop old keys
                        Data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    }
                    Data = Source.Parser.Parse(configStream);
                }
            }
            catch(Exception exception)
            {
                var exceptionContext = new ConsulLoadExceptionContext(this, exception);
                Source.OnLoadException?.Invoke(exceptionContext);
                if (!exceptionContext.Ignore)
                {
                    throw exception;
                }
            }
        }
    }
}