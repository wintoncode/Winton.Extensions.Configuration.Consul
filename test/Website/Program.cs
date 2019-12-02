using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Winton.Extensions.Configuration.Consul.Website
{
    internal sealed class Program
    {
        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host
                .CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(builder => builder.UseStartup<Startup>())
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
                                    options.PollWaitTime = TimeSpan.FromSeconds(5);
                                    options.ReloadOnChange = true;
                                })
                            .AddEnvironmentVariables();
                    });
        }

        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }
    }
}