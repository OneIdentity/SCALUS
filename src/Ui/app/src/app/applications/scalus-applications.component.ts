import { Component, OnInit, Inject, ViewEncapsulation, ChangeDetectionStrategy } from '@angular/core';
import { ApiService, ScalusConfig, ApplicationConfig, ApplicationConfigDisplay, ParserConfig, ParserConfigDisplay, Platform } from '../api/api.service';
import { EuiSidesheetService, EUI_SIDESHEET_DATA } from '@elemental-ui/core';
import { MatDialog } from '@angular/material/dialog';
import { ScalusApplicationsTokensDialogComponent } from "./tokens/scalus-applications-tokens-dialog.component";
import { ErrorDialogComponent } from '../error/error-dialog.component';

@Component({
  selector: 'applications',
  templateUrl: './scalus-applications.component.html',
  styleUrls: ['./scalus-applications.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  encapsulation: ViewEncapsulation.None
})
export class ScalusApplicationsComponent implements OnInit {

  config: ScalusConfig;

  applications: ApplicationConfigDisplay[];

  descriptions: object;
  tokens: object;

  constructor(private apiService: ApiService,
    private sidesheetService: EuiSidesheetService,
    private matDialog: MatDialog,
    @Inject(EUI_SIDESHEET_DATA) public sidesheetdata?: any) {
      
      this.config = <ScalusConfig>sidesheetdata.config;
      this.descriptions = sidesheetdata.descriptions;
      this.tokens = sidesheetdata.tokens;
      
      var apps = new Array();
      this.config.applications.slice().forEach(ac => {
        apps.push(this.getApplicationDisplay(ac));
      });
      this.applications = apps;
  }

  ngOnInit(): void {
    
  }

  delete(id:string) {
    this.applications.forEach((element, index) => {
      if(element.id == id) {
        this.applications.splice(index,1);
      }
    });
  }

  add() {
    var application:ApplicationConfigDisplay = <ApplicationConfigDisplay>{};
    application.id = 'new';
    application.parser = <ParserConfigDisplay>{};

    this.applications.unshift(application);
  }

  save() {
    var validationErrors = new Array();
    this.applications.slice().forEach(acd => {
      this.validateApplication(acd, validationErrors);
    });

    if (validationErrors.length > 0) {
      this.showError(validationErrors.join('\n'));
    }
    else {
      var appConfigs = new Array();
      this.applications.slice().forEach(acd => {
        appConfigs.push(this.getApplication(acd));
      });

      this.config.applications = appConfigs;
      this.apiService.validateConfig(this.config).subscribe(
      x => 
      {
        if (x.length === 0)
        {
          this.apiService.setConfig(this.config).subscribe(
          x => {
            this.sidesheetService.close();
          }, 
          error => {
            this.showError("Failed to save configuration.");
          });
        }
        else {
          this.showError(x.join('\n'));
        }
      },
      error =>{
        this.showError("Invalid configuration file.")    
      });
    }
  }

  cancel() {
    this.sidesheetService.close();
  }

  showError(msg: string) {
    this.matDialog.open(ErrorDialogComponent, {
      data: msg
    });
  }

  getApplicationDisplay(ac:ApplicationConfig) {
    var app:ApplicationConfigDisplay = <ApplicationConfigDisplay>{};
    app.id = ac.id;
    app.name = ac.name;
    app.description = ac.description;
    var platforms = new Array();
    ac.platforms?.forEach(p =>{
      if (p === 0)
      {
        platforms.push(Platform[Platform.Windows]);
      }
      else if (p === 1)
      {
        platforms.push(Platform[Platform.Linux]);
      }
      else if (p=== 2)
      {
        platforms.push(Platform[Platform.Mac]);
      }
    })
    app.platforms = platforms.join(",");
    app.protocol = ac.protocol;

    app.parser = <ParserConfigDisplay>{};
    app.parser.parserId = ac.parser.parserId;
    app.parser.options = ac.parser.options?.join(",");
    app.parser.useDefaultTemplate = ac.parser.useDefaultTemplate;
    app.parser.useTemplateFile = ac.parser.useTemplateFile;
    app.parser.postProcessingExec = ac.parser.postProcessingExec;
    app.parser.postProcessingArgs = ac.parser.postProcessingArgs?.join(",");

    app.exec = ac.exec; 
    app.args = ac.args?.join(",");

    return app;
  }

  validateApplication(ac:ApplicationConfigDisplay, errors:string[]) {
    ac.platforms?.split(",").forEach(p => {
      var platform = Platform[p];
      if (platform === undefined)
      {
        errors.push(`Invalid Platform:${p}. Valid values are:Windows,Linux,Mac`);
      }
    })
  }

  getApplication(ac:ApplicationConfigDisplay) {
    var app:ApplicationConfig = <ApplicationConfig>{};
    app.id = ac.id;
    app.name = ac.name;
    app.description = ac.description;
    var platforms = new Array();
    ac.platforms?.split(",").forEach(p => {
      var platform = Platform[p];
      if (platform !== undefined)
      {
        platforms.push(Platform[p]);
      }
    })
    app.platforms = platforms;
    app.protocol = ac.protocol;

    app.parser = <ParserConfig>{};
    app.parser.parserId = ac.parser.parserId;
    app.parser.options = ac.parser.options?.split(",");
    app.parser.useDefaultTemplate = ac.parser.useDefaultTemplate;
    app.parser.useTemplateFile = ac.parser.useTemplateFile;
    app.parser.postProcessingExec = ac.parser.postProcessingExec;
    app.parser.postProcessingArgs = ac.parser.postProcessingArgs?.split(",");
    
    app.exec = ac.exec; 
    app.args = ac.args?.split(",");

    return app;
  }

  showTokens() {
    this.matDialog.open(ScalusApplicationsTokensDialogComponent, {
      data: this.tokens
    });
  }

  getTooltip(key:string) : string {
    if (key in this.descriptions)
    {
      return this.descriptions[key];
    }
    else {
      return "";
    }
  }
}