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
    using System.Collections.Generic;
    using OneIdentity.Scalus.Dto;
    using OneIdentity.Scalus.Platform;

    internal class Registration : IRegistration
    {
        private IEnumerable<IProtocolRegistrar> Registrars { get; }

        private IUserInteraction UserInteraction { get; }

        private IOsServices OsServices { get; }

        public Registration(IEnumerable<IProtocolRegistrar> registrars, IUserInteraction userInteraction,
            IOsServices osServices
        )
        {
            Registrars = registrars;
            UserInteraction = userInteraction;
            OsServices = osServices;
        }

        public bool IsRegistered(string protocol, bool useSudo = false)
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
                if (useSudo)
                    registrar.UseSudo = true;

                registered = registered && registrar.IsScalusRegistered(protocol);
            }

            return registered;
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
                        registrar.RootMode = true;
                    if (useSudo)
                        registrar.UseSudo = true;
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
                        registrar.RootMode = true;
                    if (useSudo)
                        registrar.UseSudo = true;

                    if (registrar.IsScalusRegistered(protocol))
                    {
                        if (!registrar.Unregister(protocol))
                        {
                            UserInteraction.Error($"{protocol}: Unable to remove scalus from {registrar.Name}. Try running this program again with administrator privileges.");
                            return false;
                        }
                    }
                    else
                    {
                        UserInteraction.Message($"{protocol}: {registrar.Name}: nothing to do (scalus is not registered) ...");
                    }
                }

                UserInteraction.Message($"{protocol}: Finished unregistering SCALUS for protocol {protocol}.");
            }

            return true;
        }
    }
}
