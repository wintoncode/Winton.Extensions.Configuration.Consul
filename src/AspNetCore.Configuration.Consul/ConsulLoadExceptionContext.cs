using System;

namespace Chocolate.AspNetCore.Configuration.Consul
{
    /// <summary>
    /// Contains information about a consul load exception.
    /// </summary>
    public sealed class ConsulLoadExceptionContext
    {
        internal ConsulLoadExceptionContext(IConsulConfigurationProvider provider, Exception exception)
        {
            Provider = provider;
            Exception = exception;
        }

        /// <summary>
        /// The <see cref="IConsulConfigurationProvider"/> that caused the exception.
        /// </summary>
        public IConsulConfigurationProvider Provider { get; }

        /// <summary>
        /// The exception that occured in Load.
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// Set to true to prevent the exception from being thrown. 
        /// I.e. if the exception has been handled.
        /// </summary>
        public bool Ignore { get; set; }
    }
}