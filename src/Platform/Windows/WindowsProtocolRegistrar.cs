using scalus.Util;
using System.Linq;

namespace scalus
{
    class ProtocolRegistrar : IProtocolRegistrar
    {
        public bool UseSudo { get; set; }
        public bool RootMode { get; set; }
        public string Name { get; } = "WindowsURLRegistry";
        private static readonly string AppName = "SCALUS Protocol Handler";
        private static readonly string Clsid = "scalus.URLHandler.1";
        private static readonly string AppId = "SCALUS";
        private static readonly string AppCapabilitiesFragment = $"Software\\{AppId}\\Capabilities";
        private static readonly string CapabilitiesFragment = @"\Capabilities";
        private static readonly string CapabilitiesUrlAssociationsFragment = @$"{CapabilitiesFragment}\UrlAssociations";


        public bool IsScalusRegistered(string protocol)
        {
            return RegistryUtils.GetStringValue(GetAppPath() + CapabilitiesUrlAssociationsFragment, protocol) == Clsid;
        }

        public string GetRegisteredCommand(string protocol)
        {
            return RegistryUtils.GetStringValue(GetAppPath() + CapabilitiesUrlAssociationsFragment, protocol);
        }

        public bool Register(string protocol)
        {
            var registrationCommand = Constants.GetLaunchCommand("\"%1\"");
            Serilog.Log.Debug($"Registering to run {registrationCommand} for {protocol} URLs.");

            if(!RegisterClassId(registrationCommand))
            {
                Serilog.Log.Debug("Failed to register classid");
                return false;
            }

            if(!RegisterCapabilities(protocol))
            {
                Serilog.Log.Debug("Failed to register capabilities");
                return false;
            }

            if(!RegistryUtils.SetValue(GetRegisteredApplicationsPath(), AppName, AppCapabilitiesFragment))
            {
                Serilog.Log.Debug("Failed to register app capabilities");
                return false;
            }

            if(!RegistryUtils.SetValue(GetAppAssociationToastsPath(), $"{Clsid}_{protocol}", 0, Microsoft.Win32.RegistryValueKind.DWord))
            {
                Serilog.Log.Debug("Failed to register app association toasts");
                return false;
            }

            return true;
        }

        public bool Unregister(string protocol)
        {
            // Remove the protocol registration
            var urlAssociationsPath = GetAppPath() + CapabilitiesUrlAssociationsFragment;
            RegistryUtils.DeleteValue(urlAssociationsPath, protocol);

            // If there are more registrations, just return
            if (RegistryUtils.GetValueNames(urlAssociationsPath).Any()) return true;

            RegistryUtils.DeleteValue(GetAppAssociationToastsPath(), $"{Clsid}_{protocol}");
            RegistryUtils.DeleteValue(GetRegisteredApplicationsPath(), AppName);

            foreach (var path in new [] { GetAppPath(), GetClassRegistrationPath()})
            {
                if (RegistryUtils.PathExists(path) && !RegistryUtils.DeleteKey(path))
                {
                    Serilog.Log.Debug($"Failed to remove registry path: {path}");
                    return false;
                }
            }
            return true;
        }

        private bool RegisterClassId(string registrationCommand)
        {
            var path = GetClassRegistrationPath();
            if (RegistryUtils.GetKey(path) != null) return true;

            return RegistryUtils.SetValue(path, "", AppName) &&
                   RegistryUtils.SetValue(path, "URL Protocol", "") &&
                   RegistryUtils.SetValue(path + "\\shell\\open\\command", "", registrationCommand);
        }

        private bool RegisterCapabilities(string protocol)
        {
            var path = GetAppPath();
            return RegistryUtils.SetValue(path + CapabilitiesFragment, "ApplicationDescription", AppName)
                && RegistryUtils.SetValue(path + CapabilitiesFragment, "ApplicationName", AppId)
                && RegistryUtils.SetValue(path + CapabilitiesUrlAssociationsFragment, protocol, Clsid);
        }

        private string GetClassRegistrationPath()
        {
            return $"HKEY_CURRENT_USER\\SOFTWARE\\Classes\\{Clsid}";
        }

        private string GetAppPath()
        {
            return $"HKEY_CURRENT_USER\\SOFTWARE\\{AppId}";
        }

        private string GetRegisteredApplicationsPath()
        {
            return @"HKEY_CURRENT_USER\SOFTWARE\RegisteredApplications";
        }

        private string GetAppAssociationToastsPath()
        {
            return @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\ApplicationAssociationToasts";
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
