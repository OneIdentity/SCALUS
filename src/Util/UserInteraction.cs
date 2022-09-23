﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UserInteraction.cs" company="One Identity Inc.">
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

namespace OneIdentity.Scalus.Util
{
    using System;

    internal class UserInteraction : IUserInteraction
    {
        public void Error(string error)
        {
            Console.Error.WriteLine(error);
        }

        public void Message(string message)
        {
            Console.WriteLine(message);
        }
    }

    //class GuiUserInteraction : IUserInteraction
    //{
    //    public void Error(string error)
    //    {
    //        System.Windows.Forms.MessageBox.Show(error);
    //    }

    //    public void Message(string message)
    //    {
    //        System.Windows.Forms.MessageBox.Show(message);
    //    }
    //}
}
