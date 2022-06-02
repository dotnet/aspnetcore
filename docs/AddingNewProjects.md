# Adding New Projects To The Repo

Sample PR of final result: https://github.com/dotnet/aspnetcore/pull/41945

## Creating a new project
1. Create a new folder that will house your `.csproj` and other project-related files.
2. (EXTREMELY IMPORTANT) Inside our new folder make a new folder called `src`.
3. In VS, Add a new `Solution Folder` in the new folder from the first step.
4. Create the project via the VS `Add` menu (select the folder -> right click -> Add -> follow the wizard).
5. (OPTIONAL) Add the following line to the `.csproj`'s `PropertyGroup` to include your project in the SharedFx API:
    ```XML
    <IsAspNetCoreApp>true</IsAspNetCoreApp>
    ```

**Note:** Depending on what kind of project you are creating, VS will create different files in your project. Please make sure that it includes the following minimal subset of files:
- `AssemblyInfo.cs`
  - Lists various properties of your compiled assembly, such as to which other packages your `internal` properties and methods are available to.
- `[Your project name].csproj`
  - Lists various properties about your project such as dependencies, what files to compile, tags, description of the project, etc.
- `PublicAPI.Shipped.txt`
  - Lists publically visible APIs that are exported from your final compiled `.dll`.
  - This only lists APIs that have already been shipped to customers and cannot be changed.
- `PublicAPI.UnShipped.txt`
  - Lists publicly visible APIs that are exported from your final compiled `.dll`. If this is not configured properly, you will get build errors. VS will warn you though with green squiggly lines. If you see these squiggly lines, open the VS Quick Actions (CTRL + '.') and select the option to and it to the public API.
  - This only lists APIs that have NOT already been shipped to customers. So, these can still change.

## Adding to the rest of the repo
1. VS Should have already registered your `.csproj` in the corresponding solution (`.sln`) and solution filter (`.slnf`) files. See this [Example](https://github.com/dotnet/aspnetcore/pull/41945/commits/586ccc8c895862b65645c4b0f979db1eecd29626)
  - If VS has not already modified these files, make sure to add it manually as is visible in the example listed above.
2. Run the `eng/scripts/GenerateProjectList.ps1` file to regenerate all the reference assemblies.

**Note:** If you are adding a new project to the root `src` directory, you will also need to add a reference in both of the `DotNetProjects` lists of the `eng/Build.props` file as shown below:
  ```XML
   <DotNetProjects Include="
                          $(RepoRoot)src\[YOUR FOLDER]\src\[Your .csproj file];
                          ...
  ```
