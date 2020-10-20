using CommandLine;
using System.Collections.Generic;

namespace Sulu.Register
{
    [Verb("register", HelpText = "Register Sulu to handle URLs")]
    public class Options : IVerb
    {
        [Option('f', "force", Required = false, HelpText = "Overwrite an existing registration")]
        public bool Force { get; set; }

        [Option('p', "protocols", Required = false, HelpText = "A space-separated list of URL protocols to handle (Default: ssh rdp telnet)", Default = new string[] { "ssh", "rdp", "telnet" })]
        public IEnumerable<string> Protocols { get; set; }
    }
}
