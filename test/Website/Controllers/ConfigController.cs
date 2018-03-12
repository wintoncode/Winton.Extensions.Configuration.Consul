using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Winton.Extensions.Configuration.Consul.Website.Controllers
{
    [Route("[controller]")]
    public sealed class ConfigController : Controller
    {
        private readonly IConfiguration _configurationRoot;

        public ConfigController(IConfiguration configuration)
        {
            _configurationRoot = configuration;
        }

        [HttpGet("{key}")]
        public IActionResult GetValueForKey(string key)
        {
            return Json(_configurationRoot[key]);
        }
    }
}