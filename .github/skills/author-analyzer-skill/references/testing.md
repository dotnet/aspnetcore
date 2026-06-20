# Testing analyzers & code fixes

Tests use **Microsoft.CodeAnalysis.Testing** through the repo's `CSharpAnalyzerVerifier<TAnalyzer>` and `CSharpCodeFixVerifier<TAnalyzer, TCodeFix>` helpers (in each area's `test/Verifiers/`). Tests are xUnit `[Fact]`/`[Theory]`. Copy the style of neighboring tests in the same area.

## Table of contents
- [Set up the verifier alias](#set-up-the-verifier-alias)
- [Analyzer tests](#analyzer-tests)
- [Code-fix tests](#code-fix-tests)
- [What to cover](#what-to-cover)

## Set up the verifier alias

Alias the generic verifier to your analyzer at the top of the test file:

```csharp
using VerifyCS = Microsoft.AspNetCore.Analyzers.Verifiers.CSharpAnalyzerVerifier<
    Microsoft.AspNetCore.Analyzers.Http.HeaderDictionaryIndexerAnalyzer>;
```

For code-fix tests, alias `CSharpCodeFixVerifier<TAnalyzer, TCodeFix>` instead.

## Analyzer tests

- Embed the test source as a string. The expected diagnostic location is marked inline with `{|#0:...|}` (location `0`, `1`, ...). The verifier sets `OutputKind = ConsoleApplication`, so **top-level statements work** — write programs the way users do.
- Assert with `new DiagnosticResult(DiagnosticDescriptors.MyRule).WithLocation(0)`, optionally `.WithMessage(...)` (use the generated `Resources.Format<Key>(args...)` helper) and `.WithArguments(...)`.

```csharp
[Fact]
public async Task IHeaderDictionary_Get_MismatchCase_ReturnDiagnostic()
{
    await VerifyCS.VerifyAnalyzerAsync(@"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
var webApp = WebApplication.Create();
webApp.Use(async (HttpContext context, System.Func<System.Threading.Tasks.Task> next) =>
{
    var s = {|#0:context.Request.Headers[""content-type""]|};
    await next();
});
",
        new DiagnosticResult(DiagnosticDescriptors.UseHeaderDictionaryPropertiesInsteadOfIndexer)
            .WithLocation(0)
            .WithMessage(Resources.FormatAnalyzer_HeaderDictionaryIndexer_Message("content-type", "ContentType")));
}
```

- **Negative tests are mandatory.** For every "reports" test, write a "no diagnostic" sibling for the near-miss cases (different type, unknown member, already-correct code). Pass no `DiagnosticResult` to assert nothing fires:

```csharp
[Fact]
public async Task IHeaderDictionary_Get_UnknownProperty_NoDiagnostic()
    => await VerifyCS.VerifyAnalyzerAsync(@"/* source that must NOT trigger */");
```

## Code-fix tests

`VerifyCodeFixAsync(source, expected, fixedSource)` runs the analyzer on `source`, applies the fix, and asserts the result equals `fixedSource`:

```csharp
await VerifyCS.VerifyCodeFixAsync(
    source,                 // contains {|#0:...|}
    new[] { new DiagnosticResult(DiagnosticDescriptors.MyRule).WithLocation(0) },
    fixedSource);           // expected code after the fix
```

- Optional args on the repo helper: `expectedIterations` (`NumberOfFixAllIterations` for FixAll), `usageSource` (an extra unchanged source file), and `codeActionEquivalenceKey` (to pin which fix when several are offered — must match the analyzer's `equivalenceKey`).
- `fixedSource` must compile and be correctly formatted — trailing trivia/indentation matters.

## What to cover

- ✅ Positive cases at the exact `{|#0:...|}` span, including each variant that should trigger.
- ✅ Negative cases: similar-but-correct code, unrelated types, generated code, partial/erroneous code.
- ✅ Message arguments via the `Resources.Format...` helper so localization stays in sync.
- ✅ Code fix: before → after for each shape, plus a FixAll iteration test when `BatchFixer` is used.
- ✅ Reproduce any reported false-positive issue as a `_NoDiagnostic` regression test before fixing it.

Run the suite with the local SDK activated (`. ./activate.ps1` / `source activate.sh`), then `./src/<Area>/build.sh -test` (e.g. `./src/Framework/build.sh -test`).
