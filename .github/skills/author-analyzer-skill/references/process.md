# Process: designing, documenting, and shipping a new analyzer

Map the cases first, build to that boundary, then document the analyzer in its PR. For a significant or behavior-defining rule, the same write-up can go through an analyzer proposal *before* implementation so severity, category, and false-positive scope are reviewed early; changing the severity of, or removing, a rule that already shipped is a churn/compat cost. Routine or low-risk analyzers are often added without a formal proposal, so use judgment based on how visible and opinionated the rule is.

## Table of contents
- [1. Analyze the cases (and document them)](#1-analyze-the-cases-and-document-them)
- [Categories](#categories)
- [Severity](#severity)
- [2. Implement](#2-implement)
- [3. Test, document, ship](#3-test-document-ship)
- [Checklist](#checklist)

## 1. Analyze the cases (and document them)

Before coding, map the input space. This both bounds the implementation and becomes the analyzer's documentation. Start with the two concrete test sets:

- **Should fire (true positives)**: each concrete pattern the rule flags. Enumerate the variants; each becomes a positive test.
- **Should not fire (true negatives)**: look-alikes that are correct or intentional: already-correct code, a similar-but-different API, an explicit opt-out. Each becomes a negative test and is a false-positive guard.

For everything beyond those two sets, coverage is a **return-on-investment call, not a binary include/exclude**. Default to being **aggressive: try to detect the issue.** Bail out of a case only when you can name a concrete reason the return doesn't justify the cost, grounded in:

- **Effort and complexity**: the runtime cost in the per-keystroke IDE budget, and the implementation/maintenance complexity of detecting it reliably.
- **Commonality and impact**: how often the case occurs and how much the mistake hurts.

Investing where the return is high and skipping the low-return long tail is what 80/20 means here.

- **Cheap and decidable: go for full coverage.** If the current compilation can decide the case unambiguously and cheaply, for code that compiles, flag all of it with no false positives.
- **Common cases cheap, long tail expensive or complex: cover the common cases, make the rest opt-in.** Handle the common cases reliably, and offer the deeper analysis that catches the long tail behind a **configuration option** (an effort/strictness key read from `.editorconfig`/`AnalyzerConfigOptions`, e.g. a `dotnet_diagnostic`-style key or a `build_property`, read once in `RegisterCompilationStartAction`) with a conservative default, rather than dropping it silently or paying its cost for everyone. Live IDE editing stays fast; CI can opt into deeper, stricter analysis.
- **Reliable detection impossible from here: stop, and say so.** When a case depends on a referenced assembly's *implementation*, runtime values, or whole-program reasoning that can't be made sound, detection isn't tractable. An analyzer reasons over the current compilation's source plus the **symbols/metadata** of its references, never their **bodies**, so cross-assembly facts usually fall here. Document it as a known limitation; offer an opt-in heuristic only if it earns its keep.

**Give the developer the call, and show your work.** Record where you drew the line and why (effort, complexity, commonality) in the analyzer's documentation, and expose a configuration knob wherever the right answer depends on the consumer's context, so they can push for more or less detection.

**Document the analyzer using the proposal template as the follow-up PR description.** The `60_analyzer_proposal.md` template (`.github/ISSUE_TEMPLATE/60_analyzer_proposal.md`, labels `api-suggestion`, `analyzer`) is the structure to explain the analyzer in its PR:

- **Background and motivation**: the real problem (e.g. a perf pitfall, insecure pattern, or a better API). The bar: is it common and confusing enough to warrant an always-on check?
- **Analyzer behavior and message**: when it fires and the exact message text.
- **Category** and **severity** (from the lists below).
- **Usage scenarios**: the *should-fire* and *should-not-fire* buckets, mirrored 1:1 by the tests.
- **Non-goals and limitations**: the cases you chose not to detect (or made opt-in) and why, grounded in effort, complexity, and commonality, plus any configuration knob and its default.
- **Risks**: false-positive surface, breaking changes, perf.

Browse open issues labeled `analyzer` for prior art and to avoid duplicates. When a rule does go through formal review, it usually follows the [API review process](https://github.com/dotnet/aspnetcore/blob/main/docs/APIReviewProcess.md).

## Categories

Use the standard [.NET analyzer categories](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/categories). Pick the closest existing one; in-repo the common choices are **Usage**, **Security**, **Naming**, and **Encapsulation** (Components). Others: Design, Documentation, Globalization, Interoperability, Maintainability, Performance, Reliability, Style.

## Severity

See [configuring severity](https://learn.microsoft.com/visualstudio/code-quality/use-roslyn-analyzers#configure-severity-levels).

| Severity | Use when |
|----------|----------|
| `Error` | The code is essentially always wrong / will fail at runtime or build; no legitimate exceptions. |
| `Warning` | Strongly recommended fix, default-on. The most common choice for ASP.NET Core guidance. |
| `Info` | A suggestion / lower-confidence improvement; surfaces lightly in the IDE. |
| `Hidden` | No visible squiggle; exists mainly to light up a code fix or fade-out. |

`isEnabledByDefault: true` for shipped rules unless there's a specific reason to default-off (e.g. an opt-in style rule). Users can always override per-project via `dotnet_diagnostic.<ID>.severity = ...` in `.editorconfig`; every `DiagnosticDescriptor` supports this automatically, so you rarely need a custom option. A separate **effort/strictness** option (above) is different from severity: severity controls how loudly a found issue is reported, the effort option controls how hard the analyzer looks.

## 2. Implement

1. **Reserve the ID** in `docs/list-of-diagnostics.md` and add the descriptor to the area's `DiagnosticDescriptors.cs` with localized strings + help link (see [conventions.md](conventions.md)).
2. **Write the analyzer** following the canonical shape in the main SKILL and the [best practices](best-practices.md). Register any new well-known types in `WellKnownTypeData.cs`. Implement to the boundary from step 1: handle the decidable cases precisely and stop at the documented non-goals.
3. **Write a code fix** if the correction is mechanical. Keep it minimal and FixAll-safe.
4. Add `Resources.resx` strings for the title/message (and code-fix title if you localize it).

## 3. Test, document, ship

- Unit-test with the repo verifiers: positive (diagnostic at the marked span) **and** negative (no diagnostic) cases, plus code-fix before/after. Add boundary-pinning negative tests for the *too-expensive* and *undecidable* buckets so those gaps stay intentional. See [testing.md](testing.md).
- Build & test the area with the local SDK activated: `. ./activate.ps1` (Windows) / `source activate.sh`, then `./src/<Area>/build.sh -test`.
- The PR description is the analyzer's documentation (the buckets from step 1). User-facing docs for ASP IDs live at `learn.microsoft.com/aspnet/core/diagnostics/<id>` (the help link target); coordinate them with the PR.

## Checklist

- [ ] Cases analyzed: should-fire and should-not-fire enumerated; coverage trade-offs (effort, complexity, commonality) decided, with any non-goals and configuration knobs recorded.
- [ ] Design reviewed, or a proposal filed (template `60_analyzer_proposal.md`), if the rule is opinionated or behavior-changing.
- [ ] ID reserved in `docs/list-of-diagnostics.md`; descriptor added to `DiagnosticDescriptors.cs`.
- [ ] Title/message in `Resources.resx`; title has no period, message ends with one.
- [ ] Help link via `GetHelpLinkUri(id)` (don't hand-write).
- [ ] `EnableConcurrentExecution()` + `ConfigureGeneratedCodeAnalysis(None)` set.
- [ ] Well-known symbols cached via `WellKnownTypes.GetOrCreate`; nothing stored in fields.
- [ ] `SymbolEqualityComparer.Default` for symbol comparisons; no Workspaces types in the analyzer.
- [ ] Reports on the smallest span; false-positive guards (parse errors, directives) in place.
- [ ] Code fix (if any): exported, `FixableDiagnosticIds` set, `GetFixAllProvider` overridden, non-null `equivalenceKey`, trivia preserved.
- [ ] Positive + negative tests, including boundary tests for the documented non-goals; code-fix before/after tests; area `build.sh -test` green.
- [ ] PR description documents behavior, the fire/no-fire scenarios, and the non-goals/limitations.
