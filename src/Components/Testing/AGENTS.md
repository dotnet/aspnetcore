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

## Source generator wiring

Keep the generator `ProjectReference` with `OutputItemType="Analyzer"` in the consuming project file. Do not move it into a `Directory.Build.props`, even to deduplicate the copies in `src` and `testassets`: `eng/targets/ResolveReferences.targets` rebuilds the `ProjectReference` set while resolving `Reference` items and silently drops an analyzer reference contributed from a props file. The generator then emits nothing, and the build fails with errors that do not mention generators, such as `MSTEST0030` (no `[TestClass]` found) or `CS0119`. `OutputItemType` items only materialize during a real build, so confirm the reference survives with an actual build rather than evaluation alone.

To inspect generated code, set `EmitCompilerGeneratedFiles` and `CompilerGeneratedFilesOutputPath` in the one project under investigation, not as global `-p:` properties. Global values flow into every transitively built project, make unrelated generators re-emit their sources, cause spurious `CS0757` errors, and leave stray output folders across the tree.

The unit-test project uses xUnit v3, while the E2E test projects in `testassets` use MSTest on Microsoft.Testing.Platform. With implicit usings enabled, the repository adds `using Xunit;` to every test project and MSTest adds `Microsoft.VisualStudio.TestTools.UnitTesting`; the two collide on `Assert` and `TestContext`; this is why `test/Microsoft.AspNetCore.Components.Testing.Tests.csproj` sets `ImplicitUsings` to `disable`.
