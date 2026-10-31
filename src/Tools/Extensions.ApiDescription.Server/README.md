# Microsoft.Extensions.ApiDescription.Server

MSBuild glue for OpenAPI document generation.

## How To Use

See partner packages such as [NSwag.AspNetCore](https://www.nuget.org/packages/NSwag.AspNetCore/) or
[Swashbuckle.AspNetCore](https://www.nuget.org/packages/Swashbuckle.AspNetCore/) for intended use.

## Troubleshooting

When using the .NET Terminal Logger, OpenAPI document generation output may not appear during `dotnet build`.

To surface detailed OpenAPI document-generation output, use:

    dotnet build -tlp:v=d
