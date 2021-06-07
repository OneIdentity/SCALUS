using System;
using System.Collections.Generic;
using System.Linq;
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
        IScalusApiConfiguration Configuration { get; }
        IRegistration Registration { get; }

        public ConfigurationController(IScalusApiConfiguration configuration, IRegistration registration)
        {
            Configuration = configuration;
            Registration = registration;
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

        [HttpGet, Route("Registrations")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IList<string>))]
        public IActionResult Registrations()
        {
            var registeredProtocols = new List<string>();
            var config = Configuration.GetConfiguration();
            foreach (var one in config.Protocols)
            {
                if (Registration.IsRegistered(one.Protocol))
                {
                    registeredProtocols.Add(one.Protocol);
                }
            }
            return Ok(registeredProtocols);
        }

        [HttpPut, Route("Register")]
        public void Register([FromBody] ProtocolMapping protocolMapping)
        {
            var config = Configuration.GetConfiguration();
            foreach (var one in config.Protocols)
            {
                if (one.Protocol.Equals(protocolMapping.Protocol) &&
                    !Registration.IsRegistered(one.Protocol))
                {
                    var protocols = new List<string> { one.Protocol };
                    Registration.Register(protocols);
                }
            }
        }

        [HttpPut, Route("UnRegister")]
        public void UnRegister([FromBody] ProtocolMapping protocolMapping)
        {
            var config = Configuration.GetConfiguration();
            foreach (var one in config.Protocols)
            {
                if (one.Protocol.Equals(protocolMapping.Protocol) &&
                    Registration.IsRegistered(one.Protocol))
                {
                    var protocols = new List<string> { one.Protocol };
                    Registration.UnRegister(protocols);
                }
            }
        }

        [HttpGet, Route("Tokens")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Dictionary<string, string>))]
        public IActionResult Tokens()
        {
            var tokens = new Dictionary<string, string>();
            foreach (var one in Enum.GetValues(typeof(ParserConfigDefinitions.Token)))
            {
                tokens.Add(one.ToString()!, ParserConfigDefinitions.TokenDescription[(ParserConfigDefinitions.Token)one]);
            }
            return Ok(tokens);
        }

        [HttpGet, Route("ApplicationDescriptions")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Dictionary<string, string>))]
        public IActionResult ApplicationDescriptions()
        {
            var descriptions = ApplicationConfig.DtoPropertyDescription;
            return Ok(descriptions);
        }
    }
}
