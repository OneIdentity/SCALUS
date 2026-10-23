// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UninstallPrompt.cs" company="One Identity Inc.">
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
    using System.Runtime.InteropServices;

    // Asks the user whether their SCALUS configuration data should be removed. This is the "custom
    // action" the Windows installer invokes during an interactive uninstall: the installer runs
    // scalus.exe (this native binary), which shows a modal Yes/No dialog so the user can choose
    // whether to keep or delete their settings. On other platforms (or when run from a terminal) it
    // falls back to a console prompt. The default answer is always "No" (keep the data) so an
    // unattended context never deletes configuration silently.
    internal static partial class UninstallPrompt
    {
        private const string Title = "SCALUS Uninstall";

        private const string Message =
            "Do you want to remove your SCALUS configuration data (settings and templates)?\r\n\r\n" +
            "Choose No to keep it for a future installation.";

        public static bool ConfirmDeleteConfiguration()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return ConfirmWindows();
            }

            return ConfirmConsole();
        }

        private static bool ConfirmWindows()
        {
            const uint MbYesNo = 0x00000004;
            const uint MbIconQuestion = 0x00000020;
            const uint MbDefButton2 = 0x00000100;
            const uint MbSetForeground = 0x00010000;
            const uint MbTopMost = 0x00040000;
            const int IdYes = 6;

            try
            {
                var result = MessageBoxW(
                    IntPtr.Zero,
                    Message,
                    Title,
                    MbYesNo | MbIconQuestion | MbDefButton2 | MbSetForeground | MbTopMost);
                return result == IdYes;
            }
            catch (Exception)
            {
                // If the dialog cannot be shown for any reason, err on the side of keeping the data.
                return false;
            }
        }

        private static bool ConfirmConsole()
        {
            try
            {
                if (Console.IsInputRedirected)
                {
                    // No interactive terminal to prompt on; keep the configuration.
                    return false;
                }

                Console.Write($"{Message} [y/N] ");
                var response = Console.ReadLine()?.Trim();
                return string.Equals(response, "y", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(response, "yes", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception)
            {
                return false;
            }
        }

        [LibraryImport("user32.dll", EntryPoint = "MessageBoxW", StringMarshalling = StringMarshalling.Utf16)]
        private static partial int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);
    }
}
