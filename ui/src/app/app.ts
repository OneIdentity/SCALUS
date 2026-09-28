import { CommonModule } from '@angular/common';
import { Component, ElementRef, HostListener, Inject, OnInit, QueryList, ViewChildren } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { UiBadgeComponent } from './shared/ui/badge.component';
import { UiButtonComponent } from './shared/ui/button.component';
import { UiCardComponent } from './shared/ui/card.component';
import { UiComboComponent } from './shared/ui/combo.component';
import { UiDrawerComponent } from './shared/ui/drawer.component';
import { UiModalComponent } from './shared/ui/modal.component';
import { UiSegmentedControlComponent } from './shared/ui/segmented-control.component';
import { UiSelectComponent } from './shared/ui/select.component';
import { UiToggleComponent } from './shared/ui/toggle.component';
import { ApplicationConfig, LaunchRecord, Platform, ProtocolMapping, RegistrationScope, RegistrationStatus, SCALUS_BRIDGE, ScalusBridge, ScalusConfig, TemplateEncoding, TemplateLineEnding, TerminalOption } from './core/bridge/scalus-bridge';
import { cloneConfig, normalizeApplication, normalizeConfig } from './core/bridge/seed-data';
import { DEFAULT_RDP_TEMPLATE } from './core/bridge/default-template';

type Tab = 'protocols' | 'applications' | 'settings' | 'logs' | 'io' | 'about';
type EditorMode = 'new' | 'edit';
interface ConfirmState {
  open: boolean;
  title: string;
  message: string;
  confirmLabel: string;
  variant: 'primary' | 'danger';
  resolve?: (ok: boolean) => void;
}

const BUILT_IN_PROTOCOLS = new Set(['rdp', 'ssh', 'telnet']);
const TOKEN_GROUPS: Record<string, { connection: string[]; safeguard: boolean }> = {
  rdp: { connection: ['%Host%', '%Port%', '%User%', '%Protocol%', '%AlternateShell%', '%Remoteapplicationname%', '%Remoteapplicationprogram%', '%Remoteapplicationcmdline%'], safeguard: true },
  ssh: { connection: ['%Host%', '%Port%', '%User%', '%Protocol%'], safeguard: true },
  telnet: { connection: ['%Host%', '%Port%', '%User%', '%Protocol%'], safeguard: true },
  url: { connection: ['%Host%', '%Port%', '%User%', '%Protocol%', '%Path%', '%Query%', '%Fragment%'], safeguard: false }
};
const ENV_TOKENS = ['%GeneratedFile%', '%OriginalUrl%', '%RelativeUrl%', '%Home%', '%AppData%', '%TempPath%'];
const SAFEGUARD_TOKENS = ['%Token%', '%Vault%', '%TargetUser%', '%TargetHost%', '%TargetPort%', '%Account%', '%Asset%'];

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, FormsModule, UiBadgeComponent, UiButtonComponent, UiCardComponent, UiComboComponent, UiDrawerComponent, UiModalComponent, UiSegmentedControlComponent, UiSelectComponent, UiToggleComponent],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App implements OnInit {
  @ViewChildren('applicationMenu') applicationMenus!: QueryList<ElementRef<HTMLDetailsElement>>;

  tab: Tab = 'protocols';
  config: ScalusConfig = { Protocols: [], Applications: [] };
  registrations = new Map<string, RegistrationStatus>();
  scope: RegistrationScope = 'user';
  platform: Platform = 'Windows';
  parsers: string[] = [];
  terminals: TerminalOption[] = [];
  readonly lineEndings: TemplateLineEnding[] = ['Default', 'Lf', 'CrLf', 'Platform'];
  readonly encodings: TemplateEncoding[] = ['Default', 'Utf8', 'Utf8Bom', 'Utf16LeBom', 'Ansi'];
  readonly eolLabels: Record<TemplateLineEnding, string> = {
    Default: 'Default (CRLF for .rdp, else LF)',
    Lf: 'LF (\\n)',
    CrLf: 'CRLF (\\r\\n)',
    Platform: 'This platform'
  };
  readonly encLabels: Record<TemplateEncoding, string> = {
    Default: 'Default (UTF-16 LE + BOM for .rdp, else UTF-8)',
    Utf8: 'UTF-8 (no BOM)',
    Utf8Bom: 'UTF-8 with BOM',
    Utf16LeBom: 'UTF-16 LE with BOM',
    Ansi: 'ANSI (Latin-1)'
  };
  tokens: Record<string, string> = {};
  private tokenTips: Record<string, string> = {};
  info = '';
  version = '';
  message = '';
  editorOpen = false;
  editorMode: EditorMode = 'new';
  editorOriginalId: string | null = null;
  editor: ApplicationConfig | null = null;
  editorDirty = false;
  editorErrors: string[] = [];
  protocolModalOpen = false;
  newProtocol = '';
  protocolError = '';
  confirm: ConfirmState = { open: false, title: '', message: '', confirmLabel: 'Confirm', variant: 'primary' };

  nav: { id: Tab; label: string; icon: string }[] = [
    { id: 'protocols', label: 'Protocols', icon: 'eye' },
    { id: 'applications', label: 'Applications', icon: 'grid' },
    { id: 'settings', label: 'Settings', icon: 'settings' },
    { id: 'logs', label: 'Logs', icon: 'list' },
    { id: 'io', label: 'Import / Export', icon: 'download' },
    { id: 'about', label: 'About', icon: 'info' }
  ];
  scopeOptions = [{ label: 'This user (preferred)', value: 'user' }, { label: 'All users', value: 'all' }];
  canElevateAllUsers = true;
  platformOptions: Platform[] = ['Windows', 'Linux', 'Mac'];

  constructor(@Inject(SCALUS_BRIDGE) private bridge: ScalusBridge) {}

  async ngOnInit(): Promise<void> {
    await this.reload();
    this.parsers = await this.bridge.getParsers();
    this.terminals = await this.bridge.getTerminals();
    this.tokens = await this.bridge.getTokens();
    // The bridge keys descriptions by the Token enum name (e.g. "TargetUser"),
    // but the chips are the wire form ("%TargetUser%"). Index case-insensitively
    // on the bare name so every chip's tooltip resolves.
    this.tokenTips = {};
    for (const [name, desc] of Object.entries(this.tokens)) {
      this.tokenTips[name.toLowerCase()] = desc;
    }
    this.platform = await this.bridge.getPlatform();
    this.version = await this.bridge.getVersion();

    // All-users registration needs elevation, which macOS can't provide for URL schemes — hide the
    // scope switch there and pin to per-user.
    const caps = await this.bridge.getCapabilities();
    this.canElevateAllUsers = caps.canElevateAllUsers;
    if (!this.canElevateAllUsers) {
      this.scopeOptions = [{ label: 'This user', value: 'user' }];
      this.scope = 'user';
    }

    // A "--show-logs=<id>" invocation (from the launch-failure dialog) deep-links straight to
    // the failed record.
    const startup = await this.bridge.getStartupAction();
    if (startup?.ShowLogs) { await this.showLaunch(startup.ShowLogs); }
  }

  async showLaunch(launchId: string): Promise<void> {
    this.tab = 'logs';
    await this.loadLaunchRecords();
    const record = this.launchRecords.find(r => r.LaunchId === launchId);
    if (record) { await this.selectLaunch(record); }
  }

  async reload(): Promise<void> {
    this.config = await this.bridge.getConfig();
    await this.reloadStatuses();
  }

  async reloadStatuses(): Promise<void> {
    const list = await this.bridge.getRegistrationStatus(this.scope);
    this.registrations = new Map(list.map(s => [s.Protocol, s]));
    // Keep the About > Diagnostics text in sync with the live registration
    // state; getInfo() recomputes "Registered handlers" server-side each call.
    this.info = await this.bridge.getInfo();
  }

  setTab(tab: Tab): void {
    this.tab = tab;
    if (tab === 'logs') { void this.loadLaunchRecords(); }
  }

  async setScope(value: string): Promise<void> {
    this.scope = value as RegistrationScope;
    // Status reflects the selected scope's layer (per-user vs machine), so re-read on switch.
    await this.reloadStatuses();
  }
  get scopeLabel(): string { return this.scope === 'all' ? 'all-users' : 'current-user'; }

  get registeredCount(): number { return this.config.Protocols.filter(p => this.isRegistered(p.Protocol)).length; }
  get conflictCount(): number { return this.config.Protocols.filter(p => this.isConflict(p.Protocol)).length; }
  get handlerStatus(): string {
    const total = this.config.Protocols.length;
    if (!total) return 'No protocols configured';
    const base = !this.registeredCount ? 'No handlers registered'
      : this.registeredCount === total ? 'All handlers registered'
      : `${this.registeredCount} of ${total} handlers registered`;
    const conflicts = this.conflictCount;
    return conflicts ? `${base} · ${conflicts} conflict${conflicts > 1 ? 's' : ''}` : base;
  }
  get handlerTone(): 'ok' | 'warn' | 'muted' {
    if (this.conflictCount) return 'warn';
    return this.registeredCount === 0 ? 'muted' : this.registeredCount === this.config.Protocols.length ? 'ok' : 'warn';
  }
  get elevationText(): string { return this.platform === 'Windows' ? 'Requires administrator' : 'Requires sudo'; }
  get versionLine(): string { return this.version ? `SCALUS ${this.version}` : 'SCALUS'; }

  get preferredTerminal(): string { return this.config.PreferredTerminal || 'auto'; }
  async setPreferredTerminal(value: string | null): Promise<void> {
    this.config.PreferredTerminal = value && value !== 'auto' ? value : undefined;
    await this.saveCurrentConfig('Preferred terminal updated.');
  }
  terminalOptions(): { label: string; value: string }[] {
    return this.terminals.map(t => ({
      label: t.Available ? t.Name : `${t.Name} (not detected)`,
      value: t.Id
    }));
  }
  get preferredTerminalName(): string {
    const match = this.terminals.find(t => t.Id === this.preferredTerminal);
    return match?.Name ?? 'Automatic';
  }
  get sshAppsUsingTerminal(): ApplicationConfig[] {
    return this.config.Applications.filter(a => a.Parser?.RunInTerminal);
  }
  get termAppNames(): string {
    const names = this.sshAppsUsingTerminal.map(a => a.Name);
    if (names.length <= 2) return names.join(' and ');
    return `${names.slice(0, 2).join(', ')} and ${names.length - 2} more`;
  }

  readonly logLevels: { label: string; value: string }[] = [
    { label: 'Verbose — trace every step (most detail)', value: 'Verbose' },
    { label: 'Debug — detailed diagnostics (default)', value: 'Debug' },
    { label: 'Information — high-level events only', value: 'Information' },
    { label: 'Warning — warnings and errors only', value: 'Warning' },
    { label: 'Error — failures only', value: 'Error' }
  ];
  get logLevel(): string { return this.config.Settings?.LogLevel || 'Debug'; }
  async setLogLevel(value: string | null): Promise<void> {
    const settings = { ...(this.config.Settings ?? {}) };
    if (value && value !== 'Debug') {
      settings.LogLevel = value;
    } else {
      delete settings.LogLevel;
    }
    this.config.Settings = Object.keys(settings).length ? settings : undefined;
    await this.saveCurrentConfig('Log level updated.');
  }
  get logLevelName(): string {
    return this.logLevels.find(l => l.value === this.logLevel)?.value ?? 'Debug';
  }

  // --- Logs view -----------------------------------------------------------
  launchRecords: LaunchRecord[] = [];
  launchRecordsLoaded = false;
  logsLoading = false;
  logsError = '';
  selectedLaunch: LaunchRecord | null = null;
  selectedLaunchFile: string | null = null;
  loadingLaunchFile = false;

  async loadLaunchRecords(): Promise<void> {
    this.logsLoading = true;
    this.logsError = '';
    try {
      this.launchRecords = await this.bridge.getLaunchRecords(200);
    } catch (err) {
      this.logsError = err instanceof Error ? err.message : 'Failed to read launch records.';
      this.launchRecords = [];
    } finally {
      this.launchRecordsLoaded = true;
      this.logsLoading = false;
    }
  }

  async selectLaunch(record: LaunchRecord): Promise<void> {
    this.selectedLaunch = record;
    this.selectedLaunchFile = null;
    if (!record.GeneratedFile) { return; }
    this.loadingLaunchFile = true;
    try {
      this.selectedLaunchFile = this.normalizeEol(await this.bridge.getLaunchFile(record.GeneratedFile));
    } catch {
      this.selectedLaunchFile = null;
    } finally {
      this.loadingLaunchFile = false;
    }
  }

  closeLaunch(): void { this.selectedLaunch = null; this.selectedLaunchFile = null; }

  async openLogsFolder(): Promise<void> {
    try { await this.bridge.openLogsFolder(); }
    catch { this.flash('Could not open the logs folder.'); }
  }

  async exportForIssue(): Promise<void> {
    if (!this.launchRecords.length) { this.flash('No launch records to export.'); return; }
    const lines: string[] = ['# SCALUS launch records', '', `Exported: ${new Date().toISOString()}`, ''];
    for (const r of this.launchRecords) {
      lines.push(`## ${r.Protocol ?? '?'} · ${r.Outcome} · ${r.TimestampUtc}`);
      lines.push(`- launchId: ${r.LaunchId}`);
      if (r.LauncherBinary) { lines.push(`- launcher: ${r.LauncherBinary}`); }
      if (r.Url) { lines.push(`- url: ${r.Url}`); }
      if (r.ApplicationId) { lines.push(`- application: ${r.ApplicationId}`); }
      if (r.Command) { lines.push(`- command: ${r.Command}`); }
      if (r.Args) { lines.push(`- args: ${r.Args}`); }
      if (r.GeneratedFile) { lines.push(`- generatedFile: ${r.GeneratedFile}`); }
      if (r.ExitCode != null) { lines.push(`- exitCode: ${r.ExitCode}`); }
      lines.push(`- duration: ${r.DurationMs} ms`);
      if (r.Error) { lines.push(`- error: ${r.Error}`); }
      lines.push('');
    }
    try {
      const ok = await this.bridge.exportToFile('scalus-launches.md', lines.join('\n'));
      if (ok) { this.flash('Exported launch records.'); }
    } catch { this.flash('Export failed.'); }
  }

  launchTitle(r: LaunchRecord): string {
    return `${(r.Protocol ?? 'launch').toUpperCase()}${r.ApplicationId ? ' · ' + r.ApplicationId : ''}`;
  }
  launchOutcomeTone(r: LaunchRecord): 'ok' | 'warn' | 'muted' {
    if (r.Outcome === 'preview') { return 'muted'; }
    return r.Success ? 'ok' : 'warn';
  }
  launchOutcomeLabel(r: LaunchRecord): string {
    switch (r.Outcome) {
      case 'spawned': return 'Launched';
      case 'preview': return 'Preview';
      case 'spawn-failed': return 'Spawn failed';
      case 'config-error': return 'Config error';
      case 'post-execute-error': return 'Post-process error';
      default: return r.Success ? 'OK' : 'Failed';
    }
  }
  launchTime(r: LaunchRecord): string {
    const d = new Date(r.TimestampUtc);
    return isNaN(d.getTime()) ? r.TimestampUtc : d.toLocaleString();
  }

  appById(id?: string | null): ApplicationConfig | undefined { return this.config.Applications.find(app => app.Id === id); }
  appOptionsFor(protocol: ProtocolMapping): { label: string; value: string }[] {
    const family = this.protocolFamily(protocol.Protocol);
    const showParser = !this.isBuiltIn(protocol.Protocol);
    return this.config.Applications
      .filter(app => this.appMatchesFamily(app, family))
      .filter(app => app.Platforms?.includes(this.platform))
      .map(app => ({
        label: showParser ? `${app.Name} · ${app.Parser.ParserId.toUpperCase()}` : app.Name,
        value: app.Id,
      }));
  }
  protocolFamily(protocol: string): string { return protocol.trim().toLowerCase(); }
  // Suggestions for the Protocol combobox: the built-in schemes plus any custom
  // schemes already declared in this config. The field stays free-text so a brand
  // new scheme can still be typed.
  get protocolSuggestions(): string[] {
    const seen = new Set<string>(BUILT_IN_PROTOCOLS);
    for (const p of this.config.Protocols) {
      const s = p.Protocol?.trim().toLowerCase();
      if (s) seen.add(s);
    }
    return [...seen].sort();
  }
  appMatchesFamily(app: ApplicationConfig, family: string): boolean {
    const normalizedFamily = family.trim().toLowerCase();
    const appProtocol = app.Protocol?.trim().toLowerCase();
    if (!BUILT_IN_PROTOCOLS.has(normalizedFamily)) return appProtocol === normalizedFamily;
    return app.Parser.ParserId?.trim().toLowerCase() === normalizedFamily || appProtocol === normalizedFamily;
  }
  isBuiltIn(protocol: string): boolean { return BUILT_IN_PROTOCOLS.has(protocol); }
  registrationState(protocol: string): 'registered' | 'conflict' | 'unregistered' { return this.registrations.get(protocol)?.State ?? 'unregistered'; }
  isRegistered(protocol: string): boolean { return this.registrationState(protocol) === 'registered'; }
  isConflict(protocol: string): boolean { return this.registrationState(protocol) === 'conflict'; }
  conflictInfo(protocol: string): RegistrationStatus | undefined {
    const status = this.registrations.get(protocol);
    return status?.State === 'conflict' ? status : undefined;
  }
  conflictTooltip(protocol: string): string {
    const info = this.conflictInfo(protocol);
    if (!info) return '';
    const who = info.Program || 'an unknown application';
    const detail = info.Path || info.Command || '';
    return `Currently handled by ${who}${detail ? `\n${detail}` : ''}`;
  }
  handlerStatusLine(protocol: ProtocolMapping): string {
    if (!protocol.AppId) return 'Disabled · no application';
    switch (this.registrationState(protocol.Protocol)) {
      case 'registered': return 'On · registered with OS';
      case 'conflict': return 'Off · conflict';
      default: return 'Off · not registered';
    }
  }
  protocolIcon(protocol: string): string { return protocol === 'rdp' ? 'monitor' : protocol === 'ssh' ? 'terminal' : protocol === 'telnet' ? 'window' : 'link'; }
  protocolLine(mapping: ProtocolMapping): string {
    const app = this.appById(mapping.AppId);
    if (!app) return 'Assign an application to enable its handler';
    if (this.isRegistered(mapping.Protocol)) return `${app.Name} is the registered OS handler`;
    if (this.isConflict(mapping.Protocol)) {
      const who = this.conflictInfo(mapping.Protocol)?.Program || 'Another application';
      return `${who} currently handles this protocol — turn on to replace it with ${app.Name}`;
    }
    return `Turn on Register handler to make ${app.Name} the OS handler`;
  }
  execLeaf(exec: string): string { return (exec || '').replace(/^"([^"]+)".*$/, '$1').split(/[\\/]/).pop()?.split(/\s+/)[0] || exec; }
  appStatus(app: ApplicationConfig): { label: string; tone: 'ok' | 'warn' | 'muted' } {
    if (!app.Platforms.includes(this.platform)) return { label: 'Other platform', tone: 'muted' };
    if (this.config.Protocols.some(p => p.AppId === app.Id)) return { label: 'In use', tone: 'ok' };
    return { label: 'Available', tone: 'muted' };
  }

  async onAppForProtocol(protocol: ProtocolMapping, appId: string | null): Promise<void> {
    if (!appId && this.isRegistered(protocol.Protocol)) await this.toggleRegistration(protocol, false);
    protocol.AppId = appId;
    await this.saveCurrentConfig('Protocol mapping updated.');
  }

  async toggleRegistration(protocol: ProtocolMapping, on: boolean): Promise<void> {
    if (on && !protocol.AppId) return;
    if (on) {
      if (this.isConflict(protocol.Protocol)) {
        const info = this.conflictInfo(protocol.Protocol);
        const who = info?.Program || info?.Path || 'Another application';
        const ok = await this.askConfirm({
          title: `Replace the handler for ${protocol.Protocol}://?`,
          message: `${who} is currently registered to handle ${protocol.Protocol}:// links. Registering SCALUS replaces it as the ${this.scopeLabel} handler.`,
          confirmLabel: 'Replace & register',
          variant: 'danger'
        });
        if (!ok) return;
      }
      const result = await this.bridge.register(protocol.Protocol, this.scope);
      if (result?.cancelled) { this.flash('Registration cancelled.'); return; }
    } else {
      const result = await this.bridge.unregister(protocol.Protocol, this.scope);
      if (result?.cancelled) { this.flash('Unregister cancelled.'); return; }
    }

    // Re-read the real status for the selected scope rather than assuming success — an all-users
    // write goes through an elevated helper and the resulting machine-layer state is authoritative.
    await this.reloadStatuses();
    this.flash(on ? `${protocol.Protocol}:// registered.` : `${protocol.Protocol}:// unregistered.`);
  }

  openProtocolModal(): void { this.newProtocol = ''; this.protocolError = ''; this.protocolModalOpen = true; }
  closeProtocolModal(): void { this.protocolModalOpen = false; }
  async addProtocol(): Promise<void> {
    const scheme = this.newProtocol.trim().toLowerCase().replace(/:\/\/$/, '');
    if (!/^[a-z][a-z0-9+.-]*$/.test(scheme)) { this.protocolError = 'Use a valid URI scheme such as myapp.'; return; }
    if (this.config.Protocols.some(p => p.Protocol === scheme)) { this.protocolError = `${scheme}:// already exists.`; return; }
    this.config.Protocols.push({ Protocol: scheme, AppId: null });
    await this.saveCurrentConfig('Protocol added.');
    this.closeProtocolModal();
  }
  async removeProtocol(protocol: ProtocolMapping): Promise<void> {
    const registered = this.isRegistered(protocol.Protocol);
    const reAdd = this.isBuiltIn(protocol.Protocol) ? ' You can add it back later with “New protocol”.' : '';
    const ok = await this.askConfirm({
      title: `Remove ${protocol.Protocol}://?`,
      message: registered
        ? `${protocol.Protocol}:// is currently registered as an OS handler. Removing it unregisters the handler and deletes the protocol from SCALUS.${reAdd}`
        : `This deletes ${protocol.Protocol}:// from SCALUS.${reAdd}`,
      confirmLabel: registered ? 'Unregister & remove' : 'Remove protocol',
      variant: 'danger'
    });
    if (!ok) return;

    if (registered) {
      await this.toggleRegistration(protocol, false);
    }
    this.config.Protocols = this.config.Protocols.filter(p => p !== protocol);
    await this.saveCurrentConfig('Protocol removed.');
  }

  newApplication(): void {
    this.editorMode = 'new';
    this.editorOriginalId = null;
    this.editor = { Id: '', Name: '', Description: '', Platforms: [this.platform], Protocol: 'rdp', Parser: { ParserId: 'rdp', Options: [], TemplateContent: DEFAULT_RDP_TEMPLATE, TemplateExtension: '.rdp' }, Exec: '', Args: ['%GeneratedFile%'] };
    this.editorOpen = true; this.editorDirty = false; this.editorErrors = [];
  }
  editApplication(app: ApplicationConfig): void {
    this.editorMode = 'edit'; this.editorOriginalId = app.Id; this.editor = JSON.parse(JSON.stringify(app)); this.editorOpen = true; this.editorDirty = false; this.editorErrors = [];
  }
  async closeEditor(): Promise<void> {
    if (!this.editorDirty || await this.askConfirm({
      title: 'Discard unsaved changes?',
      message: 'Your edits to this application will be lost. This cannot be undone.',
      confirmLabel: 'Discard changes',
      variant: 'danger'
    })) {
      this.editorOpen = false;
    }
  }
  private askConfirm(opts: { title: string; message: string; confirmLabel?: string; variant?: 'primary' | 'danger' }): Promise<boolean> {
    return new Promise<boolean>(resolve => {
      this.confirm = {
        open: true,
        title: opts.title,
        message: opts.message,
        confirmLabel: opts.confirmLabel ?? 'Confirm',
        variant: opts.variant ?? 'primary',
        resolve
      };
    });
  }
  resolveConfirm(ok: boolean): void {
    const resolve = this.confirm.resolve;
    this.confirm = { ...this.confirm, open: false, resolve: undefined };
    resolve?.(ok);
  }
  markDirty(): void { this.editorDirty = true; }
  idError(): string {
    const e = this.editor;
    if (!e) return '';
    const id = (e.Id || '').trim();
    if (!id) return '';
    if (!/^[a-z0-9][a-z0-9._-]*$/i.test(id)) return 'Only letters, numbers, dots, dashes, and underscores — must start with a letter or number.';
    if (this.config.Applications.some(a => a.Id === id && a.Id !== this.editorOriginalId)) return `Another application already uses the ID “${id}”.`;
    return '';
  }
  parserChanged(parser: string): void {
    if (!this.editor) return;
    this.editor.Parser.ParserId = parser;
    this.editor.Protocol = parser === 'url' ? this.editor.Protocol : parser;
    if (parser === 'rdp') {
      if (this.editor.Parser.TemplateContent == null) {
        this.editor.Parser.TemplateContent = DEFAULT_RDP_TEMPLATE;
        this.editor.Parser.TemplateExtension = '.rdp';
      }
      this.editor.Args = this.editor.Args?.length ? this.editor.Args : ['%GeneratedFile%'];
    }
    this.markDirty();
  }
  templateEnabled(): boolean {
    return this.editor?.Parser.TemplateContent != null;
  }
  toggleTemplate(on: boolean): void {
    if (!this.editor) return;
    const parser = this.editor.Parser;
    if (on) {
      parser.TemplateContent = parser.TemplateContent ?? (parser.ParserId === 'rdp' ? DEFAULT_RDP_TEMPLATE : '');
      if (!parser.TemplateExtension) parser.TemplateExtension = parser.ParserId === 'rdp' ? '.rdp' : '';
      if (this.editor.Args == null || this.editor.Args.length === 0) this.editor.Args = ['%GeneratedFile%'];
    } else {
      delete parser.TemplateContent;
      delete parser.TemplateExtension;
      delete parser.LineEnding;
      delete parser.Encoding;
    }
    this.markDirty();
  }
  toggleRunInTerminal(on: boolean): void {
    if (!this.editor) return;
    if (on) this.editor.Parser.RunInTerminal = true;
    else delete this.editor.Parser.RunInTerminal;
    this.markDirty();
  }
  templateContentText(): string { return this.editor?.Parser.TemplateContent ?? ''; }
  setTemplateContentText(value: string): void {
    if (this.editor) { this.editor.Parser.TemplateContent = value; this.markDirty(); }
  }
  resetTemplateToDefault(): void {
    if (!this.editor) return;
    this.editor.Parser.TemplateContent = DEFAULT_RDP_TEMPLATE;
    this.editor.Parser.TemplateExtension = '.rdp';
    this.markDirty();
  }
  setLineEnding(value: TemplateLineEnding): void {
    if (this.editor) { this.editor.Parser.LineEnding = value; this.markDirty(); }
  }
  setEncoding(value: TemplateEncoding): void {
    if (this.editor) { this.editor.Parser.Encoding = value; this.markDirty(); }
  }
  togglePlatform(platform: Platform, checked: boolean): void {
    if (!this.editor) return;
    const set = new Set(this.editor.Platforms);
    checked ? set.add(platform) : set.delete(platform);
    this.editor.Platforms = Array.from(set);
    this.markDirty();
  }
  setArgsText(value: string): void { if (this.editor) { this.editor.Args = value.split('\n').map(x => x.trim()).filter(Boolean); this.markDirty(); } }
  argsText(): string { return (this.editor?.Args || []).join('\n'); }
  waitMode(): 'default' | 'exit' | 'inputidle' | 'timed' {
    const opts = this.editor?.Parser.Options ?? [];
    if (opts.some(o => /^waitforexit$/i.test(o))) return 'exit';
    if (opts.some(o => /^waitforinputidle$/i.test(o))) return 'inputidle';
    if (opts.some(o => /^wait(:\d+)?$/i.test(o))) return 'timed';
    return 'default';
  }
  setWaitMode(mode: 'default' | 'exit' | 'inputidle' | 'timed'): void {
    if (!this.editor) return;
    const seconds = this.waitSeconds();
    if (mode === 'default') this.editor.Parser.Options = [];
    else if (mode === 'exit') this.editor.Parser.Options = ['waitforexit'];
    else if (mode === 'inputidle') this.editor.Parser.Options = ['waitforinputidle'];
    else this.editor.Parser.Options = [`wait:${seconds}`];
    this.markDirty();
  }
  waitSeconds(): number {
    const opt = (this.editor?.Parser.Options ?? []).find(o => /^wait:\d+$/i.test(o));
    return opt ? Number(opt.split(':')[1]) : 10;
  }
  setWaitSeconds(value: number | string): void {
    if (!this.editor) return;
    const n = Math.max(0, Math.floor(Number(value) || 0));
    this.editor.Parser.Options = [`wait:${n}`];
    this.markDirty();
  }
  tokenGroups(): { label: string; tokens: string[]; tone: 'brand' | 'warn' | 'muted' }[] {
    const parser = this.editor?.Parser.ParserId || 'url';
    const spec = TOKEN_GROUPS[parser] || TOKEN_GROUPS['url'];
    const env = !this.templateEnabled() ? ENV_TOKENS.filter(t => t !== '%GeneratedFile%') : ENV_TOKENS;
    const groups: { label: string; tokens: string[]; tone: 'brand' | 'warn' | 'muted' }[] = [
      { label: `Connection tokens · ${parser}`, tokens: spec.connection, tone: 'brand' },
      { label: 'Generated file & environment', tokens: env, tone: 'muted' }
    ];
    if (spec.safeguard) groups.push({ label: 'Safeguard in-band tokens', tokens: SAFEGUARD_TOKENS, tone: 'warn' });
    return groups;
  }
  tokenTip(token: string): string {
    return this.tokenTips[token.replace(/%/g, '').toLowerCase()] ?? token;
  }
  insertToken(token: string, target: HTMLTextAreaElement): void {
    const start = target.selectionStart ?? target.value.length;
    const end = target.selectionEnd ?? target.value.length;
    target.value = target.value.slice(0, start) + token + target.value.slice(end);
    target.dispatchEvent(new Event('input'));
    target.focus(); target.selectionStart = target.selectionEnd = start + token.length;
  }
  commandPreview(): string {
    if (!this.editor) return '';
    const sample: Record<string, string> = { '%Host%': 'sps.example.com', '%Port%': '3389', '%User%': 'gwuser\\account~svc-admin%asset~db01%token~a1b2c3', '%GeneratedFile%': 'C:\\Users\\you\\AppData\\Local\\Temp\\scalus-8f21.rdp', '%OriginalUrl%': 'rdp://â€¦', '%RelativeUrl%': 'full address:s:sps.example.com', '%Token%': 'a1b2c3', '%TargetHost%': 'db01.internal' };
    return `${this.editor.Exec || 'client.exe'} ${(this.editor.Args || []).join(' ')}`.replace(/%[A-Za-z]+%/g, token => sample[token] || token);
  }
  async saveEditor(): Promise<void> {
    if (!this.editor) return;
    const app = JSON.parse(JSON.stringify(this.editor)) as ApplicationConfig;
    app.Name = (app.Name || '').trim();
    app.Id = (app.Id || '').trim() || this.uniqueId(app.Name || 'application');
    const localErrors = this.validateEditor(app);
    if (localErrors.length) { this.editorErrors = localErrors; return; }
    this.editorErrors = await this.bridge.validate({ ...this.config, Applications: this.upsertApplication(this.config.Applications, app, this.editorOriginalId) });
    if (this.editorErrors.length) return;
    this.config.Applications = this.upsertApplication(this.config.Applications, app, this.editorOriginalId);
    if (this.editorOriginalId && this.editorOriginalId !== app.Id) {
      this.config.Protocols.forEach(p => { if (p.AppId === this.editorOriginalId) p.AppId = app.Id; });
    }
    await this.saveCurrentConfig('Application saved.');
    this.editorOpen = false;
  }
  private validateEditor(app: ApplicationConfig): string[] {
    const errors: string[] = [];
    if (!app.Name) errors.push('Name is required.');
    if (!app.Id) errors.push('Application ID is required.');
    else if (!/^[a-z0-9][a-z0-9._-]*$/i.test(app.Id)) errors.push('Application ID must start with a letter or number and use only letters, numbers, dots, dashes, or underscores (no spaces).');
    if (app.Id && this.config.Applications.some(a => a.Id === app.Id && a.Id !== this.editorOriginalId)) {
      errors.push(`Another application already uses the ID “${app.Id}”. IDs must be unique.`);
    }
    return errors;
  }
  upsertApplication(apps: ApplicationConfig[], app: ApplicationConfig, originalId: string | null): ApplicationConfig[] {
    const next = apps.filter(a => a.Id !== (originalId ?? app.Id));
    next.push(app);
    return next.sort((a, b) => a.Name.localeCompare(b.Name));
  }
  async duplicateApplication(app: ApplicationConfig): Promise<void> {
    const copy = JSON.parse(JSON.stringify(app)) as ApplicationConfig;
    copy.Id = this.uniqueId(`${app.Id}-copy`); copy.Name = this.uniqueName(`${app.Name} (copy)`);
    this.config.Applications.push(copy);
    await this.saveCurrentConfig(`Duplicated as ${copy.Name}.`);
  }
  closeApplicationMenus(): void {
    this.applicationMenus?.forEach(menu => menu.nativeElement.open = false);
  }
  toggleApplicationMenu(menu: HTMLDetailsElement, event: MouseEvent): void {
    event.preventDefault();
    const open = !menu.open;
    this.closeApplicationMenus();
    menu.open = open;
  }
  @HostListener('document:mousedown', ['$event'])
  closeApplicationMenusOnOutsideClick(event: MouseEvent): void {
    const target = event.target as Node;
    if (!this.applicationMenus?.some(menu => menu.nativeElement.contains(target))) {
      this.closeApplicationMenus();
    }
  }
  async removeApplication(app: ApplicationConfig): Promise<void> {
    const assignedProtocols = this.config.Protocols.filter(p => p.AppId === app.Id);
    const registeredProtocols = assignedProtocols.filter(p => this.isRegistered(p.Protocol));
    const assignments = assignedProtocols.length
      ? ` It is assigned to ${assignedProtocols.map(p => `${p.Protocol}://`).join(', ')}; those assignments will be cleared.`
      : '';
    const registrations = registeredProtocols.length
      ? ` Registered handlers will also be unregistered.`
      : '';
    const ok = await this.askConfirm({
      title: `Remove ${app.Name}?`,
      message: `This permanently removes the application from SCALUS.${assignments}${registrations}`,
      confirmLabel: 'Remove application',
      variant: 'danger'
    });
    if (!ok) return;

    for (const p of assignedProtocols) {
      if (this.isRegistered(p.Protocol)) await this.toggleRegistration(p, false);
      p.AppId = null;
    }
    this.config.Applications = this.config.Applications.filter(a => a.Id !== app.Id);
    await this.saveCurrentConfig('Application removed.');
    if (this.editor?.Id === app.Id) this.editorOpen = false;
  }
  async exportApplication(app: ApplicationConfig): Promise<void> {
    const payload = { schemaVersion: 1, kind: 'application', Applications: [app] };
    await this.bridge.exportToFile(`${app.Id}.scalus-app.json`, JSON.stringify(payload, null, 2));
  }

  async exportAll(): Promise<void> {
    const payload = { schemaVersion: 1, kind: 'full', ...this.config };
    await this.bridge.exportToFile('scalus.json', JSON.stringify(payload, null, 2));
  }
  async importApplication(): Promise<void> {
    const text = await this.bridge.importFromFile(); if (!text) return;
    const raw = JSON.parse(text);
    const appsRaw = raw.kind === 'application' ? (raw.Applications ?? []) : (raw.Applications?.length === 1 ? raw.Applications : []);
    if (!appsRaw.length) { this.flash('No application found in import file.'); return; }
    for (const rawApp of appsRaw) {
      const app = normalizeApplication(rawApp); if (!app) continue;
      app.Id = this.uniqueId(app.Id); app.Name = this.uniqueName(app.Name);
      this.config.Applications.push(app);
    }
    await this.saveCurrentConfig('Application imported.');
    this.tab = 'applications';
  }
  async importReplace(): Promise<void> {
    const text = await this.bridge.importFromFile(); if (!text) return;
    const parsed = normalizeConfig(JSON.parse(text));
    if (!parsed) { this.flash('Import file is not a full SCALUS configuration.'); return; }
    if (!await this.askConfirm({
      title: 'Replace configuration?',
      message: `This overwrites everything with ${parsed.Applications.length} applications and ${parsed.Protocols.length} protocols. Your current configuration will be lost.`,
      confirmLabel: 'Replace everything',
      variant: 'danger'
    })) return;
    this.config = parsed;
    await this.saveCurrentConfig('Configuration replaced.');
    await this.reloadStatuses();
  }

  private async saveCurrentConfig(success: string): Promise<void> {
    const result = await this.bridge.saveConfig(cloneConfig(this.config));
    this.editorErrors = result.errors;
    if (!result.errors.length) this.flash(success);
  }
  private uniqueId(seed: string): string {
    const base = (seed || 'application').toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, '') || 'application';
    const ids = new Set(this.config.Applications.map(a => a.Id));
    let id = base, i = 2;
    while (ids.has(id)) id = `${base}-${i++}`;
    return id;
  }
  private uniqueName(seed: string): string {
    const names = new Set(this.config.Applications.map(a => a.Name));
    let name = seed, i = 2;
    while (names.has(name)) name = `${seed} ${i++}`;
    return name;
  }
  private flash(message: string): void { this.message = message; window.setTimeout(() => { if (this.message === message) this.message = ''; }, 2600); }

  copiedKey: string | null = null;
  private copiedTimer: ReturnType<typeof setTimeout> | null = null;

  // Collapse any line-ending flavor to a single '\n'. Windows rdpsign writes '\r\r\n'
  // between lines; treating '\r+\n' and lone '\r' as one break keeps genuine blank
  // lines intact while preventing double-spaced display and paste.
  private normalizeEol(value: string | null | undefined): string | null {
    if (value == null) { return value ?? null; }
    return String(value).replace(/\r+\n|\r/g, '\n');
  }

  async copyField(value: string | null | undefined, key: string): Promise<void> {
    if (value == null || value === '') { return; }
    const text = this.normalizeEol(value) ?? '';
    let ok = false;
    try {
      if (navigator.clipboard && typeof navigator.clipboard.writeText === 'function') {
        await navigator.clipboard.writeText(text);
        ok = true;
      }
    } catch { ok = false; }
    if (!ok) { ok = this.legacyCopy(text); }
    if (ok) {
      this.copiedKey = key;
      if (this.copiedTimer) { clearTimeout(this.copiedTimer); }
      this.copiedTimer = setTimeout(() => { this.copiedKey = null; this.copiedTimer = null; }, 1300);
    } else {
      this.flash('Copy failed — select the text and copy manually.');
    }
  }

  private legacyCopy(text: string): boolean {
    try {
      // Provide a selection so execCommand('copy') dispatches a copy event, then
      // override the clipboard payload with the exact text. Copying a textarea's
      // value directly would re-expand '\n' to the platform EOL ('\r\n' on Windows),
      // which pastes as double-spaced lines; setData writes the string verbatim.
      const ta = document.createElement('textarea');
      ta.value = text;
      ta.setAttribute('readonly', '');
      ta.style.position = 'fixed';
      ta.style.top = '-1000px';
      ta.style.opacity = '0';
      document.body.appendChild(ta);
      ta.select();
      const onCopy = (e: ClipboardEvent): void => {
        if (e.clipboardData) {
          e.clipboardData.setData('text/plain', text);
          e.preventDefault();
        }
      };
      document.addEventListener('copy', onCopy);
      const done = document.execCommand('copy');
      document.removeEventListener('copy', onCopy);
      document.body.removeChild(ta);
      return done;
    } catch { return false; }
  }

  iconPath(name: string): string {
    const paths: Record<string, string> = {
      eye: 'M247.31 124.76c-.35-.79-8.82-19.58-27.65-38.41C194.57 61.26 162.88 48 128 48S61.43 61.26 36.34 86.35C17.51 105.18 9 124 8.69 124.76a8 8 0 0 0 0 6.5c.35.79 8.82 19.57 27.65 38.4C61.43 194.74 93.12 208 128 208s66.57-13.26 91.66-38.34c18.83-18.83 27.3-37.61 27.65-38.4a8 8 0 0 0 0-6.5ZM128 168a40 40 0 1 1 40-40 40 40 0 0 1-40 40Z',
      grid: 'M104 40H56a16 16 0 0 0-16 16v48a16 16 0 0 0 16 16h48a16 16 0 0 0 16-16V56a16 16 0 0 0-16-16Zm96 0h-48a16 16 0 0 0-16 16v48a16 16 0 0 0 16 16h48a16 16 0 0 0 16-16V56a16 16 0 0 0-16-16Zm-96 96H56a16 16 0 0 0-16 16v48a16 16 0 0 0 16 16h48a16 16 0 0 0 16-16v-48a16 16 0 0 0-16-16Zm96 0h-48a16 16 0 0 0-16 16v48a16 16 0 0 0 16 16h48a16 16 0 0 0 16-16v-48a16 16 0 0 0-16-16Z',
      download: 'M224 152v56a16 16 0 0 1-16 16H48a16 16 0 0 1-16-16v-56a8 8 0 0 1 16 0v56h160v-56a8 8 0 0 1 16 0Zm-101.66 5.66a8 8 0 0 0 11.32 0l40-40a8 8 0 0 0-11.32-11.32L136 132.69V40a8 8 0 0 0-16 0v92.69l-26.34-26.35a8 8 0 1 0-11.32 11.32Z',
      info: 'M128 24a104 104 0 1 0 104 104A104.11 104.11 0 0 0 128 24Zm-4 48a12 12 0 1 1-12 12 12 12 0 0 1 12-12Zm12 112a16 16 0 0 1-16-16v-40a8 8 0 0 1 0-16 16 16 0 0 1 16 16v40a8 8 0 0 1 0 16Z',
      monitor: 'M208 40H48a24 24 0 0 0-24 24v112a24 24 0 0 0 24 24h64v16H88a8 8 0 0 0 0 16h80a8 8 0 0 0 0-16h-24v-16h64a24 24 0 0 0 24-24V64a24 24 0 0 0-24-24Zm8 136a8 8 0 0 1-8 8H48a8 8 0 0 1-8-8V64a8 8 0 0 1 8-8h160a8 8 0 0 1 8 8Z',
      terminal: 'M216 40H40a16 16 0 0 0-16 16v144a16 16 0 0 0 16 16h176a16 16 0 0 0 16-16V56a16 16 0 0 0-16-16ZM104 158.4l-40 30a8 8 0 0 1-9.6-12.8L86.67 152 54.4 127.9a8 8 0 1 1 9.6-12.8l40 30a8 8 0 0 1 0 12.8ZM192 168h-56a8 8 0 0 1 0-16h56a8 8 0 0 1 0 16Z',
      window: 'M216 40H40a16 16 0 0 0-16 16v144a16 16 0 0 0 16 16h176a16 16 0 0 0 16-16V56a16 16 0 0 0-16-16Zm0 160H40V56h176Z',
      link: 'M137.54 186.36a8 8 0 0 1 0 11.31l-9.94 9.94a56 56 0 0 1-79.22-79.22l24.12-24.12a56 56 0 0 1 76.81-2.28 8 8 0 1 1-10.64 12 40 40 0 0 0-54.85 1.63L59.7 139.72a40 40 0 0 0 56.58 56.58l9.94-9.94a8 8 0 0 1 11.32 0Zm70.08-138a56.08 56.08 0 0 0-79.22 0l-9.94 9.94a8 8 0 0 0 11.32 11.32l9.94-9.94a40 40 0 0 1 56.58 56.58l-24.12 24.12a40 40 0 0 1-54.85 1.63 8 8 0 1 0-10.64 12 56 56 0 0 0 76.81-2.28l24.12-24.12a56.08 56.08 0 0 0 0-79.22Z',
      square: 'M200 40H56a16 16 0 0 0-16 16v144a16 16 0 0 0 16 16h144a16 16 0 0 0 16-16V56a16 16 0 0 0-16-16Z',
      settings: 'M128 80a48 48 0 1 0 48 48 48.05 48.05 0 0 0-48-48Zm0 80a32 32 0 1 1 32-32 32 32 0 0 1-32 32Zm88-29.84q.06-2.16 0-4.32l14.92-18.64a8 8 0 0 0 1.48-7.06 107.21 107.21 0 0 0-10.88-26.25 8 8 0 0 0-6-3.93l-23.72-2.64q-1.48-1.56-3-3L181 34.48a8 8 0 0 0-3.94-6 107.71 107.71 0 0 0-26.25-10.87 8 8 0 0 0-7.06 1.49L125.16 24h-4.32L102.2 9.11a8 8 0 0 0-7.06-1.48 107.6 107.6 0 0 0-26.25 10.88 8 8 0 0 0-3.93 6l-2.64 23.76q-1.56 1.49-3 3L34.48 75a8 8 0 0 0-6 3.94 107.71 107.71 0 0 0-10.87 26.25 8 8 0 0 0 1.49 7.06L24 130.84v4.32L9.11 153.8a8 8 0 0 0-1.48 7.06 107.21 107.21 0 0 0 10.88 26.25 8 8 0 0 0 6 3.93l23.72 2.64q1.49 1.56 3 3L75 221.52a8 8 0 0 0 3.94 6 107.71 107.71 0 0 0 26.25 10.87 8 8 0 0 0 7.06-1.49L130.84 232h4.32l18.64 14.92a8 8 0 0 0 7.06 1.48 107.21 107.21 0 0 0 26.25-10.88 8 8 0 0 0 3.93-6l2.64-23.72q1.56-1.48 3-3L221.52 181a8 8 0 0 0 6-3.94 107.71 107.71 0 0 0 10.87-26.25 8 8 0 0 0-1.49-7.06Zm-16.1-6.5a73.93 73.93 0 0 1 0 8.68 8 8 0 0 0 1.74 5.48l14.19 17.73a91.57 91.57 0 0 1-6.23 15l-22.6 2.56a8 8 0 0 0-5.1 2.64 74.11 74.11 0 0 1-6.14 6.14 8 8 0 0 0-2.64 5.1l-2.51 22.58a91.32 91.32 0 0 1-15 6.23l-17.74-14.19a8 8 0 0 0-5-1.75h-.48a73.93 73.93 0 0 1-8.68 0 8 8 0 0 0-5.48 1.74l-17.78 14.2a91.57 91.57 0 0 1-15-6.23L82.89 187a8 8 0 0 0-2.64-5.1 74.11 74.11 0 0 1-6.14-6.14 8 8 0 0 0-5.1-2.64l-22.58-2.51a91.32 91.32 0 0 1-6.23-15l14.19-17.74a8 8 0 0 0 1.74-5.48 73.93 73.93 0 0 1 0-8.68 8 8 0 0 0-1.74-5.48L40.19 100.9a91.57 91.57 0 0 1 6.23-15L69 83.11a8 8 0 0 0 5.1-2.64 74.11 74.11 0 0 1 6.14-6.14 8 8 0 0 0 2.64-5.1l2.51-22.58a91.32 91.32 0 0 1 15-6.23l17.74 14.19a8 8 0 0 0 5.48 1.74 73.93 73.93 0 0 1 8.68 0 8 8 0 0 0 5.48-1.74l17.74-14.19a91.57 91.57 0 0 1 15 6.23L187 69a8 8 0 0 0 2.64 5.1 74.11 74.11 0 0 1 6.14 6.14 8 8 0 0 0 5.1 2.64l22.58 2.51a91.32 91.32 0 0 1 6.23 15l-14.19 17.74a8 8 0 0 0-1.74 5.48Z',
      warning: 'M236.8 188.09 149.35 36.22a24.76 24.76 0 0 0-42.7 0L19.2 188.09a23.51 23.51 0 0 0 0 23.72A24.35 24.35 0 0 0 40.55 224h174.9a24.35 24.35 0 0 0 21.35-12.19 23.51 23.51 0 0 0 0-23.72ZM120 104a8 8 0 0 1 16 0v40a8 8 0 0 1-16 0Zm8 88a12 12 0 1 1 12-12 12 12 0 0 1-12 12Z',
      list: 'M80 64a8 8 0 0 1 8-8h128a8 8 0 0 1 0 16H88a8 8 0 0 1-8-8Zm136 56H88a8 8 0 0 0 0 16h128a8 8 0 0 1 0-16Zm0 64H88a8 8 0 0 0 0 16h128a8 8 0 0 0 0-16ZM44 52a12 12 0 1 0 12 12 12 12 0 0 0-12-12Zm0 64a12 12 0 1 0 12 12 12 12 0 0 0-12-12Zm0 64a12 12 0 1 0 12 12 12 12       0 0 0-12-12Z',
            copy: 'M216 32H88a8 8 0 0 0-8 8v40H40a8 8 0 0 0-8 8v128a8 8 0 0 0 8 8h128a8 8 0 0 0 8-8v-40h40a8 8 0 0 0 8-8V40a8 8 0 0 0-8-8Zm-56 176H48V96h112Zm48-48h-32V88a8 8 0 0 0-8-8H96V48h112Z',
            check: 'M229.66 77.66l-128 128a8 8 0 0 1-11.32 0l-56-56a8 8 0 0 1 11.32-11.32L96 188.69 218.34 66.34a8 8 0 0 1 11.32 11.32Z'
          };
          return paths[name] || paths['link'];
  }
}
