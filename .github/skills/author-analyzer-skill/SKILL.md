---
name: author-analyzer-skill
description: >-
  Author, review, and ship Roslyn analyzers and code fixes in the dotnet/aspnetcore repo.
  USE FOR writing a new DiagnosticAnalyzer or CodeFixProvider, reviewing an analyzer PR,
  choosing a diagnostic ID/category/severity, wiring localized messages, registering well-known
  types, writing analyzer/code-fix unit tests, fixing an analyzer false positive, diagnosing why a
  code fix's "Fix all"/FixAll doesn't appear, or following the analyzer proposal-to-ship process.
  Covers ASP/MVC/API/BL diagnostic IDs, DiagnosticDescriptors conventions, WellKnownTypes infra,
  Microsoft.CodeAnalysis.Testing verifiers, performance/correctness rules, and do's & don'ts.
  DO NOT USE FOR writing source generators, Razor compiler changes, or non-analyzer runtime code.
---

# Writing analyzers in ASP.NET Core

This repo ships Roslyn analyzers and code fixes that guide users toward correct, secure, performant ASP.NET Core code. This skill captures the repo's conventions and the Roslyn best practices that keep analyzers fast, correct, and free of false positives.

> An analyzer runs on every build and live in the IDE, on every keystroke, across millions of nodes. A slow or wrong analyzer degrades everyone's experience. Hold a high bar: **when in doubt, don't report.**

## Where analyzers live

| Area | Analyzer project | ID prefix |
|------|------------------|-----------|
| Framework (minimal APIs, routing, Kestrel, auth, headers) | `src/Framework/AspNetCoreAnalyzers/src/Analyzers` | `ASP####` |
| Code fixes for the above | `src/Framework/AspNetCoreAnalyzers/src/CodeFixes` | — |
| MVC | `src/Mvc/Mvc.Analyzers` | `MVC####` |
| MVC API (OpenAPI conventions) | `src/Mvc/Mvc.Api.Analyzers` | `API####` |
| Blazor Components | `src/Components/Analyzers` | `BL####` |

Pick the project matching the API area you are guarding. Most new work lands in the Framework set.

> **Scope note.** This skill's conventions describe the **Framework set** (`src/Framework/AspNetCoreAnalyzers`) — the model to follow for new analyzers. The MVC, API, and Components projects have their own, sometimes older patterns (e.g. inline diagnostic strings, or an analyzer that references Workspaces). When editing those, **match the surrounding area** rather than blindly applying the Framework conventions.

## Decision: should this be an analyzer?

Write one only when **all** hold (details in [references/process.md](references/process.md)):
- The mistake is detectable at compile time from syntax/symbols/operations **without false positives**.
- It is actionable — the message (or a code fix) tells the user exactly what to change.
- It is not already caught by the C# compiler.

Runtime-only or highly context-dependent concerns belong in tests or docs, not an analyzer. New analyzers go through the **analyzer proposal** review (issue template `60_analyzer_proposal.md`) before implementation — the same bar as an API proposal.

## Authoring workflow

1. **Reserve the ID & design the diagnostic.** Pick the next free ID in the area's prefix, choose a [category](references/process.md#categories) and [severity](references/process.md#severity), and register it in `docs/list-of-diagnostics.md`. Add the descriptor to the area's `DiagnosticDescriptors.cs` using `CreateLocalizableResourceString(nameof(Resources.<Key>))` for title/message and the shared `GetHelpLinkUri(id)` helper. Add the strings to `Resources.resx`.
2. **Write the analyzer.** Follow the canonical shape below. Prefer `IOperation`/symbol analysis over raw syntax. Cache well-known types once via the shared `WellKnownTypes` infra.
3. **Write the code fix** (optional but encouraged) in the CodeFixes project.
4. **Write tests** with the `CSharpAnalyzerVerifier`/`CSharpCodeFixVerifier` helpers and `{|#0:...|}` markup. See [references/testing.md](references/testing.md).
5. **Build & test the area:** activate the local SDK first (`. ./activate.ps1` on Windows, `source activate.sh` otherwise), then run that area's `build.sh -test`, e.g. `./src/Framework/build.sh -test`.

### Canonical analyzer shape

```csharp
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MyAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.MyRule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None); // skip generated code
        context.EnableConcurrentExecution();                                     // required for IDE perf
        context.RegisterCompilationStartAction(static compilationContext =>
        {
            // Cache well-known symbols ONCE per compilation, then close over them.
            var wellKnownTypes = WellKnownTypes.GetOrCreate(compilationContext.Compilation);
            compilationContext.RegisterOperationAction(
                operationContext => Analyze(operationContext, wellKnownTypes),
                OperationKind.Invocation);
        });
    }
}
```

Need a new well-known type? Add it to the `WellKnownType` enum in the Framework set's `WellKnownTypeData.cs` (and the parallel metadata-name array), then resolve it through the cached `WellKnownTypes.GetOrCreate(compilation)` instance — use `.Get(...)` for types that must exist and `.GetOptional(...)` for ones that legitimately may be absent. Don't call `GetTypeByMetadataName` per node.

Not every analyzer needs the compilation-start cache: if you don't look up well-known types, you can register an operation/symbol action directly in `Initialize` (e.g. `HeaderDictionaryIndexerAnalyzer`). Use `RegisterCompilationStartAction` when you need to resolve and cache symbols once per compilation. This `WellKnownTypes` infrastructure is the Framework set's; MVC/Components analyzers have their own symbol caches (e.g. `ComponentSymbols`).

## The five rules that matter most

These prevent the bugs that actually ship. Full catalog with citations in [references/best-practices.md](references/best-practices.md).

1. **Cache, don't re-resolve.** When you need well-known symbols, resolve them once in `RegisterCompilationStartAction` and close over them; never store `Compilation`/`ISymbol`/`IOperation` in analyzer fields (they go stale across compilations — RS1008).
2. **Be cheap and bail early.** Order checks cheapest-first and `return` immediately. No LINQ/allocations or `DescendantNodes()` walks in per-node callbacks. Prefer `IOperation` over syntax for semantics.
3. **Compare symbols correctly.** Use `SymbolEqualityComparer.Default.Equals(...)`, never `==` (RS1024). In the Framework set, don't reference `Document`/`Solution`/`Workspace` types from an analyzer (RS1022, enforced via `EnforceExtendedAnalyzerRules`) — only from a fixer.
4. **Localize and link (Framework set).** A `DiagnosticDescriptor`'s title/message/description are `LocalizableResourceString` from `Resources.resx` (RS1007) with a help link; the title has no trailing period, the message ends with one. (Code-fix *action* titles may be plain strings.)
5. **Don't cry wolf.** Bail on parse errors and interleaved `#directives`; report on the **smallest** meaningful span; pass data to the fixer via `diagnostic.Properties` instead of re-computing.

## Code fixes

- `[ExportCodeFixProvider(LanguageNames.CSharp), Shared]`; list exactly your IDs in `FixableDiagnosticIds`.
- Override `GetFixAllProvider()` — return `WellKnownFixAllProviders.BatchFixer` for simple, local, non-overlapping fixes; a custom singleton `FixAllProvider` otherwise.
- Every `CodeAction.Create(...)` needs a non-null `equivalenceKey` (RS1010). Keep the fix minimal, preserve trivia, accept and propagate `CancellationToken` with `ConfigureAwait(false)`.

```csharp
[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class MyFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.MyRule.Id);

    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        foreach (var diagnostic in context.Diagnostics)
        {
            context.RegisterCodeFix(
                CodeAction.Create("Use the recommended API",
                    ct => ApplyFixAsync(context.Document, root!, diagnostic, ct),
                    equivalenceKey: DiagnosticDescriptors.MyRule.Id), // non-null key => FixAll works
                diagnostic);
        }
    }
}
```

> "Fix all occurrences" missing in the IDE almost always means a missing `GetFixAllProvider()` override or a null `equivalenceKey`.

If a fix only needs symbols, an analyzer can pass pre-computed data through `diagnostic.Properties` so the fixer doesn't redo analysis. A code-fix project *may* reference Workspaces types; an analyzer must not.

See [references/testing.md](references/testing.md) for test patterns and [references/conventions.md](references/conventions.md) for project/packaging/localization details.

## Reviewing an analyzer change

Apply the same bar: confirm the ID is reserved in `docs/list-of-diagnostics.md`, the descriptor is localized with a help link, `EnableConcurrentExecution`/`ConfigureGeneratedCodeAnalysis` are set, symbols are cached and compared with `SymbolEqualityComparer`, no Workspaces types leak into the analyzer, the reported span is minimal, false-positive guards exist, and there are positive **and** negative tests.
