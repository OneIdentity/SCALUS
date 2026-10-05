using System;
using System.IO;
using Xunit;

namespace OneIdentity.Scalus.Test
{
    public class TestUnregisterApplication
    {
        private static readonly string[] ConfiguredProtocols =
            { "rdp", "vnc", "VNC", "custom", null, string.Empty };

        private static readonly string[] RegisteredProtocols = { "machine-custom", "VNC" };

        private static readonly string[] ExpectedProtocols =
            { "ssh", "rdp", "telnet", "vnc", "custom", "machine-custom" };

        [Fact]
        public void DefaultProtocolsIncludeBuiltInAndCustomProtocols()
        {
            var protocols = Unregister.Application.GetProtocols(
                ConfiguredProtocols,
                RegisteredProtocols);

            Assert.Equal(ExpectedProtocols, protocols);
        }

        [Fact]
        public void DeleteUserSettingsRemovesConfigurationAndWindowSettings()
        {
            var directory = Path.Combine(Path.GetTempPath(), "scalus-uninstall-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var configurationPath = Path.Combine(directory, "SCALUS.json");
            var windowSettingsPath = Path.Combine(directory, "ui-window.json");
            File.WriteAllText(configurationPath, "{}");
            File.WriteAllText(windowSettingsPath, "{}");
            try
            {
                Unregister.Application.DeleteUserSettings(configurationPath, windowSettingsPath);

                Assert.False(File.Exists(configurationPath));
                Assert.False(File.Exists(windowSettingsPath));
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        [Fact]
        public void DeleteUserSettingsAllowsMissingFiles()
        {
            var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

            Unregister.Application.DeleteUserSettings(
                Path.Combine(directory, "SCALUS.json"),
                Path.Combine(directory, "ui-window.json"));

            Assert.False(Directory.Exists(directory));
        }

        [Fact]
        public void PromptedConfigurationRemovalUsesUserChoice()
        {
            Assert.True(Unregister.Application.ShouldRemoveConfiguration(false, true, () => true));
            Assert.False(Unregister.Application.ShouldRemoveConfiguration(false, true, () => false));
        }

        [Fact]
        public void ExplicitConfigurationRemovalDoesNotPrompt()
        {
            Assert.True(Unregister.Application.ShouldRemoveConfiguration(
                true,
                false,
                () => throw new InvalidOperationException("Prompt should not be called")));
        }

    }
}
