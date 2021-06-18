using CommandLine;

namespace scalus.Verify
{
    [Verb("verify", HelpText = "Run a syntax check on a scalus configuration file")]
    public class Options : IVerb
    {
        [Option('p', "path", Required = false, HelpText = "Path of an alternate scalus configuration file to verify instead")]
        public string Path { get; set; }

    }
}
