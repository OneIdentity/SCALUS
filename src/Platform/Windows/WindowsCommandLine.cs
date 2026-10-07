// --------------------------------------------------------------------------------------------------------------------
// <copyright file="WindowsCommandLine.cs" company="One Identity Inc.">
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
    using System.IO;

    // Helpers for reasoning about a registered handler command line. A registration is
    // only "ours" when one of its tokens resolves to a SCALUS launcher (scalus or
    // scalus-ui) living in our own install directory. Either binary is accepted because
    // the CLI (scalus) is now the canonical registered handler but existing installs and
    // the dev tree may still be registered to the GUI (scalus-ui); both are the same
    // product in the same directory. A command left behind by a previous install (an
    // older copy under a *different* directory) mentions the scalus name but launches a
    // different executable, so it must not be treated as an active registration.
    internal static class WindowsCommandLine
    {
        public static bool InvokesThisBinary(string command)
        {
            if (string.IsNullOrEmpty(command))
            {
                return false;
            }

            string installRoot;
            try
            {
                installRoot = GetInstallRoot(Constants.GetBinaryDir());
            }
            catch (Exception)
            {
                return false;
            }

            if (string.IsNullOrEmpty(installRoot))
            {
                return false;
            }

            var executable = GetExecutable(command);
            if (string.IsNullOrEmpty(executable))
            {
                return false;
            }

            string full;
            try
            {
                full = GetFullPath(executable);
            }
            catch (Exception)
            {
                return false;
            }

            if (!IsScalusLauncherName(Path.GetFileNameWithoutExtension(full)))
            {
                return false;
            }

            return IsLauncherInInstall(full, installRoot);
        }

        public static bool InvokesBinary(string command, string binaryPath)
        {
            if (string.IsNullOrEmpty(command) || string.IsNullOrEmpty(binaryPath))
            {
                return false;
            }

            var executable = GetExecutable(command);
            if (string.IsNullOrEmpty(executable))
            {
                return false;
            }

            try
            {
                return string.Equals(
                    GetFullPath(executable),
                    GetFullPath(binaryPath),
                    StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception)
            {
                return false;
            }
        }

        // Returns the executable portion of a registered command. Older SCALUS versions wrote
        // an unquoted path, so recover the complete path through its .exe suffix instead of
        // treating the first whitespace-delimited segment (for example C:\Program) as the app.
        public static string GetExecutable(string command)
        {
            if (string.IsNullOrEmpty(command))
            {
                return null;
            }

            var trimmed = command.TrimStart();
            if (trimmed.Length == 0)
            {
                return null;
            }

            if (trimmed[0] == '"')
            {
                var closingQuote = trimmed.IndexOf('"', 1);
                return closingQuote > 1 ? trimmed.Substring(1, closingQuote - 1) : null;
            }

            var executableEnd = trimmed.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
            if (executableEnd >= 0)
            {
                return trimmed.Substring(0, executableEnd + 4);
            }

            var firstWhitespace = -1;
            for (var i = 0; i < trimmed.Length; i++)
            {
                if (char.IsWhiteSpace(trimmed[i]))
                {
                    firstWhitespace = i;
                    break;
                }
            }

            return firstWhitespace < 0 ? trimmed : trimmed.Substring(0, firstWhitespace);
        }

        internal static bool IsLauncherInInstall(string launcherPath, string binaryDirectory)
        {
            if (string.IsNullOrEmpty(launcherPath) || string.IsNullOrEmpty(binaryDirectory))
            {
                return false;
            }

            try
            {
                var root = GetInstallRoot(binaryDirectory);
                var full = GetFullPath(launcherPath);
                var cli = Path.Combine(root, "scalus.exe");
                var ui = Path.Combine(root, "ui", "scalus-ui.exe");
                var colocatedUi = Path.Combine(root, "scalus-ui.exe");
                return string.Equals(full, cli, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(full, ui, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(full, colocatedUi, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool IsScalusLauncherName(string name) =>
            string.Equals(name, "scalus", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(name, "scalus-ui", StringComparison.OrdinalIgnoreCase);

        private static string GetInstallRoot(string binaryDirectory)
        {
            var directory = NormalizeDirectory(binaryDirectory);
            if (string.Equals(Path.GetFileName(directory), "ui", StringComparison.OrdinalIgnoreCase))
            {
                directory = NormalizeDirectory(Path.GetDirectoryName(directory));
            }

            return directory;
        }

        private static string NormalizeDirectory(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }

            return GetFullPath(path)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        // These helpers reason about Windows command lines, but the unit tests also run on
        // Linux CI. Convert Windows separators to the host separator before using System.IO.Path
        // so directory names such as "ui" are recognized consistently on every test host.
        private static string GetFullPath(string path) =>
            Path.GetFullPath(
                path.Replace('\\', Path.DirectorySeparatorChar)
                    .Replace('/', Path.DirectorySeparatorChar));
    }
}
