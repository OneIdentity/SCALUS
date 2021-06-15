using System;
using System.Collections.Generic;
using System.IO;
using Autofac;
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
        ILifetimeScope Container { get; }
 
        public ConfigurationController(IScalusApiConfiguration configuration, IRegistration registration, ILifetimeScope container)
        {
            Configuration = configuration;
            Registration = registration;
            Container = container;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type =typeof(ScalusConfig))]
        public IActionResult Get()
        {
            var config = Configuration.GetConfiguration();
            if (config.Protocols == null || config.Applications == null)
            {
                throw new Exception("Bad configuration file");
            }
            return Ok(config);
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
            if (config.Protocols == null || config.Applications == null)
            {
                throw new Exception("Bad configuration file");
            }
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

        [HttpGet, Route("Info")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
        public IActionResult Info()
        {
            var sw = new StringWriter();
            Console.SetOut(sw);
            var app = Container.ResolveNamed<IApplication>("InfoApplication");
            app.Run();
            var info = sw.ToString();
            return Ok(info);
        }
    }
}
