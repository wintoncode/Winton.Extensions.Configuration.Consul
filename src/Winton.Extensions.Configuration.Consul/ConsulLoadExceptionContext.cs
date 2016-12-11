using System;

namespace Winton.Extensions.Configuration.Consul
{
    /// <summary>
    /// Contains information about exceptions that occuring during a configuration load from Consul.
    /// </summary>
    public sealed class ConsulLoadExceptionContext
    {
        internal ConsulLoadExceptionContext(IConsulConfigurationSource source, Exception exception)
        {
            Source = source;
            Exception = exception;
        }

        /// <summary>
        /// The exception that occured in Load.
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// Set to true to prevent the exception from being thrown. 
        /// I.e. if the exception has been handled.
        /// </summary>
        public bool Ignore { get; set; }

        /// <summary>
        /// The <see cref="IConsulConfigurationSource"/> of the provider that caused the exception.
        /// </summary>
        public IConsulConfigurationSource Source { get; }
    }
}