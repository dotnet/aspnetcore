dotnet-sql-cache
================

`dotnet-sql-cache` is a command line tool that creates table and indexes in Microsoft SQL Server database to be used for distributed caching

### How To Install

Install `Microsoft.Extensions.Caching.SqlConfig.Tools` as a `DotNetCliToolReference` to your project.

```xml
  <ItemGroup>
    <DotNetCliToolReference Include="Microsoft.Extensions.Caching.SqlConfig.Tools" Version="1.0.0" />
  </ItemGroup>
```

### How To Use

Run `dotnet sql-cache --help` for more information about usage.
