## About

`Microsoft.AspNetCore.Authentication.Certificate` provides ASP.NET Core middleware that enables an application to support certificate authentication.

## How to Use

To use `Microsoft.AspNetCore.Authentication.Certificate`, follow these steps:

### Installation

```shell
dotnet add package Microsoft.AspNetCore.Authentication.Certificate
```

### Configuration

1. Acquire an HTTPS certificate, install it, and [configure your server](https://learn.microsoft.com/aspnet/core/security/authentication/certauth#configure-your-server-to-require-certificates) to require certificates
2. Configure the middleware in your APS.NET Core app's `Program.cs`:
    ```csharp
    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddAuthentication(
            CertificateAuthenticationDefaults.AuthenticationScheme)
        .AddCertificate();

    var app = builder.Build();

    app.UseAuthentication();
    ```

## Main Types

* `CertificateAuthenticationOptions`: Provides options to configure certificate authentication
* `ICertificateValidationCache`: Provides a cache used to store `AuthenticateResult` results after the certificate has been validated

## Additional Documentation

For additional documentation and examples, refer to the [official documentation](https://learn.microsoft.com/aspnet/core/security/authentication/certauth) on certificate authentication in ASP.NET Core.

## Feedback &amp; Contributing

`Microsoft.AspNetCore.Authentication.Certificate` is released as open-source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/aspnetcore).
