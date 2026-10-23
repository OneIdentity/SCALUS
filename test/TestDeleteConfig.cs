using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using OneIdentity.Scalus;
using OneIdentity.Scalus.Util;

namespace OneIdentity.Scalus.Test
{
    // Covers the "delete configuration data on uninstall" option: the recursive-delete helper behind
    // ConfigurationManager.DeleteConfiguration(), and the two new `unregister` command-line flags
    // (--delete-config / --prompt-delete-config) that an uninstall uses to trigger it.
    public class TestDeleteConfig
    {
        [Fact]
        public void TryDeleteDirectory_RemovesPopulatedDirectory()
        {
            var dir = Path.Combine(Path.GetTempPath(), "scalus-del-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(dir, "templates"));
            File.WriteAllText(Path.Combine(dir, "SCALUS.json"), "{}");
            File.WriteAllText(Path.Combine(dir, "templates", "a.tmpl"), "x");

            var removed = ConfigurationManager.TryDeleteDirectory(dir);

            Assert.True(removed);
            Assert.False(Directory.Exists(dir));
        }

        [Fact]
        public void TryDeleteDirectory_MissingDirectoryReturnsFalse()
        {
            var dir = Path.Combine(Path.GetTempPath(), "scalus-del-" + Guid.NewGuid().ToString("N"));
            Assert.False(Directory.Exists(dir));

            Assert.False(ConfigurationManager.TryDeleteDirectory(dir));
        }

        [Fact]
        public void Unregister_ParsesDeleteConfigFlag()
        {
            var options = Parse("unregister", "--delete-config");

            Assert.True(options.DeleteConfig);
            Assert.False(options.PromptDeleteConfig);
        }

        [Fact]
        public void Unregister_ParsesPromptDeleteConfigFlag()
        {
            var options = Parse("unregister", "--prompt-delete-config");

            Assert.True(options.PromptDeleteConfig);
            Assert.False(options.DeleteConfig);
        }

        [Fact]
        public void Unregister_DefaultsLeaveConfigurationUntouched()
        {
            var options = Parse("unregister");

            Assert.False(options.DeleteConfig);
            Assert.False(options.PromptDeleteConfig);
        }

        private static OneIdentity.Scalus.Unregister.Options Parse(params string[] args)
        {
            object captured = null;
            var handler = new CommandLineHandler(new IVerb[] { new OneIdentity.Scalus.Unregister.Options() });
            handler.Build(args, o => { captured = o; return null; }, out _);
            return Assert.IsType<OneIdentity.Scalus.Unregister.Options>(captured);
        }
    }
}
