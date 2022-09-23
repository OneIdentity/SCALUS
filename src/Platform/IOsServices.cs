// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IOsServices.cs" company="One Identity Inc.">
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
    using System.Collections.Generic;
    using System.Diagnostics;

    public interface IOsServices
    {
        Process OpenDefault(string file);

        Process Execute(string binary, IEnumerable<string> args);

        /// <summary>
        /// Open a text editor to display message
        /// </summary>
        void OpenText(string message);

        bool IsAdministrator();

        int Execute(string command, IEnumerable<string> args, out string stdOut, out string stdErr);
    }
}
