# ProjectName

These projects enable diagnostics integration for apps deployed in Azure App Service. For additional detail see the [App Service docs](https://learn.microsoft.com/aspnet/core/host-and-deploy/azure-apps/).

## Description

There are two main projects in this area:
- Microsoft.AspNetCore.AzureAppServicesIntegration
- Microsoft.AspNetCore.AzureAppServices.HostingStartup

AzureAppServicesIntegration provides the IWebHostBuilder.UseAzureAppServices() API for manually enabling Microsoft.Extensions.Logging to output to App Service's diagnostics infrastructure.

AzureAppServices.HostingStartup Uses an automatic loading mechanism so that the same logging infrastructure can be enabled by only adding this package reference and an environment variable, no code changes are required.

## Development Setup

### Build

To build this specific project from source, follow the instructions [on building the project](../../docs/BuildFromSource.md).

If all of the prerequisites have already been set up, run the following command inside this directory.
```powershell
> ./build.cmd
```

### Test

To run the tests for this project, [run the tests on the command line](../../docs/BuildFromSource.md#running-tests-on-command-line) in this directory.

If all of the prerequisites have already been set up, run the following command inside this directory.
```powershell
> ./build.cmd -t
```

## More Information

For more information, see the [ASP.NET Core README](../../README.md).
