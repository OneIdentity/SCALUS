import { mapToMapExpression } from '@angular/compiler/src/render3/util';
import { Component, OnInit } from '@angular/core';
import { ApiService, SuluConfig, ApplicationConfig } from './api/api.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent implements OnInit {

  title = 'Sulu';
  state = 'loading';
  config: SuluConfig;
  selectedRdpAppId: string = "";
  rdpApps: ApplicationConfig[];

  selectedSshAppId: string = "";
  sshApps: ApplicationConfig[];

  selectedTelnetAppId: string = "";
  telnetApps: ApplicationConfig[];

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

  getMappedProtocol(config:SuluConfig, protocol:string) : ApplicationConfig {
    var appId = ""
    config.protocols.forEach(element => {
    if(element.protocol == protocol) {
        appId = element.appId;
      }
    });
    var protocolConfig: ApplicationConfig;
    if(appId) {
      config.applications.forEach(element => {
        if(element.id == appId) {
          protocolConfig = element;
        }
      })
    }
    return protocolConfig;
  }

  setApp(protocol:string, appId:string){
    this.config.protocols.forEach(x =>{
      if(x.protocol == protocol) {
        x.appId = appId;
      }
    });
    this.apiService.setConfig(this.config).subscribe(x => {}, 
      error => {
        this.handleError(error, "Failed to save configuration");
    });
  }

  getSelectedDescription(appId:string) {
    return this.getApplicationConfig(appId)?.description;
  }

  getApps(config:SuluConfig, protocol:string) : ApplicationConfig[] {
    var result = new Array();
    config.applications.forEach(x => {
      if (x.protocol == protocol) {
        result.push(x);
      }
    });
    return result;
  }

  getApplicationConfig(id:string) : ApplicationConfig {
    if(this.config == null) {
      return null;
    }
    return this.config.applications.filter(x => x.id == id)[0];
  }

  handleError(error:any, msg:string){
    alert("ERROR: " + msg + " (" + error + ")");
  }
}
