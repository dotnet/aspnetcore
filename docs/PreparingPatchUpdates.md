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

* **For packages with source code in this repo (not a submodule):** Update the list of packages in [eng/PatchConfig.props](/eng/PatchConfig.props) to list which packages should be patching in this release.

* **For packages still building from submodules:** Update the list of repositories which will contain changes in [build/submodules.props](/build/submodules.props).

    * `<ShippedRepository>` items represent repos which were released in a previous patch, and will not contain servicing updates in the next patch.
    * `<Repository>` items represent repos which will produce new packages in this patch.
    * It is usually best to move everything to `<ShippedRepository>` and then iteratively add them back to `<Repository>` as new repos receive approval to patch.
    * Don't change the `PatchPolicy` attribute. The build system uses this to ensure patching rules are obeyed.

* For each repository still listed as a `<Repository>`, update the version.props file in that submodule. For example, https://github.com/aspnet/Templating/pull/824

    * The version prefix in repos should match the version of ASP.NET Core.
        * Exception: SignalR, which is "1.1", not "2.1".
    * This leaves holes in versioning, which is okay. This may mean you increment the patch value by more than one. Example:
        * EF Core ships patches in 2.1.4 as "2.1.4"
        * EF Core does not ship patches in 2.1.5 or 2.1.6
        * EF Core ships in 2.1.7, therefore, EFCore's version.props file should jump from 2.1.4 to 2.1.7.

        ```diff
        <!-- Example change to modules/EntityFrameworkCore/version.props -->
        - <VersionPrefix>2.1.4</VersionPrefix>
        + <VersionPrefix>2.1.7</VersionPrefix>
        ```
