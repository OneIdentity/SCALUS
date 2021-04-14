using System.Collections.Generic;

namespace scalus.Dto
{
    public class Executable
    {
        public string Exec { get; set; }
        public List<string> Args { get; set; }
    }
    public class TemplateConfig
    {
        public string Filename { get; set; }
        public Executable PostProcess { get; set; }
    }
    public class ParserConfig
    {
        public string Id { get; set; }
        public List<string> Options { get; set; }
        public bool UseDefaultTemplate { get; set; }
        public string UseTemplateFile { get; set; }
        public List<string> PostProcessingCmd { get; set; }
    }
}
