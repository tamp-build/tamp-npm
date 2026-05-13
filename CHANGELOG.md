# Changelog

All notable changes to **Tamp.Npm.V10** are recorded here.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/);
versions follow [SemVer](https://semver.org/spec/v2.0.0.html).

## [0.2.0] — pending — idempotent `Npm.SetVersion` typed verb (TAM-208)

### Added

- **`Npm.SetVersion(NpmBin, version)`** — idempotent version stamping for
  `package.json`. Wraps `npm version <ver> --no-git-tag-version
  --allow-same-version`. Symmetric with `Cargo.SetPackageVersion` (TAM-203)
  and `Msix.SetAppxManifestVersion` — adopters can stamp every manifest
  uniformly without remembering per-tool magic flags.

  ```csharp
  Target StampVersion => _ => _
      .Before(nameof(BuildFrontend))
      .Executes(() =>
      {
          Cargo.SetPackageVersion(ServiceCrate / "Cargo.toml", Version);
          Msix.SetAppxManifestVersion(AppxManifest, Version);
          Npm.SetVersion(NpmBin, Version);   // idempotent; survives retries
      });
  ```

  Filed under TAM-208 from DasBook canary friction batch #3 #12 (2026-05-13).

### Why

Raw `npm version <ver>` exits 1 when `package.json` is already at the
target version — npm considers no-op a failure. This kills CI retry
patterns and any inner-loop "rebuild after small change" workflow that
re-runs the stamp target.

The non-idempotency is asymmetric with the rest of Tamp's version-stamping
surface (`cargo set-version` and `Msix.SetAppxManifestVersion` are both
idempotent). DasBook hit it on second run of `PackMsix` after the
StampVersion target succeeded once. `--allow-same-version` (npm 6+) flips
no-op back to exit 0.

`--no-git-tag-version` is also bundled because build scripts manage
versioning at a higher layer than SCM tagging — npm's default of creating
a commit + tag mid-build is the wrong behavior for Tamp targets.

### Tests

- 8 new tests in `NpmTests`: argument structure (`version <ver>
  --no-git-tag-version --allow-same-version`), SemVer compatibility
  (pre-release + build-metadata variants), idempotency flag presence,
  no-git-tag-version flag presence, null/empty/whitespace version
  rejection, null tool rejection, object-init overload, fail-fast at plan
  time when Version unset.

## [0.1.0] - 2026-05-13

### Added

- Initial release. Verb surface: `Install`, `Ci`, `Run`, `Publish`, `Audit`, `Raw`.
  Sibling to `Tamp.Yarn.V4`; same shape, npm-flavored knobs (`--save-dev`,
  `--ignore-scripts`, `--no-audit`, `--no-fund`, `--prefer-offline`).

- `NpmAuthToken` propagates a `Secret`-typed token to `NPM_TOKEN` on the spawned
  process; the runner's redaction table covers it.

- `NpmPublishSettings.Otp` is `Secret`-typed and redacted.

- Object-init overloads on every verb in addition to fluent. Filed under TAM-172.

### Notes

- Driven by Strata's adoption-wave gap list 2026-05-13 (PR #226 BuildFrontend was the
  blocking case). Pinned to `Tamp.Core` / `Tamp.NetCli.V10` at 1.4.1 (the version
  whose `InternalsVisibleTo` list includes `Tamp.Npm.V10`).
