## About

`Microsoft.AspNetCore.Authentication.OpenIdConnect` provides middleware that enables an application to support the OpenID Connect authentication workflow.

## Key Features

* Single sign-on and single sign-out support
* Integration with external identity providers
* Token validation and management
* Configuration and mapping of user claims

## How to Use

To use `Microsoft.AspNetCore.Authentication.OpenIdConnect`, follow these steps:

### Installation

```shell
dotnet add package Microsoft.AspNetCore.Authentication.OpenIdConnect
```

### Configuration

To configure `Microsoft.AspNetCore.Authentication.OpenIdConnect`, you need to add the necessary services and middleware to your application.

1. In the `Program.cs` of your ASP.NET Core app, add the following code to register the OpenID Connect authentication services:
    ```csharp
    builder.Services
        .AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
        })
        .AddCookie()
        .AddOpenIdConnect(options =>
        {
            // Configure the authentication options
            options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.Authority = "your-identity-provider";
            options.ClientId = "your-client-id";
            options.ClientSecret = "your-client-secret-from-user-secrets-or-keyvault";
            options.ResponseType = "code";
            options.Scope.Add("profile");
            options.SaveTokens = true;
        });
    ```

    Make sure to replace `your-identity-provider`, `your-client-id`, and `your-client-secret-from-user-secrets-or-keyvault`, with the appropriate values for your application and identity provider.

2. Add the following code to enable the OpenID Connect authentication middleware:
    ```csharp
    var app = builder.Build();

    app.UseAuthentication();
    ```
    This ensures that the authentication middleware is added to the request pipeline.

## Main Types

The main types provided by `Microsoft.AspNetCore.Authentication.OpenIdConnect` are:

* `OpenIdConnectOptions`: Represents the options for configuring the OpenID Connect authentication middleware
* `OpenIdConnectEvents`: Provides event handlers for various stages of the OpenID Connect authentication workflow

For more information on these types and their usage, refer to the [official documentation](https://learn.microsoft.com/dotnet/api/microsoft.aspnetcore.authentication.openidconnect).

## Additional Documentation

For additional documentation on using OpenID Connect authentication in ASP.NET Core, you can refer to the following resources:

* [ASP.NET Core Authentication](https://learn.microsoft.com/aspnet/core/security/authentication)
* [OpenID Connect](https://openid.net/developers/how-connect-works)
* [Entra ID documentation](https://learn.microsoft.com/entra/identity)

## Feedback & Contributing

`Microsoft.AspNetCore.Authentication.OpenIdConnect` is released as open-source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/aspnetcore).
