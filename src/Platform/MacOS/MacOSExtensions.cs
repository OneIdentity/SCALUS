using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using scalus.Platform;

namespace scalus
{
    public static class MacOsExtensions 
    {
        public static readonly string ScalusHandler = "com.oneidentity.scalus.macos";
        private static readonly string _lsRegisterCmd =
            "/System/Library/Frameworks/CoreServices.framework/Frameworks/LaunchServices.framework/Support/lsregister";

        public static bool RunCommand(this IProtocolRegistrar registrar, string cmd, List<string> args,  out string output)
        {
            try {
                string err;
                var res = registrar.OsServices.Execute(cmd, args, out output, out err);
                if (res == 0)
                {
                    return true;
                }
                if (!string.IsNullOrEmpty(err))
                {
                    output = $"{output}\n{err}";
                }
                return false;
            }
            catch (Exception e)
            {
                output = e.Message;
                return false;
            }
        }

        public static string GetAppPath(this IProtocolRegistrar registrar)
        {
            return GetAppPath(registrar.OsServices);
        }

        public static string GetAppPath(IOsServices osServices)
        {
            string path;
            var apath = string.Empty;
            var res = osServices.Execute("/usr/bin/mdfind",
                new List<string> {$"kMDItemCFBundleIdentifier='{ScalusHandler}'"}, out path, out _);
            if (res == 0)
            {
                apath = path?.Split('\r', '\n', StringSplitOptions.RemoveEmptyEntries).ToList().FirstOrDefault();
                apath = apath?.Trim('\r', '\n');
                Serilog.Log.Information($"split: [{apath}]");
            }
            Serilog.Log.Information($"check found app path:[{apath}]");

            if (!string.IsNullOrEmpty(apath) && Directory.Exists(apath) && Directory.Exists($"{apath}/Contents"))
            {
                return apath;
            }
            if (string.IsNullOrEmpty(apath))
                apath = Constants.GetBinaryPath();

            Serilog.Log.Warning($"Handler path cannot be determined - running from {apath}");
            //throw new Exception($"Handler path cannot be determined");
            return apath;
        }

        public static bool Refresh(this IProtocolRegistrar registrar)
        {
            string output;
            var home = Environment.GetEnvironmentVariable("HOME");
            var args = new List<string> {"-c", $"HOME=\"{home}\"; export HOME; {_lsRegisterCmd} -kill -v -f {registrar.GetAppPath()}"};
            var res = registrar.RunCommand("/bin/sh", args, out output);
            if (!res)
            {
                Serilog.Log.Warning($"Failed to update the Launch Services database:{output}");
                return false;
            }
            return true;
        }
    }
}
