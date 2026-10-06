import { Injectable } from '@angular/core';
import { Capabilities, LaunchRecord, Platform, RegistrationScope, RegistrationStatus, RegistrationWriteResult, SaveConfigResult, ScalusBridge, ScalusConfig, StartupAction, TerminalOption } from './scalus-bridge';

type PhotinoExternal = {
  sendMessage(message: string): void;
  receiveMessage?: (handler: (message: string) => void) => void;
};

type Pending = { resolve: (value: any) => void; reject: (reason?: any) => void };

@Injectable()
export class PhotinoBridge implements ScalusBridge {
  private pending = new Map<string, Pending>();

  constructor() {
    const external = (window as any).external as PhotinoExternal | undefined;
    if (external?.receiveMessage) {
      external.receiveMessage((message: string) => this.receive(message));
    }
  }

  private call<T>(method: string, ...args: unknown[]): Promise<T> {
    const id = `${Date.now()}-${Math.random().toString(16).slice(2)}`;
    const external = (window as any).external as PhotinoExternal | undefined;
    if (!external?.sendMessage) return Promise.reject(new Error('Photino bridge is unavailable.'));
    return new Promise<T>((resolve, reject) => {
      this.pending.set(id, { resolve, reject });
      external.sendMessage(JSON.stringify({ id, method, args }));
    });
  }

  private receive(message: string): void {
    let payload: any;
    try { payload = JSON.parse(message); } catch { return; }
    const item = this.pending.get(payload.id);
    if (!item) return;
    this.pending.delete(payload.id);
    if (payload.ok) item.resolve(payload.result);
    else item.reject(new Error(payload.error || 'SCALUS host call failed.'));
  }

  getConfig(): Promise<ScalusConfig> { return this.call('getConfig'); }
  saveConfig(config: ScalusConfig): Promise<SaveConfigResult> { return this.call('saveConfig', config); }
  validate(config: ScalusConfig): Promise<string[]> { return this.call('validate', config); }
  getRegistrations(): Promise<string[]> { return this.call('getRegistrations'); }
  getRegistrationStatus(scope: RegistrationScope): Promise<RegistrationStatus[]> { return this.call('getRegistrationStatus', scope); }
  register(protocol: string, scope: RegistrationScope): Promise<RegistrationWriteResult> { return this.call('register', protocol, scope); }
  unregister(protocol: string, scope: RegistrationScope): Promise<RegistrationWriteResult> { return this.call('unregister', protocol, scope); }
  getCapabilities(): Promise<Capabilities> { return this.call('getCapabilities'); }
  getTokens(): Promise<Record<string, string>> { return this.call('getTokens'); }
  getApplicationDescriptions(): Promise<Record<string, string>> { return this.call('getApplicationDescriptions'); }
  getParsers(): Promise<string[]> { return this.call('getParsers'); }
  getTerminals(): Promise<TerminalOption[]> { return this.call('getTerminals'); }
  getInfo(): Promise<string> { return this.call('getInfo'); }
  getVersion(): Promise<string> { return this.call('getVersion'); }
  getStartupAction(): Promise<StartupAction> { return this.call('getStartupAction'); }
  getLaunchRecords(max?: number): Promise<LaunchRecord[]> { return this.call('getLaunchRecords', max); }
  getLaunchFile(fileName: string): Promise<string | null> { return this.call('getLaunchFile', fileName); }
  openLogsFolder(): Promise<boolean> { return this.call('openLogsFolder'); }
  exportToFile(defaultName: string, contents: string): Promise<boolean> { return this.call('exportToFile', defaultName, contents); }
  importFromFile(): Promise<string | null> { return this.call('importFromFile'); }
  getPlatform(): Promise<Platform> { return this.call('getPlatform'); }
}
