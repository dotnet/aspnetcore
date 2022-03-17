ASP.NET Core Diagnostics
===

## Development

Diagnostics middleware like `DeveloperExceptionPage` uses compiled Razor views. After updating the `*.cshtml` file you must run the [RazorPageGenerator](https://github.com/dotnet/aspnetcore/tree/77599445aabd7bf357feb5cf8dfec7187148f1af/src/Middleware/tools/RazorPageGenerator) tool to generate an updated compiled Razor view.

Run the following command in `src\Middleware\tools\RazorPageGenerator`:

```
dotnet run Microsoft.AspNetCore.Diagnostics.RazorViews path-to-aspnetcore-middleware-diagnostics-src
```
