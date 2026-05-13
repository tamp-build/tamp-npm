# Changelog

All notable changes to **Tamp.Npm.V10** are recorded here.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/);
versions follow [SemVer](https://semver.org/spec/v2.0.0.html).

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
