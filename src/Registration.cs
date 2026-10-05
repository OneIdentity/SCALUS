// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Registration.cs" company="One Identity Inc.">
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
    using System.Diagnostics;
    using System.IO;
    using OneIdentity.Scalus.Dto;
    using OneIdentity.Scalus.Platform;

    internal class Registration : IRegistration
    {
        public Registration(IEnumerable<IProtocolRegistrar> registrars, IUserInteraction userInteraction, IOsServices osServices)
        {
            Registrars = registrars;
            UserInteraction = userInteraction;
            OsServices = osServices;
        }

        private IEnumerable<IProtocolRegistrar> Registrars { get; }

        private IUserInteraction UserInteraction { get; }

        private IOsServices OsServices { get; }

        public bool IsRegistered(string protocol, bool rootMode = false, bool useSudo = false)
        {
            if (!ProtocolMapping.ValidateProtocol(protocol, out string err))
            {
                Serilog.Log.Error($"Invalid protocol:{protocol}");
                UserInteraction.Message($"{protocol}: The protocol is invalid.");
                return false;
            }

            var registered = true;
            foreach (var registrar in Registrars)
            {
                // Detection must read the currently-selected scope's layer (HKCU vs HKLM,
                // user vs system). RootMode is otherwise only set during mutation.
                registrar.RootMode = rootMode;
                if (useSudo)
                {
                    registrar.UseSudo = true;
                }

                registered = registered && registrar.IsScalusRegistered(protocol);
            }

            return registered;
        }

        public RegistrationStatus GetStatus(string protocol, bool rootMode = false, bool useSudo = false)
        {
            var status = new RegistrationStatus
            {
                Protocol = protocol,
                State = RegistrationStatus.Unregistered,
            };

            if (!ProtocolMapping.ValidateProtocol(protocol, out string err))
            {
                Serilog.Log.Error($"Invalid protocol:{protocol}");
                return status;
            }

            var allScalus = true;
            string conflictCommand = null;
            foreach (var registrar in Registrars)
            {
                // Report status for the currently-selected scope only (owner's requirement:
                // "registrations only report on the mode selected").
                registrar.RootMode = rootMode;
                if (useSudo)
                {
                    registrar.UseSudo = true;
                }

                if (registrar.IsScalusRegistered(protocol))
                {
                    continue;
                }

                allScalus = false;
                if (conflictCommand == null)
                {
                    var command = registrar.GetRegisteredCommand(protocol);
                    if (!string.IsNullOrEmpty(command))
                    {
                        conflictCommand = command;
                    }
                }
            }

            if (conflictCommand != null)
            {
                status.State = RegistrationStatus.Conflict;
                status.Command = conflictCommand;
                status.Path = WindowsCommandLine.GetExecutable(conflictCommand);
                status.Program = DescribeProgram(status.Path);
            }
            else if (allScalus)
            {
                status.State = RegistrationStatus.Registered;
            }

            return status;
        }

        public bool Register(IEnumerable<string> protocols, bool force, bool rootMode = false, bool useSudo = false)
        {
            var retval = false;
            foreach (var protocol in protocols)
            {
                retval = true;
                if (!ProtocolMapping.ValidateProtocol(protocol, out string err))
                {
                    retval = false;
                    Serilog.Log.Error($"{err}");
                    UserInteraction.Message($"{protocol}: {err}");
                    continue;
                }

                foreach (var registrar in Registrars)
                {
                    if (rootMode)
                    {
                        registrar.RootMode = true;
                    }

                    if (useSudo)
                    {
                        registrar.UseSudo = true;
                    }

                    if (registrar.IsScalusRegistered(protocol))
                    {
                        UserInteraction.Message($"{protocol}: {registrar.Name}: nothing to do (scalus is already registered)...");
                        continue;
                    }

                    var command = registrar.GetRegisteredCommand(protocol);
                    var res = false;
                    if (!string.IsNullOrEmpty(command))
                    {
                        if (!force)
                        {
                            UserInteraction.Error(
                                $"{protocol}: another application is already registered with {registrar.Name} to launch:{command}. Use -f to overwrite.");
                            continue;
                        }

                        res = registrar.ReplaceRegistration(protocol);
                    }
                    else
                    {
                        res = registrar.Register(protocol);
                    }

                    if (!res)
                    {
                        UserInteraction.Error($"{protocol}: Failed to register SCALUS with {registrar.Name} as the default protocol handler. Try running this program again with administrator privileges.");
                        retval = false;
                    }
                }

                if (retval == false)
                {
                    UserInteraction.Error($"Failed to register {protocol}");
                    continue;
                }

                UserInteraction.Message($"{protocol}: Finished registering SCALUS for protocol {protocol}.");
            }

            return retval;
        }

        public bool UnRegister(IEnumerable<string> protocols, bool rootMode = false, bool useSudo = false)
        {
            foreach (var protocol in protocols)
            {
                if (!ProtocolMapping.ValidateProtocol(protocol, out string err))
                {
                    Serilog.Log.Error($"{err}");
                    UserInteraction.Message($"{protocol}: {err}");
                    continue;
                }

                foreach (var registrar in Registrars)
                {
                    if (rootMode)
                    {
                        registrar.RootMode = true;
                    }

                    if (useSudo)
                    {
                        registrar.UseSudo = true;
                    }

                    if (registrar.IsScalusRegistered(protocol))
                    {
                        Serilog.Log.Debug($"{protocol}: {registrar.Name}: scalus is registered, attempting to unregister...");
                        if (!registrar.Unregister(protocol))
                        {
                            Serilog.Log.Error($"{protocol}: {registrar.Name}: Failed to unregister SCALUS with {registrar.Name} as the default protocol handler. Try running this program again with administrator privileges.");
                            UserInteraction.Error($"{protocol}: Unable to remove scalus from {registrar.Name}. Try running this program again with administrator privileges.");
                            return false;
                        }

                        Serilog.Log.Debug($"{protocol}: {registrar.Name}: scalus unregistered successfully.");
                    }
                    else
                    {
                        Serilog.Log.Debug($"{protocol}: {registrar.Name}: scalus is not registered, nothing to do.");
                        UserInteraction.Message($"{protocol}: {registrar.Name}: nothing to do (scalus is not registered) ...");
                    }
                }

                Serilog.Log.Debug($"{protocol}: Finished unregistering SCALUS for protocol {protocol}.");
                UserInteraction.Message($"{protocol}: Finished unregistering SCALUS for protocol {protocol}.");
            }

            return true;
        }

        // Best-effort friendly name for a conflicting handler's executable. On Windows the file's
        // description/product name is preferred; otherwise the bare file name is used.
        private static string DescribeProgram(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            try
            {
                if (OperatingSystem.IsWindows() && File.Exists(path))
                {
                    var info = FileVersionInfo.GetVersionInfo(path);
                    var name = info.FileDescription;
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        name = info.ProductName;
                    }

                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        return name.Trim();
                    }
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Debug(ex, $"Could not read version info for {path}");
            }

            try
            {
                return Path.GetFileName(path);
            }
            catch (Exception)
            {
                return path;
            }
        }
    }
}
