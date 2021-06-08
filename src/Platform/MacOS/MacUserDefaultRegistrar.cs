using System;
using scalus.Platform;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace scalus
{
    public class MacOsUserDefaultRegistrar : IProtocolRegistrar
    {
        public bool UseSudo { get; set; }
        public bool RootMode { get; set; }
        public string Name { get; } = "MacOsUserDefault";
        public IOsServices OsServices { get; }

        private const string _prefs =
            "Library/Preferences/com.apple.LaunchServices/com.apple.launchservices.secure.plist";

        private string _prefsPath;
        //user preferences are saved in "~Library/Preferences/com.apple.LaunchServices/com.apple.launchservices.secure.plist";

        public MacOsUserDefaultRegistrar(IOsServices osServices)
        {
            OsServices = osServices;
            _prefsPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}/{_prefs}";
        }
        
        private bool UpdateConfiguredDefault(string protocol, bool add = true)
        {
            var handler = add ? MacOsExtensions.ScalusHandler : string.Empty;
            var res= this.RunCommand( "python", 
                new List<string> {
                    "-c",
                    $"from LaunchServices import LSSetDefaultHandlerForURLScheme; LSSetDefaultHandlerForURLScheme(\"{protocol}\", \"{handler}\")"}, 
                out var output);
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
                this.RunCommand("python",
                    new List<string> { "-c", $"from LaunchServices import LSCopyDefaultHandlerForURLScheme; print LSCopyDefaultHandlerForURLScheme('{protocol}')" },
                    out string output);
            if (!res)
            {
                Serilog.Log.Warning($"Failed to get the default protocol handler for:{protocol}: {output}");
                return string.Empty;
            }
            output = Regex.Replace(output, @"\t|\n|\r", "");
            Serilog.Log.Information($"Registered command is :{output}.");
            return Regex.IsMatch(output, "none", RegexOptions.IgnoreCase) ? string.Empty : output;
        }

        public bool IsScalusRegistered(string protocol)
        {
            var handler = GetRegisteredCommand(protocol);
            var res = Regex.IsMatch(handler, MacOsExtensions.ScalusHandler, RegexOptions.IgnoreCase);
            Serilog.Log.Information($"{protocol} reporting as default handler:{res}");
            //is it really registered or just cached?
            return res;
        }

        public bool Unregister(string protocol)
        {
            if (!IsScalusRegistered(protocol))
            {
                return true;
            }
            return UpdateConfiguredDefault(protocol, false);
        }


        public bool Register(string protocol)
        {
            return UpdateConfiguredDefault(protocol);
        }   
        public bool ReplaceRegistration(string protocol)
        {
            return Register(protocol);
        }
    }
}
