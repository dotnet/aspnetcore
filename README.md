dotnet-watch
===
`dotnet-watch` is a file watcher for `dotnet` that restarts the specified application when changes in the source code are detected.

### How To Install

Add `dotnet-watch` to the `tools` section of your `project.json` file:

```
{
...
  "tools": {
    "dotnet-watch": "1.0.0-*"
  }
...
}
```

### How To Use

```dotnet watch <watcher args> -- <app args>```

- `dotnet watch` (runs the application without arguments)
- `dotnet watch foo bar` (runs the application with the arguments `foo bar`)
- `dotnet watch --exit-on-change -- foo bar` (runs the application with the arguments `foo bar`. In addition, it passes `--exit-on-change` to the watcher).
- `dotnet watch --command test -- -parallel none` (runs `dotnet test` with the arguments `-parallel none`)

AppVeyor: [![AppVeyor](https://ci.appveyor.com/api/projects/status/fxhto3omtehio3aj/branch/dev?svg=true)](https://ci.appveyor.com/project/aspnetci/dnx-watch/branch/dev)

Travis:   [![Travis](https://travis-ci.org/aspnet/dotnet-watch.svg?branch=dev)](https://travis-ci.org/aspnet/dotnet-watch)


This project is part of ASP.NET 5. You can find samples, documentation and getting started instructions for ASP.NET 5 at the [Home](https://github.com/aspnet/home) repo.
