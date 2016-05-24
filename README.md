dotnet-watch
===
`dotnet-watch` is a file watcher for `dotnet` that restarts the specified application when changes in the source code are detected.

### How To Install

Add `Microsoft.DotNet.Watcher.Tools` to the `tools` section of your `project.json` file:

```
{
...
  "tools": {
    "Microsoft.DotNet.Watcher.Tools": {
      "version": "1.0.0-*",
      "imports": "portable-net451+win8"
    }
  }
...
}
```

### How To Use

    dotnet watch [dotnet arguments]

Add `watch` after `dotnet` in the command that you want to run:
   
| What you want to run                           | Dotnet watch command                                          |
| ---------------------------------------------- | -------------------------------------------------------- |
| dotnet run                                     | dotnet **watch** run                                     |
| dotnet run --arg1 value1                       | dotnet **watch** run --arg1 value                        |
| dotnet run --framework net451 -- --arg1 value1 | dotnet **watch** run --framework net451 -- --arg1 value1 |
| dotnet test                                    | dotnet **watch** test                                    |

AppVeyor: [![AppVeyor](https://ci.appveyor.com/api/projects/status/fxhto3omtehio3aj/branch/dev?svg=true)](https://ci.appveyor.com/project/aspnetci/dnx-watch/branch/dev)

Travis:   [![Travis](https://travis-ci.org/aspnet/dotnet-watch.svg?branch=dev)](https://travis-ci.org/aspnet/dotnet-watch)

This project is part of ASP.NET Core. You can find samples, documentation and getting started instructions for ASP.NET Core at the [Home](https://github.com/aspnet/home) repo.
