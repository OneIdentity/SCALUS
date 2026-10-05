// --------------------------------------------------------------------------------------------------------------------
// <copyright file="WindowsConfigurationRemovalPrompt.cs" company="One Identity Inc.">
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
    using System.Runtime.InteropServices;

    internal static partial class WindowsConfigurationRemovalPrompt
    {
        private const uint MbYesNo = 0x00000004;
        private const uint MbIconQuestion = 0x00000020;
        private const uint MbSetForeground = 0x00010000;
        private const int IdNo = 7;

        public static bool Confirm()
        {
            if (!OperatingSystem.IsWindows())
            {
                return true;
            }

            const string message =
                "Do you want to remove your SCALUS configuration data?\n\n" +
                "Choose Yes to remove SCALUS.json and window settings. Log files will be preserved.\n\n" +
                "Choose No to keep the configuration data for a future installation.";

            var result = MessageBoxW(
                IntPtr.Zero,
                message,
                "Uninstall SCALUS",
                MbYesNo | MbIconQuestion | MbSetForeground);

            // If Windows cannot display the prompt, preserve the uninstall's default cleanup behavior.
            return result != IdNo;
        }

        [LibraryImport("user32.dll", EntryPoint = "MessageBoxW", StringMarshalling = StringMarshalling.Utf16)]
        private static partial int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);
    }
}
