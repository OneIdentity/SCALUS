# SCALUS UI Developer Guide

SCALUS GUI is an Angular web app hosted by SCALUS CLI and an ASP.NET web API listening on localhost. The CLI opens a browser to the localhost and serves the UI. The CLI provides an API that allows the browser to make local changes to the configuration of the system. This is similar to an electron-based node.js + angular application, but with ASP.NET as the backend instead of node.

## Requirements

* [Node](https://nodejs.org)
* Node Package Manager
* [Angular CLI](https://angular.io/guide/setup-local)

## Component Folders

### `src/Ui`: Web service host

To start the web service change to the `src` directory and run the CLI in UI mode:

`dotnet run ui`

SCALUS will start the local web server on http://localhost:42000 and launch the browser. This will serve static files from the `src/Ui/Web` folder. When the browser window closes there's a hook that tells the CLI to shutdown. You can disable this by setting `Liftime:IgnoreShutdown = true` in `appconfig.json`. When this is set, you must stop the web server manually, but it makes things easier when working on the Angular app.


### `src/Ui/app`: Angular app

To build the Angular app change to `src/Ui/app`:

1. Run `npm install`
2. Run `ng serve`
3. Launch the browser to http://localhost:4200

Angular will run a local development server that hosts the application and supports debugging. This is different than the web server hosted by SCALUS CLI, but they can easily work together. The `proxy.conf.json` configures Angular to redirect API calls to the web service running on http://localhost:42000.

If you are using Visual Studio Code, the `src/Ui/app/.vscode` folder contains a launch.json that will start up the Angular development server and launch Chrome in the debugger. You can modify this to launch and debug with the browser of your choice.

## Build Integration

The `src/Ui/app/build.ps1` is a PowerShell script that will build the Angular application and copy artifacts to the `src/Ui/Web` folder which will then be what is served by default when running SCALUS in UI mode.