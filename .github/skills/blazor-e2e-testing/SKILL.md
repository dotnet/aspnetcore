---
name: blazor-e2e-testing
description: >-
  Run a bounded selection of Blazor end-to-end tests in dotnet/aspnetcore. USE FOR validating a focused Components change with one test or class, and validating a major Blazor change by splitting affected Selenium E2E tests into small logical groups and running them sequentially. DO NOT USE FOR running the full Components E2E suite locally, repository setup or build troubleshooting, temporary sample validation (use validate-blazor-feature), unit tests, or non-Components areas.
---

# Run bounded Blazor E2E test groups

`src/Components/AGENTS.md` is the source of truth for repository setup, asset freshness, build commands, interactive debugging, and test policy. Follow its current instructions before using this workflow; if this skill differs from that file, `AGENTS.md` wins.

Determine only **which tests to run and in what order**. Keep local validation bounded; full coverage belongs in CI.

## Focused changes

For a localized change, select the nearest existing test method. Include additional methods only when they exercise another affected render mode, runtime, or lifecycle boundary.

After completing the dependency-aware E2E build from `AGENTS.md`, run a method:

```powershell
dotnet test src/Components/test/E2ETest/Microsoft.AspNetCore.Components.E2ETests.csproj `
    --no-build `
    --filter "FullyQualifiedName~Namespace.TestClass.TestMethod" `
    -l "console;verbosity=minimal"
```

Use the containing class instead when its methods jointly cover the affected behavior:

```powershell
dotnet test src/Components/test/E2ETest/Microsoft.AspNetCore.Components.E2ETests.csproj `
    --no-build `
    --filter "FullyQualifiedName~Namespace.TestClass" `
    -l "console;verbosity=minimal"
```

## Major changes

Split the affected tests into logical groups that each have one reason to fail, such as:

1. A small canary group covering the affected startup paths and render modes.
2. Feature-area groups covering the changed behavior in depth.
3. Cross-cutting groups covering affected navigation, prerendering, reconnection, or state-restoration boundaries.

Construct each group by OR-combining the fully qualified names of relevant existing methods or classes:

```powershell
dotnet test src/Components/test/E2ETest/Microsoft.AspNetCore.Components.E2ETests.csproj `
    --no-build `
    --filter "FullyQualifiedName~FirstTestClass|FullyQualifiedName~SecondTestClass" `
    -l "console;verbosity=minimal"
```

Complete the same dependency-aware E2E build from `AGENTS.md` before running the first group.

Run groups sequentially, from the smallest canary group to the broader feature groups. Stop at the first failing group, narrow it to the failing method, and follow the manual test-server debugging workflow in `AGENTS.md`. Resume with that group only after the focused failure passes.

Choose groups from the actual impact of the change; do not maintain a fixed list that drifts as tests move. A group must remain small enough that its failure identifies one feature or lifecycle boundary.

## Completion criteria

The selected tests cover every affected behavior boundary, each filter matches the intended nonzero set of tests, and every planned group passes independently.
