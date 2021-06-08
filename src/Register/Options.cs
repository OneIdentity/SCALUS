using CommandLine;
using System.Collections.Generic;

namespace scalus.Register
{
    [Verb("register", HelpText = "Register SCALUS to handle URLs")]
    public class Options : IVerb
    {
        [Option('f', "force", Required = false, HelpText = "Overwrite an existing registration")]
        public bool Force { get; set; }

        [Option('p', "protocols", Required = false, HelpText = "A space-separated list of URL protocols to handle (Default: ssh rdp telnet)", Default = new string[] { "ssh", "rdp", "telnet" })]
        public IEnumerable<string> Protocols { get; set; }

        [Option('r', "root", Required = false, HelpText = "Update system files as well as user files")]
        public bool RootMode {get;set;}

        [Option('s', "sudo", Required = false, HelpText = "use (passwordless) sudo to update system files on supported platforms")]
        public bool UseSudo { get; set;}
    }
}
