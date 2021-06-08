using scalus.Platform;
using System;
using System.Collections.Generic;

namespace scalus
{
    public static class MacOsExtensions 
    {
        public static readonly string ScalusHandler = "com.oneidentity.scalus.macos";

        public static bool RunCommand(this MacOsProtocolRegistrar registrar, string cmd, List<string> args,
            out string output)
        {
            return RunCommand(registrar.OsServices, cmd, args, out output);
        }
        public static bool RunCommand(this MacOsUserDefaultRegistrar registrar, string cmd, List<string> args,
            out string output)
        {
            return RunCommand(registrar.OsServices, cmd, args, out output);
        }
        public static bool RunCommand(IOsServices osServices, string cmd, List<string> args,  out string output)
        {
            try {
                string err;
                var res = osServices.Execute(cmd, args, out output, out err);
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
    }
}
