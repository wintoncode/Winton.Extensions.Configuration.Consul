// Copyright (c) Winton. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENCE in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;

namespace Winton.Extensions.Configuration.Consul
{
    /// <summary>Provides client access for getting and watching config values in Consul.</summary>
    internal interface IConsulConfigurationClient
    {
        /// <summary>Gets the config from consul asynchronously.</summary>
        /// <returns>A Task containing the result of the query for the config.</returns>
        Task<IConfigQueryResult> GetConfig();

        /// <summary>Watches the config for changes.</summary>
        /// <param name="onException">An action to be invoked if an exception occurs during the watch.</param>
        /// <returns>An <see cref="IChangeToken"/> that will indicated when changes have occured.</returns>
        IChangeToken Watch(Action<ConsulWatchExceptionContext> onException);
    }
}