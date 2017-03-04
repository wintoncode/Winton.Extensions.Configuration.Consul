// Copyright (c) Winton. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENCE in the project root for license information.

using System;
using System.Threading;

namespace Winton.Extensions.Configuration.Consul
{
    /// <summary>
    /// Contains information about a consul load exception.
    /// </summary>
    public sealed class ConsulWatchExceptionContext
    {
        internal ConsulWatchExceptionContext(IConsulConfigurationSource source, Exception exception)
        {
            Exception = exception;
            Source = source;
        }

        /// <summary>
        /// Gets the <see cref="Exception"/> that occured.
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// Gets the <see cref="IConsulConfigurationSource"/> of the provider that caused the exception.
        /// Can be used to access the <see cref="CancellationToken"/> which can terminate the watcher.
        /// </summary>
        public IConsulConfigurationSource Source { get; }
    }
}