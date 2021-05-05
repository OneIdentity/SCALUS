import { Component, OnInit } from '@angular/core';
import { EuiSidesheetService, EuiSidesheetConfig } from '@elemental-ui/core';
import { MatDialog } from '@angular/material/dialog';
import { ApiService, ScalusConfig, ApplicationConfig, ProtocolMapping, ProtocolMappingDisplay } from './api/api.service';
import { ScalusApplicationsComponent } from './applications/scalus-applications.component';
import { ErrorDialogComponent } from './error/error-dialog.component';
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

  constructor(
    private apiService: ApiService,
    private sidesheetService: EuiSidesheetService,
    private matDialog: MatDialog,) {
  }

  ngOnInit(): void {
    this.apiService.getConfig().subscribe(x => {
      this.loadConfig(x);
      this.state = 'loaded';
    }, error => {
      this.showError(error, "Failed to load configuration");
    });
  }

  loadConfig(config:ScalusConfig)
  {
    this.config = config;
    this.protocols = this.getProtocols(this.config);
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

  setApp(protocol:string, appId:string){
    this.config.protocols.forEach(x =>{
      if(x.protocol == protocol) {
        x.appId = appId;
      }
    });
    this.apiService.setConfig(this.config).subscribe(x => {}, 
      error => {
        this.showError(error, "Failed to save configuration");
    });
  }

  getSelectedDescription(appId:string) {
    return this.getApplicationConfig(appId)?.description;
  }

  getApplicationConfig(id:string) : ApplicationConfig {
    if(this.config == null) {
      return null;
    }
    return this.config.applications.filter(x => x.id == id)[0];
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
        this.showError(error, "Failed to save configuration");
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
        this.showError(error, "Failed to save configuration");
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
        try {
          var fileResults = fileReader.result as string;
          var config = JSON.parse(fileResults);
          this.apiService.setConfig(config).subscribe(
            x => {
              this.loadConfig(config);
            }, 
            error => {
              this.showError(error, "Failed to save configuration");
          });
        } 
        catch(error) {
          this.showError(error, "Invalid configuration file");
        }
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

  showError(error: any, msg: string) {
    var errorMessage = msg + " (" + error + ")";
    this.matDialog.open(ErrorDialogComponent, {
      data: errorMessage
    });
  }

}
