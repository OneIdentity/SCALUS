// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MacOSProtocolRegistrar.cs" company="One Identity Inc.">
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
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using OneIdentity.Scalus.Platform;

    public class MacOSProtocolRegistrar : IProtocolRegistrar
    {
        private readonly string appPath;
        private readonly string appInfo;
        private readonly string appInfoPlist;

        private List<string> handledUrlList;

#if LocalOnly
#else
        private List<string> registeredProtocols = new List<string> { "rdp", "ssh", "telnet" };

#endif
        public MacOSProtocolRegistrar(IOsServices osServices)
        {
            OsServices = osServices;
            appPath = this.GetAppPath();
            appInfo = $"{appPath}/Contents/Info";
            appInfoPlist = $"{appInfo}.plist";
#if LocalOnly
#else
            handledUrlList = registeredProtocols;
#endif
        }

        public bool UseSudo { get; set; }

        public bool RootMode { get; set; }

        public string Name { get; } = "MacOsAppRegistration";

        public IOsServices OsServices { get; }

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
            return !string.IsNullOrEmpty(handler);
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
            var pattern = "[\\n\\r(]";
            var stripped = Regex.Replace(str, pattern, string.Empty, RegexOptions.Singleline);
            var matchPattern = $"CFBundleURLSchemes\\s*=\\s*([^)]+)";
            var match = Regex.Match(stripped,
               matchPattern,
               RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (match.Success)
            {
                var schemes = match.Groups[1].Value;
                var patternSchemes = "\\s+";
                schemes = Regex.Replace(schemes, patternSchemes, string.Empty);
                list = schemes.Split(",", StringSplitOptions.RemoveEmptyEntries).ToList();
            }

            return list;
        }

        // check which URLs the application Info.plist file handles
        private List<string> GetCurrentRegistrations()
        {
            if (handledUrlList != null)
            {
                return handledUrlList;
            }

            string output;
            if (!File.Exists(appInfoPlist) && !UseSudo)
            {
                throw new RegistrarException($"scalus application file :{appInfoPlist} does not exist or is inaccessible");
            }

            var cmd = "defaults";
            var args = new List<string>();
            if (UseSudo)
            {
                cmd = "sudo";
                args = new List<string> { "defaults" };
            }

            args.AddRange(new List<string> { "read", appInfo, "CFBundleURLTypes" });
            var res = this.RunCommand(cmd, args, out output);
            if (!res)
            {
                Serilog.Log.Warning($"Failed to get the current registrations:{output}");
                return new List<string>();
            }

            handledUrlList = ParseList(output);
            Serilog.Log.Information($"scalus is registered to handle URLs:{string.Join(',', handledUrlList)}");
            return handledUrlList;
        }

        // update application Info.plist (can only do this if testing locally)
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
            Serilog.Log.Warning($"This registrar does not support adding or removing protocols. The following protocols are registered:{string.Join(',', registeredProtocols)}");
            return false;
#endif
        }
    }
}
