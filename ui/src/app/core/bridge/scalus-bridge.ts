import { InjectionToken } from '@angular/core';

export type Platform = 'Windows' | 'Linux' | 'Mac';

export type TemplateLineEnding = 'Default' | 'Lf' | 'CrLf' | 'Platform';
export type TemplateEncoding = 'Default' | 'Utf8' | 'Utf8Bom' | 'Utf16LeBom' | 'Ansi';

export interface ParserConfig {
  ParserId: string;
  Options?: string[];
  TemplateContent?: string | null;
  TemplateExtension?: string | null;
  LineEnding?: TemplateLineEnding;
  Encoding?: TemplateEncoding;
  /** Legacy SCALUS 1.x field retained only until the host migrates the template. */
  UseDefaultTemplate?: boolean;
  /** Legacy SCALUS 1.x field retained only until the host migrates the template. */
  UseTemplateFile?: string | null;
  PostProcessingExec?: string;
  PostProcessingArgs?: string[];
  /** Host this launch in the user's preferred terminal (SSH/telnet clients). */
  RunInTerminal?: boolean;
}

export interface ApplicationConfig {
  Id: string;
  Name: string;
  Description?: string;
  Platforms: Platform[];
  Protocol: string;
  Parser: ParserConfig;
  Exec: string;
  Args?: string[];
}

export interface ProtocolMapping {
  Protocol: string;
  AppId?: string | null;
}

export interface ScalusConfig {
  Protocols: ProtocolMapping[];
  Applications: ApplicationConfig[];
  /** Global preferred terminal id for terminal-hosted launches (SSH). Omitted/`auto` = detect. */
  PreferredTerminal?: string | null;
  /** User preferences (logging level, console output) persisted in the per-user config. */
  Settings?: ScalusSettings | null;
}

/** User-editable preferences that persist in the per-user SCALUS.json and are read by the launcher. */
export interface ScalusSettings {
  /** Minimum Serilog level name (Verbose | Debug | Information | Warning | Error). Omitted = Debug. */
  LogLevel?: string | null;
  /** Whether the launcher also writes log output to the console. Omitted = off. */
  Console?: boolean | null;
}

/** A terminal choice offered for the global "Preferred terminal" setting. */
export interface TerminalOption {
  Id: string;
  Name: string;
  Available: boolean;
}

export type RegistrationScope = 'user' | 'all';

export type RegistrationState = 'registered' | 'conflict' | 'unregistered';

export interface RegistrationStatus {
  Protocol: string;
  State: RegistrationState;
  /** Friendly name of the conflicting application (conflict only). */
  Program?: string | null;
  /** Resolved executable path of the conflicting handler (conflict only). */
  Path?: string | null;
  /** Raw registered command line of the conflicting handler (conflict only). */
  Command?: string | null;
}

export interface SaveConfigResult {
  errors: string[];
  config: ScalusConfig;
}

export interface ScalusBridge {
  getConfig(): Promise<ScalusConfig>;
  saveConfig(config: ScalusConfig): Promise<SaveConfigResult>;
  validate(config: ScalusConfig): Promise<string[]>;
  getRegistrations(): Promise<string[]>;
  getRegistrationStatus(scope: RegistrationScope): Promise<RegistrationStatus[]>;
  register(protocol: string, scope: RegistrationScope): Promise<RegistrationWriteResult>;
  unregister(protocol: string, scope: RegistrationScope): Promise<RegistrationWriteResult>;
  getCapabilities(): Promise<Capabilities>;
  getTokens(): Promise<Record<string, string>>;
  getApplicationDescriptions(): Promise<Record<string, string>>;
  getParsers(): Promise<string[]>;
  getTerminals(): Promise<TerminalOption[]>;
  getInfo(): Promise<string>;
  getVersion(): Promise<string>;
  getStartupAction(): Promise<StartupAction>;
  getLaunchRecords(max?: number): Promise<LaunchRecord[]>;
  getLaunchFile(fileName: string): Promise<string | null>;
  openLogsFolder(): Promise<boolean>;
  exportToFile(defaultName: string, contents: string): Promise<boolean>;
  importFromFile(): Promise<string | null>;
  getPlatform(): Promise<Platform>;
}

/** Host capabilities used to shape the UI (e.g. hide All-users where elevation isn't possible). */
export interface Capabilities {
  platform: Platform;
  /** True when the host can elevate to write the machine layer (Windows/Linux); false on macOS. */
  canElevateAllUsers: boolean;
}

/** Outcome of a scoped register/unregister. `cancelled` is true when the user dismissed an elevation prompt. */
export interface RegistrationWriteResult {
  cancelled?: boolean;
}

export const SCALUS_BRIDGE = new InjectionToken<ScalusBridge>('SCALUS_BRIDGE');

/** A one-time startup instruction from the host. ShowLogs carries a launch id to deep-link to. */
export interface StartupAction {
  ShowLogs?: string | null;
}

/** A single launch attempt (success or failure) recorded by the launcher. Mirrors Dto/LaunchRecord.cs. */
export interface LaunchRecord {
  LaunchId: string;
  TimestampUtc: string;
  LauncherBinary?: string | null;
  Protocol?: string | null;
  Url?: string | null;
  ApplicationId?: string | null;
  Command?: string | null;
  Args?: string | null;
  /** File name (beside the record) of the exact file the parser generated for this launch. */
  GeneratedFile?: string | null;
  /** spawned | preview | config-error | spawn-failed | post-execute-error | error. */
  Outcome: string;
  Success: boolean;
  ExitCode?: number | null;
  Error?: string | null;
  DurationMs: number;
}