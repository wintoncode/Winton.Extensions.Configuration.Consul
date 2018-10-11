using System;
using System.Threading;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace Winton.Extensions.Configuration.Consul.Website
{
    internal sealed class Program
    {
        public static void Main(string[] args)
        {
            var cancellationTokenSource = new CancellationTokenSource();
            WebHost
                .CreateDefaultBuilder(args)
                .ConfigureAppConfiguration(
                    (hostingContext, builder) =>
                    {
                        builder
                            .AddConsul(
                                "appsettings.json",
                                cancellationTokenSource.Token,
                                options =>
                                {
                                    options.ConsulConfigurationOptions =
                                        cco => { cco.Address = new Uri("http://consul:8500"); };
                                    options.Optional = true;
                                    options.ReloadOnChange = true;
                                    options.OnLoadException = exceptionContext => { exceptionContext.Ignore = true; };
                                })
                            .AddEnvironmentVariables();
                    })
                .UseStartup<Startup>()
                .Build()
                .Run();

            cancellationTokenSource.Cancel();
        }
    }
}