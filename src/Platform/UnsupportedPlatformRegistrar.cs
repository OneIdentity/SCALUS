// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UnsupportedPlatformRegistrar.cs" company="One Identity Inc.">
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

namespace OneIdentity.Scalus.Platform
{
    internal class UnsupportedPlatformRegistrar : IProtocolRegistrar
    {
        public UnsupportedPlatformRegistrar(IUserInteraction userInteraction)
        {
            UserInteraction = userInteraction;
        }

        public IOsServices OsServices { get; }

        public bool UseSudo { get; set; }

        public bool RootMode { get; set; }

        public string Name { get; } = "Unknown";

        private IUserInteraction UserInteraction { get; }

        public string GetRegisteredCommand(string protocol)
        {
            return null;
        }

        public bool IsScalusRegistered(string command)
        {
            return false;
        }

        public bool Register(string protocol)
        {
            var registrationCommand = Constants.GetLaunchCommand();
            var message = $@"
SCALUS doesn't know how to register a URL protocol handler on this platform.
You can register SCALUS manually using this command: {registrationCommand}
";
            UserInteraction.Message(message);
            return true;
        }

        public bool Unregister(string protocol)
        {
            UserInteraction.Message("SCALUS doesn't know how to unregister as a URL protocol handler on this platform.");
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
    }
}
