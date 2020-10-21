import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';

export interface ParserConfig {
  type: string;
  id: string;
}

export interface ProtocolMapping {
  protocol: string;
  id: string;
}

export interface ProtocolConfig {
  id: string;
  description: string;
  protocol: string;
  parser: ParserConfig;
  exec: string; 
  args: string;
}

export interface SuluConfig {
  map: ProtocolMapping[];
  protocols: ProtocolConfig[];
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
