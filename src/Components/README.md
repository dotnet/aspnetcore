# Components

**Build modern, interactive web-based UIs with C# and Razor.**

This repo contains the underlying *components* programming model that powers both server-side [Razor Components](#razor-components) and client-side [Blazor](#blazor) applications.

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

To see Blazor in action, check out [Steve Sanderson's demo at NDC Minnesota](https://www.youtube.com/watch?v=JU-6pAxqAa4). You can also try out a [simple live Blazor app](https://blazor-demo.github.io/).

## Getting Started

To get started and build your first web app check out our [getting started guide](https://go.microsoft.com/fwlink/?linkid=870449).

## Building from source

You only need to do this if you want to contribute to this repo. You do not need to build from source if you just want to [build your own application](https://go.microsoft.com/fwlink/?linkid=870449).

For general guidance on building ASP.NET Core sources, see the [developer documentation](https://github.com/aspnet/Home/wiki/Building-from-source). **Please read this document and check your PATH setup if you have trouble building or using Visual Studio.**

### 1. Prerequisites

Ensure you have the following:

- [Node.js](https://nodejs.org/) (>10.0)


### 2. Clone

Clone this repo, and switch to its directory:

```
git clone https://github.com/aspnet/AspNetCore.git
cd AspNetCore
```

### 3. Build

Run `build.cmd` or `build.sh` from `src/Components`.

Windows users:

```
cd src\Components
build.cmd
```

Linux/Mac users:

```
cd src/Components
./build.sh
```

## Run unit tests

While inside `src/Components`, run `build.cmd /t:Test` or `build.sh /t:Test`

## Run end-to-end tests

Prerequisites:
- Install [selenium-standalone](https://www.npmjs.com/package/selenium-standalone) (requires Java 8 or 9)
  - [Open JDK9](http://jdk.java.net/java-se-ri/9)
  - `npm install -g selenium-standalone`
  - `selenium-standalone install`
- Chrome

Run `selenium-standalone start`

In a separate command prompt, run `build.cmd /t:Test /p:BlazorAllTests=true` or `build.sh /t:Test /p:BlazorAllTests=true`

## Opening in Visual Studio

Prerequisites:

- Visual Studio 2017 15.9 - [download](https://www.visualstudio.com/thank-you-downloading-visual-studio/?ch=pre&sku=Enterprise&rel=15)

When installing Visual Studio choose the following workloads:
- ASP.NET and Web Development
- Visual Studio extension development features

Now open `src/Components/Component.sln` in Visual Studio.

If you have problems using Visual Studio with `Components.sln` please refer to the [developer documentation](https://github.com/aspnet/Home/wiki/Building-from-source).

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
    <PackageReference Include="Microsoft.AspNetCore.Components.Build" Version="0.3.0-preview1-10220" PrivateAssets="all" />
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
