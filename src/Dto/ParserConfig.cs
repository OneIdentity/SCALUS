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
        private static Dictionary<string, string> _dtoPropertyDescription = new Dictionary<string, string>
        {
            {"Id","Unique Identity"},
            {"Options","Identifies how scalus handles the running application. Valid values are 'waitforinputidle' (wait for , 'waitforexit' (wait for the application to finish), 'wait:<n>' (wait for n seconds)"},
            {"UseDefaultTemplate","Supported for the rdp parser. A default TODO"},
            {"UseTemplateFile","Supports tokens. The full path to a template file to use for the application. A copy will be made (any tokens it contains will be processed) and this temp file can be passed to the application using the %GeneratedFile% token. "},
            {"PostProcessingExec","TODO"},
            {"PostProcessingArgs","TODO"}
        };
        public static Dictionary<string, string> DtoPropertyDescription
        {
            get
            {
                return _dtoPropertyDescription;
            }
        }
    }
}
