## About

This Azure App Service site extension installs the `Microsoft.AspNetCore.App` and `Microsoft.NETCore.App` shared runtimes and the latest native ASP.NET Core Module (ANCM) for IIS.

Azure App Service normally provides current .NET and ASP.NET Core runtimes. This extension is primarily useful for running preview and nightly ASP.NET Core builds before those components are available on the platform.

## Included Components

* The `Microsoft.NETCore.App` shared runtime
* The `Microsoft.AspNetCore.App` shared runtime
* The native ASP.NET Core Module for IIS

This package is an Azure App Service site extension and isn't intended to be referenced directly by applications.

## Additional Documentation

For more information about ASP.NET Core on Azure App Service and the ASP.NET Core Module, see:

* [Configure an ASP.NET Core app in Azure App Service](https://learn.microsoft.com/azure/app-service/configure-language-dotnetcore)
* [ASP.NET Core Module for IIS](https://learn.microsoft.com/aspnet/core/host-and-deploy/aspnet-core-module)
