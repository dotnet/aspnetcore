## About

`Microsoft.AspNetCore.Authentication.Negotiate` contains an authentication handler used to authenticate requests using Windows Authentication (also known as Negotiate, Kerberos, or NTLM).

## How to Use

To use `Microsoft.AspNetCore.Authentication.Negotiate`, follow these steps:

### Installation

```shell
dotnet add package Microsoft.AspNetCore.Authentication.Negotiate
```

### Configuration

To use the middleware, configure it in your ASP.NET Core app's `Program.cs`:

```csharp
builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
   .AddNegotiate();

builder.Services.AddAuthorization();

var app = builder.Build();

Next, refer to the [official documentation](https://learn.microsoft.com/aspnet/core/security/authentication/windowsauth) for configuration details specific to your app's web server.

## Main Types

The main types provided by this package are:

* `NegotiateOptions`: Represents the options for configuring Negotiate Authentication handler behavior
* `NegotiateHandler`: The authentication handler responsible for processing Negotiate authentication requests and generating the appropriate authentication ticket

## Additional Documentation

For additional documentation and examples, refer to the [official documentation](https://learn.microsoft.com/aspnet/core/security/authentication/windowsauth) on Windows Authentication in ASP.NET Core.

## Feedback &amp; Contributing

`Microsoft.AspNetCore.Authentication.Negotiate` is released as open-source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/aspnetcore).
