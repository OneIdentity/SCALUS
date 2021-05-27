using CommandLine;

namespace scalus.Info
{
    [Verb("info", HelpText = "Show information about the current scalus configuration")]
    public class Options : IVerb
    {
        [Option('d', "dto", Required = false, HelpText = "Show DTO description")]
        public bool Dto { get; set; }

        [Option('t', "tokens", Required = false, HelpText = "Show the list of tokens")]
        public bool Tokens { get; set; }

    }
}
