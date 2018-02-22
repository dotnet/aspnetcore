dotnet-watch
============
`dotnet-watch` is a file watcher for `dotnet` that restarts the specified application when changes in the source code are detected.

### How To Use

The command must be executed in the directory that contains the project to be watched.

    Usage: dotnet watch [options] [[--] <args>...]

    Options:
      -?|-h|--help  Show help information
      -q|--quiet    Suppresses all output except warnings and errors
      -v|--verbose  Show verbose output

Add `watch` after `dotnet` and before the command arguments that you want to run:

| What you want to run                           | Dotnet watch command                                     |
| ---------------------------------------------- | -------------------------------------------------------- |
| dotnet run                                     | dotnet **watch** run                                     |
| dotnet run --arg1 value1                       | dotnet **watch** run --arg1 value                        |
| dotnet run --framework net451 -- --arg1 value1 | dotnet **watch** run --framework net451 -- --arg1 value1 |
| dotnet test                                    | dotnet **watch** test                                    |

### Environment variables

Some configuration options can be passed to `dotnet watch` through environment variables. The available variables are:

| Variable                                       | Effect                                                   |
| ---------------------------------------------- | -------------------------------------------------------- |
| DOTNET_USE_POLLING_FILE_WATCHER                | If set to "1" or "true", `dotnet watch` will use a polling file watcher instead of CoreFx's `FileSystemWatcher`. Used when watching files on network shares or Docker mounted volumes.                       |

### MSBuild

dotnet-watch can be configured from the MSBuild project file being watched.

**Watch items**

dotnet-watch will watch all items in the **Watch** item group.
By default, this group inclues all items in **Compile** and **EmbeddedResource**.

More items can be added to watch in a project file by adding items to 'Watch'.

```xml
<ItemGroup>
    <!-- extends watching group to include *.js files -->
    <Watch Include="**\*.js" Exclude="node_modules\**\*.js;$(DefaultExcludes)" />
</ItemGroup>
```

dotnet-watch will ignore Compile and EmbeddedResource items with the `Watch="false"` attribute.

Example:

```xml
<ItemGroup>
    <!-- exclude Generated.cs from dotnet-watch -->
    <Compile Update="Generated.cs" Watch="false" />
    <!-- exclude Strings.resx from dotnet-watch -->
    <EmbeddedResource Update="Strings.resx" Watch="false" />
</ItemGroup>
```

**Project References**

By default, dotnet-watch will scan the entire graph of project references and watch all files within those projects.

dotnet-watch will ignore project references with the `Watch="false"` attribute.

```xml
<ItemGroup>
  <ProjectReference Include="..\ClassLibrary1\ClassLibrary1.csproj" Watch="false" />
</ItemGroup>
```


**Advanced configuration**

dotnet-watch performs a design-time build to find items to watch.
When this build is run, dotnet-watch will set the property `DotNetWatchBuild=true`.

Example:

```xml
  <ItemGroup Condition="'$(DotNetWatchBuild)'=='true'">
    <!-- only included in the project when dotnet-watch is running -->
  </ItemGroup>
```
