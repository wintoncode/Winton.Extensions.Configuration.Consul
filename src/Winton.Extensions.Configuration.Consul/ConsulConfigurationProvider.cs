// Copyright (c) Winton. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENCE in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
                    () => _consulConfigClient.Watch(_source.Key, _source.OnWatchException, _source.CancellationToken),
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
                if (aggregateException.InnerException != null)
                {
                    throw aggregateException.InnerException;
                }

                throw;
            }
        }

        private async Task DoLoad(bool reloading)
        {
            var isSetData = false;
            try
            {
                QueryResult<KVPair[]> result = await _consulConfigClient
                    .GetConfig(_source.Key, _source.CancellationToken)
                    .ConfigureAwait(false);
                if (!result.HasValue() && !_source.Optional)
                {
                    if (!reloading)
                    {
                        throw new Exception(
                            $"The configuration for key {_source.Key} was not found and is not optional.");
                    }

                    // Don't overwrite mandatory config with empty data if not found when reloading
                    return;
                }

                string keyToRemove = _source.KeyToRemove ?? _source.Key;

                Data = (result?.Response ?? new KVPair[0])
                    .Where(kvp => kvp.HasValue())
                    .SelectMany(kvp => kvp.ConvertToConfig(keyToRemove, _source.Parser))
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.OrdinalIgnoreCase);

                isSetData = true;

                // ADD Persistence json
                if (_source.PersistenceToLocal && (result?.Response?.Length ?? 0) > 0)
                {
                    var subPaths = _source.Key.Trim().Trim('/').Split('/');
                    var path = AppDomain.CurrentDomain.BaseDirectory;
                    foreach (var sub in subPaths)
                    {
                        path = Path.Combine(path, sub);
                    }

                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }

                    foreach (var item in result.Response)
                    {
                        if (string.IsNullOrWhiteSpace(item.Key) || (item.Value?.Length ?? 0) <= 0)
                        {
                            continue;
                        }

                        var name = RemoveStart(item.Key, keyToRemove).Trim().Trim('/');
                        var fileName = Path.Combine(path, $"{name}.json");
                        File.WriteAllBytes(fileName, item.Value);
                    }
                }
            }
            catch (Exception exception)
            {
                try
                {
                    // Unable to connect to the network, Read local configuration
                    if (_source.PersistenceToLocal && !isSetData)
                    {
                        var subPaths = _source.Key.Trim().Trim('/').Split('/');
                        var path = AppDomain.CurrentDomain.BaseDirectory;
                        foreach (var sub in subPaths)
                        {
                            path = Path.Combine(path, sub);
                        }

                        var files = Directory.GetFiles(path, "*.json", SearchOption.TopDirectoryOnly);
                        var kvs = new List<KVPair>();
                        foreach (var filePath in files)
                        {
                            if (File.Exists(filePath))
                            {
                                var bs = File.ReadAllBytes(filePath);
                                var fileName = Path.GetFileName(filePath);
                                if ((bs?.Length ?? 0) > 0)
                                {
                                    var kv = new KVPair(fileName.Replace(".json", string.Empty));
                                    kv.Value = bs;
                                    kvs.Add(kv);
                                }
                            }
                        }

                        string keyToRemove = _source.KeyToRemove ?? _source.Key;

                        Data = kvs.SelectMany(kvp => kvp.ConvertToConfig(keyToRemove, _source.Parser))
                            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.OrdinalIgnoreCase);
                    }
                }
                catch (Exception perExp)
                {
                    exception = perExp;
                }

                var exceptionContext = new ConsulLoadExceptionContext(_source, exception);
                _source.OnLoadException?.Invoke(exceptionContext);
                if (!exceptionContext.Ignore)
                {
                    throw;
                }
            }
        }

        private string RemoveStart(string s, string toRemove)
        {
            return s.StartsWith(toRemove) ? s.Remove(0, toRemove.Length) : s;
        }
    }
}