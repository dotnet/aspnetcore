## About

`Microsoft.AspNetCore.Authentication.MicrosoftAccount` provides ASP.NET Core middleware that enables applications to support the Microsoft Account authentication workflow.

## How to Use

To use `Microsoft.AspNetCore.Authentication.MicrosoftAccount`, follow these steps:

### Installation

```shell
dotnet add package Microsoft.AspNetCore.Authentication.MicrosoftAccount
```

### Configuration

1. Refer to the guide in the official documentation [here](https://learn.microsoft.com/aspnet/core/security/authentication/social/microsoft-logins#create-the-app-in-microsoft-developer-portal) to create the app in Microsoft Developer Portal
2. Follow the steps in the official documentation [here](https://learn.microsoft.com/aspnet/core/security/authentication/social/microsoft-logins#store-the-microsoft-client-id-and-secret) to store the Microsoft client ID and secret
3. Add the Authentication service to your app's `Program.cs`:
    ```csharp
    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddAuthentication().AddMicrosoftAccount(microsoftOptions =>
    {
        microsoftOptions.ClientId = builder.Configuration["Authentication:Microsoft:ClientId"];
        microsoftOptions.ClientSecret = builder.Configuration["Authentication:Microsoft:ClientSecret"];
    });
    ```

## Main Types

The main types provided by this package are:
* `MicrosoftAccountOptions`: Represents the options for configuring Microsoft Account authentication
* `MicrosoftAccountHandler`: The authentication handler responsible for processing Microsoft Account authentication requests and generating the appropriate authentication ticket

## Additional Documentation

For additional documentation and examples, refer to the [official documentation](https://learn.microsoft.com/aspnet/core/security/authentication/social/microsoft-logins) on Microsoft Account login setup in ASP.NET Core.

## Feedback &amp; Contributing

`Microsoft.AspNetCore.Authentication.MicrosoftAccount` is released as open-source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/aspnetcore).
