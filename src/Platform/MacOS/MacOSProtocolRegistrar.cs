using scalus.Platform;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace scalus
{
    public class MacOSProtocolRegistrar : IProtocolRegistrar
    {
        private readonly IOsServices _osServices;
        private static readonly string _scalusHandler = "com.oneidentity.scalus.macos";

        //user preferences are saved in "~Library/Preferences/com.apple.LaunchServices/com.apple.launchservices.secure.plist";

        private readonly string _lsRegisterCmd =
            "/System/Library/Frameworks/CoreServices.framework/Frameworks/LaunchServices.framework/Support/lsregister";

        private  readonly List<string> _lsRegisterArgs = new List<string> {"-kill", "-r", "-domain", "local", "-domain", "system", "-domain", "user"};

        private const string AppPath = "/Applications/scalus.app";
        private string _appPath;
        private string _appInfo;

        private string _appInfoPlist;
        public MacOSProtocolRegistrar(IOsServices osServices)
        {
            _osServices = osServices;
            string path;
            string err;
            var res = _osServices.Execute("/usr/bin/mdfind", new List<string>{$"kMDItemCFBundleIdentifier='{_scalusHandler}'" }, out path, out err);
            path = Regex.Replace(path, "[\r\n ]", "", RegexOptions.Singleline);
            if (res != 0 || string.IsNullOrEmpty(path))
            {
                Serilog.Log.Warning($"Handler:{_scalusHandler} is not installed");
                _appPath = AppPath;
            }
            if (!Directory.Exists(path))
            {
                Serilog.Log.Warning($"Handler path:{path} does not exist or is not accessible - using default path");
                _appPath = AppPath;
            }
            else
            {
                _appPath = path;
            }
            _appInfo = $"{_appPath}/Contents/Info";
            _appInfoPlist = $"{_appInfo}.plist";
            Serilog.Log.Information($"Using configured app from path: {_appPath}");
        }
        private bool RunCommand(string cmd, List<string> args,  out string output)
        {
            try {
                string err;
                var res = _osServices.Execute(cmd, args, out output, out err);
                if (res == 0)
                {
                    return true;
                }
                if (!string.IsNullOrEmpty(err))
                {
                    output = $"{output}\n{err}";
                }
                return res == 0;
            }
            catch (Exception e)
            {
                output = e.Message;
                return false;
            }
        }
        
        private bool UpdateConfiguredDefault(string protocol, bool add = true)
        {
            var handler = add ? _scalusHandler : string.Empty;
            string output;
            var res= RunCommand( "python", 
                new List<string> {
                    "-c",
                    $"from LaunchServices import LSSetDefaultHandlerForURLScheme; LSSetDefaultHandlerForURLScheme(\"{protocol}\", \"{handler}\")"}, 
                out output);
            if (!res)
            {
                Serilog.Log.Error($"Failed to update the configured default:{output}");
            }
            return res;
        }
        //get the current configured default 
        public string GetRegisteredCommand(string protocol)
        {
            var res =
                RunCommand("python",
                    new List<string> { "-c", $"from LaunchServices import LSCopyDefaultHandlerForURLScheme; print LSCopyDefaultHandlerForURLScheme('{protocol}')" },
                    out string output);
            if (!res)
            {
                Serilog.Log.Warning($"Failed to get the default protocol handler for:{protocol}: {output}");
                return string.Empty;
            }
            return Regex.IsMatch(output, "none", RegexOptions.IgnoreCase) ? string.Empty : output;
        }

        public bool IsScalusRegistered(string protocol)
        {
            var handler = GetRegisteredCommand(protocol);
            var res = Regex.IsMatch(handler, _scalusHandler, RegexOptions.IgnoreCase);
            Serilog.Log.Information($"{protocol} registered:{res}");
            return res;
        }

        public bool Unregister(string protocol, bool userMode = false, bool useSudo= false)
        {
            var res1=true;
            var res2 = true;
            
            if (!userMode)
            {
                res2=DeRegisterProtocol(protocol, useSudo);
            }
            if (IsScalusRegistered(protocol))
            {
                res1=UpdateConfiguredDefault(protocol, false);
            }
            return res1 && res2;
        }

        public static List<string> ParseList(string str)
        {
            var list = new List<string>();
            var stripped = Regex.Replace(str, "[\\n\\r(]", "", RegexOptions.Singleline);
            var match = Regex.Match(stripped, 
                $"CFBundleURLSchemes\\s*=\\s*([^)]+)",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (match.Success)
            {
                var schemes = match.Groups[1].Value;
                schemes = Regex.Replace(schemes, "\\s+", "");
                list = schemes.Split(",", StringSplitOptions.RemoveEmptyEntries).ToList();
            }

            return list;
        }

        public static string ConstructNewValue(List<string> list)
        {
            var newvalue = new StringBuilder(
                    $"'<array><dict><key>CFBundleURLName</key><string>{_scalusHandler}</string><key>CFBundleURLSchemes</key><array>");
            foreach (var prot in list)
            {
                newvalue.Append($"<string>{prot}</string>");
            }
            newvalue.Append("</array> </dict> </array>'");
            return newvalue.ToString();
        }

        private List<string> GetCurrentRegistrations(bool useSudo)
        {
            string output;
            if (!File.Exists(_appInfoPlist) && !useSudo)
            {
                throw new Exception($"scalus application file :{_appInfoPlist} does not exist or is inaccessible");
            }

            var cmd = "defaults";
            var args = new List<string>( );
            if (useSudo)
            { 
                cmd = "sudo";
                args = new List<string>{"defaults"};
            }
            args.AddRange(new List<string>{"read", _appInfo, "CFBundleURLTypes"});
            var res = RunCommand (cmd, args, out output);
            if (!res )
            {
                Serilog.Log.Warning($"Failed to get the current registrations:{output}");
                return new List<string>();
            }
            return ParseList(output);
        }


        private bool WriteNewDefaults(string path, string key, string value, bool useSudo)
        {
            var cmd = "/bin/sh"; 
            var args = new List<string>( );
            if (useSudo)
            { 
                //try using sudo
                cmd = "sudo";
                args.Add("/bin/sh" );

            }
            args.AddRange(new List<string>{$"-c", $"defaults write {path} {key} {value}" });
            string output;
            var res = RunCommand (cmd, args, out output);

            if (!res)
            {
                Serilog.Log.Warning($"Failed to update {key} value in {path}:{output}");
            }
            return res;
        }

        private bool UpdateRegistration(List<string> newlist, bool useSudo)
        {
            var newvalue = ConstructNewValue(newlist);
            if (!File.Exists(_appInfoPlist))
            {
                throw new Exception($"scalus application file:{_appInfoPlist} is not installed");
            }

            var res = WriteNewDefaults(_appInfo, "CFBundleURLTypes", newvalue, useSudo);
            if (!res)
            {
                return false;
            }
            string output;
            res = RunCommand(_lsRegisterCmd, _lsRegisterArgs, out output);
            if (!res)
            {
                Serilog.Log.Warning($"Failed to update the registration database:{output}");
                return false;
            }
            return res;
        }



        private bool DeRegisterProtocol(string protocol, bool useSudo = false)
        {
            var list = GetCurrentRegistrations(useSudo);
            if (!list.Contains(protocol, StringComparer.OrdinalIgnoreCase))
            {
                return true;
            }

            list.Remove(protocol);
            return UpdateRegistration(list, useSudo);
        }

        private bool RegisterProtocol(string protocol, bool useSudo)
        {
            var list = GetCurrentRegistrations(useSudo);
            if (!list.Contains(protocol, StringComparer.OrdinalIgnoreCase))
            {
                list.Add(protocol);
            }
            return UpdateRegistration(list, useSudo);
        }

        public bool Register(string protocol, bool userMode = false, bool useSudo= false)
        {
            var res1 = true;
            if (!userMode)
            {
                res1 = RegisterProtocol(protocol, useSudo);
            }
            var res2 = UpdateConfiguredDefault(protocol);
            return res1 && res2;
        }   
        public bool ReplaceRegistration(string protocol, bool userMode = false, bool useSudo = false)
        {
            return Register(protocol, userMode, useSudo);
        }
    }
}
