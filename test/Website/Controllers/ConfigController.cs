using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace WebApplication.Controllers
{
    [Route("[controller]")]
    public sealed class ConfigController : Controller
    {
        private readonly IConfigurationRoot _configurationRoot;
        private readonly IHostingEnvironment _hostingEnvironment;

        public ConfigController(IConfigurationRoot configuration, IHostingEnvironment hostingEnvironment)
        {
            _configurationRoot = configuration;
            _hostingEnvironment = hostingEnvironment;
        }

        [HttpGet("{key}")]
        public IActionResult GetValueForKey(string key)
        {
            return Json(_configurationRoot.GetSection(key));
        }
    }
}