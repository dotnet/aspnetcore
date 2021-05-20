# SiteExtensions

Site extensions are extensions specific to Azure App Services.

## Description

The following contains a description of each sub-directory in the `SiteExtensions` directory.

- `LoggingAggregate`: Site extensions for logging integration for ASP.NET Core applications on Azure App Service.
- `LoggingBranch`: Site extension which enables additional functionality for ASP.NET Core on Azure WebSites, such as enabling Azure logging.
- `Runtime`: This site extension installs Microsoft.AspNetCore.App and Microsoft.NetCore.App shared runtimes.

## Development Setup

### Build

To build this specific project from source, follow the instructions [on building the project](../../docs/BuildFromSource.md#step-3-build-the-repo).

Or for the less detailed explanation, run the following command inside this directory.
```powershell
> ./build.cmd
```

## More Information

For more information, see the [ASP.NET Core README](../../README.md).
