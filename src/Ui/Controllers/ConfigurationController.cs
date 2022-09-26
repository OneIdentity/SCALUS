// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationController.cs" company="One Identity Inc.">
//   This software is licensed under the Apache 2.0 open source license.
//   https://github.com/OneIdentity/SCALUS/blob/master/LICENSE
//
//
//   Copyright One Identity LLC.
//   ALL RIGHTS RESERVED.
//
//   ONE IDENTITY LLC. MAKES NO REPRESENTATIONS OR
//   WARRANTIES ABOUT THE SUITABILITY OF THE SOFTWARE,
//   EITHER EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED
//   TO THE IMPLIED WARRANTIES OF MERCHANTABILITY,
//   FITNESS FOR A PARTICULAR PURPOSE, OR
//   NON-INFRINGEMENT.  ONE IDENTITY LLC. SHALL NOT BE
//   LIABLE FOR ANY DAMAGES SUFFERED BY LICENSEE
//   AS A RESULT OF USING, MODIFYING OR DISTRIBUTING
//   THIS SOFTWARE OR ITS DERIVATIVES.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace OneIdentity.Scalus.Ui.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Autofac;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using OneIdentity.Scalus.Dto;

    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")]
    public class ConfigurationController : ControllerBase
    {
        public ConfigurationController(IScalusApiConfiguration configuration, IRegistration registration, ILifetimeScope container)
        {
            Configuration = configuration;
            Registration = registration;
            Container = container;
        }

        private IScalusApiConfiguration Configuration { get; }

        private IRegistration Registration { get; }

        private ILifetimeScope Container { get; }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ScalusConfig))]
        public IActionResult Get()
        {
            var config = Configuration.GetConfiguration();
            if (Configuration.ValidationErrors?.Count > 0)
            {
                throw new ConfigurationException("Bad configuration file");
            }

            return Ok(config);
        }

        [HttpPut]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult Post([FromBody] ScalusConfig value)
        {
            var errs = Configuration.SaveConfiguration(value);
            if (errs?.Count > 0)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            return Ok();
        }

        [HttpGet]
        [Route("Registrations")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IList<string>))]
        public IActionResult Registrations()
        {
            var registeredProtocols = new List<string>();
            var config = Configuration.GetConfiguration();
            if (config.Protocols == null || config.Applications == null)
            {
                throw new ConfigurationException("Bad configuration file");
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

        [HttpPut]
        [Route("Register")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult Register([FromBody] ProtocolMapping protocolMapping)
        {
            if (!ProtocolMapping.ValidateProtocol(protocolMapping.Protocol, out string err))
            {
                Serilog.Log.Error($"Cannot register invalid protocol:{protocolMapping.Protocol}:{err}");
                return BadRequest();
            }

            if (Registration.IsRegistered(protocolMapping.Protocol))
            {
                return Ok();
            }

            if (Registration.Register(new List<string> { protocolMapping.Protocol }, true))
            {
                return Ok();
            }

            Serilog.Log.Error($"Cannot register {protocolMapping.Protocol} :  registration reported failure");
            return UnprocessableEntity();
        }

        [HttpPut]
        [Route("UnRegister")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult UnRegister([FromBody] ProtocolMapping protocolMapping)
        {
            if (!ProtocolMapping.ValidateProtocol(protocolMapping.Protocol, out string err))
            {
                Serilog.Log.Error($"Cannot register invalid protocol:{protocolMapping.Protocol}:{err}");
                return BadRequest();
            }

            if (!Registration.IsRegistered(protocolMapping.Protocol))
            {
                return Ok();
            }

            if (Registration.UnRegister(new List<string> { protocolMapping.Protocol }))
            {
                return Ok();
            }

            Serilog.Log.Error($"Protocol: {protocolMapping.Protocol} -deregistration reported failure");
            return UnprocessableEntity();
        }

        [HttpGet]
        [Route("Tokens")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Dictionary<string, string>))]
        public IActionResult Tokens()
        {
            var tokens = new Dictionary<string, string>();
            foreach (var one in Enum.GetValues(typeof(ParserConfigDefinitions.Token)))
            {
                tokens.Add("%" + one.ToString() ! + "%", ParserConfigDefinitions.TokenDescription[(ParserConfigDefinitions.Token)one]);
            }

            return Ok(tokens);
        }

        [HttpGet]
        [Route("ApplicationDescriptions")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Dictionary<string, string>))]
        public IActionResult ApplicationDescriptions()
        {
            var descriptions = ApplicationConfig.DtoPropertyDescription;
            return Ok(descriptions);
        }

        [HttpGet]
        [Route("Info")]
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

        [HttpPut]
        [Route("Validate")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string[]))]
        public IActionResult Validate([FromBody] ScalusConfig config)
        {
            var validationErrors = new List<string>();
            config.Validate(validationErrors);
            return Ok(validationErrors.ToArray());
        }
    }
}
