// --------------------------------------------------------------------------------------------------------------------
// <copyright file="WindowsBasicProtocolRegistrar.cs" company="One Identity Inc.">
//   This software is licensed under the Apache 2.0 open source license.
//   https://github.com/OneIdentity/SCALUS/blob/master/LICENSE
//
//
//   Copyright One Identity LLC.
//   ALL RIGHTS RESERVED.
//
//   ONE IDENTITY LLC. MAKES NO REPRESENTATIONS OR
//   WARRANTIES ABOUT THE SUITABILITY OF THE SOFTWARE,
//   EITHER EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED
//   TO THE IMPLIED WARRANTIES OF MERCHANTABILITY,
//   FITNESS FOR A PARTICULAR PURPOSE, OR
//   NON-INFRINGEMENT.  ONE IDENTITY LLC. SHALL NOT BE
//   LIABLE FOR ANY DAMAGES SUFFERED BY LICENSEE
//   AS A RESULT OF USING, MODIFYING OR DISTRIBUTING
//   THIS SOFTWARE OR ITS DERIVATIVES.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace OneIdentity.Scalus
{
    using System;
    using System.Runtime.Versioning;
    using OneIdentity.Scalus.Platform;
    using OneIdentity.Scalus.Util;

    [SupportedOSPlatform("windows")]
    internal class WindowsBasicProtocolRegistrar : IProtocolRegistrar
    {
        public IOsServices OsServices { get; }

        public bool UseSudo { get; set; }

        public bool RootMode { get; set; }

        public string Name { get; } = "WindowsProtocolRegistry";

        public bool IsScalusRegistered(string protocol)
        {
            var command = GetRegisteredCommand(protocol);

            if (string.IsNullOrEmpty(command))
            {
                return false;
            }

            return command.Contains(Constants.GetBinaryName(), StringComparison.OrdinalIgnoreCase);
        }

        public string GetRegisteredCommand(string protocol)
        {
            var path = GetPathRoot(protocol);

            if (!RegistryUtils.PathExists(path))
            {
                return null;
            }

            var commandPath = path + @"\shell\open\command";
            if (!RegistryUtils.PathExists(commandPath))
            {
                return null;
            }

            return RegistryUtils.GetStringValue(commandPath, string.Empty);
        }

        public bool Register(string protocol)
        {
            var path = GetPathRoot(protocol);
            var registrationCommand = Constants.GetLaunchCommand("\"%1\"");
            Serilog.Log.Debug($"Registering to run {registrationCommand} for {protocol} URLs.");

            if (RegistryUtils.SetValue(path, string.Empty, $"SCALUS {protocol} Handler") &&
                RegistryUtils.SetValue(path, "URL Protocol", string.Empty) &&
                RegistryUtils.SetValue(path + "\\DefaultIcon", string.Empty, "%systemroot%\\system32\\mstsc.exe") &&
                RegistryUtils.SetValue(path + "\\shell\\open\\command", string.Empty, registrationCommand))
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

        public bool ReplaceRegistration(string protocol)
        {
            var res = Unregister(protocol);
            if (res)
            {
                res = Register(protocol);
            }

            return res;
        }

        private static string GetPathRoot(string protocol)
        {
            return $"HKEY_CURRENT_USER\\SOFTWARE\\Classes\\{protocol}";
        }
    }
}
