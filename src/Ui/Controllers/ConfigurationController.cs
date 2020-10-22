using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Sulu.Dto;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Sulu.Ui.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")]
    public class ConfigurationController : ControllerBase
    {
        ISuluApiConfiguration Configuration { get; set; }
        public ConfigurationController(ISuluApiConfiguration configuration)
        {
            Configuration = configuration;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type =typeof(SuluConfig))]
        public IActionResult Get()
        {
            return Ok(Configuration.GetConfiguration());
        }
        
        [HttpPut]
        public void Post([FromBody] SuluConfig value)
        {
            Configuration.SaveConfiguration(value);
        }
    }
}
