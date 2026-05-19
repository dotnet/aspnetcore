Authentication
===========================

Contains the authentication components for ASP.NET Core. See the [ASP.NET Core authentication documentation](https://learn.microsoft.com/aspnet/core/security/authentication/).

Some community projects related to authentication and authorization for ASP.NET Core are listed in the [documentation](https://learn.microsoft.com/aspnet/core/security/authentication/community).

## Description

This area has the following subdirectories:
- [Certificate/](Certificate/): An authentication handler for client certificates.
- [Cookies/](Cookies/): An authentication handler that uses cookies.
- [Core/](Core/): Shared authentication components.
- [Facebook/](Facebook/): An OAuth2 authentication handler for Facebook.
- [Google/](Google/): An OAuth2 authentication handler for Google.
- [JwtBearer/](JwtBearer/): An OpenId Connect based JWT bearer authentication handler.
- [MicrosoftAccount/](MicrosoftAccount/): An OAuth2 authentication handler for Microsoft Accounts
- [Negotiate/](Negotiate/): An authentication handler for Kerberos/Negotiate/NTLM used with the Kestrel server.
- [OAuth/](OAuth/): A generic OAuth2 authentication handler.
- [OpenIdConnect/](OpenIdConnect/): A OpenId Connect authentication handler for interactive clients.
- [Twitter/](Twitter/): An OAuth1a authentication handler for Twitter.
- [WsFederation/](WsFederation/): A WsFederation authentication handler for interactive clients.
- [samples/](samples/): Samples that combine some of the above feature areas.
- [test/](test/): Shared tests.

### Notes

ASP.NET Authentication does not include Basic Authentication middleware due to its potential insecurity and performance problems. If you host under IIS, you can enable Basic Authentication via IIS configuration.

## Development Setup

### Build

To build this specific project from source, you can follow the instructions [on building a subset of the code](https://github.com/dotnet/aspnetcore/blob/main/docs/BuildFromSource.md#building-a-subset-of-the-code).

Or for the less detailed explanation, run the following command inside the parent `security` directory.
```powershell
> ./build.cmd
```

### Test

To run the tests for this project, you can [run the tests on the command line](https://github.com/dotnet/aspnetcore/blob/main/docs/BuildFromSource.md#running-tests-on-command-line) in this directory.

Or for the less detailed explanation, run the following command inside the parent `security` directory.
```powershell
> ./build.cmd -t
```

## More Information

For more information, see the [Security README](../README.md) and the [ASP.NET Core README](../../../README.md).
