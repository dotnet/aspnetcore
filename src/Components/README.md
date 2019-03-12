# Components

**Build modern, interactive web-based UIs with C# and Razor.**

This folder contains the underlying *components* programming model that powers both server-side [Razor Components](#razor-components) and client-side [Blazor](#blazor) applications.

Features of the components programming model include:

- [A powerful model for building composable UI](https://blazor.net/docs/components/index.html)
- [Routing](https://blazor.net/docs/routing.html)
- [Layouts](https://blazor.net/docs/layouts.html)
- Forms and validation
- [Dependency injection](https://blazor.net/docs/dependency-injection.html)
- [JavaScript interop](https://blazor.net/docs/javascript-interop.html)
- Live reloading in the browser during development
- Server-side rendering
- Full .NET debugging
- Rich IntelliSense and tooling

## Razor Components

Razor Components is a built-in feature of ASP.NET Core 3.0. It provides a convenient and powerful way to create sophisticated UIs that update in real-time, while keeping all your code in .NET running on the server.

## Blazor

Blazor is a project to run Razor Components truly client-side, in any browser, on WebAssembly. It's a full single-page application (SPA) framework inspired by the latest JavaScript SPA frameworks, featuring support for offline/PWA applications, app size trimming, and browser-based debugging.

Blazor uses only the latest web standards. No plugins or transpilation needed. It runs in the browser on a real .NET runtime ([Mono](http://www.mono-project.com/news/2017/08/09/hello-webassembly/)) implemented in [WebAssembly](http://webassembly.org) that executes normal .NET assemblies.

[![Gitter](https://badges.gitter.im/aspnet/Blazor.svg)](https://gitter.im/aspnet/Blazor?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge)

> Note: client-side Blazor is an *experimental* project. It's not yet a committed product . This is to allow time to fully investigate the technical issues associated with running .NET in the browser and to ensure we can build something that developers love and can be productive with. During this experimental phase, we expect to engage deeply with early Blazor adopters like you to hear your feedback and suggestions.

To see Blazor in action, check out [Steve Sanderson's demo at NDC London](https://www.youtube.com/watch?v=Qe8UW5543-s). You can also try out a [simple live Blazor app](https://blazor-demo.github.io/).

## Getting Started

To get started and build your first web app check out our [getting started guide](https://go.microsoft.com/fwlink/?linkid=870449).

## Building from source

See [these instructions](/docs/BuildFromSource.md) for more details on how to build this project on your own.

## Developing the Blazor VS Tooling

To do local development of the Blazor tooling experience in VS, select the `Microsoft.VisualStudio.BlazorExtension`
project and launch the debugger.

The Blazor Visual Studio tooling will build as part of the command line build when on Windows.

## Using CI Builds of Blazor

To use a nightly or developer CI build of the Blazor package, ensure that you have the Blazor package feed configured, and update your package version numbers. You should use developer builds only with the expectation that things will break and change without any sort of announcement.

Update your projects to include the Blazor development feed (`https://dotnet.myget.org/f/blazor-dev/api/v3/index.json`). You can do this in a project file with MSBuild:

```xml
<RestoreAdditionalProjectSources>
    https://dotnet.myget.org/f/blazor-dev/api/v3/index.json;
</RestoreAdditionalProjectSources>
```

Or in a NuGet.config in the same directory as the solution file:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
 <packageSources>
    <clear />
    <add key="blazor" value="https://dotnet.myget.org/f/blazor-dev/api/v3/index.json" />
    <add key="nuget" value="https://api.nuget.org/v3/index.json" />
 </packageSources>
</configuration>
```

You can browse https://dotnet.myget.org/gallery/blazor-dev to find the current versions of packages. We recommend picking a specific version of the packages and using it across your projects.

```xml
<ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Blazor" Version="0.3.0-preview1-10220" />
    <PackageReference Include="Microsoft.AspNetCore.Blazor.Build" Version="0.3.0-preview1-10220" PrivateAssets="all" />
    <DotNetCliToolReference Include="Microsoft.AspNetCore.Blazor.Cli" Version="0.3.0-preview1-10220" />
</ItemGroup>
```

To install a developer CI build of the Blazor Language Service extension for Visual Studio, add https://dotnet.myget.org/F/blazor-dev/vsix/ as an additional extension gallery by going to Tools -> Options -> Environment -> Extensions and Updates:

![image](https://user-images.githubusercontent.com/1874516/39077607-2729edb2-44b8-11e8-8798-701ba632fdd4.png)

You should then be able to install or update the Blazor Language Service extension from the developer CI feed using the Extensions and Updates dialog.

## Contributing

There are lots of ways that you can contribute to Blazor! Read our [contributing guide](https://github.com/aspnet/Blazor/blob/master/CONTRIBUTING.md) to learn about our development process and how to propose bug fixes and improvements.

## Still got questions?

Check out our [FAQ](https://github.com/aspnet/Blazor/wiki/FAQ) or open an [issue](https://github.com/aspnet/Blazor/issues).
