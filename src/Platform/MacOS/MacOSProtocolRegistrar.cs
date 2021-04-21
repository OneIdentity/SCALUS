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

        //private readonly string _userPath = "Library/Preferences/com.apple.LaunchServices/com.apple.launchservices.secure";

        private readonly string _lsRegisterCmd =
            "/System/Library/Frameworks/CoreServices.framework/Frameworks/LaunchServices.framework/Support/lsregister";

        private  readonly List<string> _lsRegisterArgs = new List<string> {"-kill", "-r", "-domain", "local", "-domain", "system", "-domain", "user"};


        private static readonly string _appInfo = "/Applications/scalus.app/Contents/Info";

        private readonly string _appInfoPlist = $"{_appInfo}.plist";
        public MacOSProtocolRegistrar(IOsServices osServices)
        {
            _osServices = osServices;
        }

        public bool RunCommand(string cmd, List<string> args, out string output)
        {
            Serilog.Log.Information($"DEBUG: running:{cmd} args:{string.Join(',', args)}");
            var process = _osServices.Execute(cmd, args, out output, out string err);
            process.WaitForExit();
            Serilog.Log.Information($"DEBUG: exit:{process.ExitCode}, output:{output}, err:{err}");
            if (process.HasExited)
            {
                if (process.ExitCode != 0)
                {
                    Serilog.Log.Error($"Failed to run command:{err}");
                    output = err;
                }

                return process.ExitCode == 0;
            }
            Serilog.Log.Error($"Command has not finished:{err}");
            return false;
        }

       

        private bool UpdateConfiguredDefault(string protocol, bool add)
        {
            var handler = add ? _scalusHandler : "none";
            var res= RunCommand( "python", 
                new List<string> {
                    "-c",
                    $"from LaunchServices import LSSetDefaultHandlerForURLScheme; LSSetDefaultHandlerForURLScheme(\"{protocol}\", \"{handler}\")"}, 
                out _);

            return res;
        }

        public string GetRegisteredCommand(string protocol)
        {
            string output;
            var res =
                RunCommand( "python", 
                    new List<string>{ "-c", $"from LaunchServices import LSCopyDefaultHandlerForURLScheme; print LSCopyDefaultHandlerForURLScheme('{protocol}')"}, 
                    out output);
            if (!res || Regex.IsMatch(output, "none", RegexOptions.IgnoreCase))
            {
                return string.Empty;
            }
            return output;
        }
        
        public bool IsScalusRegistered(string protocol)
        {
            var handler = GetRegisteredCommand(protocol);
            return Regex.IsMatch(handler, _scalusHandler, RegexOptions.IgnoreCase);
        }

        public bool Unregister(string protocol)
        {
            //TODO
            if (IsScalusRegistered(protocol))
            {
                UpdateConfiguredDefault(protocol, false);
            }
            return DeRegisterProtocol(protocol);
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

        private List<string> GetCurrentRegistrations()
        {
            string output;
            if (!File.Exists(_appInfoPlist))
            {
                throw new Exception($"scalus application is not installed");
            }

            var readable = false;
            try
            {
                using (var fs = new FileStream(_appInfoPlist, FileMode.Open))
                {
                    readable = fs.CanRead;
                }
            } catch {}

            var cmd = "defaults";
            var args = new List<string>( );
            if (!readable)
            { 
                //try using sudo
                cmd = "sudo";
                args = new List<string>
                    {"defaults"};
            }
            args.AddRange(new List<string>{"read", _appInfo, "CFBundleURLTypes"});

            var res = RunCommand(cmd, args, out output);
            if (!res)
            {
                return new List<string>();
            }
            return ParseList(output);
        }

        private bool UpdateRegistration(List<string> newlist)
        {
            var newvalue = ConstructNewValue(newlist);
            if (!File.Exists(_appInfoPlist))
            {
                throw new Exception($"scalus application is not installed");
            }

            var writeable = false;
            try
            {
                using (var fs = new FileStream(_appInfoPlist, FileMode.Open))
                {
                    writeable = fs.CanWrite;
                }

            }
            catch
            {
            }

            var cmd = "defaults"; 
            var args = new List<string>( );
            if (!writeable)
            { 
                //try using sudo
                cmd = "sudo";
                args = new List<string>
                    { "defaults"};
            }
            args.AddRange(new List<string>{"write", _appInfo, "CFBundleURLTypes", "-array", newvalue});

            string output;
            var res = RunCommand(cmd, args, out output);
            if (res)
            {
                RunCommand(_lsRegisterCmd, _lsRegisterArgs, out _);
            }
            return res;
        }



        private bool DeRegisterProtocol(string protocol)
        {
            var list = GetCurrentRegistrations();
            if (!list.Contains(protocol, StringComparer.OrdinalIgnoreCase))
            {
                return true;
            }

            list.Remove(protocol);
            return UpdateRegistration(list);
        }

        private bool RegisterProtocol(string protocol)
        {
            var list = GetCurrentRegistrations();
            if (!list.Contains(protocol, StringComparer.OrdinalIgnoreCase))
            {
                list.Add(protocol);
            }
            return UpdateRegistration(list);
        }

        public bool Register(string protocol)
        {
            if (!RegisterProtocol(protocol))
            {
                return false;
            }

            var res = RunCommand( "python", 
                new List<string> {
                    "-c",
                $"from LaunchServices import LSSetDefaultHandlerForURLScheme; LSSetDefaultHandlerForURLScheme(\"{protocol}\", \"{_scalusHandler}\")"}, 
                out _);

            return res;
        }
    }
}
