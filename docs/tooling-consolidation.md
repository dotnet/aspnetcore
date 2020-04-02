# AspNetCore Tooling Consolidation

## Objectives

We want to consolidate portions of dotnet/aspnetcore-tooling into dotnet/aspnetcore in our ongoing effort to reduce overall build time end to end for the .NET Core SDK. As such, we will not require all of aspnetcore-tooling to be merged into aspnetcore. For example, tooling for VSMac and VSCode will remain in aspnetcore-tooling. The main motivation is to achieve the overall goals of repo consolidation to reduce the number of repository required to build the SDK while keeping the build in aspnetcore as simple as possible without needing to support build/test for tooling scenarios such as testing on VSCode. There is also an added benefit of maintaining the current development and release workflow for aspnetcore-tooling which is more "agile" than aspnetcore (e.g. faster PR builds, release cycles that synchronize with VS releases).

To achieve this we will be migrating the following (and associated tests) from aspnetcore-tooling to aspnetcore:
```
Microsoft.AspNetCore.Mvc.Razor.Extensions.Version1_X
Microsoft.AspNetCore.Mvc.Razor.Extensions.Version2_X
Microsoft.AspNetCore.Mvc.Razor.Extensions
Microsoft.AspNetCore.Razor.Language
Microsoft.AspNetCore.Razor.Tools
Microsoft.CodeAnalysis.Razor
Microsoft.NET.Sdk.Razor
RazorPageGenerator
````
The following (and associated tests) will remain in aspnetcore-tooling:
```
Microsoft.AspNetCore.Razor.LanguageServer.Common
Microsoft.AspNetCore.Razor.LanguageServer
Microsoft.AspNetCore.Razor.OmniSharpPlugin.StrongNamed
Microsoft.AspNetCore.Razor.OmniSharpPlugin
Microsoft.AspNetCore.Razor.VSCode.Extension
Microsoft.AspNetCore.Razor.VSCode
Microsoft.CodeAnalysis.Razor.Workspaces
Microsoft.CodeAnalysis.Remote.Razor
Microsoft.VisualStudio.Editor.Razor
Microsoft.VisualStudio.LanguageServerClient.Razor
Microsoft.VisualStudio.LanguageServices.Razor
Microsoft.VisualStudio.LiveShare.Razor
Microsoft.VisualStudio.Mac.LanguageServices.Razor
Microsoft.VisualStudio.Mac.RazorAddin
Microsoft.VisualStudio.RazorExtension
RazorDeveloperTools
rzls
````

Due to the separation of tooling projects, the new dependency flow will be:

```
runtime +--> aspnetcore +--> SDK
        \               \
         +-> extensions -+-> aspnetcore-tooling
```
## Known action items

1. Migrate source with `git filter-branch`, preserving commit history
2. Migration of pending PRs to aspnetcore after source migration.
   1. Alternatively, we can freeze checkins during source migration especially since there isn't much source code that requires migration
3. Build razor SDK in pre-build step along with RepoTools and install it locally before the main build in aspnetcore
   1. Can this step be skipped if for devs who don't work in this area?
4. Invert darc subscriptions so aspnetcore flows into aspnetcore-tooling

## Considerations

### Pinning Roslyn dependencies

Due to runtime and tooling divergence of Roslyn packages, we may need to pin multiple versions of Roslyn dependencies for the migrated packages.

### Assets

The migrated projects are C# only so we will use existing infrastructure in aspnetcore to handle signing and publishing of these packages.

### Testing

The migrated projects are C# only so there are no test infrasture changes needed in aspnetcore. The existing vscode jest, and node tests will remain in aspnetcore-tooling

### Release cycles

Tooling release cycles is a cross product of .NET Core (2.1, 3.1, 5.0) and VS (vs-mac, vscode, vs) release cycles. However, the packages we are migrating are only released on .NET Core cycles so it should not add any complexity to servicing in aspnetcore. We will follow the existing branching and servicing strategies.

### Cross-repo changes

Given that some of the work in tooling scenarios may involve projects in both aspnetcore and aspnetcore-tooling, there is added complexity here to coordinate cross-repo changes. However, this concern is already present due to the dependency of aspnetcore on aspnetcore-tooling. This problem is not new but will affect different projects after the migration.

### Build time

A concern that has been voiced is that working in aspnetcore would be significantly slower than in aspnetcore-tooling given that the PR validation in aspnetcore takes longer. Note that this will only apply to the migrated projects.

## Cost and schedules

TBD