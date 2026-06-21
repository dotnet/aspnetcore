# Process: proposing and shipping a new analyzer

For a significant or behavior-defining analyzer, write up the design first (the proposal template below) so severity, category, and false-positive scope get reviewed before it ships; changing the severity of, or removing, a rule that already shipped is a churn/compat cost. Small or uncontroversial analyzers are often added without a formal proposal, so use judgment based on how visible and opinionated the rule is.

## Table of contents
- [1. Propose](#1-propose)
- [2. Categories](#categories)
- [3. Severity](#severity)
- [4. Implement](#4-implement)
- [5. Test, document, ship](#5-test-document-ship)
- [Checklist](#checklist)

## 1. Propose

When you do write a proposal, the **Analyzer proposal** template (`.github/ISSUE_TEMPLATE/60_analyzer_proposal.md`, labels `api-suggestion`, `analyzer`) captures:

- **Background and motivation**: the real problem (e.g. avoid a perf pitfall, insecure pattern, or a better API). The bar: is this common and confusing enough to warrant an always-on check?
- **Analyzer behavior and message**: when it triggers and the exact message text.
- **Category** and **severity** (choose from the lists below).
- **Usage scenarios**: code that triggers it (mark the flagged span) and code that must NOT trigger it (the false-positive cases). If a code fix is included, describe the result.
- **Risks**: breaking changes, possible false positives, perf considerations.

Browse open issues labeled `analyzer` for prior art and to avoid duplicates. When a proposal is formally reviewed, it usually follows the [API review process](https://github.com/dotnet/aspnetcore/blob/main/docs/APIReviewProcess.md).

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

`isEnabledByDefault: true` for shipped rules unless there's a specific reason to default-off (e.g. an opt-in style rule). Users can always override per-project via `dotnet_diagnostic.<ID>.severity = ...` in `.editorconfig`; every `DiagnosticDescriptor` supports this automatically, so you rarely need a custom option.

## 4. Implement

1. **Reserve the ID** in `docs/list-of-diagnostics.md` and add the descriptor to the area's `DiagnosticDescriptors.cs` with localized strings + help link (see [conventions.md](conventions.md)).
2. **Write the analyzer** following the canonical shape in the main SKILL and the [best practices](best-practices.md). Register any new well-known types in `WellKnownTypeData.cs`.
3. **Write a code fix** if the correction is mechanical. Keep it minimal and FixAll-safe.
4. Add `Resources.resx` strings for the title/message (and code-fix title if you localize it).

## 5. Test, document, ship

- Unit-test with the repo verifiers: positive (diagnostic at the marked span) **and** negative (no diagnostic) cases, plus code-fix before/after. See [testing.md](testing.md).
- Build & test the area with the local SDK activated: `. ./activate.ps1` (Windows) / `source activate.sh`, then `./src/<Area>/build.sh -test`.
- Documentation for ASP IDs lives at `learn.microsoft.com/aspnet/core/diagnostics/<id>` (the help link target). Coordinate the doc with the proposal/PR.

## Checklist

- [ ] Design reviewed, or a proposal filed (template `60_analyzer_proposal.md`), if the rule is opinionated or behavior-changing.
- [ ] ID reserved in `docs/list-of-diagnostics.md`; descriptor added to `DiagnosticDescriptors.cs`.
- [ ] Title/message in `Resources.resx`; title has no period, message ends with one.
- [ ] Help link via `GetHelpLinkUri(id)` (don't hand-write).
- [ ] `EnableConcurrentExecution()` + `ConfigureGeneratedCodeAnalysis(None)` set.
- [ ] Well-known symbols cached via `WellKnownTypes.GetOrCreate`; nothing stored in fields.
- [ ] `SymbolEqualityComparer.Default` for symbol comparisons; no Workspaces types in the analyzer.
- [ ] Reports on the smallest span; false-positive guards (parse errors, directives) in place.
- [ ] Code fix (if any): exported, `FixableDiagnosticIds` set, `GetFixAllProvider` overridden, non-null `equivalenceKey`, trivia preserved.
- [ ] Positive + negative tests; code-fix before/after tests; area `build.sh -test` green.
