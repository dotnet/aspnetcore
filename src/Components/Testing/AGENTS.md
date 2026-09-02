# Components.Testing contributor guidance

Read the parent `src/Components/AGENTS.md` first. This file adds narrower rules for work under `src/Components/Testing`.

## Design boundary

Treat the Components.Testing assembly, generators, tasks, and shipped MSBuild assets as product code intended for external package consumers. Keep them independent of the ASP.NET Core repository layout, build graph, source-build conventions, CI providers, and repository-only projects or properties.

Distinguish portable product capabilities from environment-specific orchestration. Discovering, building, publishing, packaging, launching, and diagnosing applications under test are product capabilities. Scheduling jobs, provisioning machines, uploading results, and adapting to a particular CI service belong in repository or service integration.

Shipped package assets must be self-contained and deterministic:

- Resolve only files included in the package or supplied through documented, product-neutral extension points.
- Do not depend on source-tree bootstrapping, incidental build order, stale outputs, sentinel files, or callers setting repository globals.
- Keep Build, Publish, clean, incremental, parallel, and no-build behavior consistent.
- Produce portable manifests and complete payloads that can run on a separately provisioned machine.

Keep public APIs focused on customer scenarios. Hide storage layout, path composition, build plumbing, and other implementation details unless customers need to control them directly.

## Repository integration and validation

Use `testassets/**` for sample consumer configuration, scenarios, and ASP.NET Core source-tree development hooks. Test assets should exercise the package as an external consumer would; they must not cause repository assumptions to leak into shipped assets.

Before changing repository-wide `eng/**` infrastructure, search Arcade, the .NET SDK, and existing ASP.NET Core mechanisms. Add repository-wide behavior only when the requirement is genuinely shared and no suitable extension point exists.

For changes that affect packaging or build integration:

1. Inspect the produced package, not only source-tree outputs.
2. Validate an isolated consumer using only packaged assets and normal restore sources.
3. Exercise Build and Publish, plus relevant clean, incremental, parallel, and no-build paths.
4. Verify the payload contains everything required to execute away from the source checkout.
