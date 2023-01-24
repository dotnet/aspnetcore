# Microsoft.AspNetCore.Authentication.OAuth

This project contains a generic implementation of [OAuth2 Authentication](https://learn.microsoft.com/aspnet/core/security/authentication/social/) for ASP.NET Core. While it can be used directly, it's often easier to use a derived implementation for a specific provider. Adjacent directories contain providers for Facebook, Google, Microsoft Accounts, and Twitter.

Some community projects related to OAuth for ASP.NET Core are listed in the [documentation](https://learn.microsoft.com/aspnet/core/security/authentication/social/).

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
