// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BaseParser.cs" company="One Identity Inc.">
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
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reactive.Disposables;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using System.Web;
    using OneIdentity.Scalus.Dto;
    using OneIdentity.Scalus.Platform;
    using OneIdentity.Scalus.Util;
    using Serilog;
    using static OneIdentity.Scalus.Dto.ParserConfigDefinitions;

    public abstract class BaseParser : IUrlParser
    {
        private string fileProcessorExe = null;
        private List<string> fileProcessorArgs = null;

        protected string FileExtension { get; set; } = ".scalus";

        protected IDictionary<Token, string> Dictionary { get; set; } = DefaultDictionary();

        protected static IDictionary<Token, string> DefaultDictionary()
        {
            var dictionary = new Dictionary<Token, string>();
            foreach (var one in Enum.GetValues(typeof(Token)))
            {
                dictionary[(Token)one] = string.Empty;
            }

            return dictionary;
        }

        public Regex SafeguardUserPattern = new Regex(
           @"(account[=|~]([^@%]+)[@|%]asset[=|~]([^@%]+)[@|%]vaultaddress[=|~]([^@%]+)[@|%]token[~|=]([^@%]+)[@|%]([^@%]+)[@|%](.*))", RegexOptions.IgnoreCase);


        protected ParserConfig Config { get; }


        protected CompositeDisposable Disposables { get; } = new CompositeDisposable();


        public BaseParser(ParserConfig config)
        {
            Config = config;
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public virtual void PreExecute(IOsServices services)
        {
            if (string.IsNullOrEmpty(fileProcessorExe))
                return;
            Log.Debug($"Starting file preprocessor: '{fileProcessorExe}' with args: '{string.Join(' ', fileProcessorArgs)}'");

            if (!File.Exists(fileProcessorExe))
            {
                Log.Error($"Selected file preprocessor does not exist:{fileProcessorExe}");
                return;
            }

            string output;
            string err;
            var res = services.Execute(fileProcessorExe, fileProcessorArgs, out output, out err);
            Log.Information($"File preprocess result:{res}, output:{output}, err:{err}");

        }

        public virtual void PostExecute(Process process)
        {
            var time = 0;
            if (Config.Options == null || Config.Options.Count == 0)
            {
                time = 10;
            }
            else
            {
                if (Config.Options.Any(x => string.Equals(x, ProcessingOptions.waitforexit.ToString(), StringComparison.OrdinalIgnoreCase)))
                {
                    Log.Information($"post processing - wait for exit");
                    process.WaitForExit();
                }
                else if (Config.Options.Any(x => string.Equals(x, ProcessingOptions.waitforinputidle.ToString(), StringComparison.OrdinalIgnoreCase)))
                {
                    Log.Information($"post processing - wait for inputidle");
                    process.WaitForInputIdle();
                }
                else
                {
                    time = 10;
                    var wait = Config.Options.FirstOrDefault(x =>
                        x.StartsWith($"{ProcessingOptions.wait}", StringComparison.OrdinalIgnoreCase));
                    if (!string.IsNullOrEmpty(wait))
                    {
                        if (wait.Equals($"{ProcessingOptions.wait}", StringComparison.OrdinalIgnoreCase))
                        {
                            time = 0;
                        }
                        else
                        {
                            var parts = wait.Split(":");
                            if (parts.Length > 1)
                            {
                                int.TryParse(parts[1], out time);
                            }
                        }
                    }
                }
            }

            if (time > 0)
            {
                Log.Information($"post processing - waiting for {time} seconds");
                Task.Delay(time * 1000).Wait();
            }

            if (process.HasExited)
            {
                Log.Information($"Application exited with exit code: {process.ExitCode}");
            }
            else
            {
                Log.Information($"Application still running - scalus has finished");
            }
        }

        public abstract IDictionary<Token, string> Parse(string url);

        protected void SetValue(Match match, int index, Token property, bool decode, string defValue = null)
        {
            var val = defValue ?? string.Empty;
            if (match.Success && match.Groups.Count >= index)
            {
                if (!string.IsNullOrEmpty(match.Groups[index].Value))
                {
                    val = match.Groups[index].Value;
                }
            }

            if (decode)
            {
                val = HttpUtility.UrlDecode(val);
            }

            Dictionary[property] = val;
        }

        protected static void SetValue(string user, IDictionary<Token, string> dictionary, string pattern, int index, Token property, bool decode, string defValue = null)
        {
            var re = new Regex(pattern);
            var match = re.Match(user);

            var val = defValue ?? string.Empty;
            if (match.Success && match.Groups.Count >= index)
            {
                if (!string.IsNullOrEmpty(match.Groups[index].Value))
                {
                    val = match.Groups[index].Value;
                }
            }

            if (decode)
            {
                val = HttpUtility.UrlDecode(val);
            }

            dictionary[property] = val;
        }

        public static void GetSafeguardUserValue(IDictionary<Token, string> dictionary)
        {
            var user = dictionary[Token.User];

            SetValue(user, dictionary, "vaultaddress[=|~]([^@%]+)", 1, Token.Vault, false);
            SetValue(user, dictionary, "account[=|~]([^@%]+)", 1, Token.Account, false);

            SetValue(user, dictionary, "asset[=|~]([^@%]+)", 1, Token.Asset, false);


            var tokenpattern = "(token[=|~]([^@%]+)[@|%]([^%]+)[@|%]([^%]+))";
            var i = user.IndexOf("token");
            if (i >= 0)
            {
                var tokstr = user.Substring(i);
                SetValue(tokstr, dictionary, tokenpattern, 2, Token.Token, false);
                SetValue(tokstr, dictionary, tokenpattern, 3, Token.TargetUser, false);
                SetValue(tokstr, dictionary, tokenpattern, 4, Token.TargetHost, false);

                (string host, string port) = ParseHost(dictionary[Token.TargetHost]);
                dictionary[Token.TargetHost] = host;
                dictionary[Token.TargetPort] = port;
            }

        }

        private void WriteTempFile(IEnumerable<string> lines, string ext)
        {
            try
            {
                string tempFile;
                var isSafeguard = Dictionary.ContainsKey(Token.Vault) && !string.IsNullOrEmpty(Dictionary[Token.Vault]);
                if (isSafeguard)
                {
                    var guid = Guid.NewGuid().ToString();
                    var host = Dictionary[Token.TargetHost];
                    host = Regex.Replace(host, "[.]", "~");
                    var user = Dictionary[Token.TargetUser];
                    user = user.Replace('\\', '~');
                    tempFile = Path.Combine(Path.GetTempPath(),
                        $"SG-{host}_{user}_{guid}{ext}");
                }
                else
                {
                    var host = (Dictionary.ContainsKey(Token.Host) && !string.IsNullOrEmpty(Dictionary[Token.Host])
                        ? Dictionary[Token.Host]
                        : string.Empty);
                    var user = (Dictionary.ContainsKey(Token.User) &&
                                !string.IsNullOrEmpty(Dictionary[Token.User])
                        ? Dictionary[Token.User]
                        : string.Empty);
                    if (!string.IsNullOrEmpty(host) || !string.IsNullOrEmpty(user))
                    {
                        var guid = Guid.NewGuid().ToString();
                        host = Regex.Replace(host, "[.]", "~");
                        user = user.Replace('\\', '~');

                        tempFile = Path.Combine(Path.GetTempPath(),
                        $"Scalus-{host}_{user}_{guid}{ext}");
                    }
                    else
                    {
                        tempFile = Path.GetTempFileName();
                        string renamed = Path.ChangeExtension(tempFile, ext);
                        File.Move(tempFile, renamed);
                        tempFile = renamed;
                    }
                }

                Disposables.Add(Disposable.Create(() => File.Delete(tempFile)));
                var newlines = new List<string>();
                foreach (var line in lines)
                {
                    newlines.Add(ReplaceTokens(line));
                }

                var dir = Path.GetDirectoryName(tempFile);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                File.WriteAllText(tempFile, string.Join(Environment.NewLine, newlines));
                Dictionary[Token.GeneratedFile] = tempFile;
                fileProcessorArgs = new List<string>();
                fileProcessorExe = string.Empty;
                if (!string.IsNullOrEmpty(Config.PostProcessingExec))
                {
                    var found = false;
                    fileProcessorExe = ReplaceTokens(Config.PostProcessingExec);
                    foreach (var arg in Config.PostProcessingArgs)
                    {
                        if (arg.Contains($"%{Token.GeneratedFile}%"))
                        {
                            found = true;
                        }

                        fileProcessorArgs.Add(ReplaceTokens(arg));
                    }

                    if (!found)
                    {
                        fileProcessorArgs.Add(Dictionary[Token.GeneratedFile]);
                    }
                }

                Log.Information(
                    $"Preprocessing cmd:{fileProcessorExe} args:{string.Join(',', fileProcessorArgs)}");
            }
            catch (Exception e)
            {
                Log.Error($"Failed to process temp file: {e.Message}");
            }
        }


        protected static (string host, string port) ParseHost(string host)
        {
            var sep = host.LastIndexOf(":", StringComparison.Ordinal);
            if (sep == -1)
            {
                return (host, null);
            }

            return (host.Substring(0, sep), host.Substring(sep + 1));
        }

        protected static string StripProtocol(string url)
        {
            var protocolIndex = url.IndexOf("://", StringComparison.Ordinal);
            if (protocolIndex == -1) return url;
            return url.Substring(protocolIndex + 3);
        }

        protected static string Protocol(string url, string def = null)
        {
            var protocolIndex = url.IndexOf("://", StringComparison.Ordinal);
            if (protocolIndex == -1) return def;
            return url.Substring(0, protocolIndex);
        }

        protected abstract IEnumerable<string> GetDefaultTemplate();

        public static string ReplaceToken(string tokenKey, string tokenValue, string line)
        {
            var patt = "%" + tokenKey + "%";
            var newline = line;
            newline = Regex.Replace(newline, patt, tokenValue ?? string.Empty, RegexOptions.IgnoreCase);
            if (!string.IsNullOrEmpty(tokenValue))
            {
                patt = "(%" + tokenKey + "[?]([^:]*):[^%]*%)";
            }
            else
            {
                patt = "(%" + tokenKey + "[?][^:]*:([^%]*)%)";
            }

            newline = Regex.Replace(newline, patt, x => x.Groups[2]?.Value);
            return newline;
        }

        public virtual string ReplaceTokens(string line)
        {
            var newline = line;
            foreach (var variable in Dictionary)
            {
                newline = ReplaceToken(variable.Key.ToString(), variable.Value, newline);
            }

            return newline;
        }

        public string GetFullPath(string path)
        {
            if (Path.IsPathFullyQualified(path))
            {
                return path;
            }

            var dir = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(dir, path);
        }

        protected void ParseConfig()
        {
            Dictionary[Token.Home] = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            Dictionary[Token.AppData] = ConfigurationManager.ProdAppPath;
            Dictionary[Token.TempPath] = Path.GetTempPath();
            IEnumerable<string> fileLines = null;
            if (Config.UseDefaultTemplate)
            {
                Log.Information("Using default template");
                fileLines = GetDefaultTemplate();
            }
            else if (!string.IsNullOrEmpty(Config.UseTemplateFile))
            {
                Log.Information($"Using template :{Config.UseTemplateFile}");
                var templatefile = ReplaceTokens(Config.UseTemplateFile);
                templatefile = GetFullPath(templatefile);
                Log.Information($"Using template file:{templatefile}");

                if (!File.Exists(templatefile))
                {
                    Log.Error($"Application template does not exist:{templatefile}");
                    throw new Exception($"Application template file does not exist: {templatefile}");
                }

                var ext = Path.GetExtension(templatefile);
                if (!string.IsNullOrEmpty(ext))
                {
                    FileExtension = ext;
                }

                try
                {
                    fileLines = File.ReadAllLines(templatefile);
                }
                catch (Exception e)
                {
                    Log.Error(e, $"Cannot read template file: {templatefile}");
                }
            }

            if (fileLines != null)
            {
                WriteTempFile(fileLines, FileExtension);
            }
        }

        protected void Parse(Uri url)
        {
            Dictionary = new Dictionary<Token, string>();
            try
            {
                Dictionary[Token.OriginalUrl] = url.GetComponents(UriComponents.AbsoluteUri, UriFormat.UriEscaped);
                Dictionary[Token.RelativeUrl] = StripProtocol(Dictionary[Token.OriginalUrl]);
                Dictionary[Token.Protocol] = url.GetComponents(UriComponents.Scheme, UriFormat.Unescaped);
                Dictionary[Token.Host] = url.GetComponents(UriComponents.Host, UriFormat.Unescaped);
                Dictionary[Token.Port] = url.GetComponents(UriComponents.Port, UriFormat.Unescaped);
                Dictionary[Token.Path] = url.GetComponents(UriComponents.Path, UriFormat.Unescaped);
                Dictionary[Token.User] = url.GetComponents(UriComponents.UserInfo, UriFormat.Unescaped);
                Dictionary[Token.Query] = url.GetComponents(UriComponents.Query, UriFormat.Unescaped);
                Dictionary[Token.Fragment] = url.GetComponents(UriComponents.Fragment, UriFormat.Unescaped);
                ParseConfig();
            }
            catch
            {
                Log.Warning($"The string does not appear to be a valid URL: {url} ");
            }
        }


        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Disposables.Dispose();
                }

                disposedValue = true;
            }
        }

        public List<string> ReplaceTokens(List<string> args)
        {
            var newargs = new List<string>();
            foreach (var arg in args)
            {
                var newarg = ReplaceTokens(arg.Trim());
                newargs.Add(newarg);
            }

            return newargs;
        }

        protected virtual IEnumerable<string> GetTemplateOverrides(IEnumerable<string> templateContents)
        {
            return templateContents;
        }

        private bool disposedValue;
    }
}
