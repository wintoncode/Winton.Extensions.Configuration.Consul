using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Consul;
using Microsoft.Extensions.Configuration;

namespace Chocolate.AspNetCore.Configuration.Consul
{
    /// <summary>
    /// Provides configuration from Consul
    /// </summary>
    public sealed class ConsulConfigurationProvider : ConfigurationProvider
    {
        private readonly ConsulConfigurationSource _source;

        /// <summary>
        /// Constructs a ConsulConfigurationProvider from a JsonConfigurationProvider and the given
        /// key of the root
        /// </summary>
        public ConsulConfigurationProvider(ConsulConfigurationSource source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (source.ConfigurationProvider == null)
            {
                throw new ArgumentNullException(nameof(source.ConfigurationProvider));
            }
            _source = source;
        }

        /// <inheritdoc/>
        public override void Load()
        {
            Load(reload: false);
        }

        /// <inheritdoc/>
        public override void Set(string key, string value)
        {
            throw new NotImplementedException();
        }

        private string Key => $"{_source.ApplicationName}/{_source.EnvironmentName}";

        private async Task Load(bool reload)
        {
            try 
            {
                await TryLoad();
            }
            catch(Exception exception)
            {
                if (_source.Optional || reload)
                {
                    _logger.Error(exception);
                }
            }
        }

        private async Task TryLoad()
        {
            using (var client = new ConsulClient())
            {
                QueryResult<KVPair> result = await client.KV.Get(Key);
                HandleResponse(result);
            }
        }

        private void HandleResponse(QueryResult<KVPair> result)
        {
            switch (result.StatusCode)
            {
                case HttpStatusCode.OK:
                    HandleOkResponse(result);
                    return;
                case HttpStatusCode.NotFound:
                    HandleNotFoundResponse(result);
                    return;
                default:
                    HandleErrorResponse(result);
                    return;
            }
        }

        private void HandleOkResponse(QueryResult<KVPair> result)
        {
            byte[] bytes = result.Response.Value;
            using (Stream stream = new MemoryStream(bytes))
            {
                _source.ConfigurationProvider.Load(stream);
            }
        }

        private void HandleNotFoundResponse(QueryResult<KVPair> result)
        {
            if (_source.Optional) 
            {
                return;
            }
            throw new Exception($"Config for key ${Key} is not optional and was not found");
        }

        private void HandleErrorResponse(QueryResult<KVPair> result)
        {
            if (result.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception($"Error loading configuration from consul. Status code: {result.StatusCode}");
            }
        }
    }
}