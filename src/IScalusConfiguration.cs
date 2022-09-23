// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IScalusConfiguration.cs" company="One Identity Inc.">
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

    internal interface IProtocolHandler : IDisposable
    {
        void Run(bool preview = false);
    }

    internal interface IScalusConfiguration
    {
        IProtocolHandler GetProtocolHandler(string uri);

        ScalusConfig GetConfiguration(string path = null);

        List<string> ValidationErrors { get; }
    }

    public interface IScalusApiConfiguration
    {
        ScalusConfig GetConfiguration();

        List<string> SaveConfiguration(ScalusConfig configuration);

        List<string> ValidationErrors { get; }
    }


}
