using System;
using System.Threading;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Winton.Extensions.Configuration.Consul.Website
{
    internal sealed class Program
    {
        private static readonly CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();

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
                                CancellationTokenSource.Token,
                                options =>
                                {
                                    options.ConsulConfigurationOptions =
                                        cco => { cco.Address = new Uri("http://consul:8500"); };
                                    options.Optional = true;
                                    options.ReloadOnChange = true;
                                    options.OnLoadException = context => { context.Ignore = true; };
                                })
                            .AddEnvironmentVariables();
                    });
        }

        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
            CancellationTokenSource.Cancel();
            CancellationTokenSource.Dispose();
        }
    }
}