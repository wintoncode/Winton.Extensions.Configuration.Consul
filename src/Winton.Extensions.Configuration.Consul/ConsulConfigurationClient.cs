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

        private ulong _lastIndex;
        private ConfigurationReloadToken _reloadToken = new ConfigurationReloadToken();

        public ConsulConfigurationClient(IConsulClientFactory consulClientFactory)
        {
            _consulClientFactory = consulClientFactory;
        }

        public async Task<QueryResult<KVPair[]>> GetConfig(string key, CancellationToken cancellationToken)
        {
            QueryResult<KVPair[]> result = await GetKvPairs(key, cancellationToken).ConfigureAwait(false);
            UpdateLastIndex(result);
            return result;
        }

        public IChangeToken Watch(
            string key,
            TimeSpan? backoffOnError,
            Action<ConsulWatchExceptionContext> onException,
            CancellationToken cancellationToken)
        {
            Task.Run(() => PollForChanges(key, backoffOnError, onException, cancellationToken));
            return _reloadToken;
        }

        private async Task<QueryResult<KVPair[]>> GetKvPairs(
            string key,
            CancellationToken cancellationToken,
            QueryOptions queryOptions = null)
        {
            using (IConsulClient consulClient = _consulClientFactory.Create())
            {
                QueryResult<KVPair[]> result =
                    await consulClient
                        .KV
                        .List(key, queryOptions, cancellationToken)
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

        private async Task<bool> HasValueChanged(string key, CancellationToken cancellationToken)
        {
            QueryOptions queryOptions;
            lock (_lastIndexLock)
            {
                queryOptions = new QueryOptions { WaitIndex = _lastIndex };
            }

            QueryResult<KVPair[]> result = await GetKvPairs(key, cancellationToken, queryOptions).ConfigureAwait(false);
            return result != null && UpdateLastIndex(result);
        }

        private async Task PollForChanges(
            string key,
            TimeSpan? backoffOnError,
            Action<ConsulWatchExceptionContext> onException,
            CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (await HasValueChanged(key, cancellationToken).ConfigureAwait(false))
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
                    var exceptionContext = new ConsulWatchExceptionContext(cancellationToken, exception);
                    onException?.Invoke(exceptionContext);
                    if (backoffOnError.HasValue)
                    {
                        await Task.Delay(backoffOnError.Value, cancellationToken);
                    }
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