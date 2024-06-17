## About

Microsoft.AspNetCore.Mvc.NewtonsoftJson is a NuGet package designed to enable the use of JSON serialization and deserialization using Newtonsoft.Json in ASP.NET Core MVC applications. This package provides support for handling JSON input and output in ASP.NET Core MVC controllers, allowing for seamless integration with existing Newtonsoft.Json configurations and features.

## Key Features

<!-- The key features of this package -->

* Integration of Newtonsoft.Json into ASP.NET Core MVC for JSON serialization and deserialization.
* Compatible with ASP.NET Core 3.0 and newer.
* Allows customization of JSON serialization settings.
* Supports handling JSON requests and responses in MVC controllers.

## How to Use

<!-- A compelling example on how to use this package with code, as well as any specific guidelines for when to use the package -->
To start using Microsoft.AspNetCore.Mvc.NewtonsoftJson in your ASP.NET Core MVC application, follow these steps:

### Installation

```sh
dotnet add package Microsoft.AspNetCore.Mvc.NewtonsoftJson
```

### Configuration

In your Startup.cs file, configure NewtonsoftJson as the default JSON serializer for ASP.NET Core MVC:

```C#
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

public void ConfigureServices(IServiceCollection services)
{
    services.AddControllersWithViews()
        .AddNewtonsoftJson(options =>
        {
            // Configure Newtonsoft.Json options here
            options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
        });
}
```

### Usage
Now, you can use Newtonsoft.Json serialization and deserialization in your ASP.NET Core MVC controllers:

```C#
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

public class SampleController : Controller
{
    [HttpPost]
    public IActionResult Post([FromBody] MyModel model)
    {
        // Your action logic here
    }
}

public class MyModel
{
    public string Name { get; set; }
    public int Age { get; set; }
}
```
For more information on configuring and using Newtonsoft.Json in ASP.NET Core MVC, refer to the [official documentation](https://learn.microsoft.com/aspnet/core/mvc/overview?view=aspnetcore-8.0).

## Main Types

<!-- The main types provided in this library -->

The main types provided by this library are:

* `NewtonsoftJsonOptions`: Options for configuring Newtonsoft.Json serialization settings.
* `NewtonsoftJsonInputFormatter`: Input formatter for handling JSON input using Newtonsoft.Json.
* `NewtonsoftJsonOutputFormatter`: Output formatter for handling JSON output using Newtonsoft.Json.

## Additional Documentation

<!-- Links to further documentation. Remove conceptual documentation if not available for the library. -->

* [Overview of ASP.NET Core MVC](https://learn.microsoft.com/aspnet/core/mvc/overview?view=aspnetcore-8.0)

## Feedback & Contributing

<!-- How to provide feedback on this package and contribute to it -->

Microsoft.AspNetCore.Authentication.JwtBearer is released as open-source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/aspnetcore).