using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Winton.Extensions.Configuration.Consul;

namespace Consul.Persistence.Sample
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var cancellationTokenSource = new CancellationTokenSource();

            CreateWebHostBuilder(args, cancellationTokenSource).Build().Run();

            cancellationTokenSource.Cancel();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args, CancellationTokenSource cancellationTokenSource) =>
            WebHost.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddConsul("TestApp/Dev", cancellationTokenSource.Token, options =>
                {
                    options.ConsulConfigurationOptions = _ => { _.Address = new Uri("http://127.0.0.1:8500"); };
                    options.Optional = true;
                    options.ReloadOnChange = true;
                    options.OnLoadException = exceptionContext => { exceptionContext.Ignore = true; };
                    options.OnWatchException = _ => { return new TimeSpan(0, 0, 10); };
                });
            })
            .UseStartup<Startup>();
    }
}
