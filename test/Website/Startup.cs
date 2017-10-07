using System;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Winton.Extensions.Configuration.Consul.Website
{
    public class Startup
    {
        private readonly CancellationTokenSource _consulConfigCancellationTokenSource = new CancellationTokenSource();
        private readonly IConfigurationRoot _configuration;

        public Startup(IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory
                .AddConsole(LogLevel.Debug)
                .AddDebug(LogLevel.Debug);

            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddConsul(
                    "appsettings.json",
                    _consulConfigCancellationTokenSource.Token,
                    options =>
                    {
                        options.ConsulConfigurationOptions = cco =>
                        {
                            cco.Address = new Uri("http://consul:8500");
                        };
                        options.Optional = true;
                        options.ReloadOnChange = true;
                        options.OnLoadException = (exceptionContext) =>
                        {
                            exceptionContext.Ignore = true;
                        };
                    })
                .AddEnvironmentVariables();
            _configuration = builder.Build();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddSingleton(_configuration)
                .AddMvc();
            services.AddSwaggerGen();
        }

        public void Configure(IApplicationBuilder app, IApplicationLifetime appLifetime)
        {
            app
                .UseMvc()
                .UseSwagger()
                .UseSwaggerUi("swagger");

            appLifetime.ApplicationStopping.Register(_consulConfigCancellationTokenSource.Cancel);
        }
    }
}
