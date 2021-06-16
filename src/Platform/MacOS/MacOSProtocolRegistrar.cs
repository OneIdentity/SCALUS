//#define LocalOnly
using scalus.Platform;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;


namespace scalus
{
    public class MacOsProtocolRegistrar : IProtocolRegistrar
    {
        public bool UseSudo { get; set; }
        public bool RootMode { get; set; }
        public string Name { get; } = "MacOsAppRegistration";
        public IOsServices OsServices { get; }
        private readonly string _appPath;
        private readonly string _appInfo;
        private readonly string _appInfoPlist;

        private List<string> _handledUrlList;
        #if LocalOnly
        #else
        private List<string> RegisteredProtocols = new List<string> {"rdp", "ssh", "telnet"};
        #endif
        public MacOsProtocolRegistrar(IOsServices osServices)
        {
            OsServices = osServices;
            _appPath = this.GetAppPath();
            _appInfo = $"{_appPath}/Contents/Info";
            _appInfoPlist = $"{_appInfo}.plist";
#if LocalOnly
#else
            _handledUrlList = RegisteredProtocols;
#endif
        }

        public string GetRegisteredCommand(string protocol)
        {
                
            var list = GetCurrentRegistrations();
            if (list.Contains(protocol))
            {
                return MacOsExtensions.ScalusHandler;
            }
            return string.Empty;
        }

       
        public bool IsScalusRegistered(string protocol)
        {
            var handler = GetRegisteredCommand(protocol);
            return (!string.IsNullOrEmpty(handler));
        }

        public bool Unregister(string protocol)
        {
#if LocalOnly

            var list = GetCurrentRegistrations();
            if (!list.Contains(protocol, StringComparer.OrdinalIgnoreCase))
            {
                return true;
            }

            list.Remove(protocol);
            return UpdateRegistration(list, false);
#else
            Serilog.Log.Information($"Cannot remove the predefined protocol from this registrar");
            return true;
#endif
        }

        public bool Register(string protocol)
        {
            var list = GetCurrentRegistrations();
            if (!list.Contains(protocol, StringComparer.OrdinalIgnoreCase))
            {
                list.Add(protocol);
            }
            return UpdateRegistration(list, true);
        }


        public bool ReplaceRegistration(string protocol)
        {
            return Register(protocol);
        }

        //check which URLs the application Info.plist file handles
        private List<string> GetCurrentRegistrations()
        {

            if (_handledUrlList != null)
                return _handledUrlList;

            string output;
            if (!File.Exists(_appInfoPlist) && !UseSudo)
            {
                throw new Exception($"scalus application file :{_appInfoPlist} does not exist or is inaccessible");
            }

            var cmd = "defaults";
            var args = new List<string>( );
            if (UseSudo)
            { 
                cmd = "sudo";
                args = new List<string>{"defaults"};
            }
            args.AddRange(new List<string>{"read", _appInfo, "CFBundleURLTypes"});
            var res =this.RunCommand (cmd, args, out output);
            if (!res )
            {
                Serilog.Log.Warning($"Failed to get the current registrations:{output}");
                return new List<string>();
            }
            _handledUrlList = ParseList(output);
            Serilog.Log.Information($"scalus is registered to handle URLs:{string.Join(',', _handledUrlList)}");
            return _handledUrlList;
        }

        //update application Info.plist (can only do this if testing locally)
        private bool UpdateRegistration(List<string> newlist, bool add)
        {
#if LocalOnly
            var newvalue = ConstructNewValue(newlist);
            if (!File.Exists(_appInfoPlist))
            {
                throw new Exception($"scalus application file:{_appInfoPlist} is not installed");
            }

            Serilog.Log.Information($"Updating {_appInfo} file to add:{newvalue}");
            var res = WriteNewDefaults(_appInfo, "CFBundleURLTypes", newvalue);
            if (!res)
            {
                Serilog.Log.Information($"Failed to update {_appInfo}");
                return false;
            }
            return this.Refresh(add);
#else
            Serilog.Log.Warning($"This registrar does not support adding or removing protocols. The following protocols are registered:{string.Join(',', RegisteredProtocols)}");
            return false;
#endif
        }

        public static string ConstructNewValue(List<string> list)
        {
            var newvalue = new StringBuilder(
                $"'<array><dict><key>CFBundleURLName</key><string>{MacOsExtensions.ScalusHandler}</string><key>CFBundleURLSchemes</key><array>");
            foreach (var prot in list)
            {
                newvalue.Append($"<string>{prot}</string>");
            }
            newvalue.Append("</array> </dict> </array>'");
            return newvalue.ToString();
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

        private bool WriteNewDefaults(string path, string key, string value)
        {
            var cmd = "/bin/sh"; 
            var args = new List<string>( );
            if (UseSudo)
            { 
                //try using sudo
                cmd = "sudo";
                args.Add("/bin/sh" );

            }
            args.AddRange(new List<string>{$"-c", $"defaults write {path} {key} {value}" });
            string output;
            var res = this.RunCommand (cmd, args, out output);

            if (!res)
            {
                Serilog.Log.Warning($"Failed to update {key} value in {path}:{output}");
            }
            return res;
        }
    }
}
