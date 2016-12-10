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
        /// The exception that occured in Load.
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// The <see cref="IConsulConfigurationSource"/> of the provider that caused the exception.
        /// Can be used to access the <see cref="CancellationToken"/> which can terminate the watcher.
        /// </summary>
        public IConsulConfigurationSource Source { get; }
    }
}