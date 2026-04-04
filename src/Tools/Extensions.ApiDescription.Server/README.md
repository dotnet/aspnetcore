# Microsoft.Extensions.ApiDescription.Server

MSBuild glue for OpenAPI document generation.

## How To Use

See partner packages such as [NSwag.AspNetCore](https://www.nuget.org/packages/NSwag.AspNetCore/) or
[Swashbuckle.AspNetCore](https://www.nuget.org/packages/Swashbuckle.AspNetCore/) for intended use.

## Viewing Document Generation Logs

When building, the OpenAPI document generation tool (`dotnet-getdocument`) logs output such as `Generating document named 'v1'`.

With the default [Terminal Logger](https://learn.microsoft.com/dotnet/core/tools/dotnet-build#options) introduced in .NET 8, this output is visible at the default verbosity. To see more detailed output, use the `detailed` Terminal Logger verbosity:

```shell
dotnet build -tlp:v=d
```

To disable the Terminal Logger entirely and see all build output:

```shell
dotnet build --tl:off
```
