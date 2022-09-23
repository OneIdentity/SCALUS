// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CommandLineHandler.cs" company="One Identity Inc.">
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

namespace OneIdentity.Scalus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using CommandLine;
    using CommandLine.Text;

    internal class CommandLineHandler : ICommandLineParser
    {
        private IEnumerable<IVerb> Verbs { get; }

        public CommandLineHandler(IEnumerable<IVerb> verbs)
        {
            Verbs = verbs;
        }

        public IApplication Build(string[] args, Func<object, IApplication> appResolver)
        {
            using (var parser = new CommandLine.Parser(with =>
            {
                with.HelpWriter = null;
            }))
            {
                var verbTypes = Verbs.Select(x => x.GetType()).OrderBy(x => x?.GetCustomAttribute<VerbAttribute>()?.Name)?.ToArray();
                var parserResult = parser.ParseArguments(args, verbTypes);

                IApplication application = null;
                parserResult
                    .WithParsed(x =>
                        {
                            application = appResolver(x);
                        })
                    .WithNotParsed(x => HandleErrors(parserResult, x));
                return application;
            }
        }

        private static void HandleErrors<T>(ParserResult<T> parserResult, IEnumerable<Error> errs)
        {
            var header = "Session Client Application Launch Uri System (SCALUS)";
            var copyright = "Copyright (c) 2021 One Identity LLC";

            // Handle version 
            if (errs.IsVersion())
            {
                Console.WriteLine($"{header}\r\n{copyright}\r\nVersion: {Assembly.GetEntryAssembly()?.GetName()?.Version}\r\n");
                return;
            }

            // Handle help
            if (errs.IsHelp())
            {
                string command = parserResult.TypeInfo.Current?.GetCustomAttribute<VerbAttribute>()?.Name;
                Console.WriteLine(HelpText.AutoBuild(parserResult, h =>
                {
                    h.AddDashesToOption = true;
                    h.AdditionalNewLineAfterOption = false;
                    h.Heading = header;
                    h.Copyright = copyright;

                    if (!string.IsNullOrEmpty(command))
                    {
                        h.AddPreOptionsLine($"\r\n{command} options:");
                        h.AutoVersion = false;
                    }

                    return h;
                }, e => e, true));
                return;
            }

            // Handle errors
            var helpText = HelpText.AutoBuild(parserResult, h =>
            {
                h.AddDashesToOption = true;
                h.AdditionalNewLineAfterOption = false;
                h.Heading = header;
                h.Copyright = copyright;
                h.AutoHelp = false;
                h.AutoVersion = false;
                return HelpText.DefaultParsingErrorsHandler(parserResult, h);
            }, e => e, true);
            throw new CommandLineHelpException(helpText);
        }
    }
}
