DotNetTools
===========

[![Travis build status](https://img.shields.io/travis/aspnet/DotNetTools.svg?label=travis-ci&branch=dev&style=flat-square)](https://travis-ci.org/aspnet/DotNetTools/branches)
[![AppVeyor build status](https://img.shields.io/appveyor/ci/aspnetci/DotNetTools/dev.svg?label=appveyor&style=flat-square)](https://ci.appveyor.com/project/aspnetci/DotNetTools/branch/dev)

This project is part of ASP.NET Core. You can find samples, documentation and getting started instructions for ASP.NET Core at <https://docs.asp.net>.

## Projects

The repository contains command-line tools for the .NET Core CLI. Follow the links below for more details on each tool.

 - [dotnet-watch](src/dotnet-watch/)
 - [dotnet-user-secrets](src/dotnet-user-secrets/)
 - [dotnet-sql-cache](src/dotnet-sql-cache/) (dotnet-sql-cache)
 - [dotnet-dev-certs](src/dotnet-dev-certs/) (dotnet-dev-certs)

## How to Install

Install tools using the .NET Core command-line.

```
dotnet install tool dotnet-watch
dotnet install tool dotnet-user-secrets
dotnet install tool dotnet-dev-certs
dotnet install tool dotnet-sql-cache

```

## Usage

The command line tools can be invoked as a new verb hanging off `dotnet`.

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
