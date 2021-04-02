using CommandLine;

namespace scalus.Launch
{
    [Verb("launch", HelpText = "Launch an app configured for the specified URL")]
    public class Options : IVerb
    {
        [Option('u', "url", Required = true, HelpText = "The URL to launch.")]
        public string Url { get; set; }
    }
}
