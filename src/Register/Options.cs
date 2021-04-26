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

        [Option('u', "user", Required = false, HelpText = "Update user defaults only, not system files")]
        public bool UserMode {get;set;}

        [Option('s', "sudo", Required = false, HelpText = "use (passwordless) sudo to update system files on supported platforms")]
        public bool UseSudo { get; set;}
    }
}
