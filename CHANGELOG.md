# Changelog
All notable changes to this project will be documented in this file.

The format is based on Keep a Changelog and this project adheres to Semantic Versioning.

## [1.1.20] -2025-10-30
### Changed
- Pinned build to .NET9 SDK via `global.json` (roll-forward latestFeature; allowPrerelease=true). Planned post-GA bump to `10.0.100`.
- Centralized C# `LangVersion=latest`, `Nullable=enable`, `Deterministic=true` in `Directory.Build.props`; enabled `TreatWarningsAsErrors` for packable projects in CI.
- Kept TFMs unchanged (`net8.0`). No runtime TFM change.
- Added simple benchmark scripts `scripts/bench.sh` and `scripts/bench.ps1`.

### Notes
- CI workflows updated to use `9.0.x` SDK; no other changes.
- No API changes. Patch version bump only.
