## About

Microsoft.AspNetCore.Authentication.JwtBearer is a middleware component designed for ASP.NET Core applications. It facilitates JSON Web Token (JWT) authentication, enabling secure authentication for APIs and web services. This package allows you to validate JWT tokens issued by an authentication server, ensuring secure access to your application's resources.

## Key Features

<!-- The key features of this package -->

* Seamless integration with ASP.NET Core applications.
* Supports JSON Web Token (JWT) authentication.
* Enables secure authentication for APIs and web services.
* Flexible configuration options for token validation parameters.
* Works with .NET Core 3.0 and newer, as well as .NET Standard 2.1.

## How to Use

<!-- A compelling example on how to use this package with code, as well as any specific guidelines for when to use the package -->

```C#
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

public void ConfigureServices(IServiceCollection services)
{
    services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = "your_issuer",
                ValidAudience = "your_audience",
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("your_secret_key"))
            };
        });

    // Other configurations...
}
```
For more detailed configuration options and advanced scenarios, please refer to the blog [JWT Validation and Authorization in ASP.NET Core](https://devblogs.microsoft.com/dotnet/jwt-validation-and-authorization-in-asp-net-core/).

## Main Types

<!-- The main types provided in this library -->

The main types provided by this library are:

* `JwtBearerDefaults`: Contains default values for JWT Bearer authentication.
* `JwtBearerEvents`: Events used to handle JWT Bearer authentication events.
* `JwtBearerHandler`: Handles JWT Bearer authentication requests.
* `wtBearerOptions`: Options for configuring JWT Bearer authentication.

## Additional Documentation

<!-- Links to further documentation. Remove conceptual documentation if not available for the library. -->

* [Overview of ASP.NET Core authentication](https://learn.microsoft.com/aspnet/core/security/authentication/?view=aspnetcore-8.0)
* [JwtBearer sample](https://github.com/dotnet/aspnetcore/tree/main/src/Security/Authentication/JwtBearer/samples)

## Feedback & Contributing

<!-- How to provide feedback on this package and contribute to it -->

Microsoft.AspNetCore.Authentication.JwtBearer is released as open-source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/aspnetcore).