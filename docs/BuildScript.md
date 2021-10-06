# A Guide to the Build Script

This ASP.NET Core repo contains a top-level build script located at `eng/build.cmd` and `eng/build.sh` and local build scripts within each directory. The scripts can be used to restore, build, and test the repo with support for a variety of flags. This page documents the common flags and some recommended invocation patterns.

It is _not_ recommended to run the top-level build script for the repo. You'll rarely need to build the entire repo and building a particular sub-project is usually sufficient for your workflow.

## Common Arguments

Common arguments that can be invoked on the `build.cmd` or `build.sh` scripts include:

| Property           | Description                                                  |
| ------------------ | ------------------------------------------------------------ |
| Configuration      | `Debug` or `Release`. Default = `Debug`.                     |
| TargetArchitecture | The CPU architecture to build for (x64, x86, arm, arm64).    |
| TargetOsName       | The base runtime identifier to build for (win, linux, osx, linux-musl). |

## Common Invocations

| Command                                                      | What does it do?                                             |
| ------------------------------------------------------------ | ------------------------------------------------------------ |
| `.\eng\build.cmd -all -pack -arch x64`                       | Build development packages for all the shipping projects in the repo. |
| `.\eng\build.cmd -test -projects .\src\Framework\test\Microsoft.AspNetCore.App.UnitTests.csproj` | Run all the unit tests in the `Microsoft.AspNetCore.App.UnitTests` project. |
| `.\build.cmd -Configuration Release`                         | Build projects in a subdirectory using a `Release` configuration. |
| `.\eng\build.cmd -noBuildNative -noBuildManage`              | Builds the repo and skips native and managed projects, a quicker alternative to `./restore.cmd` |
| `.\eng\build.cmd -buildInstallers`                           | Builds Windows installers for the ASP.NET Core runtime.      |

