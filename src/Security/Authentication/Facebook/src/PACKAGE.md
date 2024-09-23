## About

`Microsoft.AspNetCore.Authentication.Facebook` provides ASP.NET Core middleware that enables an application to support Facebook's OAuth 2.0 authentication workflow.

## How to Use

To use `Microsoft.AspNetCore.Authentication.Facebook`, follow these steps:

### Installation

```shell
dotnet add package Microsoft.AspNetCore.Authentication.Facebook
```

### Configuration

1. Refer to the guide in the official documentation [here](https://learn.microsoft.com/aspnet/core/security/authentication/social/facebook-logins#create-the-app-in-facebook) to create the app in Facebook.
2. Follow the steps in the official documentation [here](https://learn.microsoft.com/aspnet/core/security/authentication/social/facebook-logins#store-the-facebook-app-id-and-secret) to store the Facebook app ID and secret.
3. Configure the middleware your ASP.NET Core app's `Program.cs`:
```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication()
    .AddFacebook(options =>
    {
        options.AppId = Configuration["Authentication:Facebook:AppId"];
        options.AppSecret = Configuration["Authentication:Facebook:AppSecret"];
    });
```

## Main Types

The main types provided by this package are:

* `FacebookOptions`: Represents the options for configuring Facebook authentication
* `FacebookHandler`: The authentication handler responsible for processing Facebook authentication requests and generating the appropriate authentication ticket

## Additional Documentation

For additional documentation and examples, refer to the [official documentation](https://learn.microsoft.com/aspnet/core/security/authentication/social/facebook-logins) on Facebook login setup in ASP.NET Core.

## Feedback &amp; Contributing

`Microsoft.AspNetCore.Authentication.Facebook` is released as open-source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/aspnetcore).
