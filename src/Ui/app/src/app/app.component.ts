import { mapToMapExpression } from '@angular/compiler/src/render3/util';
import { Component, OnInit } from '@angular/core';
import { EuiSidesheetService, EuiSidesheetConfig } from '@elemental-ui/core';
import { ApiService, ScalusConfig, ApplicationConfig, ProtocolMapping, ProtocolMappingDisplay } from './api/api.service';
import { ScalusApplicationsComponent } from './applications/scalus-applications.component';
import { saveAs } from 'file-saver';
import * as $ from 'jquery';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent implements OnInit {

  title = 'SCALUS';
  state = 'loading';

  protocolName: string = '';

  config: ScalusConfig;

  protocols: ProtocolMappingDisplay[];
  
  //selectedRdpAppId: string = "";
  //rdpApps: ApplicationConfig[];

  //selectedSshAppId: string = "";
  //sshApps: ApplicationConfig[];

  //selectedTelnetAppId: string = "";
  //telnetApps: ApplicationConfig[];

  constructor(
    private apiService: ApiService,
    private sidesheetService: EuiSidesheetService) {
  }

  ngOnInit(): void {
    this.apiService.getConfig().subscribe(x => {
      this.loadConfig(x);
      this.state = 'loaded';
    }, error => {
      this.handleError(error, "Failed to load configuration");
    });
  }

  loadConfig(config:ScalusConfig)
  {
    this.config = config;
    this.protocols = this.getProtocols(this.config);
    //this.rdpApps = this.getApps(this.config, 'rdp');
    //this.sshApps = this.getApps(this.config, 'ssh');
    //this.selectedRdpAppId = this.getMappedProtocol(this.config, "rdp")?.id;
    //this.selectedSshAppId = this.getMappedProtocol(this.config, "ssh")?.id;
  }

  getProtocols(config: ScalusConfig) {
    var protocols: ProtocolMappingDisplay[] = new Array();
    
    this.config.protocols.forEach(pm => {
      var protocolMapping: ProtocolMappingDisplay = <ProtocolMappingDisplay>{};
      protocolMapping.id = pm.protocol;
      protocolMapping.mapping = pm;
      protocolMapping.configs = new Array();
      config.applications.forEach(ac => {
        if (ac.protocol == pm.protocol) {
          protocolMapping.configs.push(ac);
        }
      });
      protocols.push(protocolMapping);
    });

    return protocols;
  }

  // getMappedProtocol(config:SuluConfig, protocol:string) : ApplicationConfig {
  //   var appId = ""
  //   config.protocols.forEach(element => {
  //   if(element.protocol == protocol) {
  //       appId = element.appId;
  //     }
  //   });
  //   var protocolConfig: ApplicationConfig;
  //   if(appId) {
  //     config.applications.forEach(element => {
  //       if(element.id == appId) {
  //         protocolConfig = element;
  //       }
  //     })
  //   }
  //   return protocolConfig;
  // }

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

  // getApps(config:SuluConfig, protocol:string) : ApplicationConfig[] {
  //   var result = new Array();
  //   config.applications.forEach(x => {
  //     if (x.protocol == protocol) {
  //       result.push(x);
  //     }
  //   });
  //   return result;
  // }

  getApplicationConfig(id:string) : ApplicationConfig {
    if(this.config == null) {
      return null;
    }
    return this.config.applications.filter(x => x.id == id)[0];
  }

  handleError(error:any, msg:string){
    alert("ERROR: " + msg + " (" + error + ")");
  }

  canAddProtocol() {
    return this.protocolName.length === 0; 
  }

  addProtocol() {
    var mapping:ProtocolMapping = <ProtocolMapping>{};
    mapping.protocol = this.protocolName;
    this.config.protocols.push(mapping);
    this.apiService.setConfig(this.config).subscribe(
      x => {
        this.protocolName = "";
        this.loadConfig(this.config);
      }, 
      error => {
        this.handleError(error, "Failed to save configuration");
    });
  }

  deleteProtocol(id: string) {
    this.config.protocols.forEach((element, index) => {
      if(element.protocol == id) {
        this.config.protocols.splice(index,1);
      }
    });

    this.apiService.setConfig(this.config).subscribe(
      x => {
        this.protocolName = "";
        this.loadConfig(this.config);
      }, 
      error => {
        this.handleError(error, "Failed to save configuration");
    });
  }

  manageApplications() {
    const config: EuiSidesheetConfig = {
      title: 'Applications',
      testId: 'applications-sidesheet',
      data: this.config
    };
    var ref = this.sidesheetService.open(ScalusApplicationsComponent, config);
    ref.afterClosed().subscribe(
      () => {
        this.loadConfig(this.config);
      });
  }

  import() {
    var fileInput = $('<input type="file"/>');
    fileInput.on('change', () => {
      var file = fileInput.prop('files')[0];
      var fileReader: FileReader = new FileReader();
      fileReader.onloadend = (e) => {
        var config = JSON.parse(fileReader.result as string);
        this.apiService.setConfig(config).subscribe(
          x => {
            this.loadConfig(config);
          }, 
          error => {
            this.handleError(error, "Failed to save configuration");
        });
      }
      fileReader.readAsText(file);
    });
    fileInput.trigger("click");
  }

  export() {
    var configString = JSON.stringify(this.config);
    var blob = new Blob([configString], {type : 'application/json'});
    saveAs(blob, 'scalus.json');
  }

}
