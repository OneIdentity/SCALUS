import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';

export enum Edition {
  Community,
  Supported
}

export interface ScalusConfig {
  protocols: ProtocolMapping[];
  applications: ApplicationConfig[];
}

export interface ScalusServerConfig extends ScalusConfig {
  edition: string;
}

export interface ProtocolMapping {
  protocol: string;
  appId: string;
}

export interface ProtocolMappingDisplay {
  id: string;
  mapping: ProtocolMapping;
  configs: ApplicationConfig[];
  registered: boolean;
}

export enum Platform {
  Windows,
  Linux,
  Mac
}

export interface ApplicationConfig {
  id: string;
  name: string;
  description: string;
  platforms: string[];
  protocol: string;
  parser: ParserConfig;
  exec: string; 
  args: string[];
}

export interface ApplicationConfigDisplay {
  id: string;
  name: string;
  description: string;
  platforms: string;
  protocol: string;
  parser: ParserConfigDisplay;
  exec: string; 
  args: string;
}

export interface ParserConfig {
  parserId: string;
  options: string[];
  useDefaultTemplate: boolean;
  useTemplateFile: string;
  postProcessingExec: string;
  postProcessingArgs: string[];
}

export interface ParserConfigDisplay {
  parserId: string;
  options: string;
  useDefaultTemplate: boolean;
  useTemplateFile: string;
  postProcessingExec: string;
  postProcessingArgs: string;
}

export interface TokenDisplay {
  name: string;
  description: string;
}

@Injectable({
  providedIn: 'root'
})
export class ApiService {

  constructor(private http: HttpClient) { }

  private configUrl = 'api/Configuration'
  private registrationsUrl = this.configUrl.concat('/Registrations');
  private registerUrl = this.configUrl.concat('/Register');
  private unregisterUrl = this.configUrl.concat('/UnRegister');
  private tokensUrl = this.configUrl.concat('/Tokens');
  private applicationDescriptionsUrl = this.configUrl.concat('/ApplicationDescriptions');
  private infoUrl = this.configUrl.concat('/Info');
  private validateUrl = this.configUrl.concat('/Validate');

  getConfig() {
    return this.http.get<ScalusServerConfig>(this.configUrl);
  }

  setConfig(config:ScalusConfig){
    return this.http.put<ScalusConfig>(this.configUrl, config);
  }

  getRegistrations() {
    return this.http.get<Array<string>>(this.registrationsUrl);
  }

  register(protocol:ProtocolMapping) {
    return this.http.put<ProtocolMapping>(this.registerUrl, protocol);
  }

  unregister(protocol:ProtocolMapping) {
    return this.http.put<ProtocolMapping>(this.unregisterUrl, protocol);
  }

  getTokens() {
    return this.http.get<object>(this.tokensUrl);
  }

  getapplicationDescriptions() {
    return this.http.get<object>(this.applicationDescriptionsUrl);
  }

  getInfo() {
    return this.http.get<string>(this.infoUrl);
  }

  validateConfig(config:ScalusConfig){
    return this.http.put<Array<string>>(this.validateUrl, config);
  }
}
