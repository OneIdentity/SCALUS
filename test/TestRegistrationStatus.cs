using System.Collections.Generic;
using OneIdentity.Scalus.Dto;
using OneIdentity.Scalus.Platform;
using Xunit;

namespace OneIdentity.Scalus.Test
{
    // Tests the per-protocol registration status aggregation that drives the UI's
    // three-state handler display (registered / conflict / unregistered). "conflict"
    // means a foreign application owns the scheme; it must take precedence over an
    // otherwise-unregistered reading so the UI can offer to replace it.
    public class TestRegistrationStatus
    {
        private const string ForeignCommand = "\"C:\\__scalus_test_missing__\\putty.exe\" -ssh %1";

        private static Registration Build(params FakeRegistrar[] registrars) =>
            new Registration(registrars, null, null);

        [Fact]
        public void AllRegistrarsScalus_ReportsRegistered()
        {
            var status = Build(new FakeRegistrar(true, null), new FakeRegistrar(true, null)).GetStatus("rdp");
            Assert.Equal(RegistrationStatus.Registered, status.State);
            Assert.Equal("rdp", status.Protocol);
        }

        [Fact]
        public void NoCommandsAnywhere_ReportsUnregistered()
        {
            var status = Build(new FakeRegistrar(false, null), new FakeRegistrar(false, string.Empty)).GetStatus("rdp");
            Assert.Equal(RegistrationStatus.Unregistered, status.State);
        }

        [Fact]
        public void ForeignCommand_ReportsConflictWithDetail()
        {
            var status = Build(new FakeRegistrar(false, ForeignCommand), new FakeRegistrar(false, null)).GetStatus("ssh");
            Assert.Equal(RegistrationStatus.Conflict, status.State);
            Assert.Equal(ForeignCommand, status.Command);
            Assert.Equal("C:\\__scalus_test_missing__\\putty.exe", status.Path);
            // The exe does not exist on the test box, so the friendly name falls back to the file name.
            Assert.Equal("putty.exe", status.Program);
        }

        [Fact]
        public void ConflictTakesPrecedenceOverAPartialScalusRegistration()
        {
            // One registrar is ours, the other has a foreign handler: the foreign one wins.
            var status = Build(new FakeRegistrar(true, null), new FakeRegistrar(false, ForeignCommand)).GetStatus("ssh");
            Assert.Equal(RegistrationStatus.Conflict, status.State);
        }

        [Fact]
        public void InvalidProtocol_ReportsUnregistered()
        {
            var status = Build(new FakeRegistrar(true, null)).GetStatus("not a scheme");
            Assert.Equal(RegistrationStatus.Unregistered, status.State);
        }

        private sealed class FakeRegistrar : IProtocolRegistrar
        {
            private readonly bool isScalus;
            private readonly string command;

            public FakeRegistrar(bool isScalus, string command)
            {
                this.isScalus = isScalus;
                this.command = command;
            }

            public IOsServices OsServices => null;

            public bool UseSudo { get; set; }

            public bool RootMode { get; set; }

            public string Name => "fake";

            public string GetRegisteredCommand(string protocol) => command;

            public bool IsScalusRegistered(string protocol) => isScalus;

            public bool Unregister(string protocol) => true;

            public bool Register(string protocol) => true;

            public bool ReplaceRegistration(string protocol) => true;
        }
    }
}
