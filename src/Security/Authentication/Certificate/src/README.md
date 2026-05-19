# Microsoft.AspNetCore.Authentication.Certificate

This project contains an implementation of [Certificate Authentication](https://tools.ietf.org/html/rfc5246#section-7.4.4) for ASP.NET Core. Certificate authentication happens at the TLS level, long before it ever gets to ASP.NET Core, so, more accurately this is an authentication handler that validates the certificate and then gives you an event where you can resolve that certificate to a ClaimsPrincipal.

For more information, see [Configure certificate authentication in ASP.NET Core](https://learn.microsoft.com/aspnet/core/security/authentication/certauth).

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

You can also run project specific tests by running `dotnet test` in the `tests` directory next to the `src` directory of the project.

## More Information

For more information, see the [Security README](../../../README.md) and the [ASP.NET Core README](../../../../../README.md).
