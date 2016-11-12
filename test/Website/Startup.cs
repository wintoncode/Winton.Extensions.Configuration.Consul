using Chocolate.AspNetCore.Configuration.Consul;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace WebApplication
{
    public class Startup
    {
        public Startup(IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory
                .AddConsole()
                .AddDebug();

            ILogger logger = loggerFactory.CreateLogger(nameof(Startup));

            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddConsul(options => {
                    options.Key = $"{env.ApplicationName}/{env.EnvironmentName.ToLower()}/appsettings.json";
                })
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddSingleton<IConfigurationRoot>(Configuration)
                .AddMvc();
            services.AddSwaggerGen();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app
                .UseMvc()
                .UseSwagger()
                .UseSwaggerUi("api");
        }
    }
}
