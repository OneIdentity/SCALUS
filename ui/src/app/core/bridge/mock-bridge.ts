import { Injectable } from '@angular/core';
import { cloneConfig, FIELD_DESCRIPTIONS, normalizeConfig, SEED_CONFIG, TOKENS } from './seed-data';
import { Capabilities, LaunchRecord, Platform, RegistrationScope, RegistrationStatus, RegistrationWriteResult, SaveConfigResult, ScalusBridge, ScalusConfig, StartupAction, TerminalOption } from './scalus-bridge';

// A demo timeline so the Logs view is populated in mock/browser mode.
const NOW = Date.now();
const MOCK_LAUNCH_FILES: Record<string, string> = {
  '20260708T140233517Z-a1b2c3d4.rdp':
    'full address:s:sps.example.com:3389\r\n' +
    'username:s:vaultaddress~vault.example.com%token~9f3c1a...%svc-admin%web01.corp.local\r\n' +
    'authentication level:i:0\r\n' +
    'screen mode id:i:2\r\n',
};
const MOCK_LAUNCH_RECORDS: LaunchRecord[] = [
  {
    LaunchId: 'a1b2c3d4', TimestampUtc: new Date(NOW - 42_000).toISOString(),
    LauncherBinary: 'scalus', Protocol: 'rdp', ApplicationId: 'rdp-mstsc',
    Url: 'rdp://full%20address=sps.example.com:3389&username=...',
    Command: 'C:\\Windows\\System32\\mstsc.exe',
    Args: 'C:\\Users\\you\\AppData\\Local\\SCALUS\\logs\\launches\\20260708T140233517Z-a1b2c3d4.rdp',
    GeneratedFile: '20260708T140233517Z-a1b2c3d4.rdp',
    Outcome: 'spawned', Success: true, ExitCode: null, DurationMs: 128,
  },
  {
    LaunchId: 'b2c3d4e5', TimestampUtc: new Date(NOW - 5 * 60_000).toISOString(),
    LauncherBinary: 'scalus', Protocol: 'ssh', ApplicationId: 'ssh-openssh',
    Url: 'ssh://vaultaddress=vault.example.com@token=...@svc-admin@web01@sps.example.com:22',
    Command: 'C:\\Windows\\System32\\OpenSSH\\ssh.exe',
    Args: '-l vaultaddress=vault.example.com@token=...@svc-admin@web01 sps.example.com -p 22',
    Outcome: 'spawned', Success: true, ExitCode: null, DurationMs: 74,
  },
  {
    LaunchId: 'c3d4e5f6', TimestampUtc: new Date(NOW - 22 * 60_000).toISOString(),
    LauncherBinary: 'scalus', Protocol: 'rdp', ApplicationId: 'rdp-mstsc',
    Url: 'rdp://full%20address=sps.example.com:3389&username=...',
    Command: 'C:\\Windows\\System32\\mstsc.exe',
    Args: 'C:\\Users\\you\\AppData\\Local\\SCALUS\\logs\\launches\\stale.rdp',
    Outcome: 'spawn-failed', Success: false, ExitCode: null, DurationMs: 12,
    Error: "The system cannot find the file specified: 'mstsc.exe'.",
  },
  {
    LaunchId: 'd4e5f6a7', TimestampUtc: new Date(NOW - 3 * 3_600_000).toISOString(),
    LauncherBinary: 'scalus', Protocol: 'telnet',
    Url: 'telnet://sps.example.com:23',
    Outcome: 'config-error', Success: false, DurationMs: 3,
    Error: "No application is assigned to protocol 'telnet'.",
  },
];

@Injectable()
export class MockBridge implements ScalusBridge {
  private config = cloneConfig(SEED_CONFIG);
  // Registrations are tracked per scope so the mock reflects that switching scope reports a
  // different machine vs per-user state (as the real host does).
  private registrations: Record<RegistrationScope, Set<string>> = {
    user: new Set(['rdp']),
    all: new Set<string>(),
  };
  // Demo seed: something other than this SCALUS owns ssh:// so the conflict state is visible.
  private conflicts = new Map<string, { Program: string; Path: string; Command: string }>([
    ['ssh', { Program: 'PuTTY', Path: 'C:\\Program Files\\PuTTY\\putty.exe', Command: '"C:\\Program Files\\PuTTY\\putty.exe" -ssh %1' }],
  ]);

  async getConfig(): Promise<ScalusConfig> { return cloneConfig(this.config); }

  async saveConfig(config: ScalusConfig): Promise<SaveConfigResult> {
    const errors = await this.validate(config);
    if (!errors.length) this.config = cloneConfig(config);
    return { errors, config: cloneConfig(this.config) };
  }

  async validate(config: ScalusConfig): Promise<string[]> {
    const errors: string[] = [];
    const ids = new Set<string>();
    for (const app of config.Applications) {
      if (!app.Id?.trim()) errors.push('Every application needs an Id.');
      if (!app.Name?.trim()) errors.push(`Application ${app.Id || '(new)'} needs a name.`);
      if (!app.Exec?.trim()) errors.push(`${app.Name || app.Id} needs an executable.`);
      if (!app.Parser?.ParserId) errors.push(`${app.Name || app.Id} needs a parser.`);
      if (ids.has(app.Id)) errors.push(`Duplicate application Id: ${app.Id}`);
      ids.add(app.Id);
    }
    for (const protocol of config.Protocols) {
      if (!protocol.Protocol?.trim()) errors.push('Every protocol needs a scheme.');
      if (protocol.AppId && !ids.has(protocol.AppId)) errors.push(`${protocol.Protocol} points at a missing application.`);
    }
    return Array.from(new Set(errors));
  }

  async getRegistrations(): Promise<string[]> { return Array.from(this.registrations.user).sort(); }

  async getRegistrationStatus(scope: RegistrationScope): Promise<RegistrationStatus[]> {
    const registered = this.registrations[scope] ?? new Set<string>();
    const schemes = new Set<string>(['rdp', 'ssh']);
    for (const p of this.config.Protocols) { if (p.Protocol) schemes.add(p.Protocol); }
    return Array.from(schemes).sort().map((protocol): RegistrationStatus => {
      if (registered.has(protocol)) return { Protocol: protocol, State: 'registered' };
      // The demo conflict only exists in the per-user layer.
      const c = scope === 'user' ? this.conflicts.get(protocol) : undefined;
      if (c) return { Protocol: protocol, State: 'conflict', Program: c.Program, Path: c.Path, Command: c.Command };
      return { Protocol: protocol, State: 'unregistered' };
    });
  }

  async register(protocol: string, scope: RegistrationScope): Promise<RegistrationWriteResult> {
    this.registrations[scope].add(protocol);
    if (scope === 'user') { this.conflicts.delete(protocol); }
    return {};
  }

  async unregister(protocol: string, scope: RegistrationScope): Promise<RegistrationWriteResult> {
    this.registrations[scope].delete(protocol);
    return {};
  }

  async getCapabilities(): Promise<Capabilities> {
    const platform = await this.getPlatform();
    return { platform, canElevateAllUsers: platform !== 'Mac' };
  }

  async getTokens(): Promise<Record<string, string>> { return { ...TOKENS }; }
  async getApplicationDescriptions(): Promise<Record<string, string>> { return { ...FIELD_DESCRIPTIONS }; }
  async getParsers(): Promise<string[]> { return ['rdp', 'ssh', 'telnet', 'url']; }
  async getTerminals(): Promise<TerminalOption[]> {
    return [
      { Id: 'auto', Name: 'Automatic (detect default)', Available: true },
      { Id: 'windows-terminal', Name: 'Windows Terminal', Available: true },
      { Id: 'conhost', Name: 'Windows Console Host (legacy)', Available: true },
    ];
  }
  async getInfo(): Promise<string> { return 'Platform: Windows\nConfiguration file: (in-memory browser seed)\nRuntime: .NET 10 / Photino host\nUI bridge: MockBridge'; }
  async getVersion(): Promise<string> { return '2.0.0-dev'; }
  async getStartupAction(): Promise<StartupAction> { return { ShowLogs: null }; }
  async getLaunchRecords(max = 200): Promise<LaunchRecord[]> { return MOCK_LAUNCH_RECORDS.slice(0, max).map(r => ({ ...r })); }
  async getLaunchFile(fileName: string): Promise<string | null> { return MOCK_LAUNCH_FILES[fileName] ?? null; }
  async openLogsFolder(): Promise<boolean> { return true; }
  async getPlatform(): Promise<Platform> { return 'Windows'; }

  async exportToFile(defaultName: string, contents: string): Promise<boolean> {
    const blob = new Blob([contents], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = defaultName;
    anchor.click();
    URL.revokeObjectURL(url);
    return true;
  }

  async importFromFile(): Promise<string | null> {
    if ('showOpenFilePicker' in window) {
      try {
        const [handle] = await (window as any).showOpenFilePicker({ types: [{ description: 'SCALUS JSON', accept: { 'application/json': ['.json', '.scalus-app.json'] } }] });
        const file = await handle.getFile();
        return await file.text();
      } catch { return null; }
    }
    return new Promise((resolve) => {
      const input = document.createElement('input');
      input.type = 'file';
      input.accept = '.json,.scalus-app.json,application/json';
      input.onchange = () => {
        const file = input.files?.[0];
        if (!file) { resolve(null); return; }
        const reader = new FileReader();
        reader.onload = () => resolve(String(reader.result ?? ''));
        reader.onerror = () => resolve(null);
        reader.readAsText(file);
      };
      input.click();
    });
  }

  importRawForTest(raw: string): ScalusConfig | null { return normalizeConfig(JSON.parse(raw)); }
}
