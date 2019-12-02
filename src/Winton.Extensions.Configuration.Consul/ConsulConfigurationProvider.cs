// Copyright (c) Winton. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Consul;
using Microsoft.Extensions.Configuration;
using Winton.Extensions.Configuration.Consul.Extensions;

namespace Winton.Extensions.Configuration.Consul
{
    /// <summary>
    ///     Each instance loads configuration for the key in Consul that is specified in
    ///     the contained <see cref="IConsulConfigurationSource" />.
    ///     It has the ability to automatically reload the config if it changes in Consul.
    /// </summary>
    /// <remarks>
    ///     Each instance maintains its own <c>lastIndex</c> and uses this to detect changes.
    ///     Each instance ensures calls to Consul are serialised, to avoid concurrent access to <c>lastIndex</c>.
    /// </remarks>
    internal sealed class ConsulConfigurationProvider : ConfigurationProvider, IDisposable
    {
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly IConsulClientFactory _consulClientFactory;
        private readonly IConsulConfigurationSource _source;
        private ulong _lastIndex;
        private Task? _pollTask;

        public ConsulConfigurationProvider(
            IConsulConfigurationSource source,
            IConsulClientFactory consulClientFactory)
        {
            if (source.Parser == null)
            {
                throw new ArgumentNullException(nameof(source.Parser));
            }

            _source = source;
            _consulClientFactory = consulClientFactory;
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
            _pollTask?.Wait(500);
            _cancellationTokenSource.Dispose();
        }

        public override void Load()
        {
            // If polling has already begun then calling load is pointless
            if (_pollTask != null)
            {
                return;
            }

            DoLoad().GetAwaiter().GetResult();

            // Polling starts after the initial load to ensure no concurrent access to the key from this instance
            if (_source.ReloadOnChange)
            {
                _pollTask = Task.Run(PollingLoop);
            }
        }

        private async Task DoLoad()
        {
            try
            {
                var result = await GetKvPairs(false).ConfigureAwait(false);

                if (result.HasValue())
                {
                    SetData(result);
                }
                else if (!_source.Optional)
                {
                    throw new Exception($"The configuration for key {_source.Key} was not found and is not optional.");
                }

                SetLastIndex(result);
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

        private async Task<QueryResult<KVPair[]>> GetKvPairs(bool waitForChange)
        {
            using var consulClient = _consulClientFactory.Create();
            var queryOptions = new QueryOptions
            {
                WaitTime = _source.PollWaitTime,
                WaitIndex = waitForChange ? _lastIndex : 0
            };

            var result =
                await consulClient
                    .KV
                    .List(_source.Key, queryOptions, _cancellationTokenSource.Token)
                    .ConfigureAwait(false);

            return result.StatusCode switch
            {
                HttpStatusCode.OK => result,
                HttpStatusCode.NotFound => result,
                _ =>
                    throw
                        new Exception($"Error loading configuration from consul. Status code: {result.StatusCode}.")
                };
        }

        private async Task PollingLoop()
        {
            var consecutiveFailureCount = 0;
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    var result = await GetKvPairs(true).ConfigureAwait(false);

                    if (result.HasValue() && result.LastIndex > _lastIndex)
                    {
                        SetData(result);
                        OnReload();
                    }

                    SetLastIndex(result);
                    consecutiveFailureCount = 0;
                }
                catch (Exception exception)
                {
                    var wait =
                        _source.OnWatchException?.Invoke(
                            new ConsulWatchExceptionContext(exception, ++consecutiveFailureCount, _source)) ??
                        TimeSpan.FromSeconds(5);
                    await Task.Delay(wait, _cancellationTokenSource.Token);
                }
            }
        }

        private void SetData(QueryResult<KVPair[]> result)
        {
            Data = result.ToConfigDictionary(_source.KeyToRemove, _source.Parser);
        }

        private void SetLastIndex(QueryResult result)
        {
            _lastIndex = result.LastIndex == 0
                ? 1
                : result.LastIndex < _lastIndex
                    ? 0
                    : result.LastIndex;
        }
    }
}