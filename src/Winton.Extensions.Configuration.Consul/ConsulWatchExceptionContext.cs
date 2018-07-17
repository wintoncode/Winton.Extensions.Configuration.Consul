// Copyright (c) Winton. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENCE in the project root for license information.

using System;
using System.Threading;

namespace Winton.Extensions.Configuration.Consul
{
    /// <summary>
    ///     Contains information about a consul load exception.
    /// </summary>
    public sealed class ConsulWatchExceptionContext
    {
        internal ConsulWatchExceptionContext(CancellationToken cancellationToken, Exception exception)
        {
            Exception = exception;
            CancellationToken = cancellationToken;
        }

        /// <summary>
        ///     Gets the <see cref="Exception" /> that occured.
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        ///     Gets the <see cref="CancellationToken" /> for the watch task which can be used to terminate it.
        /// </summary>
        public CancellationToken CancellationToken { get; }
    }
}