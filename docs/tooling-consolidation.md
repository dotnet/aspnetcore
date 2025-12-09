# AspNetCore Tooling Consolidation

## Objectives

We want to consolidate dotnet/aspnetcore-tooling into dotnet/aspnetcore to achieve 3 goals:

1. Reduce overall build time end to end for the .NET Core SDK.
2. Reduce the complexity of maintaining multiple repositories.
3. Maintain, or if possible, improve, developer productivity.

We are prioritizing the first objective since it is part of a cross-team effort to reduce the SDK build time. To ensure we are able to achieve this goal quickly and with minimal risk, we plan to take a multi-phase approach. The first phase will involve moving the language components from aspnetcore-tooling to aspnetcore which is required for the SDK build. The second phase will involve a more gradual migration for the remaining tooling components with an emphasis on maintaining developer productivity.

## Phase one: Migration of language components

In this phase, we are primarily concerned with the overall goals of repo consolidation to reduce the number of repositories required to build the SDK while keeping the build in aspnetcore as simple as possible without needing to support build/test for tooling scenarios such as testing on VSCode. As such, we will not require all of aspnetcore-tooling to be merged into aspnetcore. For example, tooling for VSMac and VSCode will remain in aspnetcore-tooling. There is also an added benefit of maintaining the current development and release workflow for aspnetcore-tooling which is more "agile" than aspnetcore (e.g. faster PR builds, release cycles that synchronize with VS releases).

To achieve this we will be migrating the following (and associated tests) from aspnetcore-tooling to aspnetcore:

```text
Microsoft.AspNetCore.Mvc.Razor.Extensions.Version1_X
Microsoft.AspNetCore.Mvc.Razor.Extensions.Version2_X
Microsoft.AspNetCore.Mvc.Razor.Extensions
Microsoft.AspNetCore.Razor.Language
Microsoft.AspNetCore.Razor.Tools
Microsoft.CodeAnalysis.Razor
Microsoft.NET.Sdk.Razor
```

The following (and associated tests) will remain in aspnetcore-tooling:

```text
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
```

The following will be deleted:

```text
RazorPageGenerator
```

Due to the separation of tooling projects, the new dependency flow will be:

```text
runtime +--> aspnetcore +--> SDK
        \               \
         +-> extensions -+-> aspnetcore-tooling
```

### Known action items

1. Migrate source with `git filter-branch`, preserving commit history
2. Migration of pending PRs to aspnetcore after source migration.
   1. Alternatively, we can freeze checkins during source migration especially since there isn't much source code that requires migration
3. It seems like we do not need a pre-build step to build the Razor SDK for consumption elsewhere in the repo. It is possible to add a project dependency along with appropriate props/target imports
4. Invert darc subscriptions so aspnetcore flows into aspnetcore-tooling

## Phase two: Migration of tooling components

After phase one is completed, we will migrate the tooling components remaining in aspnetcore-tooling piecemeal at a time as required. Our current understanding is that we will eventually be moving all of aspnetcore-tooling into aspnetcore but this can be scoped as appropriate given time/resource constraints. The overall goal here is to reduce build complexity in our repos and maintain developer productivity. As such, more discussions will be made with area owners to identify workflow impacts.

## Major considerations

### Pinning Roslyn dependencies

Due to runtime and tooling divergence of Roslyn packages, we may need to pin multiple versions of Roslyn dependencies for the migrated packages.

### Assets

The migrated projects are C# only in phase one so we will use existing infrastructure in aspnetcore to handle signing and publishing of these packages. Phase two will involve additional asset types including vsix, zips, npm packages and mpacks. We have asset publishing mechanisms for most of these asset types but may need to add additional asset publishing mechanisms and/or release management for some assets (mpacks for example). We will evaluate the additional requirements when phase two is discussed.

### Developer efficiency (PR builds)

A concern that has been voiced is that working in aspnetcore would be significantly slower than in aspnetcore-tooling given that the PR validation in aspnetcore takes longer. For comparison, it takes about 30 mins to run builds and tests in aspnetcore-tooling whereas builds and tests in aspnetcore takes about 1.5 - 2 hours. We are making efforts in improving this experience but it will be out of scope of aspnetcore-tooling consolidation.

 Note that this will only apply to the migrated language projects in phase one. In phase two, we will explore several approaches to improve this area. We are considering adopting additional logic (from dotnet/runtime) that will allow us to run only portions of tests based on what source changes were detected. However, we will always build the entire repository regardless of what source changes were made.

### Testing

In phase one, the migrated projects are C# only so there are no test infrastructure changes needed in aspnetcore. The existing vscode jest, and node tests will remain in aspnetcore-tooling during phase one and will be migrated in phase two.

### Reliability

There is concern that there are test reliability issues in both repos and combining them would aggravate the problem in aspnetcore. This has been deemed a non-issue in phase one since the tests to be migrated are found to be very reliable. For phase two, we will ensure tests are reliable before we migrate each tooling component over to aspnetcore.

### Release cycles

Tooling release cycles is a cross product of .NET Core (2.1, 3.1, 5.0) and VS (vs-mac, vscode, vs) release cycles. In phase one, the packages we are migrating are only released on .NET Core cycles so it should not add any complexity to servicing in aspnetcore. We will follow the existing branching and servicing strategies. In phase two, we will evaluate branching and release policies for tooling components.

### Cross-repo changes

Given that some of the work in tooling scenarios may involve projects in both aspnetcore and aspnetcore-tooling, there is added complexity here to coordinate cross-repo changes. However, this concern is already present due to the dependency of aspnetcore on aspnetcore-tooling. This problem is not new but will affect different projects after the migration; some current cross-repo changes will be easier while some changes between language and tooling packages will be more burdensome.

## Timeline and costs

Phase one can begin as soon as possible and will likely take about 1 week. Phase two's schedule and costs are TBD.
