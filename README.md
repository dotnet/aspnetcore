DotNetTools
===========

[![Travis build status](https://img.shields.io/travis/aspnet/DotNetTools.svg?label=travis-ci&branch=dev&style=flat-square)](https://travis-ci.org/aspnet/DotNetTools/branches)
[![AppVeyor build status](https://img.shields.io/appveyor/ci/aspnetci/DotNetTools/dev.svg?label=appveyor&style=flat-square)](https://ci.appveyor.com/project/aspnetci/DotNetTools/branch/dev)

This project is part of ASP.NET Core. You can find samples, documentation and getting started instructions for ASP.NET Core at <https://docs.asp.net>.

## Projects

The repository contains command-line tools for ASP.NET Core that are bundled* in the [.NET Core CLI](https://github.com/dotnet/cli).
Follow the links below for more details on each tool.

 - [dotnet-watch](src/dotnet-watch/README.md)
 - [dotnet-user-secrets](src/dotnet-user-secrets/README.md)
 - [dotnet-sql-cache](src/dotnet-sql-cache/README.md)
 - [dotnet-dev-certs](src/dotnet-dev-certs/README.md)

*\*This applies to .NET Core CLI 2.1.300-preview2 and up. For earlier versions of the CLI, these tools must be installed separately.*

*For 2.0 CLI and earlier, see <https://github.com/aspnet/DotNetTools/tree/rel/2.0.0/README.md> for details.*

*For 2.1.300-preview1 CLI, see <https://github.com/aspnet/DotNetTools/tree/2.1.0-preview1-final/README.md> for details.*

## Usage

The command line tools can be invoked as a subcommand of `dotnet`.

```sh
dotnet watch
dotnet user-secrets
dotnet sql-cache
dotnet dev-certs
```

Add `--help` to see more details. For example,

```
dotnet watch --help
```
