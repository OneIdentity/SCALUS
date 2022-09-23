// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IScalusApiConfiguration.cs" company="One Identity Inc.">
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
    using OneIdentity.Scalus.Dto;

    public interface IScalusApiConfiguration
    {
        List<string> ValidationErrors { get; }

        ScalusConfig GetConfiguration();

        List<string> SaveConfiguration(ScalusConfig configuration);
    }

    internal interface IScalusConfiguration
    {
        List<string> ValidationErrors { get; }

        IProtocolHandler GetProtocolHandler(string uri);

        ScalusConfig GetConfiguration(string path = null);
    }

    internal interface IProtocolHandler : IDisposable
    {
        void Run(bool preview = false);
    }
}
