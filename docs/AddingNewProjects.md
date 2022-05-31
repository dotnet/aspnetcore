# Adding New Projects To The Repo

Sample PR of final result: https://github.com/dotnet/aspnetcore/pull/41945

## Creating a new project
1. Create a new folder that will house your `.csproj` and other project-related files.
2. In VS, Add a new `Solution Folder` in the same place as the new folder from the previous step.
3. Create the project via the VS `Add` menu (select the folder -> right click -> Add -> follow the wizard).
4. Add the following `ItemGroup` to your newly generated `.csproj`:
    ```XML
    <ItemGroup>
        <None Remove="PublicAPI.Shipped.txt" />
        <None Remove="PublicAPI.Unshipped.txt" />
    </ItemGroup>
    ```
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
- VS Should have already registered your `.csproj` in the corresponding solution (`.sln`) and solution filter (`.slnf`) files. See this [Example](https://github.com/dotnet/aspnetcore/pull/41945/files#diff-cd977e0a76b37d35c04d9d819ea66ef8a35d9ef7f86a9a7c774d751e8119db4fR1713-R11118)
  - If VS has not already modified these files, make sure to add it manually as is visible in the example listed above.
- Add the following new line to the `eng/ProjectReferences.props` file to include your new project:
    ```XML
    <ProjectReferenceProvider Include="[Your project id]" ProjectPath="$(RepoRoot)src\[Rest of the path to your project]" />
    ```
- (OPTIONAL: only necessary if you want to add your project to the SharedFX API)Add the following new line to the `eng/SharedFramework.Local.props` file to include your new project:
    ```XML
    <AspNetCoreAppReference Include="[Your project id]" />
    ```

