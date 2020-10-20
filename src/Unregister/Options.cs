using CommandLine;
using System.Collections.Generic;

namespace Sulu.Unregister
{
    [Verb("unregister", HelpText = "Unregister Sulu for URL handling")]
    public class Options : IVerb
    {
        [Option('p', "protocols", Required = false, HelpText = "A space-separated list of URL protocols to handle (Default: ssh rdp telnet)", Default = new string[] { "ssh", "rdp", "telnet" })]
        public IEnumerable<string> Protocols { get; set; }
    }
}
