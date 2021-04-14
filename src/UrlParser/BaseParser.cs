using scalus.Dto;
using scalus.Platform;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace scalus.UrlParser
{
    public abstract class BaseParser : IUrlParser
    {  
        private string _fileProcessorExe = null;
        private List<string> _fileProcessorArgs = null;       
        protected string FileExtension { get; set; } = ".sulu";
        protected IDictionary<Token,string> Dictionary {get; set; }  = new Dictionary<Token,string>();

        public Regex SafeguardUserPattern = new Regex(
           @"(vaultaddress[=|~]([^@%]+)[@|%]token[~|=]([^@%]+)[@|%]([^@%]+)[@|%](.*))", RegexOptions.IgnoreCase);

        protected ParserConfig Config { get; }
          
        public Dictionary<Token, string> TokenDescription {get; }  = new Dictionary<Token, string>
        {
            {  Token.OriginalUrl, "The full string passed to the application. This is available even if it is not a valid URL" },
            {  Token.Protocol, "The protocol to use, eg. scheme part of the URL" },
            {  Token.User, "The user (userinfo of the URL). For Safeguard URLs, this will contain the auth token information" },
            {  Token.Host, "The host part of the URL" },
            {  Token.Port, "The port part of the URL" },
            {  Token.Path, "The path part of a standard URL" },
            {  Token.Query, "The optional query of a standard URL" },
            {  Token.Fragment, "The fragment part of a standard URL" },
            {  Token.Vault, "The vault address of a Safeguard URL" },
            {  Token.Token, "The token string of a Safeguard URL" },
            {  Token.TargetUser, "The target user part of a Safeguard URL" },
            {  Token.TargetHost, "The target host part of a Safeguard URL" },
            {  Token.GeneratedFile, "The generated file that will be passed to the application. The file extension will be set to that of the template (if provided), or determined by the parser" },
            {  Token.TempPath, "The user's temp directory on this platform" },
            {  Token.Home, "The user's home directory on this platform" },
       };

        protected CompositeDisposable Disposables { get; } = new CompositeDisposable();

       
        public BaseParser( ParserConfig config)
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
            if (string.IsNullOrEmpty(_fileProcessorExe ))
                return;
            Serilog.Log.Debug($"Starting file preprocessor: '{_fileProcessorExe}' with args: '{string.Join(' ', _fileProcessorArgs)}'");

            if (!File.Exists(_fileProcessorExe ))
            {
                Serilog.Log.Error($"Selected file preprocessor does not exist:{_fileProcessorExe}");
                return;
            }
            var process = services.Execute(_fileProcessorExe, _fileProcessorArgs);
            process.WaitForExit();
        }

        public virtual void PostExecute(Process process)
        {
            if(Config.Options.Any(x => string.Equals(x, ProcessingOptions.waitforexit.ToString(), StringComparison.OrdinalIgnoreCase)))
            {
                process.WaitForExit();
            }
            if (Config.Options.Any(x => string.Equals(x, ProcessingOptions.waitforinputidle.ToString(), StringComparison.OrdinalIgnoreCase)))
            {

                process.WaitForInputIdle();
            }
            var wait = Config.Options.FirstOrDefault(x => x.StartsWith($"{ProcessingOptions.wait}:", StringComparison.OrdinalIgnoreCase));
            if(!string.IsNullOrEmpty(wait))
            {

                var parts = wait.Split(":");
                int time = 0;
                if (parts.Length > 1)
                {
                    int.TryParse(parts[1], out time);
                }
                if(time > 0)
                {
                    Task.Delay(time * 1000).Wait();
                }
            }
        }

        public abstract IDictionary<Token, string> Parse(string url);

        protected void SetValue(Match match, int index, Token property, bool decode, string defValue = null)
        {
            var val = defValue??string.Empty;
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

        protected void GetSafeguardUserValue()
        {
            var match = SafeguardUserPattern.Match(Dictionary[Token.User]);
           
            SetValue(match, 2, Token.Vault, false);
            SetValue(match, 3, Token.Token,false );
            SetValue(match, 4, Token.TargetUser, false);
            
            SetValue(match, 5, Token.TargetHost, false);
            (string host, string port) = ParseHost(Dictionary[Token.TargetHost]);
            Dictionary[Token.TargetHost] = host;
            Dictionary[Token.TargetPort] = port;
         }

        private void WriteTempFile(IEnumerable<string> lines, string ext)
        {
            //TODO check platform specific
            var tempFile = Path.GetTempFileName();
            string renamed = Path.ChangeExtension(tempFile, ext);
            File.Move(tempFile, renamed);
            tempFile = renamed;
            Disposables.Add(Disposable.Create(() => File.Delete(tempFile)));
            var newlines = new List<string>();
            foreach(var line in lines)
            {
                var newline = line;
                foreach(var onevar in Dictionary)
                {
                     newline = Regex.Replace(newline, $"%{onevar.Key}%", $"{onevar.Value}", RegexOptions.IgnoreCase);
                }
                newlines.Add(newline);
            } 
            File.WriteAllText(tempFile, string.Join(Environment.NewLine, newlines));
            Dictionary[Token.GeneratedFile] = tempFile;
            _fileProcessorArgs = new List<string>();
            _fileProcessorExe = string.Empty;
            if (Config.PostProcessingCmd?.Count > 0)
            {              
                var found = false;
                _fileProcessorExe = ReplaceTokens(Config.PostProcessingCmd[0]);
                for (var c=1; c<Config.PostProcessingCmd.Count; c++ )
                {
                    if (Config.PostProcessingCmd[c].Contains($"%{Token.GeneratedFile}%"))
                    { 
                        found = true;
                    }
                    _fileProcessorArgs.Add(ReplaceTokens(Config.PostProcessingCmd[c]));
                }
                if (!found)
                {
                    _fileProcessorArgs.Add($"%{Dictionary[Token.GeneratedFile]}%");
                }
            }

            Serilog.Log.Information($"Preprocessing cmd:{_fileProcessorExe} args:{string.Join(',', _fileProcessorArgs)}");
        }
    

        protected (string host,string port) ParseHost(string host)
        {
            var sep = host.LastIndexOf(":");
            if (sep == -1)
            {
                return (host, null);
            }
            return (host.Substring(0, sep), host.Substring(sep+1));
        }
        protected static string StripProtocol(string url)
        {
            var protocolIndex = url.IndexOf("://");
            if (protocolIndex == -1) return url;
            return  url.Substring(protocolIndex + 3);
        }
        protected static string Protocol(string url, string def = null)
        {
            var protocolIndex = url.IndexOf("://");
            if (protocolIndex == -1) return def;
            return url.Substring(0, protocolIndex);
        }

        protected abstract IEnumerable<string> GetDefaultTemplate();
    
        private string ReplaceTokens(string line)
        {
            var newline = line;
            foreach (var variable in Dictionary)
            {
                // TODO: Make this more robust. Edge case escapes don't work.
                newline = Regex.Replace(newline, $"%{variable.Key}%", variable.Value??string.Empty, RegexOptions.IgnoreCase);
            }
            return newline;
        }

        protected void ParseConfig()
        {
            Dictionary[Token.Home]= Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            Dictionary[Token.TempPath]= Path.GetTempPath();
            IEnumerable<string> fileLines = null;
            if (Config.UseDefaultTemplate)
            {
                fileLines = GetDefaultTemplate();
            }
            else if (!string.IsNullOrEmpty(Config.UseTemplateFile))
            {
                var templatefile = ReplaceTokens(Config.UseTemplateFile);
                if (!File.Exists(templatefile))
                {
                    Serilog.Log.Error($"Application template does not exist:{templatefile}");
                    throw new Exception($"Application template file does not exist: {templatefile}");
                }
                
                var ext = Path.GetExtension(templatefile);
                if (!string.IsNullOrEmpty(ext))
                {
                    FileExtension = ext;
                }
                try {
                    fileLines = File.ReadAllLines(templatefile);
                }
                catch (Exception e)
                {
                    Serilog.Log.Error(e, $"Cannot read template file: {templatefile}");
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
            try {
                Dictionary[Token.OriginalUrl]=url.GetComponents(UriComponents.AbsoluteUri, UriFormat.SafeUnescaped);
                Dictionary[Token.RelativeUrl] = StripProtocol(Dictionary[Token.OriginalUrl]);
                Dictionary[Token.Protocol] = url.GetComponents(UriComponents.Scheme, UriFormat.SafeUnescaped);
                Dictionary[Token.Host] = url.GetComponents(UriComponents.Host, UriFormat.SafeUnescaped);
                Dictionary[Token.Port] = url.GetComponents(UriComponents.Port, UriFormat.SafeUnescaped);
                Dictionary[Token.Path] = url.GetComponents(UriComponents.Path, UriFormat.SafeUnescaped);
                Dictionary[Token.User] = url.GetComponents(UriComponents.UserInfo, UriFormat.SafeUnescaped);
                Dictionary[Token.Query] = url.GetComponents(UriComponents.Query, UriFormat.SafeUnescaped);
                Dictionary[Token.Fragment] = url.GetComponents(UriComponents.Fragment, UriFormat.SafeUnescaped);
                ParseConfig();
            }
            catch
            {
                Serilog.Log.Warning($"The string does not appear to be a valid URL: {url} ");
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
                var newarg = ReplaceTokens(arg);
                newargs.Add(newarg);
            }
            return newargs;
        }

        private bool disposedValue;
    }
}
