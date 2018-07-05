// Copyright (c) Winton. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENCE in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Consul;
using Microsoft.Extensions.Primitives;

namespace Winton.Extensions.Configuration.Consul
{
    /// <summary>Provides client access for getting and watching config values in Consul.</summary>
    internal interface IConsulConfigurationClient
    {
        /// <summary>Gets the config from consul asynchronously.</summary>
        /// <param name="key">The key at which the config is located.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task containing the result of the query for the config.</returns>
        Task<QueryResult<KVPair[]>> GetConfig(string key, CancellationToken cancellationToken);

        /// <summary>Watches for config changes at a specified key.</summary>
        /// <param name="key">The key whose value should be watched for changes.</param>
        /// <param name="onException">An action to be invoked if an exception occurs during the watch.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>An <see cref="IChangeToken" /> that will indicated when changes have occured.</returns>
        IChangeToken Watch(string key, Action<ConsulWatchExceptionContext> onException, CancellationToken cancellationToken);
    }
}