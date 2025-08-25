# Microsoft.Extensions.ApiDescription.Server

MSBuild glue for OpenAPI document generation.

## How To Use

See partner packages such as [NSwag.AspNetCore](https://www.nuget.org/packages/NSwag.AspNetCore/) or
[Swashbuckle.AspNetCore](https://www.nuget.org/packages/Swashbuckle.AspNetCore/) for intended use.

## Viewing build-time OpenAPI logs (Terminal Logger)

When `Microsoft.Extensions.ApiDescription.Server` runs the **GetDocument** step during `dotnet build`, its progress messages may not appear with the .NET **Terminal Logger** at default verbosity (the default in .NET 8+).  
To surface these messages while building:


```powershell
dotnet build -tlp:v=d   # Detailed terminal logger verbosity
# or
dotnet build --tl:off   # Disable terminal logger and use legacy-style logs
```

This will display messages like Generating document named 'v1' which are hidden at normal verbosity and can be useful for diagnosing document generation issues.
