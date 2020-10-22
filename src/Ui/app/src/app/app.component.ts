import { mapToMapExpression } from '@angular/compiler/src/render3/util';
import { Component, OnInit } from '@angular/core';
import { ApiService, SuluConfig, ProtocolConfig } from './api/api.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent implements OnInit {

  title = 'Sulu';
  state = 'loading';
  config: SuluConfig;
  selectedRdpAppId: string;
  rdpApps: ProtocolConfig[];

  selectedSshAppId: string;
  sshApps: ProtocolConfig[];

  selectedTelnetAppId: string;
  telnetApps: ProtocolConfig[];

  constructor(private apiService: ApiService) {
  }

  ngOnInit(): void {
    this.apiService.getConfig().subscribe(x => {
      this.config = x;
      this.rdpApps = this.getApps(this.config, 'rdp');
      this.sshApps = this.getApps(this.config, 'ssh');
      this.selectedRdpAppId = this.getMappedProtocol(this.config, "rdp")?.id;
      this.selectedSshAppId = this.getMappedProtocol(this.config, "ssh")?.id;
      this.state = 'loaded';
    }, error => {
      this.handleError(error, "Failed to load configuration");
    });
  }

  getMappedProtocol(config:SuluConfig, protocol:string) : ProtocolConfig {
    var protocolId = ""
    config.map.forEach(element => {
    if(element.protocol == protocol) {
        protocolId = element.id;
      }
    });
    var protocolConfig: ProtocolConfig;
    if(protocolId) {
      config.protocols.forEach(element => {
        if(element.id == protocolId) {
          protocolConfig = element;
        }
      })
    }
    return protocolConfig;
  }

  setApp(protocol:string, appId:string){
    this.config.map.forEach(x =>{
      if(x.protocol == protocol) {
        x.id = appId;
      }
    });
    this.apiService.setConfig(this.config).subscribe(x => {}, 
      error => {
        this.handleError(error, "Failed to save configuration");
    });
  }

  getSelectedRdpDescription() {
    return this.getProtocolConfig(this.selectedRdpAppId).description;
  }

  getApps(config:SuluConfig, protocol:string) : ProtocolConfig[] {
    var result = new Array();
    config.protocols.forEach(x => {
      if (x.protocol == protocol) {
        result.push(x);
      }
    });
    return result;
  }

  getProtocolConfig(id:string) {
    return this.config.protocols.filter(x => x.id == id)[0];
  }

  handleError(error:any, msg:string){
    alert("ERROR: " + msg + " (" + error + ")");
  }
}
