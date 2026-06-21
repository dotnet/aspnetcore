# Roslyn analyzer & code-fix best practices

Each rule names the enforcing `RSxxxx` meta-analyzer where one exists, so you can look it up. `EnforceExtendedAnalyzerRules=true` in the Framework analyzer projects turns many of these on as build errors; treat them as hard requirements, not suggestions. Enforced rules are stated as one-liners (the `RS` code is the rationale); judgment calls are spelled out as *Trigger / Decision / Why / Scope*.

## Table of contents
- [Performance](#performance)
- [Concurrency & state](#concurrency--state)
- [Correctness](#correctness)
- [Diagnostic design](#diagnostic-design)
- [Code fixes](#code-fixes)
- [Anti-patterns (do NOT do)](#anti-patterns-do-not-do)

## Performance

An analyzer's callbacks run on every node/symbol/operation of their kind, in every file, live in the IDE. Allocation and redundant work here are felt as editor lag.

- **Cache well-known symbols once per compilation.** Resolve them inside `RegisterCompilationStartAction` and close over the result; never resolve per node/symbol. In this repo, use `WellKnownTypes.GetOrCreate(compilation)`.
- Use `node.IsKind(SyntaxKind.X)`, not `node.Kind() == SyntaxKind.X` (RS1034).
- Never call `Compilation.GetSemanticModel()` inside a callback (RS1030); use the model already on the action context. It bypasses the host's semantic-model cache and forces a full re-bind, causing visible IDE hangs.
- No allocations in hot callbacks: avoid `new List<>()`, LINQ, and `DescendantNodes()` walks inside per-node/per-operation actions. Scope walks with `RegisterCodeBlockStartAction` / `RegisterOperationBlockStartAction`; when you must accumulate, reuse buffers or Roslyn's pooled collections (e.g. `ArrayBuilder<T>.GetInstance(...)`) where accessible.

- **Prefer `IOperation`/symbol analysis over raw syntax.** *Trigger:* the check needs semantics (what a call resolves to, a type, a value) rather than pure source shape. *Decision:* register an operation or symbol action and read symbols/operations; reserve `RegisterSyntaxNodeAction` for purely structural checks. *Why:* operation/symbol analysis is faster, allocates less, and is language-version- and syntax-agnostic, so it keeps working when new C# syntax expresses the same construct. *Scope:* common (use syntax when the rule genuinely is about a token or structure, e.g. a missing modifier).
- **Order gates cheapest-first and bail at the first failure.** *Trigger:* a callback that does any non-trivial work. *Decision:* check the cheap discriminators first (language version, a syntactic shape, parse errors) and `return` before the expensive semantic lookups. *Why:* the callback fires on huge numbers of nodes, so the common (non-matching) case must reach a `return` after only a few cheap comparisons. *Scope:* near-universal.

## Concurrency & state

- Call `context.EnableConcurrentExecution()` in `Initialize` (RS1026) so the host can parallelize.
- **Analyzers must be stateless across compilations.** Never store `Compilation`, `ISymbol`, or `IOperation` (or anything holding them) in instance fields (RS1008); instances are reused across compilations and concurrent callbacks. Flow state through start-action closures.
- With concurrency enabled, any state shared between your own callbacks must be thread-safe (`ConcurrentDictionary`, immutable collections). Don't depend on action ordering except the start to inner to end nesting guarantee, and don't rely on compilation-end actions running in the IDE.

- **Configure generated-code analysis to match what the rule inspects.** *Trigger:* setting the `ConfigureGeneratedCodeAnalysis(...)` flags (RS1025), which every analyzer must call. *Decision:* pass `GeneratedCodeAnalysisFlags.None` for rules about hand-written code; pass `Analyze | ReportDiagnostics` when the rule must inspect generated output. *Why:* Razor/Blazor markup (`.razor`/`.cshtml`) compiles to generated C#, so a rule meant to flag that output is silently skipped under `None`. *Scope:* `None` is the common choice; switch only when the target is generated code.

## Correctness

- `SupportedDiagnostics` must list every descriptor you ever report (RS1005); expose it as a cached `ImmutableArray`.
- Compare symbols with `SymbolEqualityComparer.Default` (RS1024), never `==`/`!=`; symbol identity isn't stable across compilations.
- Diagnostic title/message/description are `LocalizableResourceString` (RS1007) with a non-null help link (RS1015, derived from the ID in this repo); the title has no trailing period (RS1031), the message ends with one (RS1032).
- Diagnostic IDs are compile-time constants (RS1017), unique (RS1019), and must avoid reserved prefixes like `CS`/`CA`/`IDE` (RS1029); use the area prefix (`ASP`/`MVC`/`API`/`BL`).
- Don't register a start action with no inner action (RS1012/RS1013).
- **Don't reference `Microsoft.CodeAnalysis.Workspaces` types (`Document`, `Solution`, `Workspace`) from a `DiagnosticAnalyzer`.** *Trigger:* an analyzer (not a code fix) needs a `Document`/`Solution`/`Workspace`. *Decision:* keep those types in the code-fix assembly; do the analyzer's work with `Compilation`/`SemanticModel`/`IOperation` instead. *Why:* the Workspaces assembly isn't loaded during `dotnet build`, so referencing it makes the analyzer fail outside the IDE (RS1022, enforced in the Framework set via `EnforceExtendedAnalyzerRules`). *Scope:* firm for new analyzers; the Components and MVC API analyzer projects deliberately reference Workspaces for VS compatibility, so this is a Framework-set convention, not a repo-wide invariant. Match the area you edit.

## Diagnostic design

- **Severity:** see [process.md](process.md#severity); default-on `Warning` is the common choice.
- Pass pre-computed data to the fixer via `diagnostic.Properties` (an `ImmutableDictionary<string,string?>`) so the fix doesn't redo semantic analysis.
- Use `WellKnownDiagnosticTags.Unnecessary` for fade-out (grey) diagnostics; add the `CompilationEnd` tag to descriptors reported only from compilation-end actions (RS1037).

- **Pick the registration kind from what the rule reasons about.** *Trigger:* choosing which `Register*Action` to use. *Decision:* a declared symbol uses `RegisterSymbolAction`; what executable code does uses `RegisterOperationAction`; pure syntactic shape uses `RegisterSyntaxNodeAction`; cross-file aggregation uses compilation start/end; a whole body with shared state uses operation-block start + inner + end. *Why:* the right granularity lets the host fire your callback only when relevant and hands you the typed model (symbol vs operation vs node) the check actually needs. *Scope:* near-universal.
- **Report on the smallest meaningful span.** *Trigger:* choosing the `Location` to report. *Decision:* flag the keyword/token/operator at fault, not the whole statement or declaration. *Why:* the squiggle and the fix target follow that span, so a wide span misleads the reader and can make the code fix touch unrelated trivia. *Scope:* near-universal.
- **Bail instead of guessing when the input is incomplete or ambiguous.** *Trigger:* the target node has parse errors (`GetDiagnostics().Any(d => d.Severity == Error)`), contains interleaved `#directives`, or the pattern can't be confirmed from symbols/operations. *Decision:* return without reporting. *Why:* the analyzer runs live on every keystroke over half-typed code, so a false positive erodes trust and gets the rule suppressed wholesale, whereas a missed diagnostic just defers a true hit to the next build. *Scope:* near-universal; when in doubt, don't report.
- **Use a `DiagnosticSuppressor`, not an analyzer, to quiet an existing diagnostic.** *Trigger:* the goal is to silence a built-in compiler/other diagnostic that is a known false positive in an ASP.NET Core pattern (e.g. async-without-await in a minimal API handler). *Decision:* write a `DiagnosticSuppressor` for it; don't use a suppressor to hide your own analyzer's output. *Why:* suppressors are the supported channel for "this existing warning is wrong here," carrying a justification and leaving the rule enabled everywhere else. *Scope:* narrow (only for suppressing other tools' diagnostics).

## Code fixes

- `[ExportCodeFixProvider(LanguageNames.CSharp), Shared]`; `FixableDiagnosticIds` lists exactly the IDs handled.
- Every `CodeAction` needs a non-null `equivalenceKey` (RS1010) and you must override `GetFixAllProvider()` (RS1016); without both, "Fix all occurrences" doesn't appear.
- Keep fixes minimal and syntactically correct; preserve/transfer trivia and add `Formatter.Annotation` for reformatting. Propagate `CancellationToken` and `await ... .ConfigureAwait(false)`.

- **Choose the `equivalenceKey` for the FixAll scope you want.** *Trigger:* setting the key on `CodeAction.Create(...)`. *Decision:* use the rule ID to batch all occurrences of the rule together; use a per-occurrence key to batch only identical fixes. *Why:* FixAll groups actions by equivalence key, so the key directly decides what "fix all" sweeps in one pass. *Scope:* common (the rule ID is the usual choice for a single uniform fix).
- **Match the FixAll provider to the fix's complexity.** *Trigger:* implementing `GetFixAllProvider()`. *Decision:* return `WellKnownFixAllProviders.BatchFixer` when fixes are local, non-overlapping, and apply-changes only; write a singleton custom `FixAllProvider` otherwise. *Why:* `BatchFixer` re-applies the single-fix logic per diagnostic and merges the results, which breaks when fixes overlap or depend on one another. *Scope:* common (`BatchFixer` covers most mechanical fixes).

## Anti-patterns (do NOT do)

| ❌ Don't | ✅ Do | Enforced by |
|---------|------|-------------|
| Store `Compilation`/`ISymbol`/`IOperation` in a field | Close over them in a start action | RS1008 |
| `Compilation.GetSemanticModel()` in a callback | Use the context's semantic model | RS1030 |
| Reference `Document`/`Solution`/`Workspace` in an analyzer | Keep those in the code fix | RS1022 |
| Compare symbols with `==` | `SymbolEqualityComparer.Default.Equals` | RS1024 |
| Allocate / LINQ / `DescendantNodes()` in per-node callbacks | Pooled builders; scope to blocks | n/a |
| Plain-string title/message | `LocalizableResourceString` | RS1007 |
| `CodeAction.Create` without an equivalence key | Pass a stable `equivalenceKey` | RS1010 |
| Report on a whole statement/declaration | Report on the smallest faulting span | n/a |
| Report when the target has parse errors or interleaved directives | Bail out to avoid false positives | n/a |
| Reuse a retired diagnostic ID | Allocate the next free ID | RS1019 |
