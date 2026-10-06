import { ApplicationConfig, ScalusConfig } from './scalus-bridge';
import { DEFAULT_RDP_TEMPLATE, DEFAULT_RDP_TEMPLATE_STATIC } from './default-template';

const REMMINA_RDP_TEMPLATE = [
  '[remmina]',
  'name=Safeguard RDP',
  'protocol=RDP',
  'server=%Host%',
  'username=%User%',
  'password=Safeguard',
].join('\n');

const REMMINA_SSH_TEMPLATE = [
  '[remmina]',
  'name=Safeguard SSH',
  'protocol=SSH',
  'server=%Host%',
  'ssh_username=%User%',
].join('\n');

export const SEED_CONFIG: ScalusConfig = {
  Protocols: [
    { Protocol: 'rdp', AppId: 'windows-rdp' },
    { Protocol: 'ssh', AppId: 'windows-openssh' },
    { Protocol: 'telnet', AppId: null }
  ],
  Applications: [
    {
      Id: 'windows-rdp',
      Name: 'Windows RDP [dynamic]',
      Description: 'Microsoft Remote Desktop (mstsc.exe): full desktop or RemoteApp. Resolution tracks the client display.',
      Platforms: ['Windows'],
      Protocol: 'rdp',
      Parser: { ParserId: 'rdp', Options: ['waitForInputIdle'], TemplateContent: DEFAULT_RDP_TEMPLATE, TemplateExtension: '.rdp' },
      Exec: 'C:\\windows\\system32\\mstsc.exe',
      Args: ['%GeneratedFile%']
    },
    {
      Id: 'windows-rdp-static',
      Name: 'Windows RDP [static]',
      Description: 'Microsoft Remote Desktop (mstsc.exe): full desktop or RemoteApp. Fixed 1920x1080 resolution.',
      Platforms: ['Windows'],
      Protocol: 'rdp',
      Parser: { ParserId: 'rdp', Options: ['waitForInputIdle'], TemplateContent: DEFAULT_RDP_TEMPLATE_STATIC, TemplateExtension: '.rdp' },
      Exec: 'C:\\windows\\system32\\mstsc.exe',
      Args: ['%GeneratedFile%']
    },
    {
      Id: 'windows-openssh',
      Name: 'Windows OpenSSH',
      Description: 'Run the Windows OpenSSH client.',
      Platforms: ['Windows'],
      Protocol: 'ssh',
      Parser: { ParserId: 'ssh', Options: [] },
      Exec: 'C:\\Windows\\System32\\OpenSSH\\ssh.exe',
      Args: ['-l', '%User%', '%Host%']
    },
    {
      Id: 'putty-ssh',
      Name: 'Putty',
      Description: 'Run the Putty client to connect using SSH.',
      Platforms: ['Windows'],
      Protocol: 'ssh',
      Parser: { ParserId: 'ssh', Options: [] },
      Exec: 'C:\\Program Files\\PuTTY\\putty.exe',
      Args: ['-ssh', '%user%@%host%']
    },
    {
      Id: 'putty-telnet',
      Name: 'Putty Telnet',
      Description: 'Run the Putty client to connect using telnet.',
      Platforms: ['Windows'],
      Protocol: 'telnet',
      Parser: { ParserId: 'telnet', Options: [] },
      Exec: 'C:\\Program Files\\PuTTY\\putty.exe',
      Args: ['-telnet', '%user%@%host%']
    },
    {
      Id: 'mac-rdp',
      Name: 'Mac RDP',
      Description: 'Run Microsoft Remote Desktop on macOS using default connection settings.',
      Platforms: ['Mac'],
      Protocol: 'rdp',
      Parser: { ParserId: 'rdp', Options: ['wait:60'], TemplateContent: DEFAULT_RDP_TEMPLATE, TemplateExtension: '.rdp' },
      Exec: '/usr/bin/open',
      Args: ['-nb', 'com.microsoft.rdc.macos', '%GeneratedFile%']
    },
    {
      Id: 'mac-ssh',
      Name: 'MAC TerminalSSH',
      Description: 'Run Terminal on macOS with SSH connection information.',
      Platforms: ['Mac'],
      Protocol: 'ssh',
      Parser: { ParserId: 'ssh', Options: ['waitforexit'] },
      Exec: '/usr/bin/open',
      Args: ['-b', 'com.apple.terminal', '%OriginalURL%']
    },
    {
      Id: 'mac-ssh-iTerm',
      Name: 'MAC iTermSSH',
      Description: 'Run iTerm2 on macOS with SSH connection information.',
      Platforms: ['Mac'],
      Protocol: 'ssh',
      Parser: { ParserId: 'ssh', Options: ['wait:10'], TemplateContent: '#!/bin/sh\nexec ssh -l %User% %Host%', TemplateExtension: '.command' },
      Exec: '/usr/bin/open',
      Args: ['-b', 'com.googlecode.iterm2', '%GeneratedFile%']
    },
    {
      Id: 'freerdp',
      Name: 'FreeRDP',
      Description: 'Run FreeRDP client with supplied connection settings.',
      Platforms: ['Linux'],
      Protocol: 'rdp',
      Parser: { ParserId: 'rdp', Options: [] },
      Exec: '/usr/bin/xfreerdp',
      Args: ['/u:%User%', '/v:%Host%:%Port%', '/p:Safeguard']
    },
    {
      Id: 'gnome-terminal-ssh',
      Name: 'Gnome Terminal SSH',
      Description: 'Open Gnome Terminal on Linux to connect to SSH.',
      Platforms: ['Linux'],
      Protocol: 'ssh',
      Parser: { ParserId: 'ssh', Options: [] },
      Exec: '/usr/bin/gnome-terminal',
      Args: ['-x', 'ssh', '-t', '-l', '%User%', '%Host%']
    },
    {
      Id: 'remmina-rdp',
      Name: 'remmina Rdp client',
      Description: 'Run Remmina on Linux to connect to RDP using a template.',
      Platforms: ['Linux'],
      Protocol: 'rdp',
      Parser: { ParserId: 'rdp', Options: ['waitforexit'], TemplateContent: REMMINA_RDP_TEMPLATE, TemplateExtension: '.remmina' },
      Exec: '/usr/bin/remmina',
      Args: ['%GeneratedFile%']
    },
    {
      Id: 'remmina-ssh',
      Name: 'remmina SSH client',
      Description: 'Run Remmina on Linux to connect to SSH using a template.',
      Platforms: ['Linux'],
      Protocol: 'ssh',
      Parser: { ParserId: 'ssh', Options: ['waitforexit'], TemplateContent: REMMINA_SSH_TEMPLATE, TemplateExtension: '.remmina' },
      Exec: '/usr/bin/remmina',
      Args: ['%GeneratedFile%']
    }
  ]
};

export const FIELD_DESCRIPTIONS: Record<string, string> = {
  Name: 'A label for this launch definition.',
  Description: 'Shown on application cards and exports.',
  Platforms: 'Operating systems where this launcher applies.',
  Protocol: 'The URI scheme family this application handles.',
  Parser: 'Decodes incoming links and populates tokens.',
  Exec: 'The native client executable to launch.',
  Args: 'Arguments passed to the executable.'
};

export const TOKENS: Record<string, string> = {
  '%Host%': 'Host/IP the application connects to',
  '%Port%': 'Port the application connects to',
  '%User%': 'User info from the URL, including in-band Safeguard payloads',
  '%Protocol%': 'The URL scheme, e.g. rdp',
  '%Path%': 'Path part of a standard URL',
  '%Query%': 'Query string of a standard URL',
  '%Fragment%': 'Fragment part of a standard URL',
  '%AlternateShell%': 'Program started in the session instead of the shell (RDP)',
  '%Remoteapplicationname%': 'RemoteApp display name (RDP)',
  '%Remoteapplicationprogram%': 'RemoteApp alias/executable (RDP)',
  '%Remoteapplicationcmdline%': 'RemoteApp command-line arguments (RDP)',
  '%GeneratedFile%': 'Temp file built from a template, passed to the app',
  '%OriginalUrl%': 'The full URL string',
  '%RelativeUrl%': 'The URL without the protocol',
  '%Home%': "The user's home directory on this platform",
  '%AppData%': "The user's local app-data directory",
  '%TempPath%': "The user's temp directory",
  '%Token%': 'Safeguard auth token from the in-band username',
  '%Vault%': 'Safeguard vault host',
  '%TargetUser%': 'Target username being connected to',
  '%TargetHost%': 'Target host being connected to',
  '%TargetPort%': 'Target port being connected to',
  '%Account%': 'Safeguard account',
  '%Asset%': 'Safeguard asset'
};

export function cloneConfig(config: ScalusConfig): ScalusConfig {
  return JSON.parse(JSON.stringify(config)) as ScalusConfig;
}

export function normalizeApplication(raw: any): ApplicationConfig | null {
  if (!raw) return null;
  const get = (a: string, b = a.charAt(0).toLowerCase() + a.slice(1)) => raw[a] ?? raw[b];
  const parserRaw = get('Parser') ?? raw.parser ?? {};
  const parserGet = (a: string, b = a.charAt(0).toLowerCase() + a.slice(1)) => parserRaw[a] ?? parserRaw[b];
  const id = get('Id') ?? get('Name');
  const name = get('Name') ?? id;
  if (!id || !name) return null;
  return {
    Id: String(id),
    Name: String(name),
    Description: get('Description') ?? '',
    Platforms: (get('Platforms') ?? ['Windows']) as any,
    Protocol: String(get('Protocol') ?? parserGet('ParserId') ?? 'url'),
    Parser: {
      ParserId: String(parserGet('ParserId') ?? 'url'),
      Options: parserGet('Options') ?? [],
      TemplateContent: parserGet('TemplateContent') ?? undefined,
      TemplateExtension: parserGet('TemplateExtension') ?? undefined,
      LineEnding: parserGet('LineEnding') ?? undefined,
      Encoding: parserGet('Encoding') ?? undefined,
      UseDefaultTemplate: parserGet('UseDefaultTemplate') ?? undefined,
      UseTemplateFile: parserGet('UseTemplateFile') ?? undefined,
      PostProcessingExec: parserGet('PostProcessingExec') ?? undefined,
      PostProcessingArgs: parserGet('PostProcessingArgs') ?? undefined,
      RunInTerminal: parserGet('RunInTerminal') ?? undefined
    },
    Exec: String(get('Exec') ?? ''),
    Args: get('Args') ?? []
  };
}

export function normalizeConfig(raw: any): ScalusConfig | null {
  if (!raw) return null;
  const protocols = raw.Protocols ?? raw.protocols;
  const applications = raw.Applications ?? raw.applications;
  if (!Array.isArray(protocols) || !Array.isArray(applications)) return null;
  return {
    Protocols: protocols.map((p: any) => ({ Protocol: p.Protocol ?? p.protocol, AppId: p.AppId ?? p.appId ?? p.APpId ?? null })),
    Applications: applications.map(normalizeApplication).filter(Boolean) as ApplicationConfig[],
    PreferredTerminal: raw.PreferredTerminal ?? raw.preferredTerminal ?? undefined,
    Settings: normalizeSettings(raw.Settings ?? raw.settings)
  };
}

function normalizeSettings(raw: any): ScalusConfig['Settings'] {
  if (!raw) return undefined;
  const logLevel = raw.LogLevel ?? raw.logLevel;
  const consoleOut = raw.Console ?? raw.console;
  const settings: { LogLevel?: string; Console?: boolean } = {};
  if (logLevel) settings.LogLevel = String(logLevel);
  if (typeof consoleOut === 'boolean') settings.Console = consoleOut;
  return Object.keys(settings).length ? settings : undefined;
}
