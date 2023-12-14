Security
===========================

Contains the authentication and authorization components for ASP.NET Core. See the [ASP.NET Core security documentation](https://learn.microsoft.com/aspnet/core/security/).

Some community projects related to authentication and authorization for ASP.NET Core are listed in the [documentation](https://learn.microsoft.com/aspnet/core/security/authentication/community).

## Description

This area has the following subdirectories:
- [Authentication/](Authentication/): Components for identifying a user
- [Authorization/](Authorization/): Components for determining if a user has the required permissions
- [CookiePolicy/](CookiePolicy/): A middleware that enforces security attributes on response cookies.
- [perf/](perf/): Performance testing infrastructure.
- [samples/](samples/): Samples that combine some of the above feature areas.
- [test/](test/): Shared tests.

### Notes

ASP.NET Security does not include Basic Authentication middleware due to its potential insecurity and performance problems. If you host under IIS, you can enable Basic Authentication via IIS configuration.

## Development Setup

### Build

To build this specific project from source, you can follow the instructions [on building a subset of the code](https://github.com/dotnet/aspnetcore/blob/main/docs/BuildFromSource.md#building-a-subset-of-the-code).

Or for the less detailed explanation, run the following command inside this directory.
```powershell
> ./build.cmd
```

### Test

To run the tests for this project, you can [run the tests on the command line](https://github.com/dotnet/aspnetcore/blob/main/docs/BuildFromSource.md#running-tests-on-command-line) in this directory.

Or for the less detailed explanation, run the following command inside this directory.
```powershell
> ./build.cmd -t
```

You can also run project specific tests by running `dotnet test` in the `tests` directory next to the `src` directory of the project.

## More Information

For more information, see the [ASP.NET Core README](../../README.md).
