using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using OneIdentity.Scalus;
using OneIdentity.Scalus.Dto;
using OneIdentity.Scalus.UrlParser;
using OneIdentity.Scalus.Util;

namespace OneIdentity.Scalus.Test
{
    // End-to-end migration test: loads an older on-disk configuration that uses BOTH legacy template
    // mechanisms (UseDefaultTemplate and UseTemplateFile), pulls the external template files in from
    // disk, runs the real migration, and compares the whole result against a known-good, fully-inlined
    // golden configuration checked in under Fixtures/Migration. This guards the upgrade path so the
    // per-field behaviour never has to be eyeballed by hand.
    public class TestMigrationFixtures
    {
        private static string FixturesDir =>
            Path.Combine(AppContext.BaseDirectory, "Fixtures", "Migration");

        private static ScalusConfig Deserialize(string json) => ScalusJson.Deserialize(json);

        // Re-serialize through the production serializer so the comparison is about content, not the
        // hand-authored whitespace / key order of the golden file.
        private static string Canonicalize(ScalusConfig config) => ScalusJson.Serialize(config);

        private static string ResolvedLegacyConfigJson()
        {
            var templatesDir = Path.Combine(FixturesDir, "templates");
            var raw = File.ReadAllText(Path.Combine(FixturesDir, "legacy-config.json"));

            // Resolve the complete fixture filenames so the legacy mix of Windows and Unix
            // separators does not produce an invalid path on the current test host.
            var rdpPath = Path.Combine(templatesDir, "WinRdpTemplate.rdp").Replace("\\", "\\\\");
            var remminaPath = Path.Combine(templatesDir, "ssh.remmina").Replace("\\", "\\\\");
            return raw
                .Replace("__FIXTURES__\\\\WinRdpTemplate.rdp", rdpPath)
                .Replace("__FIXTURES__/ssh.remmina", remminaPath);
        }

        private static ScalusConfig LoadLegacyConfigWithResolvedPaths() =>
            Deserialize(ResolvedLegacyConfigJson());

        // Minimal derivation so the test can drive the real protected Load(path) pipeline
        // (file read -> validate/deserialize -> MigrateLegacyTemplates -> external template pull-in).
        private sealed class DiskLoader : ScalusConfigurationBase
        {
            public ScalusConfig LoadFrom(string path) => Load(path);
        }

        // The truest end-to-end shape of the user request: an older config sitting on disk, alongside
        // its external template files, loaded through the production Load() path and compared whole
        // against the fully-inlined golden. No direct call to the migration helper.
        [Fact]
        public void LoadFromDiskInlinesEverythingAndMatchesGolden()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "scalus-mig-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            try
            {
                var configPath = Path.Combine(tempDir, "SCALUS.json");
                File.WriteAllText(configPath, ResolvedLegacyConfigJson());

                var loaded = new DiskLoader().LoadFrom(configPath);

                var expected = Deserialize(File.ReadAllText(Path.Combine(FixturesDir, "expected-inlined.json")));
                Assert.Equal(Canonicalize(expected), Canonicalize(loaded));
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void MigrateLegacyConfigFromDiskMatchesInlinedGolden()
        {
            var config = LoadLegacyConfigWithResolvedPaths();

            var changed = ScalusConfigurationBase.MigrateLegacyTemplates(config);
            Assert.True(changed, "Expected the legacy configuration to be migrated");

            var expected = Deserialize(File.ReadAllText(Path.Combine(FixturesDir, "expected-inlined.json")));

            Assert.Equal(Canonicalize(expected), Canonicalize(config));
        }

        [Fact]
        public void MigrationLeavesNoLegacyTemplateFields()
        {
            var config = LoadLegacyConfigWithResolvedPaths();
            ScalusConfigurationBase.MigrateLegacyTemplates(config);

            foreach (var parser in config.Applications.Select(a => a.Parser))
            {
                Assert.False(parser.UseDefaultTemplate, "UseDefaultTemplate should be cleared after migration");
                Assert.True(string.IsNullOrEmpty(parser.UseTemplateFile), "UseTemplateFile should be cleared after migration");
            }
        }

        [Fact]
        public void MigrationInlinesTheEmbeddedDefaultRdpTemplateVerbatim()
        {
            var config = LoadLegacyConfigWithResolvedPaths();
            ScalusConfigurationBase.MigrateLegacyTemplates(config);

            var app = config.Applications.Single(a => a.Id == "win-rdp-default");
            Assert.Equal(DefaultRdpUrlParser.GetDefaultTemplateText(), app.Parser.TemplateContent);
            Assert.Equal(".rdp", app.Parser.TemplateExtension);
        }

        [Fact]
        public void MigrationNormalizesExternalTemplateLineEndingsAndDerivesExtension()
        {
            var config = LoadLegacyConfigWithResolvedPaths();
            ScalusConfigurationBase.MigrateLegacyTemplates(config);

            // The .rdp fixture on disk uses CRLF; migration stores it LF-canonical.
            var rdp = config.Applications.Single(a => a.Id == "win-rdp-file");
            Assert.DoesNotContain("\r", rdp.Parser.TemplateContent);
            Assert.Contains("full address:s:%Host%", rdp.Parser.TemplateContent);
            Assert.Equal(".rdp", rdp.Parser.TemplateExtension);

            // PostProcessing is not touched by the editor but must survive the migration round-trip.
            Assert.Equal("%AppData%\\sign.exe", rdp.Parser.PostProcessingExec);

            // The .remmina fixture drives extension derivation from the file name.
            var remmina = config.Applications.Single(a => a.Id == "remmina-ssh");
            Assert.Equal(".remmina", remmina.Parser.TemplateExtension);
            Assert.Contains("[remmina]", remmina.Parser.TemplateContent);
        }

        [Fact]
        public void MigrationLeavesCommandOnlyApplicationsUntouched()
        {
            var config = LoadLegacyConfigWithResolvedPaths();
            ScalusConfigurationBase.MigrateLegacyTemplates(config);

            var plain = config.Applications.Single(a => a.Id == "plain-ssh");
            Assert.False(plain.Parser.HasTemplate, "A command-only application should not gain a template");
            Assert.Null(plain.Parser.TemplateContent);
        }
    }
}
