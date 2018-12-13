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

* Use `<Reference>`
* Do not use `<PackageReference>`
* Only use `<ProjectReference>` in test projects
* Name the .csproj file to match the assembly name.
* Run `build.cmd /t:GenerateProjectList` when adding new projects
* Use [eng/tools/BaseLineGenerator/](/eng/tools/BaselineGenerator/README.md) if you need to update baselines.

## Important files

* [eng/Baseline.xml](/eng/Baseline.xml) - this contains the 'baseline' of the latest servicing release for this branch. It should be modified and used to update the generated file, Baseline.Designer.props.
* [eng/Dependencies.props](/eng/Dependencies.props) - contains a list of all package references that might be used in the repo.
* [eng/PatchConfig.props](/eng/PatchConfig.props) - lists which assemblies or packages are patching in the current build.
* [eng/ProjectReferences.props](/eng/ProjectReferences.props) - lists which assemblies or packages might be available to be referenced as a local project
* [eng/Versions.props](/eng/Versions.props) - contains a list of versions which may be updated by automation.
