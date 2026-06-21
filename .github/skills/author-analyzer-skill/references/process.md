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

Before coding, map the input space. This both bounds the implementation and becomes the analyzer's documentation. Partition every input into four buckets:

- **Should fire (true positives)**: each concrete pattern the rule flags. Enumerate the variants; each becomes a positive test.
- **Should not fire (true negatives)**: look-alikes that are correct or intentional: already-correct code, a similar-but-different API, an explicit opt-out. Each becomes a negative test and is a false-positive guard.
- **Detectable but too expensive**: patterns the rule *could* catch only with whole-program or deep data-flow analysis (e.g. following a call graph to an arbitrary depth). Decide explicitly not to handle them.
- **Undecidable from here**: facts that depend on a referenced assembly's *implementation* or another project's source. An analyzer reasons over the current compilation's source plus the **symbols/metadata** of its references, never their **bodies**, so it can't know what referenced code does at runtime or values computed elsewhere.

Design to this boundary:

- **Be precise where the answer is decidable.** For code that compiles, if the pattern is unambiguously right or wrong from the current compilation, aim to flag *all* of it with *no* false positives.
- **Make a reasonable effort elsewhere, and apply 80/20.** A non-comprehensive analyzer is valuable if it catches the common error cases cheaply and never cries wolf. Record the cases you skip as explicit non-goals rather than reporting on a guess.
- **Gate expensive depth behind a configurable effort/strictness option.** When deeper, costlier analysis would catch more, expose it as an option read from `.editorconfig`/`AnalyzerConfigOptions` (a `dotnet_diagnostic`-style key or a `build_property`), read once in `RegisterCompilationStartAction`. Default it conservative so live IDE editing stays fast, and let non-interactive runs (CI) opt into the stricter, deeper analysis. The IDE's per-keystroke budget is the constraint to protect; a batch build can afford more.

**Document the analyzer using the proposal template as the follow-up PR description.** The `60_analyzer_proposal.md` template (`.github/ISSUE_TEMPLATE/60_analyzer_proposal.md`, labels `api-suggestion`, `analyzer`) is the structure to explain the analyzer in its PR:

- **Background and motivation**: the real problem (e.g. a perf pitfall, insecure pattern, or a better API). The bar: is it common and confusing enough to warrant an always-on check?
- **Analyzer behavior and message**: when it fires and the exact message text.
- **Category** and **severity** (from the lists below).
- **Usage scenarios**: the *should-fire* and *should-not-fire* buckets, mirrored 1:1 by the tests.
- **Non-goals and limitations**: the *too-expensive* and *undecidable* buckets, with the 80/20 rationale and any effort-option behavior.
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

- [ ] Cases analyzed: should-fire / should-not-fire / too-expensive / undecidable buckets enumerated; non-goals recorded.
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
