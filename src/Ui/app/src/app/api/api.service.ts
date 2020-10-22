import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';

export interface ParserConfig {
  id: string;
  options: string[];
}

export interface ProtocolMapping {
  protocol: string;
  appId: string;
}

export interface ApplicationConfig {
  id: string;
  name: string;
  description: string;
  platforms: string[];
  protocol: string;
  parser: ParserConfig;
  exec: string; 
  args: string;
}

export interface SuluConfig {
  protocols: ProtocolMapping[];
  applications: ApplicationConfig[];
}


@Injectable({
  providedIn: 'root'
})
export class ApiService {

  constructor(private http: HttpClient) { }

  private configUrl = 'api/Configuration'

  getConfig() {
    return this.http.get<SuluConfig>(this.configUrl);
  }

  setConfig(config:SuluConfig){
    return this.http.put<SuluConfig>(this.configUrl, config);
  }
}
