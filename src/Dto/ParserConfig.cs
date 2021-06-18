using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace scalus.Dto
{

    public class ParserConfig
    {
        [JsonRequired]
        public string ParserId { get; set; }
        public List<string> Options { get; set; }
        public bool UseDefaultTemplate { get; set; }
        public string UseTemplateFile { get; set; }
        public string PostProcessingExec { get; set; }
        public List<string> PostProcessingArgs { get; set; }

        public static Dictionary<string, string> DtoPropertyDescription { get; } = new Dictionary<string, string>
        {
            {nameof(ParserId),$"The predefined parser that will be used to parse the URL. Valid values are {string.Join(',',ProtocolHandlerFactory.GetSupportedParsers())}. The default 'url' parser can be used to parse any standard URL"},
            {nameof(Options),$"Identifies how scalus handles the running application. Valid values are {string.Join(',', Enum.GetValues(typeof(ParserConfigDefinitions.ProcessingOptions)).Cast<ParserConfigDefinitions.ProcessingOptions>())}. The '{ParserConfigDefinitions.ProcessingOptions.wait}' option waits for default 20 secs, but can be configured, e.g. 'wait:<n>' (wait for n seconds)"},
            {nameof(UseDefaultTemplate),"Supported for the rdp parser only. A default template is generated in the format used by Microsoft Remote Desktop. This is copied to a temporary file that can be identified using the '%GeneratedFile%' token."},
            {nameof(UseTemplateFile),"The path to a template file to use for the application. The template will be copied to a temporary file, replacing any tokens in the template. The temporary file can be identified using the '%GeneratedFile%' token. Supported by all parsers. This path can contain any of the supported tokens."},
            {nameof(PostProcessingExec),"The path to an executable file that will be run to process the %GeneratedFile% before launching the application. This path can contain any of the supported tokens"},
            {nameof(PostProcessingArgs),"The arguments to pass to the 'PostProcessingExec' executable. These arguments can contain any of the supported tokens"}
        };

        public void Validate(List<string> errors)
        {
            var supported = ProtocolHandlerFactory.GetSupportedParsers(); 
            if (!supported.Contains(ParserId))
            {
                errors.Add($"Selected parser '{ParserId}' is not in the supported list:{string.Join(',',supported.ToArray())}");
            }

            if (Options?.Count > 0)
            {
                var opts = string.Join(',', Enum.GetValues(typeof(ParserConfigDefinitions.ProcessingOptions))
                    .Cast<ParserConfigDefinitions.ProcessingOptions>());

                foreach (var opt in Options)
                {
                    if (string.IsNullOrEmpty(opt))
                        continue;
                    if (!Enum.TryParse(typeof(ParserConfigDefinitions.ProcessingOptions), opt, true, out object o))
                    {
                        if (Regex.IsMatch(opt, $"{ParserConfigDefinitions.ProcessingOptions.wait}:\\d+"))
                        {
                            continue;
                        }

                        errors.Add($"Invalid Processing Option:{opt}. Valid values are:[{opts}]");
                    }
                }
            }

            if (UseDefaultTemplate && !string.IsNullOrEmpty(UseTemplateFile))
            {
                errors.Add($"The properties: {nameof(UseDefaultTemplate)} and {nameof(UseTemplateFile)} are mutually exclusive");
            }

            if (!string.IsNullOrEmpty(PostProcessingExec))
            {
                if (!UseDefaultTemplate && string.IsNullOrEmpty(UseTemplateFile))
                {
                    errors.Add($"{nameof(PostProcessingExec)} can only be used with {nameof(UseDefaultTemplate)} or {nameof(UseTemplateFile)}");
                }
            }
        }
    }
}
