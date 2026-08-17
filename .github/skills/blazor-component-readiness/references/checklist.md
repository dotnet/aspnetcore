# Blazor component readiness baseline

**Rubric version:** 1.3.0
**Scope schema version:** 1

This independently maintained 110-ID public core is the self-contained source of truth for
readiness reviews of released Blazor component packages. It does not require an external policy
document or private service. Organizations and adjacent deliverables may apply explicit overlays,
but a report must name the core and overlay versions it actually evaluated.

Every requirement has a stable ID. Preserve an ID when wording changes without changing the
requirement's intent. Add a new ID when the baseline adds a distinct obligation;
never reuse or renumber retired IDs. A complete core review must contain exactly one scorecard row
for every ID below. Scaffolder and AI-skill requirements are opt-in overlays under
`references/overlays/`; omit them unless the bounded deliverable actually contains that feature.

Repository-wide evidence may be reused only for the same pinned repository SHA and exact package
ID/version/digest. A requirement is `repository-wide` only when its exact truth and evidence apply
to every control at that identity without per-control behavior, API, sample, render-mode, browser,
accessibility, or test-coverage evidence. Otherwise it is `component-specific`.

`SEC-01` through `SEC-03`, `CI-04`, `CI-11`, and `SUP-09` intentionally remain
`component-specific`: each mixes a shared process with proof that the selected control was actually
covered. Do not widen them without first splitting the shared-policy and per-control obligations.

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

- **LP-01** (`repository-wide`) Uses an OSI-approved, non-copyleft license.
- **LP-02** (`repository-wide`) Public source repository for shipped versions.
- **LP-03** (`repository-wide`) Acceptable licenses for all direct and transitive dependencies.
- **LP-04** (`repository-wide`) Required third-party notices preserved.
- **LP-05** (`repository-wide`) NuGet `PackageLicenseExpression`.
- **LP-06** (`repository-wide`) NuGet `RepositoryUrl`.
- **LP-07** (`repository-wide`) NuGet `RepositoryCommit`.
- **LP-08** (`repository-wide`) NuGet `Authors`.
- **LP-09** (`repository-wide`) NuGet `ProjectUrl`.
- **LP-10** (`repository-wide`) Released package maps to an exact public source commit.

## 2. Package integrity, SBOM, and provenance

- **PI-01** (`repository-wide`) Every shipped assembly is strong-name signed.
- **PI-02** (`repository-wide`) Every shipped assembly is Authenticode signed with the maintainer's release identity.
- **PI-03** (`repository-wide`) NuGet package has a valid author signature.
- **PI-04** (`repository-wide`) NuGet.org repository signature is present where expected.
- **PI-05** (`repository-wide`) OIDC/trusted publishing is used instead of long-lived publish secrets.
- **PI-06** (`repository-wide`) SPDX or CycloneDX SBOM is published for every release.
- **PI-07** (`repository-wide`) SBOM includes direct and transitive NuGet/npm dependencies.
- **PI-08** (`repository-wide`) Bundled JS, CSS, fonts, themes, and other third-party assets are represented.
- **PI-09** (`repository-wide`) Required third-party license notices are represented.
- **PI-10** (`repository-wide`) SBOM and provenance bind to the exact final signed NuGet package digest.
- **PI-11** (`repository-wide`) Build provenance connects source SHA, workflow, dependencies, signatures, and publication.
- **PI-12** (`repository-wide`) Release evidence is retained long enough for validation and incident response.

Do not accept an SBOM for a separately rebuilt package as evidence for the NuGet.org artifact.

## 3. Security and vulnerability management

- **SEC-01** (`component-specific`) Documented threat model covers .NET, JS, browser, host page, render modes, and release pipeline.
- **SEC-02** (`component-specific`) Security review is completed before release.
- **SEC-03** (`component-specific`) Findings are resolved or accepted with documented rationale.
- **SEC-04** (`repository-wide`) `SECURITY.md` provides a private reporting channel.
- **SEC-05** (`repository-wide`) Coordinated disclosure process exists.
- **SEC-06** (`repository-wide`) Dependency vulnerability scanning runs for every release.
- **SEC-07** (`repository-wide`) No known unpatched High/Critical vulnerabilities at release time.
- **SEC-08** (`repository-wide`) Vulnerability servicing aligns with the supported .NET cadence.
- **SEC-09** (`repository-wide`) Emergency out-of-band release capability exists for exploited critical issues.
- **SEC-10** (`component-specific`) No unexpected default telemetry, phone-home behavior, or remote asset loading.
- **SEC-11** (`component-specific`) Optional telemetry is opt-in and documented.
- **SEC-12** (`component-specific`) Static SSR and Interactive Server trust boundaries are reviewed.
- **SEC-13** (`component-specific`) Browser event/input values are not represented as server authorization evidence.

## 4. Accessibility

- **A11Y-01** (`component-specific`) WCAG 2.2 AA conformance for the supported configuration.
- **A11Y-02** (`component-specific`) No known AA failures at ship.
- **A11Y-03** (`component-specific`) Automated accessibility scanning is clean for every release.
- **A11Y-04** (`component-specific`) A full accessibility assessment is completed at least once per major release.
- **A11Y-05** (`component-specific`) Representative supported screen-reader smoke testing is recorded.
- **A11Y-06** (`component-specific`) Keyboard-only operation is verified.
- **A11Y-07** (`component-specific`) Focus order, trapping, restoration, and visible focus are verified where applicable.
- **A11Y-08** (`component-specific`) Roles, names, values, states, and relationships are correct.
- **A11Y-09** (`component-specific`) Selection, expansion/collapse, validation, and async loading are announced.
- **A11Y-10** (`component-specific`) Windows High Contrast and CSS `forced-colors` are supported.
- **A11Y-11** (`component-specific`) User-facing strings are localizable.
- **A11Y-12** (`component-specific`) RTL support is recorded when claimed.

Evidence layers must remain separate:

1. source evidence;
2. automated scanner evidence;
3. browser interaction evidence;
4. screen-reader evidence;
5. formal conformance;
6. maintainer attestation.

## 5. Blazor engineering quality

- **BEQ-01** (`component-specific`) Latest stable .NET is supported on GA day.
- **BEQ-02** (`component-specific`) Supported render modes are explicit.
- **BEQ-03** (`component-specific`) Unsupported modes fail safely or are clearly documented.
- **BEQ-04** (`component-specific`) Prerendering does not throw.
- **BEQ-05** (`component-specific`) Static SSR output has a documented usefulness/accessibility contract.
- **BEQ-06** (`component-specific`) Interactive Server is tested.
- **BEQ-07** (`component-specific`) Interactive WebAssembly or standalone WASM is tested when supported.
- **BEQ-08** (`component-specific`) Auto transition behavior is tested when supported.
- **BEQ-09** (`component-specific`) `[Parameter]` properties follow framework guidance and are not mutated by the component.
- **BEQ-10** (`component-specific`) `[EditorRequired]` is used where appropriate.
- **BEQ-11** (`component-specific`) Events use `EventCallback`/`EventCallback<T>`.
- **BEQ-12** (`component-specific`) Callback tasks are awaited and failures reach the host error path.
- **BEQ-13** (`component-specific`) `InvokeAsync` and `StateHasChanged` are used correctly.
- **BEQ-14** (`component-specific`) Blocking and unobserved fire-and-forget work are absent.
- **BEQ-15** (`component-specific`) Timers, subscriptions, cancellation tokens, JS references, and modules are cleaned up.
- **BEQ-16** (`component-specific`) `IAsyncDisposable` is used when cleanup crosses JS or async boundaries.
- **BEQ-17** (`component-specific`) JS interop uses narrow module-scoped APIs.
- **BEQ-18** (`component-specific`) Initialization-time JS interop is avoided.
- **BEQ-19** (`component-specific`) Serialization is typed and correctly escapes untrusted values.
- **BEQ-20** (`component-specific`) CSS isolation or a documented scoped global-style contract is used.
- **BEQ-21** (`repository-wide`) Nullable and .NET/Blazor analyzer results are clean or have an accepted migration plan.
- **BEQ-22** (`component-specific`) Public APIs have accurate XML documentation.
- **BEQ-23** (`component-specific`) Public samples cover every supported render mode.
- **BEQ-24** (`repository-wide`) SemVer, experimental API, obsolete API, and compatibility policy are explicit.

BEQ-24 verifies that explicit package/API-governance policy exists. Whether an individual member
complies with that policy remains member-level API evidence and is not made repository-wide by this
row.

## 6. Trimming and native AOT

- **TA-01** (`component-specific`) Package-based trimmed WASM publish succeeds.
- **TA-02** (`component-specific`) Trim analyzer warnings are resolved or accepted with evidence.
- **TA-03** (`component-specific`) Browser/runtime smoke test succeeds for the trimmed artifact.
- **TA-04** (`component-specific`) Reflection/dynamic-code surfaces have appropriate annotations or generated alternatives.
- **TA-05** (`component-specific`) Native WASM AOT publish succeeds when claimed.
- **TA-06** (`component-specific`) Native AOT runtime smoke test succeeds when claimed.
- **TA-07** (`repository-wide`) The supported trim/AOT matrix is documented.
- **TA-08** (`repository-wide`) The package explicitly opts into trim analysis with `<IsTrimmable>true</IsTrimmable>` or documents an equivalent supported configuration.

Score configuration and runtime independently. A successful trimmed browser probe can verify
TA-01 through TA-03 while TA-08 remains a configuration or documentation defect.

## 7. Performance

- **PERF-01** (`component-specific`) `ShouldRender` is used only when justified.
- **PERF-02** (`component-specific`) `@key` is used where identity stability requires it.
- **PERF-03** (`component-specific`) Expensive render-time work is absent.
- **PERF-04** (`component-specific`) Large data sets use appropriate virtualization.
- **PERF-05** (`component-specific`) Cascading values do not cause unnecessary broad rerenders.
- **PERF-06** (`component-specific`) Interactive Server state and per-circuit memory are bounded.
- **PERF-07** (`component-specific`) Server-to-browser payload size, allocation, and copy costs are understood.
- **PERF-08** (`component-specific`) WASM dependency and bundle size are measured against a budget.
- **PERF-09** (`component-specific`) Startup, render, interaction, and large-data targets are documented.
- **PERF-10** (`component-specific`) Measurements cover supported representative scenarios.

## 8. CI, documentation, and release validation

- **CI-01** (`repository-wide`) PR CI restores, builds, tests, and packages relevant targets.
- **CI-02** (`component-specific`) Deterministic regression tests cover every accepted product defect.
- **CI-03** (`component-specific`) Browser tests cover claimed render modes and JS behavior.
- **CI-04** (`component-specific`) Accessibility smoke tests run at the agreed cadence.
- **CI-05** (`repository-wide`) Dependency scans and release checks are required gates.
- **CI-06** (`repository-wide`) Default and release refs require appropriate review/checks.
- **CI-07** (`repository-wide`) Untrusted build execution is separated from privileged signing/publishing.
- **CI-08** (`repository-wide`) Signing/publishing consumes a verified immutable artifact.
- **CI-09** (`component-specific`) Documentation examples compile and assert behavior, not only syntax.
- **CI-10** (`component-specific`) Node, .NET, browser, and workload prerequisites are correct.
- **CI-11** (`component-specific`) Release checklist revalidates every applicable requirement.

## 9. Support, servicing, and lifecycle

- **SUP-01** (`repository-wide`) Active maintainer/support owner is identified.
- **SUP-02** (`repository-wide`) Public contact exists.
- **SUP-03** (`repository-wide`) Response SLA is published.
- **SUP-04** (`repository-wide`) Supported versions are documented.
- **SUP-05** (`repository-wide`) Security patch cadence is documented.
- **SUP-06** (`repository-wide`) Public EOL notice precedes support termination.
- **SUP-07** (`repository-wide`) Non-security issues are tracked publicly.
- **SUP-08** (`repository-wide`) Security issues use coordinated disclosure.
- **SUP-09** (`component-specific`) Requirements are reverified for every release.
- **SUP-10** (`repository-wide`) The release process defines how readiness regressions suspend a release or supported status and how revalidation restores it.

## Version history

- **1.3.0:** Added normative per-requirement scope metadata and exact case-sensitive status
  validation. Requirement IDs, wording, intent, and status tokens are unchanged.
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
