# Updating to a new Major Version & TFM

At the end of each release cycle, we update our Major Version (branding version of build assets) & TFM (Target Framework Moniker) in `main`, in preparation for the next major release. This doc describes the process of doing those updates, which can include many subtle gotchas in the aspnetcore repo.

In the event that we do a minor release (e.g. 3.1), the guidance in this document still applies (but with all instances of `MajorVersion` replaced by `MinorVersion`).

## Updating Major Version

Typically, we will update the Major Version before updating the TFM. This is because updating Major Version only requires a logical agreement that `main` is no longer the place for work on the previous major release, while updating the TFM requires waiting for dotnet/runtime to update their TFM so that we can ingest that change. For an example, [this](https://github.com/dotnet/aspnetcore/pull/35402) is the PR where we updated our branding from 6.0.0 to 7.0.0 (note that branding does *not* have to happen in a dependency update PR - this particular branding exercise needed to include some reaction to runtime changes).

### Required changes

* In [eng/Versions.props](/eng/Versions.props):
  1. Increment `AspNetCoreMajorVersion` by 1.
  2. Change `PreReleaseVersionIteration` to `1`.
  3. Change `PreReleaseVersionLabel` to `alpha`.
  4. Change `PreReleaseBrandingLabel` to `Alpha $(PreReleaseVersionIteration)`.
* Add entries to [NuGet.config](/NuGet.config) for the new Major Version's feed. This just means copying the current feeds (e.g. `dotnet8` and `dotnet8-transport`) and adding entries for the new feeds (`dotnet9` and `dotnet9-transport`). Make an effort to remove old feeds here at the same time.
* In [src/ProjectTemplates/Shared/TemplatePackageInstaller.cs](/src/ProjectTemplates/Shared/TemplatePackageInstaller.cs), add an entry to `_templatePackages` for `Microsoft.DotNet.Web.ProjectTemplates` matching the new version.
* In [eng/targets/CSharp.Common.props](/eng/targets/CSharp.Common.props) for the previous release branch, modify the `<LangVersion>` to be a hardcoded version instead of `preview`. (e.g. If main is being updated to 8.0.0 modify the `<LangVersion>` in the release/7.0 branch). See https://learn.microsoft.com/dotnet/csharp/language-reference/configure-language-version#defaults to find what language version to use.
* Mark APIs from the previous release as Shipped by running `.\eng\scripts\mark-shipped.cmd`. **Note that it's best to do this as early as possible after the API surface is finalized from the previous release** - make sure to be careful that any new API in `main` that isn't shipped as part of the previous release, stays in `PublicAPI.Unshipped.txt` files.
  * One way to ensure this is to check out the release branch shipping the previous release (**after API surface area has been finalized**), run `.\eng\scripts\mark-shipped.cmd` there, copy over all of the `PublicAPI.Unshipped.txt` and `PublicAPI.Shipped.txt` files into a new branch based off of `main`, and build the repo. Any failures there will tell you whether or not there are new APIs in main that need to be put back into the `PublicAPI.Unshipped.txt` files.
    * The result of `.\eng\scripts\mark-shipped.cmd` should be checked in to the release branch as well, as part of the RTM release.
* Update `.\eng\Baseline.xml` to reflect the set of RTM packages that were just shipped. Then, `dotnet run` `.\eng\tools\BaselineGenerator\BaselineGenerator.csproj`, which will update `.\eng\Baseline.Designer.props`. If RTM hasn't shipped yet, do this in a separate PR once it has. See https://github.com/dotnet/aspnetcore/pull/49269.
* **In the new release branch**, add files named `.\eng\PlatformManifest.txt` and `.\eng\PackageOverrides.txt`. These files should be found by downloading the just released RTM version of the `Microsoft.AspNetCore.App.Ref` package, and copying over the files from the `data` folder.
* Update [helix-matrix.yml](https://github.com/dotnet/aspnetcore/blob/436556163a671259c8b14ae1c90d72767af62d18/.azure/pipelines/helix-matrix.yml#L12-L16) to list the currently active release branches.
    * This should be done in `main` as well as the relevant release branch.
* Update [dependabot.yml](https://github.com/dotnet/aspnetcore/blob/b43884c1b21ea71e91c539721a75e5c96b8c1263/.github/dependabot.yml#L74-L85) to list the currently active release branches for submodule updates.
    * This only needs to be done in `main`.

### Validation

* CI must be green.
* Assets produced by the build (packages, installers) should have the new branding version (e.g. if you have just updated `MajorVersion` to `8`, packages should be branded `8.0.0-alpha.1.{BuildNumber}`).
* Assemblies produced by the build should have the new `AssemblyVersion` (e.g. if you have just updated `MajorVersion` to `8`, Assemblies should have `AssemblyVersion` `8.0.0.0`).

## Updating TFM

Once dotnet/runtime has updated their TFM, we update ours in the dependency update PR ingesting that change. We won't be able to ingest new dotnet/runtime dependencies in `main` until this is done. For an example, [this](https://github.com/dotnet/aspnetcore/pull/36328) is the PR where we updated our TFM to `net7.0`. This step can be tricky - we have workarounds in [eng/tools/GenerateFiles/Directory.Build.targets.in](/eng/tools/GenerateFiles/Directory.Build.targets.in) to make the build work before we get an SDK containing runtime references with the new TFM. We copy the `KnownFrameworkReference`, `KnownRuntimePack`, and `KnownAppHostPack` from the previous TFM, give them the incoming runtime dependency versions, and give them the new TFM (these TFMs no-op most of the time - they only apply during this period when we're using an SDK that doesn't know about the new TFM). These workarounds allow us to build against the new TFM before we get an SDK with a reference to it, but there are often problems that arise in this area. The best way to debug build errors related to FrameworkReferences it to get a binlog of a failing project (`dotnet build /bl`) and look at the inputs to the task that failed. Confirm that the `Known___` items look as expected (there is an entry with the current TFM & the current dotnet/runtime dependency version), and look at the source code of the task in [dotnet/sdk](https://github.com/dotnet/sdk) for hints.

### Required changes

* In [eng/Versions.props](/eng/Versions.props), increment `DefaultNetCoreTargetFramework` by 1.
* In [eng/Versions.props](/eng/Versions.props), **if and only if** the new TFM is LTS, update `CurrentLtsTargetFramework` to match `DefaultNetCoreTargetFramework`
* In [eng/SourceBuild.props](/eng/SourceBuild.props), update `SourceBuildTargetFrameworkFilter` to include the current TFM.
* Do a global repo search for the current version string, and update almost everything by 1 (e.g. find `net8`, replace with `net9`). See the PR linked above for examples - this shouldn't be done blindly, but on a case-by-case basis. Most things should be updated, and most choices should be obvious.
  * Exceptions to this are [eng/tools/RepoTasks/RepoTasks.csproj](/eng/tools/RepoTasks/RepoTasks.csproj) and [eng/tools/RepoTasks/RepoTasks.tasks](/eng/tools/RepoTasks/RepoTasks.tasks). These build without the workarounds from [eng/tools/GenerateFiles/Directory.Build.targets.in](/eng/tools/GenerateFiles/Directory.Build.targets.in), and need to be kept at the previous TFM until we get an SDK containing a runtime with the new TFM. Generally this means we have to hard-code the previous TFM for these files, rather than using `DefaultNetCoreTargetFramework`.
* Add a reference to the new `SiteExtensions` package for the previous Major Version.
  1. Add references to [src/SiteExtensions/LoggingAggregate/src/Microsoft.AspNetCore.AzureAppServices.SiteExtension/Microsoft.AspNetCore.AzureAppServices.SiteExtension.csproj](/src/SiteExtensions/LoggingAggregate/src/Microsoft.AspNetCore.AzureAppServices.SiteExtension/Microsoft.AspNetCore.AzureAppServices.SiteExtension.csproj) to `Microsoft.AspNetCore.AzureAppServices.SiteExtension.{PreviousMajorVersion}.0.x64` and `Microsoft.AspNetCore.AzureAppServices.SiteExtension.{PreviousMajorVersion}.0.x86`.
  2. Add entries in [eng/Versions.props](/eng/Versions.props) similar to [these](https://github.com/dotnet/aspnetcore/blob/216c92b78bce31d5e81a70b589707ec2ae5ab21a/eng/Versions.props#L224-L226) - the version should be from the latest released build of .Net.
  3. Add entries in [eng/Dependencies.props](/eng/Dependencies.props) similar to [these](https://github.com/dotnet/aspnetcore/blob/a47c0a58d7002b9a530c67532366b9db96d73cc6/eng/Dependencies.props#L119-L120).
* Update AssemblyVersions for dotnet/runtime assemblies in [src/Framework/test/TestData.cs](/src/Framework/test/TestData.cs).
* Update template precedence
  1. Create a PR like [this one](https://github.com/dotnet/aspnetcore/pull/39783) in dotnet/aspnetcore that updates the `precedence`, `identity`, and (if it exists) `thirdPartyNotices` elements in all template.json files.
      * Make sure to update _all_ template.json files, including project templates and item templates.
      * Going forward, Precedence values should be (9000 + (Major Version) * 100) for 8.0 and 9.0, and (Major Version \* 1000) after that.
        * This means 8.0's Precedence should be 9800, 9.0's should be 9900, 10.0's should be 10000, 11.0's should be 11000, and so on.
        * If we need to release a Minor version of any of these, use the first zero digit after the Major version to represent that (e.g. 9810 for 8.1, 10100 for 10.1).

  2. Make sure the new aka.ms link you're referencing in `thirdPartyNotices` exists.
* In [src/Framework/AspNetCoreAnalyzers/test/Verifiers/CSharpRouteHandlerCodeFixVerifier.cs](/src/Framework/AspNetCoreAnalyzers/test/Verifiers/CSharpRouteHandlerCodeFixVerifier.cs), update the references to `ReferenceAssemblies.Net.Netx0` with the latest version.

### Validation

* CI must be green.
* Packages produced by the build should be placing assemblies in a folder named after the new TFM.

## Ingesting an SDK with the new TFM

Typically we update the SDK we use in `main` every Monday. Once we have one that contains `Microsoft.Netcore.App` entries with the new TFM, we can update [eng/tools/RepoTasks/RepoTasks.csproj](/eng/tools/RepoTasks/RepoTasks.csproj) and [eng/tools/RepoTasks/RepoTasks.tasks](/eng/tools/RepoTasks/RepoTasks.tasks) to use `DefaultNetCoreTargetFramework` again rather than hard-coding the previous TFM.
