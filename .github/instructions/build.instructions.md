---
description: Instructions for MSBuild and repository build infrastructure
applyTo: "eng/**,Directory.Build.*,**/*.props,**/*.targets"
---

# Build infrastructure changes

- Read `docs/BuildFromSource.md` and `docs/BuildErrors.md`. Prefer the existing area or repository `build` and `restore` scripts. Before invoking `dotnet` directly, activate the repository SDK with `source activate.sh` or `. ./activate.ps1`.
- Trace build properties through every applicable entry point. Check both wrapper scripts and bare `dotnet` or IDE evaluation; do not assume they assign the same defaults. In particular, `eng/build.sh` detects and passes the host architecture, while `eng/Common.props` currently defaults an unset `TargetArchitecture` to `x64`. Treat that fallback as behavior to investigate, not a convention to preserve.
- Before proposing a fix, map the producer, persisted or shared intermediate state, consumer, and resulting diagnostic. Verify that state paths and cache keys distinguish every relevant configuration, OS, architecture, runtime identifier, and target framework.
- Locate each custom task's `UsingTask` declaration or imported `.tasks` file and inspect its conditions. Ensure the task invocation is skipped in evaluations where registration is unavailable, especially `DesignTimeBuild`.
- Select validation modes that exercise the changed surface: normal wrapper builds, direct CLI and IDE/design-time evaluation, restore and no-restore transitions, source-build or CI properties, and affected OS/architecture/RID combinations. Record distinct source or observed-output evidence for each applicable mode and include an unchanged control path; one focused target invocation is not broad build-system evidence.
- Multiple reviewers or models are not independent evidence when they inspect the same entry point and evaluation mode. Treat evidence as independent only when it covers distinct property sources, state transitions, import modes, or platform dimensions.
