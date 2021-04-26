using CommandLine;
using System.Collections.Generic;

namespace scalus.Unregister
{
    [Verb("unregister", HelpText = "Unregister SCALUS for URL handling")]
    public class Options : IVerb
    {
        [Option('p', "protocols", Required = false, HelpText = "A space-separated list of URL protocols to handle (Default: ssh rdp telnet)", Default = new string[] { "ssh", "rdp", "telnet" })]
        public IEnumerable<string> Protocols { get; set; }
               
        [Option('u', "user", Required = false, HelpText = "Update user defaults only, not system files")]
        public bool UserMode {get;set;}

        [Option('s', "sudo", Required = false, HelpText = "use (passwordless) sudo to update system files on supported platforms")]
        public bool UseSudo { get; set;}

    }
}
