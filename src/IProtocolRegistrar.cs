// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IProtocolRegistrar.cs" company="One Identity Inc.">
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
    using OneIdentity.Scalus.Platform;

    public interface IProtocolRegistrar
    {
        IOsServices OsServices { get; }

        bool UseSudo { get; set; }

        bool RootMode { get; set; }

        string Name { get; }

        string GetRegisteredCommand(string protocol);

        bool IsScalusRegistered(string command);

        bool Unregister(string protocol);

        bool Register(string protocol);

        bool ReplaceRegistration(string protocol);
    }
}
