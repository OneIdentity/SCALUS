﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Application.cs" company="One Identity Inc.">
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

namespace OneIdentity.Scalus.Verify
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json;
    using OneIdentity.Scalus.Dto;
    using OneIdentity.Scalus.Util;

    internal class Application : IApplication
    {
        public Application(Options options, IScalusConfiguration config, IUserInteraction userInteraction)
        {
            Options = options;
            Configuration = config;
            UserInteraction = userInteraction;
        }

        private Options Options { get; }

        private IScalusConfiguration Configuration { get; }

        private IUserInteraction UserInteraction { get; }

        public int Run()
        {
            ShowConfig();
            return 0;
        }

        private void ShowConfig()
        {
            ScalusConfig config = Configuration.GetConfiguration(
                string.IsNullOrEmpty(Options.Path) ? null : Options.Path);

            var ok = Configuration.ValidationErrors?.Count == 0 ? "Pass" : "Fail";
            var result = new VerifyResult
            {
                Path = string.IsNullOrEmpty(Options.Path) ? ConfigurationManager.ScalusJson : Options.Path,
                Result = Configuration.ValidationErrors?.Count == 0,
                Errors = Configuration.ValidationErrors,
            };
            var json = JsonConvert.SerializeObject(result, Formatting.Indented);
            UserInteraction.Message(json);
        }
    }
}
