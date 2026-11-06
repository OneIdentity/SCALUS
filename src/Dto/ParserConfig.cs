// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ParserConfig.cs" company="One Identity Inc.">
//   This software is licensed under the Apache 2.0 open source license.
//   https://github.com/OneIdentity/SCALUS/blob/master/LICENSE
//
//
//   Copyright One Identity LLC.
//   ALL RIGHTS RESERVED.
//
//   ONE IDENTITY LLC. MAKES NO REPRESENTATIONS OR
//   WARRANTIES ABOUT THE SUITABILITY OF THE SOFTWARE,
//   EITHER EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED
//   TO THE IMPLIED WARRANTIES OF MERCHANTABILITY,
//   FITNESS FOR A PARTICULAR PURPOSE, OR
//   NON-INFRINGEMENT.  ONE IDENTITY LLC. SHALL NOT BE
//   LIABLE FOR ANY DAMAGES SUFFERED BY LICENSEE
//   AS A RESULT OF USING, MODIFYING OR DISTRIBUTING
//   THIS SOFTWARE OR ITS DERIVATIVES.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace OneIdentity.Scalus.Dto
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json.Serialization;
    using System.Text.RegularExpressions;

    public class ParserConfig
    {
        public static Dictionary<string, string> DtoPropertyDescription { get; } = new Dictionary<string, string>
        {
            { nameof(ParserId), $"The predefined parser that will be used to parse the URL. Valid values are {string.Join(',', ProtocolHandlerFactory.GetSupportedParsers())}. The default 'url' parser can be used to parse any standard URL" },
            { nameof(Options), $"Identifies how scalus handles the running application. Valid values are {string.Join(',', Enum.GetValues<ParserConfigDefinitions.ProcessingOptions>())}. The '{ParserConfigDefinitions.ProcessingOptions.wait}' option waits for default 10 secs, but can be configured, e.g. 'wait:<n>' (wait for n seconds)" },
            { nameof(TemplateContent), "The full text of the template to generate for this application. Stored inline in the configuration with LF line endings; the actual line ending and encoding are applied when the file is written (see LineEnding and Encoding). Tokens in the template are replaced at launch. The generated file can be referenced using the '%GeneratedFile%' token. Leave empty for applications that only build a command line." },
            { nameof(TemplateExtension), "The file extension used for the generated file (e.g. '.rdp', '.remmina'). If empty, the parser's built-in default extension is used." },
            { nameof(LineEnding), $"The line ending written to the generated file. Valid values are {string.Join(',', Enum.GetValues<ParserConfigDefinitions.TemplateLineEnding>())}. 'Default' resolves from the file extension (.rdp uses CrLf, others use Lf)." },
            { nameof(Encoding), $"The text encoding used to write the generated file. Valid values are {string.Join(',', Enum.GetValues<ParserConfigDefinitions.TemplateEncoding>())}. 'Default' resolves from the file extension (.rdp uses Utf16LeBom, others use Utf8)." },
            { nameof(PostProcessingExec), "The path to an executable file that will be run to process the %GeneratedFile% before launching the application. This path can contain any of the supported tokens" },
            { nameof(PostProcessingArgs), "The arguments to pass to the 'PostProcessingExec' executable. These arguments can contain any of the supported tokens" },
            { nameof(RunInTerminal), "When true, the configured Exec/Args describe a command that should run inside an interactive terminal window; SCALUS launches it in the user's preferred terminal (see ScalusConfig.PreferredTerminal) instead of directly. Intended for SSH/telnet-style clients." },
        };

        [JsonRequired]
        public string ParserId { get; set; }

        public List<string> Options { get; set; }

        // The template text is stored inline in the configuration (LF-canonical). When present, a
        // generated file is materialized at launch and can be referenced via the '%GeneratedFile%' token.
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string TemplateContent { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string TemplateExtension { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public ParserConfigDefinitions.TemplateLineEnding LineEnding { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public ParserConfigDefinitions.TemplateEncoding Encoding { get; set; }

        // Deprecated: retained for one-time migration into TemplateContent only. Not shown in the UI and
        // omitted from serialization once cleared. Do not use for new configurations.
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool UseDefaultTemplate { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string UseTemplateFile { get; set; }

        public string PostProcessingExec { get; set; }

        public List<string> PostProcessingArgs { get; set; }

        // When true, Exec/Args describe a command to run inside an interactive terminal; the launcher
        // hosts it in the user's preferred terminal (ScalusConfig.PreferredTerminal) rather than
        // spawning it directly. Off by default so existing (RDP/GUI) apps are unaffected.
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool RunInTerminal { get; set; }

        [JsonIgnore]
        public bool HasTemplate => !string.IsNullOrEmpty(TemplateContent);

        public void Validate(List<string> errors)
        {
            var supported = ProtocolHandlerFactory.GetSupportedParsers();
            if (!supported.Contains(ParserId))
            {
                errors.Add($"Selected parser '{ParserId}' is not in the supported list: {string.Join(", ", supported.ToArray())}");
            }

            if (Options?.Count > 0)
            {
                var opts = string.Join(',', Enum.GetValues<ParserConfigDefinitions.ProcessingOptions>());

                foreach (var opt in Options)
                {
                    if (string.IsNullOrEmpty(opt))
                    {
                        continue;
                    }

                    if (!Enum.TryParse(typeof(ParserConfigDefinitions.ProcessingOptions), opt, true, out object o))
                    {
                        if (Regex.IsMatch(opt, $"{ParserConfigDefinitions.ProcessingOptions.wait}:\\d+"))
                        {
                            continue;
                        }

                        errors.Add($"Invalid Processing Option: {opt}. Valid values are: [{opts}]");
                    }
                }
            }

            if (UseDefaultTemplate && !string.IsNullOrEmpty(UseTemplateFile))
            {
                errors.Add($"The properties: {nameof(UseDefaultTemplate)} and {nameof(UseTemplateFile)} are mutually exclusive");
            }

            if (!string.IsNullOrEmpty(PostProcessingExec))
            {
                var producesFile = HasTemplate || UseDefaultTemplate || !string.IsNullOrEmpty(UseTemplateFile);
                if (!producesFile)
                {
                    errors.Add($"{nameof(PostProcessingExec)} can only be used when a generated file is produced (set {nameof(TemplateContent)})");
                }
            }
        }
    }
}
