# Adding New Projects To The Repo

Sample PR of final result: https://github.com/dotnet/aspnetcore/pull/41945

## Creating a new project
1. Create a new folder that will house your `.csproj` and other project-related files.
2. (EXTREMELY IMPORTANT) Inside this new folder, make a new folder for the source files of your project. For regular functionality-adding project this will be called `src`. However, if you are adding a different kind of project, it will be called something more applicable (ex. `test/` for a test project).
3. Open the `.slnf` you want to add your project to in VS (preferably via the `startvs.cmd` script located in the same folder as the `.slnf`). Then add a new `Solution Folder` in the new folder with the same name and location as the actual folder created in the first step.
4. Create the project via the VS `Add` menu (select the folder -> right click -> Add -> New Project... -> follow the wizard).

  **Note:** (Only applicable to `src/` projects) Depending on what kind of project you are creating, VS will create different files in your project. You might also want to add the following files:
  - `PublicAPI.Shipped.txt`
    - Lists publically visible APIs that are exported from your final compiled `.dll`.
    - This only lists APIs that have already been shipped to customers and cannot be changed.
    - There is an empty template at `eng/PublicAPI.empty.txt` for your reference. You can copy and rename the file to add it to your project. Make sure the name is exactly as shown above.
  - `PublicAPI.UnShipped.txt`
    - Lists publicly visible APIs that are exported from your final compiled `.dll`. If this is not configured properly, you will get build errors. VS will warn you though with green squiggly lines. If you see these squiggly lines, open the VS Quick Actions (CTRL + '.') and select the option to add it to the public API.
    - This only lists APIs that have NOT already been shipped to customers. So, these can still change.
    - There is an empty template at `eng/PublicAPI.empty.txt` for your reference. You can copy and rename the file to add it to your project. Make sure the name is exactly as shown above.
  - You can expose internals via `@(InternalsVisibleTo)` items in your `.csproj`.
    ```XML
    <ItemGroup>
      <InternalsVisibleTo Include="Microsoft.AspNetCore.My.TestProject" />
    ```

## Adding to the rest of the repo
1. VS should have already registered your `.csproj` in the corresponding solution ([`.sln`](https://github.com/dotnet/aspnetcore/blob/586ccc8c895862b65645c4b0f979db1eecd29626/AspNetCore.sln)) and solution filter ([`.slnf`](https://github.com/dotnet/aspnetcore/blob/586ccc8c895862b65645c4b0f979db1eecd29626/src/Middleware/Middleware.slnf#L107-L109)) files.
  - If VS has not already modified these files, open the `.slnf` you want to add the project to. Create a solution folder for your project if doesn't exist already. Then right click solution folder -> Add -> Existing Project... -> follow the wizard.
1. Run the `eng/scripts/GenerateProjectList.ps1` file to regenerate a number of `eng/*.props` files e.g. ProjectReferences.props.

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

## (OPTIONAL) Including your project in SharedFx
1. Add the following line to the `.csproj`'s `PropertyGroup` to include your project in the SharedFx API:
    ```XML
    <IsAspNetCoreApp>true</IsAspNetCoreApp>
    ```
2. Re-run the `eng/scripts/GenerateProjectList.ps1` to add your project to the `eng/SharedFramework.Local.props` file and, if applicable, the `eng/TrimmableProjects.props` file.
3. Add your project name to the lists in `src\Framework\test\TestData.cs`. This is not strictly necessary for the project to work but there is a test on CI that will fail if this is not done. Make sure to include your project in a way that maintains alphabetical order.

## Manually saving solution and solution filter files
VS is pretty good at keeping the files up to date and organized correctly. It will also prompt you if it finds an error and, in most cases, offer a solution to fix the issue. Sometimes just saving the file will trigger VS to resolve any issues automatically. However, if you would like to add a new solution filter file or update one manually you can find a tutorial link [here](https://learn.microsoft.com/visualstudio/ide/filtered-solutions).
