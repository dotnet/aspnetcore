AzureIntegration
===

Features that integrate ASP.NET Core with Azure.

This project is part of ASP.NET Core. You can find samples, documentation and getting started instructions for ASP.NET Core at the [Home](https://github.com/aspnet/home) repo.


SiteExtensions
===

To install a nightly preview of the ASP.NET Core runtime site extension for testing purposes:
1. In the Azure portal select App Services -> your site -> Application settings
1. Set `SCM_SITEEXTENSIONS_FEED_URL` application setting to `https://dotnet.myget.org/F/aspnetcore-dev/`
1. Go to `DEVELOPMENT TOOLS` -> `Advanced Tools` -> `Site extensions` -> `Gallery`
1. Enter `AspNetCoreRuntime` into `Search` box and click `Search`
1. Click `+` to install site extension and wait untill installation animation finishes
1. `Extensions` tab should now show newly installed site extension
1. Click `Restart site` on the right side of the page when installation finishes (this would only restart Kudu site, not the main one)
1. Restart site in `Overview` tab of `App service`


To update ASP.NET Core runtime site extension:
1. Stop site in `Overview` tab of `App service`
1. Go to `DEVELOPMENT TOOLS` -> `Advanced Tools` -> `Site extensions`
1. Click update on site extension
1. Start site in `Overview` tab of `App service`