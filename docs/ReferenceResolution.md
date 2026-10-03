# Project and package references

Projects use standard MSBuild `<ProjectReference>` items for in-repository dependencies and
NuGet `<PackageReference>` items for external dependencies. Package versions are managed with
[NuGet Central Package Management](https://learn.microsoft.com/nuget/consume-packages/central-package-management).
There is no assembly-name-to-project-or-package conversion.

[ResolveReferences.targets](/eng/targets/ResolveReferences.targets) retains shared-framework boundary checks,
copy-local and transitivity defaults, reference-assembly selection, and framework dependency metadata for packing.
Central transitive pinning is not enabled: adding a central version does not add a direct package dependency.

## Recommendations for writing a .csproj

* Use `<ProjectReference Include="$(RepoRoot)src\Area\Project.csproj" />` for projects governed by this repository's build imports. Keep relative paths in standalone projects and copied consumers that do not define `RepoRoot`.
* Use `<PackageReference Include="Package.Name" />` for external packages, retaining any required asset metadata.
* Keep references sorted within their item groups, without changing conditions or metadata.
* Add a new package's `<PackageVersion>` to `Directory.Packages.props` and its version property to `eng/Versions.props`.
* Use `VersionOverride` only for intentional project-specific versions, such as analyzer compatibility pins.
* Standalone consumers that do not enable central package management must retain explicit `Version` attributes, including the Components.Testing package integration assets.
* Reserve `<Reference>` for actual assembly references, not package IDs or in-repository assembly names.
* Shared-source packages need `IncludeAssets="ContentFiles;Build"` and `PrivateAssets="All"`.
* If the package comes from a partner team and needs to have versions automatically updated, also add an entry `eng/Version.Details.xml`.
* Otherwise, add the package to [eng/tools/DependabotDiscovery/DependabotDiscovery.csproj](/eng/tools/DependabotDiscovery/DependabotDiscovery.csproj) so Dependabot can find and update it. See the README next to that file for details.
* Name the .csproj file to match the assembly name.
* Follow the project checklist below when adding, moving, or removing projects.

## Important files

* [Directory.Packages.props](/Directory.Packages.props) - enables central version management for repository projects and maps package IDs to version properties using `<PackageVersion>`, including source-build overrides.
* [eng/tools/DependabotDiscovery/DependabotDiscovery.csproj](/eng/tools/DependabotDiscovery/DependabotDiscovery.csproj) - exposes the selected non-Maestro-managed packages to existing Dependabot jobs. Never built.
* Generated `eng/SharedFramework.Local.props`, `eng/ShippingAssemblies.props`, and `eng/TrimmableProjects.props` retain assembly identities and producer paths for framework packs, API documentation, and trimming.
* [eng/Versions.props](/eng/Versions.props) - contains a list of versions which may be updated by automation. This is used by MSBuild to restore and build.
* [eng/Version.Details.xml](/eng/Version.Details.xml) - used by automation to update dependency variables in
  [eng/Versions.props](/eng/Versions.props) and, for SDKs and `msbuild` toolsets, [global.json](global.json).

## Adding, moving, or removing a project

Adding, moving, or removing a project changes generated repository metadata and may affect multiple solution filters.
Complete this checklist for every structural project change:

1. Create, move, or remove the project files.
2. Update `AspNetCore.slnx` and every `*.slnf` that references the project. A project referenced by a solution
   filter must also exist in `AspNetCore.slnx`.
3. Run `eng/scripts/GenerateProjectList.ps1` (or `build.cmd /t:GenerateProjectList`) and review all generated
   `eng/*.props` changes, including ordering and grouping changes.
4. Run project-list generation a second time and confirm that it produces no further changes.
5. For a move or removal, run `git grep -n -- '<old-project-path>'` and resolve every remaining tracked reference
   that is not intentionally historical documentation.
6. Run `eng/scripts/CodeCheck.ps1`. This checks that solution filters reference only projects in
   `AspNetCore.slnx` and reruns project-list generation to detect stale generated metadata.

## Example: adding a new dependency

Steps for adding a new package dependency to an existing project. Let's say I'm adding a dependency on System.Banana.

1. Add the package to the .csproj file using `<PackageReference Include="System.Banana" />`
2. Add an entry to [Directory.Packages.props](/Directory.Packages.props) e.g. `<PackageVersion Include="System.Banana" Version="$(SystemBananaVersion)" />`
3. If this package comes from another dotnet team and should be updated automatically by our bot&hellip;
    1. Add an entry to [eng/Versions.props](/eng/Versions.props) like this `<SystemBananaVersion>0.0.1-beta-1</SystemBananaVersion>`.
    2. Add an entry to [eng/Version.Details.xml](/eng/Version.Details.xml) like this:

        ```xml
        <ProductDependencies>
          <!-- ... -->
          <Dependency Name="System.Banana" Version="0.0.1-beta-1">
            <Uri>https://github.com/dotnet/corefx</Uri>
            <Sha>000000</Sha>
          </Dependency>
          <!-- ... -->
        </ProductDependencies>
        ```

        If you don't know the commit hash of the source code used to produce "0.0.1-beta-1", you can use `000000` as a
        placeholder for `Sha` as its value will be updated the next time the bot runs.

        If the new dependency comes from dotnet/runtime and you are updating dotnet/aspnetcore-tooling, add a
        `CoherentParentDependency` attribute to the `<Dependency>` element as shown below. This example indicates the
        dotnet/runtime dependency version for System.Banana should be determined based on the dotnet/aspnetcore build
        that produced the chosen Microsoft.CodeAnalysis.Razor. That is, the dotnet/runtime and dotnet/aspnetcore
        dependencies should be coherent.

        ```xml
        <Dependency Name="System.Banana" Version="0.0.1-beta-1" CoherentParentDependency="Microsoft.CodeAnalysis.Razor">
          <!-- ... -->
        </Dependency>
        ```

        The attribute value should be `"Microsoft.CodeAnalysis.Razor"` for dotnet/runtime dependencies in
        dotnet/aspnetcore-tooling.
4. Otherwise (no Maestro automation), add `<PackageReference Include="System.Banana" />`
   to [eng/tools/DependabotDiscovery/DependabotDiscovery.csproj](/eng/tools/DependabotDiscovery/DependabotDiscovery.csproj)
   so Dependabot can find and update it. `CodeCheck.ps1` fails if this file isn't kept in sync with
   `Directory.Packages.props`.

## A darc cheatsheet

`darc` is a command-line tool that is used for dependency management in the dotnet ecosystem of repos. `darc` can be installed using the `darc-init` scripts located inside the `eng/common` directory. Once `darc` is installed, you'll need to set up the appropriate access tokens as outlined [in the official Darc docs](https://github.com/dotnet/arcade/blob/master/Documentation/Darc.md#setting-up-your-darc-client).

> :warning: Much of the functionality described below can now be done via the [web UI](https://maestro.dot.net/) - it's recommended to try that out first.

Once `darc` is installed and set-up, it can be used to modify the subscriptions and dependencies in a project.

### Getting the list of subscriptions in a repo

Subscriptions are objects that define the ecosystem repos we are listening for updates to, the frequency we are looking for updates, and more.

```bash
darc get-subscriptions --target-branch main --target-repo aspnetcore$ --regex
```

### Disable/enable a subscription

```bash
darc subscription-status --id {subscriptionIdHere} --enable
darc subscription-status --id {subscriptionIdHere} --disable
```

### Trigger a subscription

Triggering a subscription will search for updates in its dependencies and open a PR in the target repo via the dotnet-maestro bot with these changes.

```bash
darc trigger-subscriptions --id {subscriptionIdHere}
```

### Manually update dependencies

If the `dotnet-maestro` bot has not correctly updated the dependencies, `darc update-dependencies` may be used to update the dependencies manually. Note, you'll need to run the commands below in a separate branch and submit a PR with the changes. These are the things that the bot should do for you if you use `trigger-subscriptions` or automatically (when the subscription fires e.g. about 15 minutes after a dependency's build completes if `Update Frequency: EveryBuild`).

```bash
darc update-dependencies --channel '.NET Core 3.1 Release'
darc update-dependencies --channel '.NET 5 Dev' --source-repo efcore
```

Generally, using `trigger-subscriptions` is preferred for creating dependency updates instead of manually updating dependencies in your own PR.

### Toggling batchability of subscription

Subscriptions can be batched. When a dependency update is detected, `darc` will bundle the commits for that update with existing dependency PRs. To toggle whether a subscription is batched or not, you will need to use the `update-subscription` command.

```bash
darc update-subscription --id {subscriptionIdHere}
```

Your shell's default editor will open and allow you to edit the metadata of the subscription.

To disable batching, set `Batchable` to `False` and update the `Merge Policies` section with the following YAML.

```yaml
  - Name: Standard
    Properties: {}
```

To enable batching, set `Batchable` to `True` and remove any `Merge Policies` set on the subscription.

Note: Merge policies can only be set on unbatched subscriptions. Be sure to set/unset the `Merge Policies` field properly as you toggle batchability.
