# Components.AI

Components.AI is an independently versioned Blazor component package for building AI conversational user interfaces on top of `Microsoft.Extensions.AI`.

## Daily packages

The scheduled `components-ai-daily` pipeline publishes signed preview packages to the .NET 11 daily feed:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="dotnet11" value="https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet11/nuget/v3/index.json" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
```

Reference the exact version produced by the pipeline:

```xml
<PackageReference Include="Microsoft.AspNetCore.Components.AI" Version="0.1.0-alpha.1.26410.1" />
```

The final two version components identify the official build and will change with each publication. Daily packages are not supported for production use. Use the exact version reported by the pipeline in shared or automated environments.
