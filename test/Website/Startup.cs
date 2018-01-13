using System;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Swagger;

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
                .AddSwaggerGen(c =>
                {
                    c.SwaggerDoc("v1", new Info { Title = "Test Website", Version = "v1" });
                })
                .AddSingleton(_configuration)
                .AddMvc();
        }

        public void Configure(IApplicationBuilder app, IApplicationLifetime appLifetime)
        {
            app
                .UseMvc()
                .UseSwagger()
                .UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Test Website");
                });

            appLifetime.ApplicationStopping.Register(_consulConfigCancellationTokenSource.Cancel);
        }
    }
}
