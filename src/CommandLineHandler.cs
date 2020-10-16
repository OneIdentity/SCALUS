using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Sulu
{
    class CommandLineHelpException : InvalidOperationException
    {
        public CommandLineHelpException(string message) : base(message) { }
    }

    class CommandLineHandler : ICommandLineParser
    {
        IEnumerable<IVerb> Verbs { get; }

        public CommandLineHandler(IEnumerable<IVerb> verbs)
        {
            Verbs = verbs;
        }

        public IApplication Build(string[] args, Func<object, IApplication> appResolver)
        {
            var parser = new CommandLine.Parser(with => {
                with.HelpWriter = null;
            });

            var verbTypes = Verbs.Select(x => x.GetType()).OrderBy(x => x.GetCustomAttribute<VerbAttribute>().Name).ToArray(); 
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

        static void HandleErrors<T>(ParserResult<T> parserResult, IEnumerable<Error> errs)
        {
            var header = "Session URL Launcher Utility (sulu)";
            var copyright = "Copyright (c) 2020 Sulu Team";

            // Handle version 
            if (errs.IsVersion())
            {
                Console.WriteLine($"{header}\r\n{copyright}\r\nVersion: {Assembly.GetEntryAssembly().GetName().Version}\r\n");
                return;
            }

            // Handle help
            if(errs.IsHelp())
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
