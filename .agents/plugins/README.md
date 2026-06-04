# Project Plugin Migration

Source: `.claude/settings.json`.

This marketplace exposes the Claude-enabled .NET plugins as Codex marketplace
entries for this repository:

- `dotnet-dnceng`
- `dotnet`
- `dotnet-test`

The entries resolve in Codex as `aspnetcore-dotnet-skills`, but direct plugin
installation currently fails with `missing plugin.json` because the upstream
.NET plugin folders use a Claude-style top-level `plugin.json` instead of a
Codex `.codex-plugin/plugin.json` manifest. Keep these entries for discovery;
install after the upstream plugin bundles add Codex manifests or after local
Codex wrappers are created for the bundles.
