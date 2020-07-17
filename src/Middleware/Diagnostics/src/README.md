ASP.NET Core Diagnostics
===

## Development

Diagnostics middleware like `DeveloperExceptionPage` uses compiled Razor views. After updating the `*.cshtml` file you must run the [RazorPageGenerator](https://github.com/dotnet/aspnetcore-tooling/tree/master/src/Razor/src/RazorPageGenerator) tool to generate an updated compiled Razor view.

Run the following command in `AspNetCore-Tooling\src\Razor\src\RazorPageGenerator`:

```
dotnet run Microsoft.AspNetCore.Diagnostics.RazorViews path-to-aspnetcore-middleware-diagnostics-src
```