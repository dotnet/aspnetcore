dotnet-watch
============
`dotnet-watch` is a file watcher for `dotnet` that restarts the specified application when changes in the source code are detected.

### How To Install

Add `Microsoft.DotNet.Watcher.Tools` to the `tools` section of your `project.json` file.

Use the version "1.0.0-preview2-final" if you are using .NET Core 1.0.0 and use "1.0.0-preview3-final" if you are using .NET Core 1.1.0.

```
{
...
  "tools": {
    "Microsoft.DotNet.Watcher.Tools": "1.0.0-preview2-final" //"1.0.0-preview3-final" for .NET Core 1.1.0
  }
...
}
```

### How To Use

    dotnet watch [-?|-h|--help]

    dotnet watch [options] [[--] <dotnet arguments>...]

    Options:
      -?|-h|--help  Show help information
      -q|--quiet    Suppresses all output except warnings and errors
      -v|--verbose  Show verbose output

Add `watch` after `dotnet` in the command that you want to run:

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

**Project References**

By default, dotnet-watch will scan the entire graph of project references and watch all files within those projects.

dotnet-watch will ignore project references with the `Watch="false"` attribute.

```xml
<ItemGroup>
  <ProjectReference Include="..\ClassLibrary1\ClassLibrary1.csproj" Watch="false" />
</ItemGroup>
```

**Watch items**

dotnet-watch will watch all items in the "<kbd>Watch</kbd>" item group.
By default, this group inclues all items in "<kbd>Compile</kbd>" and "<kbd>EmbeddedResource</kbd>".

More items can be added to watch in a project file by adding items to 'Watch'.

Example:

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
    <Compile Include="Generated.cs" Watch="false" />
    <EmbeddedResource Include="Generated.cs" Watch="false" />
</ItemGroup>
```


**Advanced configuration**

dotnet-watch performs a design-time build to find items to watch.
When this build is run, dotnet-watch will set the property `DotNetWatchBuild=true`.

Example:

```xml
  <ItemGroup Condition="'$(DotNetWatchBuild)'=='true'">
    <!-- design-time only items -->
  </ItemGroup>
```