---
name: ui-bridge
description: Use when working on the SCALUS Angular UI or adding/altering a host bridge method end-to-end between the Angular app and the C# Photino host.
---

# SCALUS UI Bridge

Read this when working in `ui/` or connecting the UI to C#. The desktop config app
is an **Angular 22.2 SPA hosted in a Photino window**. It calls into `Scalus.Core`
through a **message bridge — never HTTP, never a local web server.**

## The stack

- `ui/` — Angular 22.2 workspace (TypeScript 6.0, Node 22). `Scalus.Ui.csproj`
  builds it (`npm ci` + `npm run build` → `ui/dist/scalus-ui/browser`) and stages
  it into the host's `wwwroot`.
- `src/Scalus.Ui/` — the Photino .NET host. `Program.cs` opens the window and loads
  the staged Angular `index.html`; `BridgeDispatcher.cs` handles bridge calls.

## How the bridge works

Every call is a JSON message with `{ id, method, args }`. The TS side posts it via
`window.external.sendMessage`; C# dispatches on `method`, runs the corresponding
`Scalus.Core` code in-process, and posts back `{ id, ok, result | error }`. The TS
side resolves the pending promise by `id`.

Runtime selection happens in `bridge.provider.ts`:

- **Photino host** (`window.external.sendMessage` exists) → `PhotinoBridge`
  (real C#).
- **Plain browser** (`ng serve`) → `MockBridge` (in-memory seed data).

This is why `ng serve` works with no .NET host — great for pure UI iteration.

## Dev workflows

```bash
cd ui
npm ci
npm start        # ng serve -> http://localhost:4200, MockBridge (no .NET needed)
npm test         # Karma unit specs (incl. mock-bridge.spec.ts)
npm run build    # production build into dist/
```
```powershell
# Real host (exercises the C# bridge end-to-end):
dotnet run --project src/Scalus.Ui/Scalus.Ui.csproj
```

## The contract (single source of truth)

`ui/src/app/core/bridge/scalus-bridge.ts` defines the `ScalusBridge` interface plus
all shared DTO types (`ScalusConfig`, `ApplicationConfig`, `ProtocolMapping`,
`ParserConfig`, `ScalusSettings`, `RegistrationStatus`, `LaunchRecord`, …). The
current methods: `getConfig`, `saveConfig`, `validate`, `getRegistrations`,
`getRegistrationStatus`, `register`, `unregister`, `getCapabilities`, `getTokens`,
`getApplicationDescriptions`, `getParsers`, `getTerminals`, `getInfo`,
`getVersion`, `getStartupAction`, `getLaunchRecords`, `getLaunchFile`,
`openLogsFolder`, `exportToFile`, `importFromFile`, `getPlatform`.

> The TS DTO field names are **PascalCase** (`Protocols`, `AppId`, `ParserId`) to
> match the C# JSON exactly. Keep them aligned with `src/Dto/`.

## Adding a bridge method — the 4-file recipe

To add capability `doThing(arg)` you touch **four files** (all must stay in sync,
or the mock/real split diverges and specs break):

1. **`scalus-bridge.ts`** — add `doThing(arg: T): Promise<R>;` to the `ScalusBridge`
   interface (and any new DTO types).
2. **`photino-bridge.ts`** — implement it: `doThing(arg) { return this.call('doThing', arg); }`.
3. **`mock-bridge.ts`** — implement a plausible in-memory version so `ng serve` and
   the specs work (update `seed-data.ts` if it needs data).
4. **`BridgeDispatcher.cs`** — add a `"doThing" => DoThing(args[0]...)` arm to the
   `Dispatch` switch and write the C# method calling into `Scalus.Core`.

Then the UI consumes it by injecting the `SCALUS_BRIDGE` token (`bridge.provider.ts`).

## C# side notes (`BridgeDispatcher.cs`)

- `Dispatch(message)` parses the envelope, `switch`es on `method`, serializes the
  result as `{ id, ok = true, result }` (or `{ ok = false, error }` on exception).
- Args come in as `JsonArray`; deserialize config args with
  `args[0].Deserialize<ScalusConfig>(ScalusJson.Disk)` so the shared JSON options
  (case-insensitive, source-gen context) apply.
- The dispatcher resolves services from the DI `IServiceProvider`
  (`IScalusApiConfiguration`, registration, etc.) — the same core both binaries use.
- `getStartupAction` powers the failure deep-link (`scalus-ui --show-logs=<id>`
  opens the Logs view at a specific launch); keep that path working when touching
  startup.

## Gotchas

- Don't add REST controllers or a Kestrel server — that pattern was removed in 2.0.
- Config **round-trips** through the bridge; `GetConfiguration()` must preserve
  every top-level field (`PreferredTerminal`, `Settings`) or the UI silently wipes
  them on the next save. See the configuration skill.
- Keep `mock-bridge.ts` faithful — it's what the specs and browser dev mode run on.
