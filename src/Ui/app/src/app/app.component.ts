import { Component, OnInit } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { ApiService, ScalusConfig, ApplicationConfig, ProtocolMapping, ProtocolMappingDisplay, Platform } from './api/api.service';
import { ScalusApplicationsComponent } from './applications/scalus-applications.component';
import { ErrorDialogComponent } from './error/error-dialog.component';
import { ScalusHelpDialogComponent } from './help/scalus-help-dialog.component';
import { saveAs } from 'file-saver';
import { forkJoin } from 'rxjs';
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
  registrations:Array<string>;
  applicationDescriptions: object;
  tokens: object;

  configurationFile: string = '';
  logFile: string = '';

  protocols: ProtocolMappingDisplay[];

  constructor(
    private apiService: ApiService,
    private matDialog: MatDialog,) {
  }

  ngOnInit(): void {
    forkJoin([
    this.apiService.getConfig(),
    this.apiService.getRegistrations(),
    this.apiService.getapplicationDescriptions(),
    this.apiService.getTokens()])
    .subscribe(x => {
      this.loadConfig(x[0], x[1]);
      
      this.applicationDescriptions = x[2];
      this.tokens = x[3];

      this.state = 'loaded';
      console.log(`Loaded SCALUS ${this.config.edition} edition`);
    }, error => {
      this.showError("Failed to load configuration");
    });
  }

  loadConfig(config:ScalusConfig, registrations:Array<string>)
  {
    this.config = config;
    this.registrations = registrations;

    this.protocols = this.getProtocols(this.config);
    
    this.registrations.forEach(reg => {
      this.protocols.forEach(p => {
        if (p.id == reg){
          p.registered = true;
        }
      });
    });
  }

  getProtocols(config: ScalusConfig) {
    var protocols: ProtocolMappingDisplay[] = new Array();
    
    this.config.protocols.forEach(pm => {
      var protocolMapping: ProtocolMappingDisplay = <ProtocolMappingDisplay>{};
      protocolMapping.id = pm.protocol;
      protocolMapping.mapping = pm;
      protocolMapping.configs = new Array();
      config.applications.forEach(ac => {
        if (ac.protocol === pm.protocol && this.canAddApplication(ac)) {
          protocolMapping.configs.push(ac);
        }
      });
      protocols.push(protocolMapping);
    });

    return protocols;
  }

  canAddApplication(app: ApplicationConfig) : boolean
  {
    var canAdd = false;
    var platform = this.getOsPlatform();
    app.platforms.forEach(p => {
      if (p === platform)
      {
        canAdd = true;
      }
    });
    return canAdd;
  }

  getOsPlatform() : Platform {
    var platform = window.navigator.platform;

    if (platform.startsWith("Win"))
    {
      return Platform["Windows"];
    }
    else if (platform.startsWith("Mac"))
    {
      return Platform["Mac"];
    }
    else if (platform.startsWith("Linux"))
    {
      return Platform["Linux"];
    }
  }

  setApp(protocol:string, appId:string){
    this.config.protocols.forEach(x =>{
      if(x.protocol == protocol) {
        x.appId = appId;
      }
    });
    this.apiService.setConfig(this.config).subscribe(x => {}, 
      error => {
        this.showError("Failed to save configuration");
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
        this.loadConfig(this.config, this.registrations);
      }, 
      error => {
        this.showError("Failed to save configuration");
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
        this.loadConfig(this.config, this.registrations);
      }, 
      error => {
        this.showError("Failed to save configuration");
    });
  }

  register(id: string) {
    var mapping:ProtocolMapping;
    this.config.protocols.forEach(p => {
      if (p.protocol === id)
      {
        mapping = p;
      }
    });
    this.apiService.register(mapping).subscribe(
      x => {
        this.apiService.getRegistrations().subscribe(x =>{
          this.registrations = x;
          this.loadConfig(this.config, this.registrations);
        },
        error => {
          this.showError("Failed to register protocol");
        });
      }, 
      error => {
        this.showError("Failed to register protocol");
    });
  }

  unregister(id: string) {
    var mapping:ProtocolMapping;
    this.config.protocols.forEach(p => {
      if (p.protocol === id)
      {
        mapping = p;
      }
    });
    this.apiService.unregister(mapping).subscribe(
      x => {
        this.apiService.getRegistrations().subscribe(x =>{
          this.registrations = x;
          this.loadConfig(this.config, this.registrations);
        },
        error => {
          this.showError("Failed to unregister protocol");
        });
      }, 
      error => {
        this.showError("Failed to unregister protocol");
    });
  }

  manageApplications() {
    var applicationData = {
      config: this.config,
      descriptions: this.applicationDescriptions,
      tokens: this.tokens
    };

    var ref = this.matDialog.open(ScalusApplicationsComponent, {
      width: '100%',
      height: '100%',
      data: applicationData
    });
    ref.afterClosed().subscribe(
      () => {
        this.loadConfig(this.config, this.registrations);
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
              this.loadConfig(config, this.registrations);
            }, 
            error => {
              this.showError("Failed to save configuration");
          });
        } 
        catch(error) {
          this.showError("Invalid configuration file");
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

  showError(msg: string) {
    this.matDialog.open(ErrorDialogComponent, {
      data: msg
    });
  }

  showHelp() {
    this.apiService.getInfo().subscribe(
      x => {
        var info = <string>x;
        this.matDialog.open(ScalusHelpDialogComponent, {
          data: info
        });
      }, 
      error => {
        this.showError("Failed to show help");
    });
  }

}
