using CommandLine;

namespace scalus.Ui
{ 
    [Verb("ui", isDefault:true, HelpText = "Run the configuration UI")]
    public class Options : IVerb
    {
    }
}
