Preparing new servicing updates
===============================

In order to prepare this repo to build a new servicing update, the following changes need to be made.

* Increment the patch version in the [version.props](/version.props) file in the repository root.

    ```diff
    -  <AspNetCorePatchVersion>7</AspNetCorePatchVersion>
    +  <AspNetCorePatchVersion>8</AspNetCorePatchVersion>
    ```

* Update the package archive baselines. This is used to make sure each build of the package archives we give to Azure only contains new files and does
  not require overwriting existing files. See [src/PackageArchive/ZipManifestGenerator/](/src/PackageArchive/ZipManifestGenerator/README.md) for instructions on how to run this tool.

* Update the package baselines. This is used to ensure packages keep a consistent set of dependencies between releases.
  See [eng/tools/BaselineGenerator/](/eng/tools/BaselineGenerator/README.md) for instructions on how to run this tool.

* **For the subset of external dependencies mentioned at the top of [build/dependencies.props](/build/dependencies.props):**
  If a package (Microsoft.NetCore.App for example) shipped in the last release, update the package version properties.

  * Changes made above to external dependencies listed in [eng/Baseline.Designer.props](/eng/Baseline.Designer.props)
    should _not_ require further updates. The versions of affected packages should have been updated in
    [build/dependencies.props](/build/dependencies.props) during the previous patching cycle.

* **For packages with source code in this repo (not a submodule):** Update the list of packages in [eng/PatchConfig.props](/eng/PatchConfig.props) to list which packages should be patching in this release.

* **For packages still building from submodules:** Update the list of repositories which will contain changes in [build/submodules.props](/build/submodules.props).

  * `<ShippedRepository>` items represent repos which were released in a previous patch, and will not contain servicing updates in the next patch.
  * `<Repository>` items represent repos which will produce new packages in this patch.
  * It is usually best to move everything to `<ShippedRepository>` and then iteratively add them back to `<Repository>` as new repos receive approval to patch.
    * But, do not change the Templating item at all. That is only _treated_ as a submodule.
  * Don't change the `PatchPolicy` attribute. The build system uses this to ensure patching rules are obeyed.

* **For each repository still listed as a `<Repository>`:** Update the version.props file in that submodule. For example, https://github.com/aspnet/EntityFrameworkCore/pull/15369/files#diff-2a92b4d7f8df251ffd3a0aa63e97aad5

  * This leaves holes in versioning, which is okay. This may mean you increment the patch value by more than one. Example:
    * EF Core ships patches in 2.1.8 as "2.1.8"
    * EF Core does not ship patches in 2.1.9 or 2.1.10
    * EF Core ships in 2.1.11, therefore, EFCore's version.props file should jump from 2.1.8 to 2.1.11.

    ```diff
    <!-- Example change to modules/EntityFrameworkCore/version.props -->
    - <PatchVersion>8</PatchVersion>
    + <PatchVersion>11</PatchVersion>
    ```
