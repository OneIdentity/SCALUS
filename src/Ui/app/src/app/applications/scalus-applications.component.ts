import { Component, OnInit, Inject, ViewEncapsulation } from '@angular/core';
import { ApiService, ScalusConfig, ApplicationConfig, ApplicationConfigDisplay, ParserConfig, ParserConfigDisplay } from '../api/api.service';
import { EuiSidesheetService, EUI_SIDESHEET_DATA } from '@elemental-ui/core';
import * as $ from 'jquery';

@Component({
  selector: 'applications',
  templateUrl: './scalus-applications.component.html',
  styleUrls: ['./scalus-applications.component.scss'],
  encapsulation: ViewEncapsulation.None
})
export class ScalusApplicationsComponent implements OnInit {

  config: ScalusConfig;

  applications: ApplicationConfigDisplay[];

  constructor(private apiService: ApiService,
    private sidesheetService: EuiSidesheetService,
    @Inject(EUI_SIDESHEET_DATA) public sidesheetdata?: any) {
      this.config = <ScalusConfig>sidesheetdata;
      
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

    this.applications.push(application);
  }

  save() {
    var appConfigs = new Array();
    this.applications.slice().forEach(acd => {
      appConfigs.push(this.getApplication(acd));
    });

    this.config.applications = appConfigs;
    this.apiService.setConfig(this.config).subscribe(
      x => {
        this.sidesheetService.close();
    }, 
      error => {
        this.handleError(error, "Failed to save configuration");
    });
  }

  cancel() {
    this.sidesheetService.close();
  }

  handleError(error:any, msg:string){
    alert("ERROR: " + msg + " (" + error + ")");
  }

  getApplicationDisplay(ac:ApplicationConfig) {
    var app:ApplicationConfigDisplay = <ApplicationConfigDisplay>{};
    app.id = ac.id;
    app.name = ac.name;
    app.description = ac.description;
    app.platforms = ac.platforms?.join(",");
    app.protocol = ac.protocol;

    app.parser = <ParserConfigDisplay>{};
    app.parser.id = ac.parser.id;
    app.parser.options = ac.parser.options?.join(",");
    app.parser.useDefaultTemplate = ac.parser.useDefaultTemplate;
    app.parser.useTemplateFile = ac.parser.useTemplateFile;
    app.parser.postProcessingExec = ac.parser.postProcessingExec;
    app.parser.postProcessingArgs = ac.parser.postProcessingArgs?.join(",");

    app.exec = ac.exec; 
    app.args = ac.args?.join(",");

    return app;
  }

  getApplication(ac:ApplicationConfigDisplay) {
    var app:ApplicationConfig = <ApplicationConfig>{};
    app.id = ac.id;
    app.name = ac.name;
    app.description = ac.description;
    app.platforms = ac.platforms?.split(",");
    app.protocol = ac.protocol;

    app.parser = <ParserConfig>{};
    app.parser.id = ac.parser.id;
    app.parser.options = ac.parser.options?.split(",");
    app.parser.useDefaultTemplate = ac.parser.useDefaultTemplate;
    app.parser.useTemplateFile = ac.parser.useTemplateFile;
    app.parser.postProcessingExec = ac.parser.postProcessingExec;
    app.parser.postProcessingArgs = ac.parser.postProcessingArgs?.split(",");
    
    app.exec = ac.exec; 
    app.args = ac.args?.split(",");

    return app;
  }
}
