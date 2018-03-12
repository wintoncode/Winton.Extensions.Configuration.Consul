using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.Swagger;

namespace Winton.Extensions.Configuration.Consul.Website
{
    public class Startup
    {
        private readonly IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void Configure(IApplicationBuilder app, IApplicationLifetime appLifetime)
        {
            app
                .UseDeveloperExceptionPage()
                .UseMvc()
                .UseSwagger()
                .UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/v1/swagger.json", "Test Website"); });
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddSwaggerGen(c => { c.SwaggerDoc("v1", new Info { Title = "Test Website", Version = "v1" }); })
                .AddSingleton(_configuration)
                .AddMvc();
        }
    }
}