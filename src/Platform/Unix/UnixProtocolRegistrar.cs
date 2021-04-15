using scalus.Platform;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace scalus
{
    public class UnixProtocolRegistrar : IProtocolRegistrar
    {
        private const string xdgConfigPath = ".config";
        private const string xdgApp = "xdg-settings";
        private const string updateCacheCmd = "update-desktop-cache";
        private IOsServices OsServices;
        private string _preferredPath = null;

        public UnixProtocolRegistrar(IOsServices osServices)
        {
            OsServices = osServices;
        }

        private string GetAppPath(string protocol)
        {     
            GetPreferredConfigPath();
            return Path.Combine(_preferredPath, $"{protocol}.desktop");
        }

        public bool IsScalusRegistered(string protocol)
        {

            var registrationCommand = Constants.GetLaunchCommand("%1");
            var exe = GetRegisteredCommand(protocol);
            return (Regex.IsMatch(exe, registrationCommand, RegexOptions.IgnoreCase));           
        }
        public string GetPreferredConfigPath()
        {
            if (_preferredPath != null)
            {
                return _preferredPath ;
            }
            string stdOut;
            string stdErr;
            var args = new List<string> { "-c", $"XDG_UTILS_DEBUG_LEVEL=2 {xdgApp} get default-url-scheme-handler" };
            var result = RunCommand( "sh", args, out stdOut, out stdErr);
            if (!result)
            {
                return string.Empty;
            }
            var lines = stdOut.Split("\n");
            if (lines.Count() > 0)
            {
                var path = lines[0];
                var match = Regex.Match(path, "\\s*Checking\\s*(\\S+)", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var fname = match.Groups[1].Value;
                    _preferredPath = Path.GetDirectoryName(fname);
                    Serilog.Log.Information($"SHOUT -  prefPath is {_preferredPath}");
                    return _preferredPath ;
                }
            }
            _preferredPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), xdgConfigPath);
            Serilog.Log.Information($"SHOUT -  using default config path:{_preferredPath}");
            return _preferredPath ;
        }
        public string GetRegisteredCommand(string protocol)
        {
            var args = new List<string> { "get", "default-url-scheme-handler", protocol };
            var result = RunCommand( xdgApp, args, out string stdOut, out string stdErr);
            return (result ? stdOut : string.Empty );
        }
        private bool RunCommand(string exe, List<string> args, out string output, out string err)
        {
            output = string.Empty;
            err = string.Empty;
            try {
                var process = OsServices.Execute(exe, args, out output, out err);
                process.WaitForExit();
                if (process.ExitCode == 0)
                {
                    output = process.StandardError?.ReadToEnd() ;
                    err = process.StandardError?.ReadToEnd();
                                    Serilog.Log.Information($"SHOUT xdg ok: out:{output}, err:{err}");
                    return true;
                } 
                output = process.StandardError?.ReadToEnd() ;
                err = process.StandardError?.ReadToEnd();

                Serilog.Log.Error($"Failed to run {xdgApp} : (exitcode:{process.ExitCode})(stdout:{output}, err:{err}");
            }
            catch (Exception e)
            {
                Serilog.Log.Information($"Failed to run {xdgApp}: {e.Message}");
            }            
            return true;
        }

        private bool UpdateMimeDb(string protocol)
        {
            string stdOut;
            string stdErr;
            try {
                var args = new List<string> { "set", "default-url-scheme-handler", protocol, $"{protocol}.desktop" };
                var result = RunCommand( xdgApp, args, out stdOut, out stdErr);
                if (!result)
                {
                    Serilog.Log.Error($"Failed to run {updateCacheCmd}");
                }
            }
            catch (Exception e)
            {
                Serilog.Log.Information($"Failed to run xdg-settings: {e.Message}");
            }
            try
            {
                var result = RunCommand(updateCacheCmd, new List<string> { GetPreferredConfigPath() }, out stdOut, out stdErr);
                if (!result)
                {
                    Serilog.Log.Error($"Failed to run {updateCacheCmd}");
                }

            }
            catch (Exception e)
            {
                    Serilog.Log.Error($"Failed to run {updateCacheCmd} ({e.Message}");

            }
            return true;
        }

        public bool Register(string protocol)
        {
            try {
                var version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                var registrationCommand = Constants.GetLaunchCommand("%1");
                Serilog.Log.Debug($"Registering to run {registrationCommand} for {protocol} URLs.");


                var path = GetAppPath(protocol);
                var directory = Path.GetDirectoryName(path);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                var lines = new List<string>{
                    { "[Desktop Entry]" },
                    {  "Encoding=UTF-8" },
                    {  $"Version={version}" },
                    {  "Type=Application" },
                    {  "Terminal=false" },
                    {  $"Exec={registrationCommand}" },
                    {  "Name=Scalus" },
                    {  "Comment=" },
                    { "Icon=" },
                    { "Categories=Application;Network" },
                    { $"MimeType=x-scheme-handler/{protocol}" }
                };
                File.WriteAllLines(path, lines);
                if (UpdateMimeDb(protocol))
                {
                    return true;
                }
                
                return false;
            }
            catch (Exception e)
            {
                Serilog.Log.Error($"Failed to register protocol:{protocol} ({e.Message})");
                return false;
            }
        }

        public bool Unregister(string protocol)
        {
            // Remove the protocol registration
            var registrationCommand = Constants.GetLaunchCommand("%1");
            Serilog.Log.Debug($"Unregistering  {registrationCommand} for {protocol} URLs.");
            try {
                var path = GetAppPath(protocol);
                var exe = GetRegisteredCommand(protocol);
                if (Regex.IsMatch(exe, registrationCommand))
                {
                    File.Delete(path);
                }
                return true;
            }
            catch (Exception e)
            {
                Serilog.Log.Error($"Failed to unregister protocol:{protocol} : ({e.Message})");
                return false;
            }
        }      
    }
}
