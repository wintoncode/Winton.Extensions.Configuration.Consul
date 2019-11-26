using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Winton.Extensions.Configuration.Consul.Net461Website.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public sealed class ConfigController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public ConfigController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet("{key}")]
        public IActionResult GetValueForKey(string key)
        {
            return Ok(_configuration[key]);
        }
    }
}