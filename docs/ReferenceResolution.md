# `<Reference>` resolution

Most project files in this repo should use `<Reference>` instead of `<ProjectReference>` or `<PackageReference>`.
This was done to enable ASP.NET Core's unique requirements without requiring most ASP.NET Core contributors
to understand the complex rules for how versions and references should work. The build system will resolve
Reference items to the correct type and version of references based on our servicing and update rules.

See [ResolveReferences.targets](/eng/targets/ResolveReferences.targets) for the exact implementation of custom
`<Reference>` resolutions.

The requirements that led to this system are:

* Versions of external dependencies should be consistent and easily discovered.
* Newer versions of packages should not have lower dependency versions than previous releases.
* Minimize the cascading effect of servicing updates where possible by keeping a consistent baseline of dependencies.
* Servicing releases should not add or remove dependencies in existing packages.

As a minor point, the current system also makes our project files somewhat less verbose.

## Recommendations for writing a .csproj

* Use `<Reference>`.
* Do not use `<PackageReference>`.
* If you need to use a new package, add it to `eng/Dependencies.props` and `eng/Versions.props`.
* If the package comes from a partner team and needs to have versions automatically updated, also add an entry `eng/Version.Details.xml`.
* Only use `<ProjectReference>` in test projects.
* Name the .csproj file to match the assembly name.
* Run `eng/scripts/GenerateProjectList.ps1` (or `build.cmd /t:GenerateProjectList`) when adding new projects
* Use [eng/tools/BaseLineGenerator/](/eng/tools/BaselineGenerator/README.md) if you need to update baselines.
* If you need to make a breaking change to dependencies, you may need to add `<SuppressBaselineReference>`.

## Important files

* [eng/Baseline.xml](/eng/Baseline.xml) - this contains the 'baseline' of the latest servicing release for this branch.
  It should be modified and used to update the generated file, [eng/Baseline.Designer.props](eng/Baseline.Designer.props).
* [eng/Dependencies.props](/eng/Dependencies.props) - contains a list of all package references that might be used in the repo.
* [eng/ProjectReferences.props](/eng/ProjectReferences.props) - lists which assemblies or packages might be available to be referenced as a local project.
* [eng/Versions.props](/eng/Versions.props) - contains a list of versions which may be updated by automation. This is used by MSBuild to restore and build.
* [eng/Version.Details.xml](/eng/Version.Details.xml) - used by automation to update dependency variables in
  [eng/Versions.props](/eng/Versions.props) and, for SDKs and `msbuild` toolsets, [global.json](global.json).

## Example: adding a new project

Steps for adding a new project to this repo.

1. Create the .csproj
2. Run `eng/scripts/GenerateProjectList.ps1`
3. Add it to Mvc.sln (for example)

## Example: adding a new dependency

Steps for adding a new package dependency to an existing project. Let's say I'm adding a dependency on System.Banana.

1. Add the package to the .csproj file using `<Reference Include="System.Banana" />`
2. Add an entry to [eng/Dependencies.props](/eng/Dependencies.props) e.g. `<LatestPackageReference Include="System.Banana" />`
3. If this package comes from another dotnet team and should be updated automatically by our bot&hellip;
    1. Add an entry to [eng/Versions.props](/eng/Versions.props) like this `<SystemBananaPackageVersion>0.0.1-beta-1</SystemBananaPackageVersion>`.
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

## Example: make a breaking change to references

If Microsoft.AspNetCore.Banana in 2.1 had a reference to `Microsoft.AspNetCore.Orange`, but in 3.1 or 5.0 this reference
is changing to `Microsoft.AspNetCore.BetterThanOrange`, you would need to make these changes to the .csproj file

```diff
<!-- in Microsoft.AspNetCore.Banana.csproj -->
  <ItemGroup>
-    <Reference Include="Microsoft.AspNetCore.Orange" /> <!-- the old dependency -->
+    <Reference Include="Microsoft.AspNetCore.BetterThanOrange" /> <!-- the new dependency -->
+    <SuppressBaselineReference Include="Microsoft.AspNetCore.Orange" /> <!-- suppress as a known breaking change -->
  </ItemGroup>
```

## Updating dependencies manually

If the `dotnet-maestro` bot has not correctly updated the dependencies, it may be worthwhile running `darc` manually:

1. Install `darc` as described in <https://github.com/dotnet/arcade/blob/master/Documentation/Darc.md#setting-up-your-darc-client>
2. Run `darc update-dependencies --channel '.NET Core 3.1 Release'`
   * Use `'trigger-subscriptions'` to prod the bot to create a PR if you do not want to make local changes
   * Use `'.NET 3 Eng''` to update dependencies from dotnet/arcade
   * Use `'.NET Eng - Latest'` to update dependencies from dotnet/arcade in the `master` branch
   * Use `'VS Master'` to update dependencies from dotnet/roslyn in the `master` branch
   * Use `'.NET 5 Dev'` to update dependencies from dotnet/efcore or dotnet/runtime in the `master` branch
3. `git diff` to confirm the tool's changes
4. Proceed with usual commit and PR
