using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using scalus.Dto;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace scalus.Ui.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")]
    public class ConfigurationController : ControllerBase
    {
        IScalusApiConfiguration Configuration { get; set; }
        public ConfigurationController(IScalusApiConfiguration configuration)
        {
            Configuration = configuration;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type =typeof(ScalusConfig))]
        public IActionResult Get()
        {
            return Ok(Configuration.GetConfiguration());
        }
        
        [HttpPut]
        public void Post([FromBody] ScalusConfig value)
        {
            Configuration.SaveConfiguration(value);
        }
    }
}
