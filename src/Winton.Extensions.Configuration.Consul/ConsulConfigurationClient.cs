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
        private readonly TimeSpan _pollWaitTime;
        private readonly object _lastIndexLock = new object();

        private ulong _lastIndex;

        public ConsulConfigurationClient(IConsulClientFactory consulClientFactory, TimeSpan pollWaitTime)
        {
            _consulClientFactory = consulClientFactory;
            _pollWaitTime = pollWaitTime;
        }

        public async Task<QueryResult<KVPair[]>> GetConfig(string key, CancellationToken cancellationToken)
        {
            QueryResult<KVPair[]> result = await GetKvPairs(key, cancellationToken).ConfigureAwait(false);
            UpdateLastIndex(result);
            return result;
        }

        public async Task<bool> PollForChanges(
            string key,
            Func<ConsulWatchExceptionContext, TimeSpan> onException,
            CancellationToken cancellationToken)
        {
            var consecutiveFailureCount = 0;
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (await HasValueChanged(key, cancellationToken).ConfigureAwait(false))
                    {
                        return true;
                    }

                    consecutiveFailureCount = 0;
                }
                catch (Exception exception)
                {
                    TimeSpan wait =
                        onException?.Invoke(
                            new ConsulWatchExceptionContext(cancellationToken, exception, ++consecutiveFailureCount)) ??
                        TimeSpan.FromSeconds(5);
                    await Task.Delay(wait, cancellationToken);
                }
            }

            return false;
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
            QueryOptions queryOptions = new QueryOptions { WaitTime = _pollWaitTime };
            lock (_lastIndexLock)
            {
                queryOptions.WaitIndex = _lastIndex;
            }

            QueryResult<KVPair[]> result = await GetKvPairs(key, cancellationToken, queryOptions).ConfigureAwait(false);
            return result != null && UpdateLastIndex(result);
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