`<Reference>` resolution
========================

Most project files in this repo should use `<Reference>` instead of `<ProjectReference>` or `<PackageReference>`.
This was done to enable ASP.NET Core's unique requirements without requiring most ASP.NET Core contributors
to understand the complex rules for how versions and references should work. The build system will resolve
Reference items to the correct type and version of references based on our servicing and update rules.

See [ResolveReferences.targets](/eng/targets/ResolveReferences.targets) for the exact implementation of custom
`<Reference>` resolutions.

The requirements that led to this system are:

* Versions of external dependencies should be consistent.
* Servicing updates of ASP.NET Core should minimize the number of assemblies which need to re-build and re-ship.
* Newer versions of packages should not have lower dependency versions than previous releases.
* Minimize the cascading effect of servicing updates where possible by keeping a consistent baseline of dependencies.

## Recommendations for writing a .csproj

* Use `<Reference>`.
* Do not use `<PackageReference>`.
* If you need to use a new package, add it to `eng/Dependencies.props` and `eng/Versions.props`.
* Only use `<ProjectReference>` in test projects.
* Name the .csproj file to match the assembly name.
* Run `build.cmd /t:GenerateProjectList` when adding new projects
* Use [eng/tools/BaseLineGenerator/](/eng/tools/BaselineGenerator/README.md) if you need to update baselines.

## Important files

* [eng/Baseline.xml](/eng/Baseline.xml) - this contains the 'baseline' of the latest servicing release for this branch. It should be modified and used to update the generated file, Baseline.Designer.props.
* [eng/Dependencies.props](/eng/Dependencies.props) - contains a list of all package references that might be used in the repo.
* [eng/PatchConfig.props](/eng/PatchConfig.props) - lists which assemblies or packages are patching in the current build.
* [eng/ProjectReferences.props](/eng/ProjectReferences.props) - lists which assemblies or packages might be available to be referenced as a local project
* [eng/Versions.props](/eng/Versions.props) - contains a list of versions which may be updated by automation.

## Example: adding a new project

Steps for adding a new project to this repo.

1. Create the .csproj
2. Run `eng/scripts/GenerateProjectList.ps1`
3. Add it to Extensions.sln

## Example: adding a new dependency

Steps for adding a new package dependency to an existing project. Let's say I'm adding a dependency on System.Banana.

1. Add the package to the .csproj file using `<Reference Include="System.Banana" />`
2. Add an entry to [eng/Dependencies.props](/eng/Dependencies.props), `<LatestPackageReference Include="System.Banana" Version="0.0.1-beta-1" />`
3. If this package comes from another dotnet team and should be updated automatically by our bot...
    1. Change the LatestPackageReference entry to `Version="$(SystemBananaPackageVersion)"`.
    2. Add an entry to [eng/Versions.props](/eng/Versions.props) like this `<SystemBananaPackageVersion>0.0.1-beta-1</SystemBananaPackageVersion>`.
    3. Add an entry to [eng/Version.Details.xml](/eng/Version.Details.xml) like this:

        ```xml
        <ProductDependencies>
          <!-- ... -->
          </Dependency>
            <Dependency Name="System.Banana" Version="0.0.1-beta-1">
            <Uri>https://github.com/dotnet/corefx</Uri>
            <Sha>000000</Sha>
          </Dependency>
          <!-- ... -->
        </ProductDependencies>
        ```

       If you don't know the commit hash of the source code used to produce "0.0.1-beta-1", you can use `000000` as a placeholder for `Sha`
       as its value is unimportant and will be updated the next time the bot runs.
