using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace scalus.Ui.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LifetimeController : ControllerBase
    {
        private IWebServer WebServer { get; }

        public LifetimeController(IWebServer app)
        {
            Serilog.Log.Debug("LifetimeController created.");
            WebServer = app;
        }

        // GET api/<LifetimeController>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
        public IActionResult Get()
        {
            Serilog.Log.Debug("Server status requested.");
            return Ok("WebServer is running.");
        }


        // POST api/<LifetimeController>
        [HttpPost]
        public void Post()
        {
            Serilog.Log.Debug("Server shutdown requested.");
            WebServer.Shutdown();
        }
    }
}
