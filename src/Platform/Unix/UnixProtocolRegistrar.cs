﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UnixProtocolRegistrar.cs" company="One Identity Inc.">
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
    using System.Reflection;
    using System.Text;
    using System.Text.RegularExpressions;
    using OneIdentity.Scalus.Platform;

    public class UnixProtocolRegistrar : IProtocolRegistrar
    {
        private const string XdgConfigPath = ".config";
        private const string XdgSettings = "/usr/bin/xdg-settings";
        private const string XdgMime = "/usr/bin/xdg-mime";
        private const string UpdateDesktopDatabase = "/usr/bin/update-desktop-database";
        private const string ScalusDesktop = "scalus.desktop";
        private const string AppRelPath = ".local/share/applications";
        private const string MimeType = "MimeType";
        private const string SchemeHandler = "x-scheme-handler.";
        private const string Desktop = ".desktop";

        private string preferredConfigPath;
        private string appDataPath;

        public UnixProtocolRegistrar(IOsServices osServices)
        {
            OsServices = osServices;
        }

        public bool UseSudo { get; set; }

        public bool RootMode { get; set; }

        public string Name { get; } = "Linux";

        public IOsServices OsServices { get; }

        public string AppDataPath
        {
            get
            {
                if (appDataPath == null)
                {
                    appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), AppRelPath);
                }

                return appDataPath;
            }
        }

        public string PreferredConfigPath
        {
            get
            {
                if (preferredConfigPath == null)
                {
                    preferredConfigPath = GetPreferredConfigPath();
                }

                return preferredConfigPath;
            }
        }

        public List<string> GetRegisteredProtocolsFromScalusDesktop()
        {
            var mimeList = new List<string>();
            var path = Path.Combine(AppDataPath, ScalusDesktop);
            if (!File.Exists(path))
            {
                Serilog.Log.Information($"Scalus is not registered");
                return mimeList;
            }

            var lines = File.ReadAllLines(path);
            foreach (var line in lines)
            {
                var match = Regex.Match(line, $"^\\s*{MimeType}\\s*=\\s*(.*)", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var mlist = match.Groups[1].Value?.Split(';');
                    foreach (var one in mlist)
                    {
                        var prot = string.Empty;
                        if (!string.IsNullOrEmpty(one))
                        {
                            match = Regex.Match(one, $"{SchemeHandler}(\\S+)");
                            if (match.Success)
                            {
                                prot = match.Groups[1].Value;
                            }
                        }

                        if (!string.IsNullOrEmpty(prot))
                        {
                            mimeList.Add(prot);
                        }
                    }
                }
            }

            Serilog.Log.Information($"{mimeList.Count} protocols registered for scalus:{string.Join(',', mimeList)}");
            return mimeList;
        }

        public string GetDefaultHandlerForProtocol(string protocol)
        {
            var handler = string.Empty;
            if (File.Exists(PreferredConfigPath))
            {
                var lines = File.ReadAllLines(PreferredConfigPath);
                foreach (var line in lines)
                {
                    var match = Regex.Match(line, $"^\\s*{SchemeHandler}{protocol}\\s*=\\s*(\\S*){Desktop}", RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        handler = match.Groups[1].Value;
                        break;
                    }
                }
            }

            Serilog.Log.Information($"Default handler for protocol:{protocol} is {handler}");
            return handler;
        }

        public string RemoveDefaultHandlerForProtocol(string protocol)
        {
            if (!File.Exists(PreferredConfigPath))
            {
                return string.Empty;
            }

            var lines = File.ReadAllLines(PreferredConfigPath);
            var newLines = new List<string>();
            foreach (var line in lines)
            {
                var match = Regex.Match(line, $"^\\s*{SchemeHandler}{protocol}\\s*=\\s*(\\S*){Desktop}", RegexOptions.IgnoreCase);
                if (!match.Success)
                {
                    newLines.Add(line);
                }
            }

            File.WriteAllLines(PreferredConfigPath, newLines);
            return string.Empty;
        }

        public bool IsScalusRegistered(string protocol)
        {
            var mimeList = GetRegisteredProtocolsFromScalusDesktop();
            return mimeList.Contains(protocol);
        }

        public string GetRegisteredCommand(string protocol)
        {
            if (File.Exists(XdgSettings))
            {
                var args = new List<string> { "get", "default-url-scheme-handler", protocol };
                var exitCode = OsServices.Execute(XdgSettings, args, out string stdOut, out string stdErr);
                if (exitCode == 0)
                {
                    return stdOut;
                }
            }

            return GetDefaultHandlerForProtocol(protocol);
        }

        public bool Register(string protocol)
        {
            try
            {
                Serilog.Log.Debug($"Registering {ScalusDesktop} for {protocol} URLs.");
                if (!Directory.Exists(AppDataPath))
                {
                    Directory.CreateDirectory(AppDataPath);
                }

                var currentList = GetRegisteredProtocolsFromScalusDesktop();
                if (currentList.Contains(protocol))
                {
                    Serilog.Log.Information($"scalus is already registered handler for protocol:{protocol}");
                }
                else
                {
                    currentList.Add(protocol);
                    WriteScalusDesktopFile(currentList);
                }

                UpdateDefaultHandler(protocol);
                UpdateDesktopDb();

                return true;
            }
            catch (Exception e)
            {
                Serilog.Log.Error(e, $"Failed to register {ScalusDesktop} for protocol:{protocol}");
                return false;
            }
        }

        public bool Unregister(string protocol)
        {
            Serilog.Log.Debug($"Unregistering {ScalusDesktop} for {protocol} URLs.");
            try
            {
                // Remove the protocol registration
                var currentList = GetRegisteredProtocolsFromScalusDesktop();
                if (!currentList.Contains(protocol))
                {
                    Serilog.Log.Information($"no change required");
                    if (currentList.Count == 0)
                    {
                        if (File.Exists(ScalusDesktop))
                        {
                            File.Delete(ScalusDesktop);
                        }
                    }
                }

                currentList.Remove(protocol);
                WriteScalusDesktopFile(currentList);
                RemoveDefaultHandlerForProtocol(protocol);
                UpdateDesktopDb();
                return true;
            }
            catch (Exception e)
            {
                Serilog.Log.Error(e, $"Failed to unregister {ScalusDesktop} for protocol: {protocol}");
                return false;
            }
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

        private string GetPreferredConfigPath()
        {
            string filepath;
            var args = new List<string> { "-c", $"XDG_UTILS_DEBUG_LEVEL=2 {XdgSettings} get default-url-scheme-handler" };
            var exitCode = OsServices.Execute("sh", args, out var stdOut, out _);
            if (exitCode == 0)
            {
                var lines = stdOut.Split("\n");
                if (lines.Any())
                {
                    var path = lines[0];
                    var match = Regex.Match(path, "\\s*Checking\\s*(\\S+)", RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        filepath = match.Groups[1].Value;
                        if (Path.IsPathFullyQualified(filepath))
                        {
                            Serilog.Log.Information($"preferred config Path is {filepath}");
                            return filepath;
                        }
                    }
                }
            }

            filepath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), XdgConfigPath);
            if (!File.Exists(filepath))
            {
                filepath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), AppRelPath);
            }

            Serilog.Log.Information($"using default config path:{filepath}");
            return filepath;
        }

        private void UpdateDefaultHandler(string protocol)
        {
            if (File.Exists(XdgMime))
            {
                var args = new List<string> { "default", ScalusDesktop, $"{SchemeHandler}{protocol}" };
                var exitCode = OsServices.Execute(XdgMime, args, out string stdOut, out string stdErr);
                if (exitCode != 0)
                {
                    Serilog.Log.Warning($"Failed to run {XdgMime}, stdout:{stdOut}, stderr:{stdErr}");
                }

                return;
            }

            Serilog.Log.Warning($"Cmd:{XdgMime} was not found: cannot update default handler");
        }

        private void UpdateDesktopDb()
        {
            if (File.Exists(UpdateDesktopDatabase))
            {
                var exitCode = OsServices.Execute(UpdateDesktopDatabase, new List<string> { AppDataPath }, out string stdOut, out string stdErr);
                if (exitCode != 0)
                {
                    Serilog.Log.Warning($"Failed to run {UpdateDesktopDatabase}: Stdoutput: {stdOut}, StdErr:{stdErr}");
                }

                return;
            }

            Serilog.Log.Warning($"Cmd:{UpdateDesktopDatabase} was not found, cannot update the desktop database");
        }

        private void WriteScalusDesktopFile(List<string> protocolList)
        {
            var version = Assembly.GetExecutingAssembly()?.GetName()?.Version?.ToString() ?? string.Empty;
            var registrationCommand = Constants.GetLaunchCommand("%u");
            var mimeTypes = new StringBuilder("MimeType=");
            foreach (var one in protocolList)
            {
                mimeTypes.Append($"x-scheme-handler/{one};");
            }

            var lines = new List<string>
            {
                "[Desktop Entry]",
                $"Version={version}",
                "Type=Application",
                "Terminal=false",
                $"Exec={registrationCommand}",
                "Name=scalus",
                "Comment=Session URL Launch Utility",
                "Categories=Application;Network",
                mimeTypes.ToString(),
            };
            var path = Path.Combine(AppDataPath, ScalusDesktop);
            File.WriteAllLines(path, lines);
        }
    }
}
