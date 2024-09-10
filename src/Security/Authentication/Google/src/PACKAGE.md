## About

`Microsoft.AspNetCore.Authentication.Google` contains middleware to support Google's OAuth 2.0 authentication workflow.

## How to Use

To use `Microsoft.AspNetCore.Authentication.Google`, follow these steps:

### Installation

```shell
dotnet add package Microsoft.AspNetCore.Authentication.Google
```

### Configuration

1. Refer to the guide in the official documentation [here](https://learn.microsoft.com/aspnet/core/security/authentication/social/google-logins#create-the-google-oauth-20-client-id-and-secret) to configure the Google OAuth 2.0 client ID and secret
2. Follow the steps in the official documentation [here](https://learn.microsoft.com/aspnet/core/security/authentication/social/google-logins#store-the-google-client-id-and-secret) to store the Google client ID and secret
3. Add the Authentication service to your app's `Program.cs`:
    ```csharp
    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddAuthentication().AddGoogle(googleOptions =>
    {
        googleOptions.ClientId = builder.Configuration["Authentication:Google:ClientId"];
        googleOptions.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
    });
    ```

## Main Types

The main types provided by this package are:

* `GoogleOptions`: Represents the options for configuring Google authentication
* `GoogleHandler`: The authentication handler responsible for processing Google authentication requests and generating the appropriate authentication ticket

## Additional Documentation

For additional documentation and examples, refer to the [official documentation](https://learn.microsoft.com/aspnet/core/security/authentication/social/google-logins) on Google login setup in ASP.NET Core.

## Feedback &amp; Contributing

`Microsoft.AspNetCore.Authentication.Google` is released as open-source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/aspnetcore).
