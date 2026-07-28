# ASP.NET Core analyzer conventions

Repo-specific conventions for adding or reviewing a diagnostic. **These describe the Framework set (`src/Framework/AspNetCoreAnalyzers`), the model for new analyzers.** MVC, MVC API, and Components analyzers diverge in places (noted below); when editing those, match the surrounding area.

## Table of contents
- [Diagnostic IDs & the registry](#diagnostic-ids--the-registry)
- [DiagnosticDescriptors files](#diagnosticdescriptors-files)
- [Localization (Resources.resx)](#localization-resourcesresx)
- [Well-known types infrastructure](#well-known-types-infrastructure)
- [Project layout & packaging](#project-layout--packaging)
- [Release tracking status](#release-tracking-status)

## Diagnostic IDs & the registry

- Each area owns a prefix and an integer range: `ASP####` (Framework), `MVC####` (MVC), `API####` (MVC API), `BL####` (Components).
- **`docs/list-of-diagnostics.md` is the human-facing registry, but it lags reality**: some shipped IDs aren't listed and reserved entries may be out of date. The authoritative list of which IDs are actually taken is the set of `DiagnosticDescriptors.cs` files; scan those (plus a grep of the area for the ID prefix) to find the next free number, and record the new ID in `docs/list-of-diagnostics.md` as well to keep the human registry current.
- IDs are permanent and never reused. If a rule is removed, retire the ID; don't recycle it.
- Gaps exist and are fine (e.g. `ASP0002` is skipped); take the next unused number.

## DiagnosticDescriptors files

Each area centralizes its descriptors in one `internal static class DiagnosticDescriptors` (e.g. `src/Framework/AspNetCoreAnalyzers/src/Analyzers/DiagnosticDescriptors.cs`, `src/Mvc/Mvc.Analyzers/src/DiagnosticDescriptors.cs`, `src/Mvc/Mvc.Api.Analyzers/src/ApiDiagnosticDescriptors.cs`, `src/Components/Analyzers/src/DiagnosticDescriptors.cs`).

Conventions in the Framework set (the model to copy):

```csharp
private const string Security = "Security";
private const string Usage = "Usage";

private static LocalizableResourceString CreateLocalizableResourceString(string resource)
    => new(resource, Resources.ResourceManager, typeof(Resources));

internal static readonly DiagnosticDescriptor DoNotUseModelBindingAttributesOnRouteHandlerParameters =
    CreateDiagnosticDescriptor(
        "ASP0003",
        CreateLocalizableResourceString(nameof(Resources.Analyzer_..._Title)),
        CreateLocalizableResourceString(nameof(Resources.Analyzer_..._Message)),
        Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

// Help links are derived from the ID; do not hand-write them:
private static DiagnosticDescriptor CreateDiagnosticDescriptor(
    string id, LocalizableString title, LocalizableString messageFormat, string category,
    DiagnosticSeverity defaultSeverity, bool isEnabledByDefault,
    LocalizableString? description = null, params string[] customTags)
    => new(id, title, messageFormat, category, defaultSeverity, isEnabledByDefault,
           description, GetHelpLinkUri(id), customTags);

private static string GetHelpLinkUri(string diagnosticId)
    => $"https://learn.microsoft.com/aspnet/core/diagnostics/{diagnosticId.ToLowerInvariant()}";
```

- Descriptors are `static readonly` fields; declare once, never construct per invocation.
- Categories used in-repo: `Usage`, `Security` (Framework); `Naming`, `Usage` (MVC); `Encapsulation`, `Usage` (Components). Use the standard [.NET analyzer categories](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/categories); prefer an existing one over inventing a new string.
- Reference the descriptor from the analyzer's `SupportedDiagnostics` and from `ReportDiagnostic`.

## Localization (Resources.resx)

Diagnostic **title/message/description should be localizable** (RS1007). The Framework and Components sets wire this through the area's `Resources.resx` and the generated `Resources` accessor (the model to copy for new analyzers). *Note:* the MVC and MVC API analyzers currently use **inline string** descriptors without a help link, so don't "fix" them to match unless that's the point of your change; match the area.

1. Add string entries to `src/.../Resources.resx`, naming them `Analyzer_<RuleName>_Title`, `Analyzer_<RuleName>_Message`, and optionally `_Description`. Use `{0}`/`{1}` placeholders in the message that line up with the `messageArgs` you pass to `Diagnostic.Create(...)`.
2. Reference them via `CreateLocalizableResourceString(nameof(Resources.<Key>))`.
3. Title is a heading with **no trailing period** (RS1031). Message is a sentence that **ends with a period** (RS1032).

Code-fix titles do **not** need to be `LocalizableResourceString`; a plain `string` is fine (the `/preferreduilang` switch that motivates localized analyzer strings doesn't apply to fixes). Some Components fixers still localize their titles via `Resources` for consistency; either is acceptable.

## Well-known types infrastructure

Framework analyzers share a fast symbol cache instead of calling `Compilation.GetTypeByMetadataName` repeatedly (compiled into the project from `$(SharedSourceRoot)RoslynUtils/`):

- `WellKnownTypeData.cs`: a `WellKnownType` enum of every type the analyzers need, plus the parallel array of fully-qualified metadata names.
- `WellKnownTypes.cs`: `WellKnownTypes.GetOrCreate(compilation)` resolves and caches them per compilation (`BoundedCacheWithFactory`).

To use a new type: add an enum member (namespace segments separated by `_`) and its metadata name in `WellKnownTypeData.cs`, then read it via the cached `WellKnownTypes` instance you obtained in `RegisterCompilationStartAction`. Never store the resolved symbol in an analyzer field.

## Project layout & packaging

- Framework analyzer `.csproj`: `TargetFramework=netstandard2.0`, `IncludeBuildOutput=false`, `Nullable=Enable`, `EnforceExtendedAnalyzerRules=true`, and `References` to `Microsoft.CodeAnalysis.CSharp` (`PrivateAssets=All`) and `Microsoft.CodeAnalysis.ExternalAccess.AspNetCore`. (Other areas' csprojs differ: Components/MVC API reference Workspaces; see the Workspaces rule in [best-practices.md](best-practices.md#correctness).)
- Analyzers and code fixes are **separate projects** (e.g. `Microsoft.AspNetCore.App.Analyzers` and `Microsoft.AspNetCore.App.CodeFixes`); in the Framework set the analyzer assembly must not reference Workspaces, the code-fix assembly may (see the Workspaces rule in [best-practices.md](best-practices.md#correctness)).
- `src/` and `test/` siblings per area; tests use `InternalsVisibleTo`.
- Analyzers are packaged under `analyzers/dotnet/cs` and shipped in the shared framework / SDK; they are not standalone NuGet packages users install.

## Release tracking status

The Microsoft.CodeAnalysis release-tracking analyzer (RS2000-RS2008 / `AnalyzerReleases.*.md`) is **not** used in these projects; the descriptor files suppress RS2008 (`[SuppressMessage("MicrosoftCodeAnalysisReleaseTracking", "RS2008:...")]`). The source of truth for shipped IDs is `docs/list-of-diagnostics.md` plus the `DiagnosticDescriptors.cs` files. Don't add `AnalyzerReleases.Shipped.md`/`Unshipped.md` unless the repo convention changes; match the area you are editing.
