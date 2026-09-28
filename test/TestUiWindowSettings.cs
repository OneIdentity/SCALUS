using System;
using System.IO;
using OneIdentity.Scalus.Util;
using Xunit;

namespace OneIdentity.Scalus.Test
{
    public class TestUiWindowSettings
    {
        [Fact]
        public void MissingSettingsUseDefaultSize()
        {
            var settings = UiWindowSettings.Load(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "ui-window.json"));

            Assert.Equal(UiWindowSettings.DefaultWidth, settings.Width);
            Assert.Equal(UiWindowSettings.DefaultHeight, settings.Height);
        }

        [Fact]
        public void SavedSizeRoundTrips()
        {
            WithTemporarySettingsFile(path =>
            {
                UiWindowSettings.Save(path, 1600, 1000, true);

                var settings = UiWindowSettings.Load(path);

                Assert.Equal(1600, settings.Width);
                Assert.Equal(1000, settings.Height);
                Assert.True(settings.Maximized);
            });
        }

        [Fact]
        public void InvalidSettingsFallBackToSafeDimensions()
        {
            WithTemporarySettingsFile(path =>
            {
                File.WriteAllText(path, "{\"Width\":100,\"Height\":999999}");

                var settings = UiWindowSettings.Load(path);

                Assert.Equal(UiWindowSettings.MinimumWidth, settings.Width);
                Assert.Equal(UiWindowSettings.MaximumDimension, settings.Height);
            });
        }

        [Fact]
        public void MalformedSettingsUseDefaultSize()
        {
            WithTemporarySettingsFile(path =>
            {
                File.WriteAllText(path, "not json");

                var settings = UiWindowSettings.Load(path);

                Assert.Equal(UiWindowSettings.DefaultWidth, settings.Width);
                Assert.Equal(UiWindowSettings.DefaultHeight, settings.Height);
            });
        }

        private static void WithTemporarySettingsFile(Action<string> action)
        {
            var directory = Path.Combine(Path.GetTempPath(), "scalus-window-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                action(Path.Combine(directory, "ui-window.json"));
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }
    }
}
