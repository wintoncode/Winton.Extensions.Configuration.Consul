// Copyright (c) Winton. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENCE in the project root for license information.

using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Consul;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace Winton.Extensions.Configuration.Consul
{
    internal sealed class ConsulConfigurationClient : IConsulConfigurationClient
    {
        private readonly IConsulClientFactory _consulClientFactory;
        private readonly object _lastIndexLock = new object();
        private readonly IConsulConfigurationSource _source;

        private ulong _lastIndex;
        private ConfigurationReloadToken _reloadToken = new ConfigurationReloadToken();

        public ConsulConfigurationClient(IConsulClientFactory consulClientFactory, IConsulConfigurationSource source)
        {
            _consulClientFactory = consulClientFactory;
            _source = source;
        }

        public async Task<IConfigQueryResult> GetConfig()
        {
            QueryResult<KVPair> result = await GetKvPair().ConfigureAwait(false);
            UpdateLastIndex(result);
            return new ConfigQueryResult(result);
        }

        public IChangeToken Watch(Action<ConsulWatchExceptionContext> onException)
        {
            Task.Run(() => PollForChanges(onException));
            return _reloadToken;
        }

        private async Task<QueryResult<KVPair>> GetKvPair(QueryOptions queryOptions = null)
        {
            using (IConsulClient consulClient = _consulClientFactory.Create())
            {
                QueryResult<KVPair> result =
                    await consulClient
                        .KV
                        .Get(_source.Key, queryOptions, _source.CancellationToken)
                        .ConfigureAwait(false);

                switch (result.StatusCode)
                {
                    case HttpStatusCode.OK:
                    case HttpStatusCode.NotFound:
                        return result;
                    default:
                        throw new Exception(
                            $"Error loading configuration from consul. Status code: {result.StatusCode}.");
                }
            }
        }

        private async Task<bool> HasValueChanged()
        {
            QueryOptions queryOptions;
            lock (_lastIndexLock)
            {
                queryOptions = new QueryOptions { WaitIndex = _lastIndex };
            }

            QueryResult<KVPair> result = await GetKvPair(queryOptions).ConfigureAwait(false);
            return result != null && UpdateLastIndex(result);
        }

        private async Task PollForChanges(Action<ConsulWatchExceptionContext> onException)
        {
            while (!_source.CancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (await HasValueChanged().ConfigureAwait(false))
                    {
                        ConfigurationReloadToken previousToken = Interlocked.Exchange(
                            ref _reloadToken,
                            new ConfigurationReloadToken());
                        previousToken.OnReload();
                        return;
                    }
                }
                catch (Exception exception)
                {
                    var exceptionContext = new ConsulWatchExceptionContext(_source, exception);
                    onException?.Invoke(exceptionContext);
                }
            }
        }

        private bool UpdateLastIndex(QueryResult queryResult)
        {
            lock (_lastIndexLock)
            {
                if (queryResult.LastIndex > _lastIndex)
                {
                    _lastIndex = queryResult.LastIndex;
                    return true;
                }
            }

            return false;
        }
    }
}