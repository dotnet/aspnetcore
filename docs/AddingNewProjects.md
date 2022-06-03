# Adding New Projects To The Repo

Sample PR of final result: https://github.com/dotnet/aspnetcore/pull/41945

## Creating a new project
1. Create a new folder that will house your `.csproj` and other project-related files.
2. (EXTREMELY IMPORTANT) Inside our new folder make a new folder called `src`.
3. In VS, Add a new `Solution Folder` in the new folder from the first step.
4. Create the project via the VS `Add` menu (select the folder -> right click -> Add -> follow the wizard).

    **Note:** Depending on what kind of project you are creating, VS will create different files in your project. You might also want to add the following files:
    - `PublicAPI.Shipped.txt`
      - Lists publically visible APIs that are exported from your final compiled `.dll`.
      - This only lists APIs that have already been shipped to customers and cannot be changed.
      - There is an empty template at `eng/PublicAPI.empty.txt` for your reference.
    - `PublicAPI.UnShipped.txt`
      - Lists publicly visible APIs that are exported from your final compiled `.dll`. If this is not configured properly, you will get build errors. VS will warn you though with green squiggly lines. If you see these squiggly lines, open the VS Quick Actions (CTRL + '.') and select the option to and it to the public API.
      - This only lists APIs that have NOT already been shipped to customers. So, these can still change.
      - There is an empty template at `eng/PublicAPI.empty.txt` for your reference.
    - `AssemblyInfo.cs`
      - Lists various properties of your compiled assembly, such as to which other packages your `internal` properties and methods are available to.
      - You can also expose internals via the `@(InternalsVisibleTo)` item in your project file instead of using this `AssemblyInfo.cs` file.

## Adding to the rest of the repo
1. VS Should have already registered your `.csproj` in the corresponding solution ([`.sln`](https://github.com/dotnet/aspnetcore/blob/586ccc8c895862b65645c4b0f979db1eecd29626/AspNetCore.sln)) and solution filter ([`.slnf`](https://github.com/dotnet/aspnetcore/blob/586ccc8c895862b65645c4b0f979db1eecd29626/src/Middleware/Middleware.slnf#L107-L109)) files.
  - If VS has not already modified these files, make sure to add it manually as is visible in the example listed above.
2. Run the `eng/scripts/GenerateProjectList.ps1` file to regenerate a number of `eng/*.props` files e.g. ProjectReferences.props.

**Note:** If you are adding a new project to the root `src` directory, you will also need to add a reference in both of the `DotNetProjects` lists of the `eng/Build.props` file. The first list (the one with condition `'$(BuildMainlyReferenceProviders)' != 'true'"`) has items in the format of:
  ```XML
   <DotNetProjects Include="
                          $(RepoRoot)src\[YOUR FOLDER]\**\*.csproj;
                          ...
  ```
while the second (the one with condition `'$(BuildMainlyReferenceProviders)' == 'true'"`) has them in the format of (note the second `src`):
  ```XML
  <DotNetProjects Include="
                        $(RepoRoot)src\[YOUR FOLDER]\**\src\*.csproj;
                        ...
  ```

## Including your project in SharedFx
1. (OPTIONAL) Add the following line to the `.csproj`'s `PropertyGroup` to include your project in the SharedFx API:
    ```XML
    <IsAspNetCoreApp>true</IsAspNetCoreApp>
    ```
2. Re-run the `eng/scripts/GenerateProjectList.ps1` to add your project to the `eng/SharedFramework.Local.props` file and, if applicable, the `eng/TrimmableProjects.props` file.
3. Add your project name to the lists in `src\Framework\test\TestData.cs`. This is not strictly necessary for the project to work but there is a test on CI that will fail if this is not done. Make sure to include your project in a way that maintains alphabetical ordering.
