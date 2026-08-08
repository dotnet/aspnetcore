# Components.Testing contributor guidance

Read the parent `src/Components/AGENTS.md` first. This file adds narrower rules for work under `src/Components/Testing`.

## Product boundary

`Microsoft.AspNetCore.Components.Testing`—including its assembly, generators, tasks, and shipped MSBuild assets—is product code intended to ship as a future NuGet package. It is not ASP.NET Core repository infrastructure.

The product must work for external consumers without `RepoRoot`, `ArtifactsBinDir`, the ASP.NET Core source layout, source-build flags, Helix-specific behavior, or repository-only projects and properties.

Product capabilities include regular app testing; discovering, building, and publishing apps under test; producing portable manifests and complete test payloads; launching apps; and collecting diagnostics and artifacts. A payload may run on a separately provisioned machine or CI service such as Helix. That portability is product behavior; service-specific orchestration is not.

## Placement

- Keep product-neutral manifest generation and Build/Publish support in product code and shipped package assets.
- Keep sample consumer configuration, scenarios, ASP.NET Core wiring, and source-tree development hooks under `testassets/**`.
- Keep `_E2ETasksProject`, `ArtifactsBinDir`, and `DotNetBuildSourceOnly`/`ExcludeFromBuild` gating in `testassets` or other repository integration, never in shipped package assets.
- Keep source-tree bootstrapping and build hooks outside shipped package assets so they cannot affect package consumers.
- Do not change a generic Helix runner for this package unless the change is broadly required independently of Components.Testing.

Repository-wide `eng/**` changes are appropriate only for genuinely repository-wide behavior that cannot use existing Arcade, SDK, or ASP.NET Core infrastructure. Search for and reuse those mechanisms before adding new repository infrastructure.
