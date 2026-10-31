Authorization
===========================

Contains the authorization components for ASP.NET Core. See the [ASP.NET Core authorization documentation](https://learn.microsoft.com/aspnet/core/security/authorization/).

Some community projects related to authentication and authorization for ASP.NET Core are listed in the [documentation](https://learn.microsoft.com/aspnet/core/security/authentication/community).

## Description

This area has the following subdirectories:
- [Core/](Core/): Shared authorization components.
- [Policy/](Policy/): Policy based authorization.
- [test/](test/): Shared tests.

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
