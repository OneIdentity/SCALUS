using Sulu.Util;
using System;

namespace Sulu
{
    class WindowsBasicProtocolRegistrar : IProtocolRegistrar
    {
        public bool IsSuluRegistered(string protocol)
        {
            var command = GetRegisteredCommand(protocol);
            if (string.IsNullOrEmpty(command)) return false;
            return command.Contains("sulu.exe", StringComparison.OrdinalIgnoreCase)
                || command.Contains("sulu.so", StringComparison.OrdinalIgnoreCase)
                || command.Contains("sulu.dll", StringComparison.OrdinalIgnoreCase);
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
            var registrationCommand = Constants.GetLaunchCommand("%1");
            Serilog.Log.Debug($"Registering to run {registrationCommand} for {protocol} URLs.");

            if (RegistryUtils.SetValue(path, "", $"Sulu {protocol} Handler") &&
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
    }
}
