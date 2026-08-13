# Blazor component readiness baseline

**Rubric version:** 1.2.0

This independently maintained 110-ID public core is the self-contained source of truth for
readiness reviews of released Blazor component packages. It does not require an external policy
document or private service. Organizations and adjacent deliverables may apply explicit overlays,
but a report must name the core and overlay versions it actually evaluated.

Every requirement has a stable ID. Preserve an ID when wording changes without changing the
requirement's intent. Add a new ID when the baseline adds a distinct obligation;
never reuse or renumber retired IDs. A complete core review must contain exactly one scorecard row
for every ID below. Scaffolder and AI-skill requirements are opt-in overlays under
`references/overlays/`; omit them unless the bounded deliverable actually contains that feature.

## Scorecard columns

Use these columns for every row:

| Column | Content |
|---|---|
| Requirement ID | Stable ID from this checklist |
| Requirement | One requirement, not a category summary |
| Requirement scope | `repository-wide` or `component-specific` |
| Status | `verified`, `defect`, `maintainer evidence required`, `not tested`, or `not applicable` |
| Evidence | Exact artifact, path, probe, report, or attestation |
| Maintainer action | Remediation or evidence the maintainer owns |
| Reviewer follow-up | Bounded validation to perform |

## 1. Licensing and provenance

- **LP-01** Uses an OSI-approved, non-copyleft license.
- **LP-02** Public source repository for shipped versions.
- **LP-03** Acceptable licenses for all direct and transitive dependencies.
- **LP-04** Required third-party notices preserved.
- **LP-05** NuGet `PackageLicenseExpression`.
- **LP-06** NuGet `RepositoryUrl`.
- **LP-07** NuGet `RepositoryCommit`.
- **LP-08** NuGet `Authors`.
- **LP-09** NuGet `ProjectUrl`.
- **LP-10** Released package maps to an exact public source commit.

## 2. Package integrity, SBOM, and provenance

- **PI-01** Every shipped assembly is strong-name signed.
- **PI-02** Every shipped assembly is Authenticode signed with the maintainer's release identity.
- **PI-03** NuGet package has a valid author signature.
- **PI-04** NuGet.org repository signature is present where expected.
- **PI-05** OIDC/trusted publishing is used instead of long-lived publish secrets.
- **PI-06** SPDX or CycloneDX SBOM is published for every release.
- **PI-07** SBOM includes direct and transitive NuGet/npm dependencies.
- **PI-08** Bundled JS, CSS, fonts, themes, and other third-party assets are represented.
- **PI-09** Required third-party license notices are represented.
- **PI-10** SBOM and provenance bind to the exact final signed NuGet package digest.
- **PI-11** Build provenance connects source SHA, workflow, dependencies, signatures, and publication.
- **PI-12** Release evidence is retained long enough for validation and incident response.

Do not accept an SBOM for a separately rebuilt package as evidence for the NuGet.org artifact.

## 3. Security and vulnerability management

- **SEC-01** Documented threat model covers .NET, JS, browser, host page, render modes, and release pipeline.
- **SEC-02** Security review is completed before release.
- **SEC-03** Findings are resolved or accepted with documented rationale.
- **SEC-04** `SECURITY.md` provides a private reporting channel.
- **SEC-05** Coordinated disclosure process exists.
- **SEC-06** Dependency vulnerability scanning runs for every release.
- **SEC-07** No known unpatched High/Critical vulnerabilities at release time.
- **SEC-08** Vulnerability servicing aligns with the supported .NET cadence.
- **SEC-09** Emergency out-of-band release capability exists for exploited critical issues.
- **SEC-10** No unexpected default telemetry, phone-home behavior, or remote asset loading.
- **SEC-11** Optional telemetry is opt-in and documented.
- **SEC-12** Static SSR and Interactive Server trust boundaries are reviewed.
- **SEC-13** Browser event/input values are not represented as server authorization evidence.

## 4. Accessibility

- **A11Y-01** WCAG 2.2 AA conformance for the supported configuration.
- **A11Y-02** No known AA failures at ship.
- **A11Y-03** Automated accessibility scanning is clean for every release.
- **A11Y-04** A full accessibility assessment is completed at least once per major release.
- **A11Y-05** Representative supported screen-reader smoke testing is recorded.
- **A11Y-06** Keyboard-only operation is verified.
- **A11Y-07** Focus order, trapping, restoration, and visible focus are verified where applicable.
- **A11Y-08** Roles, names, values, states, and relationships are correct.
- **A11Y-09** Selection, expansion/collapse, validation, and async loading are announced.
- **A11Y-10** Windows High Contrast and CSS `forced-colors` are supported.
- **A11Y-11** User-facing strings are localizable.
- **A11Y-12** RTL support is recorded when claimed.

Evidence layers must remain separate:

1. source evidence;
2. automated scanner evidence;
3. browser interaction evidence;
4. screen-reader evidence;
5. formal conformance;
6. maintainer attestation.

## 5. Blazor engineering quality

- **BEQ-01** Latest stable .NET is supported on GA day.
- **BEQ-02** Supported render modes are explicit.
- **BEQ-03** Unsupported modes fail safely or are clearly documented.
- **BEQ-04** Prerendering does not throw.
- **BEQ-05** Static SSR output has a documented usefulness/accessibility contract.
- **BEQ-06** Interactive Server is tested.
- **BEQ-07** Interactive WebAssembly or standalone WASM is tested when supported.
- **BEQ-08** Auto transition behavior is tested when supported.
- **BEQ-09** `[Parameter]` properties follow framework guidance and are not mutated by the component.
- **BEQ-10** `[EditorRequired]` is used where appropriate.
- **BEQ-11** Events use `EventCallback`/`EventCallback<T>`.
- **BEQ-12** Callback tasks are awaited and failures reach the host error path.
- **BEQ-13** `InvokeAsync` and `StateHasChanged` are used correctly.
- **BEQ-14** Blocking and unobserved fire-and-forget work are absent.
- **BEQ-15** Timers, subscriptions, cancellation tokens, JS references, and modules are cleaned up.
- **BEQ-16** `IAsyncDisposable` is used when cleanup crosses JS or async boundaries.
- **BEQ-17** JS interop uses narrow module-scoped APIs.
- **BEQ-18** Initialization-time JS interop is avoided.
- **BEQ-19** Serialization is typed and correctly escapes untrusted values.
- **BEQ-20** CSS isolation or a documented scoped global-style contract is used.
- **BEQ-21** Nullable and .NET/Blazor analyzer results are clean or have an accepted migration plan.
- **BEQ-22** Public APIs have accurate XML documentation.
- **BEQ-23** Public samples cover every supported render mode.
- **BEQ-24** SemVer, experimental API, obsolete API, and compatibility policy are explicit.

## 6. Trimming and native AOT

- **TA-01** Package-based trimmed WASM publish succeeds.
- **TA-02** Trim analyzer warnings are resolved or accepted with evidence.
- **TA-03** Browser/runtime smoke test succeeds for the trimmed artifact.
- **TA-04** Reflection/dynamic-code surfaces have appropriate annotations or generated alternatives.
- **TA-05** Native WASM AOT publish succeeds when claimed.
- **TA-06** Native AOT runtime smoke test succeeds when claimed.
- **TA-07** The supported trim/AOT matrix is documented.
- **TA-08** The package explicitly opts into trim analysis with `<IsTrimmable>true</IsTrimmable>` or documents an equivalent supported configuration.

Score configuration and runtime independently. A successful trimmed browser probe can verify
TA-01 through TA-03 while TA-08 remains a configuration or documentation defect.

## 7. Performance

- **PERF-01** `ShouldRender` is used only when justified.
- **PERF-02** `@key` is used where identity stability requires it.
- **PERF-03** Expensive render-time work is absent.
- **PERF-04** Large data sets use appropriate virtualization.
- **PERF-05** Cascading values do not cause unnecessary broad rerenders.
- **PERF-06** Interactive Server state and per-circuit memory are bounded.
- **PERF-07** Server-to-browser payload size, allocation, and copy costs are understood.
- **PERF-08** WASM dependency and bundle size are measured against a budget.
- **PERF-09** Startup, render, interaction, and large-data targets are documented.
- **PERF-10** Measurements cover supported representative scenarios.

## 8. CI, documentation, and release validation

- **CI-01** PR CI restores, builds, tests, and packages relevant targets.
- **CI-02** Deterministic regression tests cover every accepted product defect.
- **CI-03** Browser tests cover claimed render modes and JS behavior.
- **CI-04** Accessibility smoke tests run at the agreed cadence.
- **CI-05** Dependency scans and release checks are required gates.
- **CI-06** Default and release refs require appropriate review/checks.
- **CI-07** Untrusted build execution is separated from privileged signing/publishing.
- **CI-08** Signing/publishing consumes a verified immutable artifact.
- **CI-09** Documentation examples compile and assert behavior, not only syntax.
- **CI-10** Node, .NET, browser, and workload prerequisites are correct.
- **CI-11** Release checklist revalidates every applicable requirement.

## 9. Support, servicing, and lifecycle

- **SUP-01** Active maintainer/support owner is identified.
- **SUP-02** Public contact exists.
- **SUP-03** Response SLA is published.
- **SUP-04** Supported versions are documented.
- **SUP-05** Security patch cadence is documented.
- **SUP-06** Public EOL notice precedes support termination.
- **SUP-07** Non-security issues are tracked publicly.
- **SUP-08** Security issues use coordinated disclosure.
- **SUP-09** Requirements are reverified for every release.
- **SUP-10** The release process defines how readiness regressions suspend a release or supported status and how revalidation restores it.

## Version history

- **1.2.0:** Standardized released-package acquisition and mode selection, shared exact-artifact
  evidence across batched controls, clarified status boundaries, added targeted starter profiles and
  probe preflight, and added structural validation receipts. Requirement IDs and intent are unchanged.
- **1.1.0:** Moved the 12 scaffolder and AI-skill requirements into opt-in overlays, leaving a
  110-ID released-package core. Rephrased accessibility tooling requirements as outcomes.
- **1.0.1:** Restored SUP-10 as a maintainer-owned release-governance obligation after migration
  replays showed that reviewer-authority wording created a cosmetic verified row without product
  evidence.
- **1.0.0:** Published the self-contained baseline and added TA-08 to separate trim-analysis
  configuration from trimmed runtime behavior.
