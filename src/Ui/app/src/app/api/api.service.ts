import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';


export interface ScalusConfig {
  protocols: ProtocolMapping[];
  applications: ApplicationConfig[];
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
  Windows = 0,
  Linux = 1,
  Mac = 2
}

export interface ApplicationConfig {
  id: string;
  name: string;
  description: string;
  platforms: Platform[];
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

@Injectable({
  providedIn: 'root'
})
export class ApiService {

  constructor(private http: HttpClient) { }

  private configUrl = 'api/Configuration'
  private registrationsUrl = 'api/Configuration/Registrations';
  private registerUrl = 'api/Configuration/Register';
  private unregisterUrl = 'api/Configuration/UnRegister';

  getConfig() {
    return this.http.get<ScalusConfig>(this.configUrl);
  }

  setConfig(config:ScalusConfig){
    return this.http.put<ScalusConfig>(this.configUrl, config);
  }

  getRegistrations(){
    return this.http.get<Array<string>>(this.registrationsUrl);
  }

  register(protocol:ProtocolMapping) {
    return this.http.put<ProtocolMapping>(this.registerUrl, protocol);
  }

  unregister(protocol:ProtocolMapping) {
    return this.http.put<ProtocolMapping>(this.unregisterUrl, protocol);
  }
}
