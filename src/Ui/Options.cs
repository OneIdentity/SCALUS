using CommandLine;

namespace Sulu.Ui
{ 
    [Verb("ui", isDefault:true, HelpText = "Show the configuration UI")]
    public class Options : IVerb
    {
    }
}
