# Tamp.Npm.V10

> Tamp wrapper for the `npm` CLI (Node 22+ / npm 10+ era). Sibling to [`Tamp.Yarn.V4`](https://github.com/tamp-build/tamp-yarn) for adopters whose frontend toolchain is npm-based.

| Package | Status |
|---|---|
| `Tamp.Npm.V10` | 0.1.0 (initial) |

## Install

```bash
dotnet add package Tamp.Npm.V10
```

Multi-targets net8 / net9 / net10.

## Quick start

```csharp
using Tamp;
using Tamp.Npm.V10;

class Build : TampBuild
{
    public static int Main(string[] args) => Execute<Build>(args);

    [FromPath("npm")] readonly Tool Npm = null!;

    Target Install => _ => _.Executes(() => Npm.Ci(s => s
        .SetWorkingDirectory(RootDirectory / "frontend")));

    Target Build => _ => _
        .DependsOn(Install)
        .Executes(() => Npm.Run(s => s
            .SetScript("build")
            .SetWorkingDirectory(RootDirectory / "frontend")));

    Target Test => _ => _
        .DependsOn(Install)
        .Executes(() => Npm.Run(s => s
            .SetScript("test")
            .AddScriptArgs("--coverage")
            .SetWorkingDirectory(RootDirectory / "frontend")));
}
```

## Verb surface

| Tamp method | npm command | Notes |
|---|---|---|
| `Npm.Install(...)` | `npm install [package...]` | Defaults to `--no-fund`. Set `IgnoreScripts=true` for supply-chain safety on CI. |
| `Npm.Ci(...)` | `npm ci` | The CI-recommended invocation. Defaults: `--no-audit`, `--no-fund`. |
| `Npm.Run(...)` | `npm run <script> [-- args...]` | Forwards extra args after `--`. |
| `Npm.Publish(...)` | `npm publish` | `Otp` is `Secret`-typed and redacted from logs. |
| `Npm.Audit(...)` | `npm audit [fix]` | Gating-friendly; supports `--audit-level` for CI threshold checks. |
| `Npm.SetVersion(...)` | `npm version <ver> --no-git-tag-version --allow-same-version` | **Idempotent** (0.2.0+). Stamp `package.json` from a `StampVersion` target. Safe to call repeatedly with the same version — no-op succeeds (raw `npm version` exits 1 on no-op). |
| `Npm.Raw(...)` | `npm <anything>` | Escape hatch — file a ticket if you reach for it often. |

### `Npm.SetVersion` worked example

```csharp
Target StampVersion => _ => _
    .Before(nameof(BuildFrontend))    // stamp before anything reads the version
    .Description("[Pack] Sync version across every manifest the build embeds")
    .Executes(() =>
    {
        Cargo.SetPackageVersion(ServiceCrate / "Cargo.toml", Version);
        Msix.SetAppxManifestVersion(AppxManifest, Version);
        Npm.SetVersion(NpmBin, Version);
    });
```

Why `Npm.SetVersion` exists as a typed verb instead of `Npm.Raw(NpmBin, "version", Version)`: raw `npm version` exits **1** when `package.json` is already at the target version, which kills CI retry loops and "rebuild after small change" inner loops. `SetVersion` always passes `--allow-same-version` so no-op succeeds, and `--no-git-tag-version` to keep npm from mid-build commit-tagging.

## Auth — private registries

`Tamp.Npm.V10` injects an npm-registry auth token via the `NPM_TOKEN` environment variable on the spawned process (the var the npm CLI's auth path reads). The token is `Secret`-typed and registered with the runner's redaction table:

```csharp
[Secret("Internal npm token", EnvironmentVariable = "INTERNAL_NPM_TOKEN")]
readonly Secret NpmToken = null!;

Target Restore => _ => _.Executes(() => Npm.Ci(s => s
    .SetNpmAuthToken(NpmToken)
    .SetWorkingDirectory(RootDirectory / "frontend")));
```

If your registry needs a `.npmrc` config beyond the token, drop a file at the project root (or workspace root) — npm picks it up automatically. Tamp doesn't try to manage `.npmrc` itself.

## Windows shim

npm ships as `npm.cmd` on Windows. The Tamp `Tool` resolution via `[FromPath("npm")]` finds the `.cmd` shim correctly via the standard PATHEXT probe. The npm self-resolution bug (where npm hunts for `npm-cli.js` in `CWD/node_modules/npm/bin/` instead of the install dir) is triggered when the `.cmd` shim is invoked through bare `ProcessStartInfo` with `UseShellExecute=false` — outside Tamp's normal path. The wrapper here uses Tamp's standard `CommandPlan` invocation which doesn't trigger the bug.

If you do hit it (e.g. invoking npm yourself in custom `Executes(Action)` code), the fix is `Tool.FromPath("npm.cmd")` or letting the system PATH resolution find it through `cmd /c`. The Tamp `[FromPath("npm")]` attribute does the right thing automatically.

## Sibling packages

- [`Tamp.Yarn.V4`](https://github.com/tamp-build/tamp-yarn) — Yarn Berry. Same shape, different CLI.
- [`Tamp.Turbo.V2`](https://github.com/tamp-build/tamp-turbo) — Turborepo. Pairs with either npm or yarn for monorepo builds.
- [`Tamp.Vite.V5`](https://github.com/tamp-build/tamp-vite) — Vite + Vitest. Common chain: `Npm.Ci → Vite.Build → Vite.Test`.

## Releasing

Releases follow the [Tamp dogfood pattern](MAINTAINERS.md): bump `<Version>` in `Directory.Build.props`, tag `v<X.Y.Z>`, GitHub Actions runs `dotnet tamp Ci` then `dotnet tamp Push`.

## Settings authoring style

Examples above use the fluent `Set*`-chain shape. Every wrapper verb also accepts a `new XxxSettings { ... }` object-init form — both produce identical `CommandPlan`s. The fluent shape stays canonical in docs and the `tamp init` template; opt into object-init scaffolding via `tamp init --settings-style=init`.

See [Build Script Authoring → Two authoring styles](https://github.com/tamp-build/tamp/wiki/Build-Script-Authoring#two-authoring-styles-for-wrapper-calls-120) on the wiki for the side-by-side comparison.

## License

MIT. See [LICENSE](LICENSE).
