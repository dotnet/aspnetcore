## About

Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation is a NuGet package designed to provide runtime compilation support for Razor views in ASP.NET Core MVC applications. This package enables developers to modify and update Razor views without needing to restart the application, facilitating a more dynamic development experience.

## Key Features

* Runtime compilation of Razor views in ASP.NET Core MVC applications.
* Allows developers to modify Razor views without restarting the application.
* Supports faster iteration and development cycles.
* Compatible with ASP.NET Core 3.0 and newer.

## Limitations

* Isn't supported for Razor components of Blazor apps.
* Doesn't support [global using directives](/dotnet/csharp/whats-new/csharp-10#global-using-directives).
* Doesn't support [implicit using directives](/dotnet/core/tutorials/top-level-templates#implicit-using-directives).
* Disables [.NET Hot Reload](xref:test/hot-reload).
* Is recommended for development, not for production.

## How to Use

To start using Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation in your ASP.NET Core MVC application, follow these steps:

### Installation

Install the package via NuGet Package Manager or .NET CLI:

```sh
dotnet add package Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation
```

### Configuration

In your Startup.cs file, configure runtime compilation for Razor views:

```C#
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

public void ConfigureServices(IServiceCollection services)
{
    services.AddControllersWithViews()
        .AddRazorRuntimeCompilation();
}
```

### Usage
Now, you can modify Razor views in your application, and the changes will be picked up dynamically without requiring a restart:

```html
<!-- Example Razor view: Views/Home/Index.cshtml -->
@{
    ViewData["Title"] = "Home Page";
}

<h2>@ViewData["Title"]</h2>

<p>Welcome to the ASP.NET Core MVC application!</p>
```

For more information on using runtime compilation for Razor views in ASP.NET Core MVC, refer to the [official documentation](https://learn.microsoft.com/en-us/aspnet/core/mvc/overview?view=aspnetcore-8.0).

## Main Types

<!-- The main types provided in this library -->

The main types provided by this library are:

* `RazorRuntimeCompilationMvcBuilderExtensions`: Extension methods for configuring runtime compilation for Razor views.
* `RazorRuntimeCompilationMvcOptions`: Options for configuring runtime compilation settings.

## Additional Documentation

<!-- Links to further documentation. Remove conceptual documentation if not available for the library. -->

* [Overview of ASP.NET Core MVC](https://learn.microsoft.com/en-us/aspnet/core/mvc/overview?view=aspnetcore-8.0)
* [Razor syntax reference for ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/mvc/views/razor?view=aspnetcore-8.0)

## Feedback & Contributing

<!-- How to provide feedback on this package and contribute to it -->

Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation is released as open-source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/aspnetcore).