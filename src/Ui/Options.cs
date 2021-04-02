using CommandLine;

namespace scalus.Ui
{ 
    [Verb("ui", isDefault:true, HelpText = "Show the configuration UI")]
    public class Options : IVerb
    {
    }
}
