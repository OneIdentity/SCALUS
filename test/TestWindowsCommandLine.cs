using Xunit;
using OneIdentity.Scalus;

namespace OneIdentity.Scalus.Test
{
    // Tests for the handler-command detection that lets SCALUS recognise when a
    // protocol is registered to a *different* binary (for example a stale install
    // under Program Files) rather than the currently running one. This is the logic
    // behind WindowsProtocolRegistrar.IsScalusRegistered, which must report "not
    // registered" so registration takes the association over instead of silently
    // deferring to the old executable that a browser like Edge would launch.
    public class TestWindowsCommandLine
    {
        private const string DevBinary = @"C:\work\SCALUS\src\Scalus.Ui\bin\Debug\net10.0\scalus-ui.exe";
        private const string StaleBinary = @"C:\Program Files\SCALUS\scalus.exe";

        [Fact]
        public void MatchesCommandThatLaunchesTheSameBinary()
        {
            var command = $"{DevBinary} launch -u \"%1\"";
            Assert.True(WindowsCommandLine.InvokesBinary(command, DevBinary));
        }

        [Fact]
        public void DoesNotMatchCommandThatLaunchesADifferentBinary()
        {
            // The stale Program Files command still mentions "scalus" but resolves to a
            // different executable, so it must not count as our registration.
            var command = $"{StaleBinary} launch -u \"%1\"";
            Assert.False(WindowsCommandLine.InvokesBinary(command, DevBinary));
        }

        [Fact]
        public void MatchesWhenBinaryPathIsQuoted()
        {
            var quoted = @"C:\Program Files\SCALUS\scalus.exe";
            var command = $"\"{quoted}\" launch -u \"%1\"";
            Assert.True(WindowsCommandLine.InvokesBinary(command, quoted));
        }

        [Fact]
        public void MatchesLegacyUnquotedBinaryPathContainingSpaces()
        {
            var binary = @"C:\Program Files\SCALUS\ui\scalus-ui.exe";
            var command = $"{binary} launch -u \"%1\"";

            Assert.True(WindowsCommandLine.InvokesBinary(command, binary));
            Assert.Equal(binary, WindowsCommandLine.GetExecutable(command));
        }

        [Fact]
        public void ReadsQuotedBinaryPathContainingSpaces()
        {
            var binary = @"C:\Program Files\SCALUS\ui\scalus-ui.exe";
            var command = $"\"{binary}\" launch -u \"%1\"";

            Assert.Equal(binary, WindowsCommandLine.GetExecutable(command));
        }

        [Fact]
        public void ComparisonIsCaseInsensitive()
        {
            var command = $"{DevBinary.ToUpperInvariant()} launch -u \"%1\"";
            Assert.True(WindowsCommandLine.InvokesBinary(command, DevBinary));
        }

        [Fact]
        public void ReturnsFalseForEmptyCommand()
        {
            Assert.False(WindowsCommandLine.InvokesBinary(string.Empty, DevBinary));
            Assert.False(WindowsCommandLine.InvokesBinary(null, DevBinary));
        }

        // The family-aware detection used by the registrars: a command is "ours" when it
        // launches scalus or scalus-ui out of our own install directory (GetBinaryDir() ==
        // AppContext.BaseDirectory at runtime and test time).
        private static string InstallDirBinary(string fileName) =>
            System.IO.Path.Combine(System.AppContext.BaseDirectory, fileName);

        [Fact]
        public void InvokesThisBinary_MatchesCanonicalCliInInstallDir()
        {
            var command = $"\"{InstallDirBinary("scalus.exe")}\" launch -u \"%1\"";
            Assert.True(WindowsCommandLine.InvokesThisBinary(command));
        }

        [Fact]
        public void InvokesThisBinary_MatchesGuiInInstallDir()
        {
            // Existing installs / the dev tree may still be registered to scalus-ui; that
            // is the same product in the same directory and must still count as ours.
            var command = $"\"{InstallDirBinary("scalus-ui.exe")}\" launch -u \"%1\"";
            Assert.True(WindowsCommandLine.InvokesThisBinary(command));
        }

        [Fact]
        public void InvokesThisBinary_RejectsScalusInADifferentDirectory()
        {
            // A stale copy under a different directory mentions the scalus name but is a
            // foreign handler we must take the association away from.
            var command = $"\"{StaleBinary}\" launch -u \"%1\"";
            Assert.False(WindowsCommandLine.InvokesThisBinary(command));
        }

        [Fact]
        public void InvokesThisBinary_RejectsForeignHandler()
        {
            var command = "\"C:\\Program Files\\PuTTY\\putty.exe\" -telnet \"%1\"";
            Assert.False(WindowsCommandLine.InvokesThisBinary(command));
        }

        [Fact]
        public void PackagedCliRecognizesUiHandlerInItsInstall()
        {
            const string root = @"C:\Program Files\SCALUS";
            Assert.True(WindowsCommandLine.IsLauncherInInstall(
                root + @"\ui\scalus-ui.exe",
                root));
        }

        [Fact]
        public void PackagedUiRecognizesCliHandlerInItsInstall()
        {
            const string root = @"C:\Program Files\SCALUS";
            Assert.True(WindowsCommandLine.IsLauncherInInstall(
                root + @"\scalus.exe",
                root + @"\ui"));
        }

        [Fact]
        public void PackagedCliRejectsUiHandlerFromDifferentInstall()
        {
            Assert.False(WindowsCommandLine.IsLauncherInInstall(
                @"D:\Old SCALUS\ui\scalus-ui.exe",
                @"C:\Program Files\SCALUS"));
        }
    }
}
