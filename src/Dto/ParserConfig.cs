using System.Collections.Generic;

namespace scalus.Dto
{   
    public class ParserConfig
    {
        public string Id { get; set; }
        public List<string> Options { get; set; }
        public bool UseDefaultTemplate { get; set; }
        public string UseTemplateFile { get; set; }
        public string PostProcessingExec { get; set; }
        public List<string> PostProcessingArgs { get; set; }
        
    }
}
