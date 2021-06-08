using scalus.Util;
using System;

namespace scalus
{
    class WindowsBasicProtocolRegistrar : IProtocolRegistrar
    {
        public bool UseSudo { get; set; }
        public bool RootMode { get; set; }
        public string Name { get; } = "WindowsProtocolRegistry";

        public bool IsScalusRegistered(string protocol)
        {
            var command = GetRegisteredCommand(protocol);
            
            if (string.IsNullOrEmpty(command)) return false;
            return command.Contains(Constants.GetBinaryName(), StringComparison.OrdinalIgnoreCase);
        }

        public string GetRegisteredCommand(string protocol)
        {
            var path = GetPathRoot(protocol);

            if (!RegistryUtils.PathExists(path)) return null;

            var commandPath = path + @"\shell\open\command";
            if (!RegistryUtils.PathExists(commandPath)) return null;

            return RegistryUtils.GetStringValue(commandPath, "");
        }

        public bool Register(string protocol)
        {
            var path = GetPathRoot(protocol);
            var registrationCommand = Constants.GetLaunchCommand("\"%1\"");
            Serilog.Log.Debug($"Registering to run {registrationCommand} for {protocol} URLs.");

            if (RegistryUtils.SetValue(path, "", $"SCALUS {protocol} Handler") &&
                RegistryUtils.SetValue(path, "URL Protocol", "") &&
                RegistryUtils.SetValue(path + "\\DefaultIcon", "", "%systemroot%\\system32\\mstsc.exe") &&
                RegistryUtils.SetValue(path + "\\shell\\open\\command", "", registrationCommand))
            {
                return true;
            }
            return false;
        }

        public bool Unregister(string protocol)
        {
            var path = GetPathRoot(protocol);
            if (RegistryUtils.PathExists(path) && !RegistryUtils.DeleteKey(path))
            {
                return false;
            }
            return true;
        }

        private string GetPathRoot(string protocol)
        {
            return $"HKEY_CURRENT_USER\\SOFTWARE\\Classes\\{protocol}";
        }
        public bool ReplaceRegistration(string protocol)
        {
            var res =Unregister(protocol);
            if (res)
            {
                res = Register(protocol);
            }
            return res;
        }

    }
}
