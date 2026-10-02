import { MockBridge } from './mock-bridge';
import { normalizeConfig } from './seed-data';

describe('MockBridge', () => {
  it('is seeded with protocol and application data', async () => {
    const bridge = new MockBridge();
    const config = await bridge.getConfig();
    expect(config.Protocols.map(p => p.Protocol)).toEqual(['rdp', 'ssh', 'telnet']);
    expect(config.Applications.length).toBeGreaterThan(5);
  });

  it('registers and unregisters schemes in memory', async () => {
    const bridge = new MockBridge();
    await bridge.register('telnet', 'user');
    expect(await bridge.getRegistrations()).toContain('telnet');
    await bridge.unregister('telnet', 'user');
    expect(await bridge.getRegistrations()).not.toContain('telnet');
  });

  it('reports registration status including a foreign conflict', async () => {
    const bridge = new MockBridge();
    const byProtocol = new Map((await bridge.getRegistrationStatus('user')).map(s => [s.Protocol, s]));
    expect(byProtocol.get('rdp')?.State).toBe('registered');
    const ssh = byProtocol.get('ssh');
    expect(ssh?.State).toBe('conflict');
    expect(ssh?.Program).toBeTruthy();
  });

  it('replaces a conflicting handler when re-registered', async () => {
    const bridge = new MockBridge();
    await bridge.register('ssh', 'user');
    const ssh = (await bridge.getRegistrationStatus('user')).find(s => s.Protocol === 'ssh');
    expect(ssh?.State).toBe('registered');
  });

  it('preserves legacy template fields while normalizing a SCALUS 1 configuration', () => {
    const config = normalizeConfig({
      protocols: [],
      applications: [{
        id: 'legacy-rdp',
        name: 'Legacy RDP',
        protocol: 'rdp',
        parser: {
          parserId: 'rdp',
          useDefaultTemplate: false,
          useTemplateFile: 'C:\\templates\\legacy.rdp'
        },
        exec: 'mstsc.exe'
      }]
    });

    expect(config?.Applications[0].Parser.UseDefaultTemplate).toBeFalse();
    expect(config?.Applications[0].Parser.UseTemplateFile).toBe('C:\\templates\\legacy.rdp');
  });
});
