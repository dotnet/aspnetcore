# Bounded evidence bundle: Meridian Tree

This synthetic fixture tests review discipline. It is not maintainer evidence.

## Scope and snapshots

- Component: Meridian Tree from package `Meridian.Blazor` 4.2.0.
- Published package SHA-256: `aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa`.
- Package metadata maps to public source commit `1111111111111111111111111111111111111111`.
- Current default branch is newer and must not replace package-source evidence.
- No scaffolder or AI skill is included.

## Public artifact and repository evidence

- Repository license is MIT. Package contains `PackageLicenseExpression=MIT`,
  `RepositoryUrl`, `RepositoryCommit`, `Authors`, and `ProjectUrl`.
- The dependency inventory and third-party notice review were not supplied.
- All net8.0 and net9.0 DLLs have strong names. Authenticode could not be checked on the review
  platform.
- NuGet author and repository signatures validate.
- Publishing uses OIDC.
- An SPDX SBOM exists for a source-tree rebuild. Its package digest differs from the published
  nupkg digest. No final-artifact provenance or retention record was supplied.

## Security and privacy evidence

- `SECURITY.md` contains a private reporting address and coordinated-disclosure wording.
- No default telemetry, phone-home request, or remote asset load was observed in source or the
  bounded browser network trace.
- A threat-model draft exists, but there is no completed security review or finding disposition.
- A current dependency scan has no High/Critical findings. There is no exact-release scan, patch
  cadence, emergency-release attestation, or render-mode trust-boundary review.

## Accessibility evidence

- Browser keyboard probes verify Arrow keys, Home, End, Enter, Space, and visible focus.
- Computed semantics expose tree, treeitem, group, expanded state, and selection.
- Async loading has no busy or live announcement. Forced-colors mode hides the focus indicator.
- RTL keyboard behavior and localized visible strings pass bounded browser probes.
- No automated accessibility scan, full assessment, screen-reader record, WCAG report, or release accessibility
  attestation was supplied.

## Blazor runtime evidence

- Package consumers run under prerendered Interactive Server and standalone WebAssembly.
- Prerender does not throw. Static SSR renders labels but has no documented interaction contract.
- Auto mode is claimed but was not tested.
- A deterministic callback-failure probe shows the component discards the task returned by
  `EventCallback.InvokeAsync`.
- A deterministic navigation/disposal probe shows a retained JS listener after component removal.
- Parameters use the standard binding pattern, serialization is typed, and JS initialization is
  post-render.
- Nullable analysis is disabled without a migration plan. Public API XML docs and samples are
  incomplete. No compatibility policy was supplied.

## Trimming, AOT, and performance evidence

- A package-based trimmed standalone WebAssembly publish succeeds without attributable warnings,
  loads in a browser, and exercises expansion and selection.
- The package does not set `<IsTrimmable>true</IsTrimmable>` and documents no equivalent supported
  trim-analysis configuration.
- Native WASM AOT is not claimed and was not tested. No trim/AOT support matrix was supplied.
- Keyed child reorder preserves identity in a deterministic probe.
- Source inspection finds no expensive synchronous render-time work.
- No representative large-tree measurement, Interactive Server circuit-memory bound, payload
  measurement, WASM bundle budget, or documented performance target was supplied.

## CI, release, and support evidence

- PR CI restores, builds, unit-tests, and packages net8.0 and net9.0.
- Browser and accessibility jobs exist but are optional.
- The release job rebuilds after PR validation before signing.
- Documentation snippets are not compiled. Tool prerequisites are documented.
- No release checklist maps evidence to requirement IDs.
- Public issues and the security route exist.
- No named support owner, response SLA, supported-version policy, patch cadence, EOL notice, or
  per-release revalidation record was supplied.
