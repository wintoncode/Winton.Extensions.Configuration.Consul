using System;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace Winton.Extensions.Configuration.Consul.Net461Website
{
    /// <summary>
    ///     This project just exists so that it's easy to check runtime compatibility with net461 clients.
    ///     Due to the fact that the Consul lib uses conditional compilation it can be easy to get runtime
    ///     errors due to missing methods if this library doesn't have the correct compilation settings.
    ///     So checking that this project starts up is a good sanity check.
    /// </summary>
    internal sealed class Program
    {
        public static IWebHostBuilder CreateHostBuilder(string[] args)
        {
            return WebHost
                .CreateDefaultBuilder(args)
                .ConfigureAppConfiguration(
                    builder =>
                    {
                        builder
                            .AddConsul(
                                "appsettings.json",
                                options =>
                                {
                                    options.ConsulConfigurationOptions =
                                        cco => { cco.Address = new Uri("http://consul:8500"); };
                                    options.Optional = true;
                                    options.ReloadOnChange = true;
                                })
                            .AddEnvironmentVariables();
                    })
                .UseStartup<Startup>();
        }

        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }
    }
}