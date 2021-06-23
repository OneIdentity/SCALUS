using CommandLine;

namespace scalus.Launch
{
    [Verb("launch", HelpText = "Launch an app configured for the specified URL")]
    public class Options : IVerb
    {
        [Option('u', "url", Required = true, HelpText = "The URL to launch.")]
        public string Url { get; set; }

        [Option('p', "preview", Required = false, HelpText = "Show me what will launch, but dont run it. This will also report the token values and show the contents of the generated file, if applicable." )]
        public bool Preview { get; set; }
    }
}
