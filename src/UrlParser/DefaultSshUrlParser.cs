// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DefaultSshUrlParser.cs" company="One Identity Inc.">
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

namespace OneIdentity.Scalus.UrlParser
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using System.Text.RegularExpressions;
    using System.Web;
    using OneIdentity.Scalus.Dto;
    using OneIdentity.Scalus.Platform;
    using OneIdentity.Scalus.Util;
    using Serilog;
    using static OneIdentity.Scalus.Dto.ParserConfigDefinitions;

    [ParserName("ssh")]
    internal class DefaultSshUrlParser : BaseParser
    {
        private Regex scpPattern = new Regex("(([^:]+)://)?((.+)@([^:]+)(:(\\d+))?)", RegexOptions.IgnoreCase);

        // This class parses a string in the format:
        //  - <protocol>://<user>@<host>[:<port>]
        //  where:
        //      protocol: ssh
        //      user    : Username - this can contain a Safeguard auth string
        // If this doesnt match, it defaults to parsing the string as a standard URL.
        public DefaultSshUrlParser(ParserConfig config)
            : base(config)
        {
            FileExtension = ".ssh";
        }

        public DefaultSshUrlParser(ParserConfig config, IDictionary<Token, string> dictionary)
            : this(config)
        {
            if (dictionary != null)
            {
                Dictionary = dictionary;
            }
        }

        public override void PreExecute(IOsServices services)
        {
            base.PreExecute(services);

            var tempFile = Dictionary[Token.GeneratedFile];

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && !string.IsNullOrEmpty(tempFile))
            {
                // Make the temp file executable so it can be passed to the Terminal.
                // Use a direct file-mode change (owner rwx only) rather than shelling out to
                // `chmod 777`, which would make the generated script world-writable (local TOCTOU)
                // and interpolate the path into a /bin/sh command line.
                Log.Information($"Changing permissions on temp file: {tempFile}");
                try
                {
                    System.IO.File.SetUnixFileMode(
                        tempFile,
                        System.IO.UnixFileMode.UserRead | System.IO.UnixFileMode.UserWrite | System.IO.UnixFileMode.UserExecute);
                }
                catch (Exception ex)
                {
                    Serilog.Log.Warning($"Failed to change permissions on temp file: {ex.Message}");
                }
            }
        }

        public override IDictionary<Token, string> Parse(string url)
        {
            Dictionary = DefaultDictionary();
            Dictionary[Token.OriginalUrl] = url;
            Dictionary[Token.Protocol] = Protocol(url) ?? "ssh";
            Dictionary[Token.RelativeUrl] = StripProtocol(url);
            url = url.TrimEnd('/');
            var match = scpPattern.Match(url);
            if (!match.Success)
            {
                if (url.Contains("%40"))
                {
                    var decoded = HttpUtility.UrlDecode(url);
                    match = scpPattern.Match(decoded);
                }
            }

            if (!match.Success)
            {
                if (!Uri.TryCreate(url, UriKind.Absolute, out Uri result))
                {
                    throw new ParserException($"The SSH parser cannot parse URL: {url}");
                }

                Parse(result);
            }
            else
            {
                SetValue(match, 2, Token.Protocol, false, "ssh");
                SetValue(match, 4, Token.User, true);
                SetValue(match, 5, Token.Host, false);
                SetValue(match, 7, Token.Port, false, "22");

                GetSafeguardUserValue(Dictionary);
                ParseConfig();
            }

            if (!Dictionary.TryGetValue(Token.User, out var userToken) || string.IsNullOrEmpty(userToken))
            {
                Log.Warning($"The SSH parser could not extract the '{Token.User}' token from the url: {url}");
            }

            if (!Dictionary.TryGetValue(Token.Host, out var hostToken) || string.IsNullOrEmpty(hostToken))
            {
                Log.Warning($"The SSH parser could not extract the '{Token.Host}' token from the url: {url}");
            }

            return Dictionary;
        }

        protected override IEnumerable<string> GetDefaultTemplate()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                var user = Dictionary[Token.User];
                var host = Dictionary[Token.Host];
                var cmd = $"ssh {user}@{host}";
                Log.Information($"Default SSH file with cmd: {cmd}");

                var list = new List<string>();
                list.Add("#!/bin/zsh");
                list.Add(cmd);
                return list;
            }
            else
            {
                Serilog.Log.Error("No default template is defined in this parser");
                throw new NotImplementedException();
            }
        }
    }
}
