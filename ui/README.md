# SCALUS configuration UI (`scalus-ui`)

The Angular front end for the SCALUS desktop configuration app. It is hosted in a
native [Photino](https://www.tryphotino.io/) window by the `Scalus.Ui` .NET
project — there is **no** browser or local web server involved at runtime.

- Angular 22.2 (standalone components), TypeScript 6.0.
- Talks to the .NET host through a small **bridge** rather than HTTP.
- Builds to static assets that the host serves from `wwwroot`.

## How it fits together

```
ui/                         Angular sources (this project)
  src/app/
    app.ts / app.html       Root component — the whole config UI
    core/bridge/            The host <-> UI contract (see below)
    shared/ui/              Reusable presentational components (ui-button, ...)
  dist/scalus-ui/browser/   Build output (generated, never committed)

src/Scalus.Ui/              The Photino host (C#). On build it runs `npm ci` +
                            `npm run build`, then stages dist/ into wwwroot and
                            answers bridge calls (BridgeDispatcher.cs).
```

The `Scalus.Ui` MSBuild project builds this front end automatically, so a normal
`dotnet build` of the desktop app produces a working GUI. You only need the
commands below when iterating on the UI itself.

## The bridge

The UI never calls a REST API. It calls methods on a `ScalusBridge`
(`src/app/core/bridge/scalus-bridge.ts`), and dependency injection picks the
implementation at runtime:

| Implementation | Used when | Purpose |
|----------------|-----------|---------|
| `PhotinoBridge` | Running inside the Photino host (`window.external.sendMessage` exists) | Marshals calls to the C# `BridgeDispatcher` |
| `MockBridge`    | Running in a plain browser via `ng serve` | In-memory seed data so the UI runs with no .NET host |

The selection lives in `core/bridge/bridge.provider.ts`. This is what makes
`ng serve` in a browser useful for fast UI work — you get the real UI backed by
realistic mock data.

Adding a bridge method touches four files (C# dispatcher, the TS interface, and
both bridge implementations). See
[`.agents/skills/ui-bridge/SKILL.md`](../.agents/skills/ui-bridge/SKILL.md) for
the full step-by-step.

## Development

Prerequisites: Node.js 22.x and npm.

```bash
npm ci          # install exact locked dependencies
npm start       # ng serve — browser dev mode, backed by MockBridge
```

Open `http://localhost:4200/`. The app reloads on source changes. Because there
is no Photino host in the browser, the UI runs against `MockBridge`.

To exercise the UI against the **real** .NET host, build and run the desktop app
instead (from the repo root):

```bash
dotnet run --project src/Scalus.Ui/Scalus.Ui.csproj
```

That rebuilds the Angular assets, stages them into `wwwroot`, and opens the
Photino window wired to the live `BridgeDispatcher`.

## Build

```bash
npm run build   # ng build -> dist/scalus-ui/browser
```

The output is a build product and is never checked in. The desktop app's
packaging (`scripts/publish.*`) produces and bundles it for release.

## Tests

```bash
npm test        # ng test (Karma) — unit specs, e.g. app.spec.ts, mock-bridge.spec.ts
```

## Code scaffolding

```bash
ng generate component component-name
ng generate --help
```

## Further reading

- [`src/README.md`](../src/README.md) — the overall developer guide.
- [`AGENTS.md`](../AGENTS.md) — repository map and conventions for agents.
- [Angular CLI reference](https://angular.dev/tools/cli).
