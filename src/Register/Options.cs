using CommandLine;
using System.Collections.Generic;

namespace Sulu.Register
{
    [Verb("register", HelpText = "Register Sulu to handle URLs")]
    public class Options : IVerb
    {
        [Option('f', "force", Required = false, HelpText = "Overwrite an existing registration")]
        public bool Force { get; set; }

        [Option('p', "protocols", Required = false, HelpText = "A comma-separated list of URL protocols to handle (Default: ssh, rdp)", Default = new string[] { "ssh", "rdp" })]
        public IEnumerable<string> Protocols { get; set; }
    }
}
