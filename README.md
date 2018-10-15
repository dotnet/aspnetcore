# Blazor

**An experimental .NET web framework using C#/Razor and HTML that runs in the browser via WebAssembly**

[![Gitter](https://badges.gitter.im/aspnet/Blazor.svg)](https://gitter.im/aspnet/Blazor?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge)

Blazor is a .NET web framework that runs in any browser. You author Blazor apps using C#/Razor and HTML.

Blazor uses only the latest web standards. No plugins or transpilation needed. It runs in the browser on a real .NET runtime ([Mono](http://www.mono-project.com/news/2017/08/09/hello-webassembly/)) implemented in [WebAssembly](http://webassembly.org) that executes normal .NET assemblies. It works in older browsers too by falling back to an [asm.js](http://asmjs.org/) based .NET runtime.

Blazor will have all the features of a modern web framework, including: 
- [A component model for building composable UI](https://blazor.net/docs/components/index.html) 
- [Routing](https://blazor.net/docs/routing.html) 
- [Layouts](https://blazor.net/docs/layouts.html) 
- Forms and validation 
- Dependency injection 
- [JavaScript interop](https://blazor.net/docs/javascript-interop.html) 
- Live reloading in the browser during development 
- Server-side rendering 
- Full .NET debugging both in browsers and in the IDE
- Rich IntelliSense and tooling
- Ability to run on older (non-WebAssembly) browsers via asm.js
- Publishing and app size trimming 

> Note: Blazor is an *experimental* project. It's not (yet) a committed product. This is to allow time to fully investigate the technical issues associated with running .NET in the browser and to ensure we can build something that developers love and can be productive with. During this experimental phase, we expect to engage deeply with early Blazor adopters like you to hear your feedback and suggestions.

To see Blazor in action, check out [Steve Sanderson's demo at NDC Minnesota](https://www.youtube.com/watch?v=JU-6pAxqAa4). You can also try out a [simple live Blazor app](https://blazor-demo.github.io/).

## Getting Started

To get started with Blazor and build your first Blazor web app check out our [getting started guide](https://go.microsoft.com/fwlink/?linkid=870449).

## Building the Repo

Prerequisites:
- [Node.js](https://nodejs.org/) (>8.3)
- Restore Git submodules by running the following command at the repo root:

      git submodule update --init --recursive

The Blazor repository uses the same set of build tools as the other ASP.NET Core projects. The [developer documentation](https://github.com/aspnet/Home/wiki/Building-from-source) for building is the authoritative guide. **Please read this document and check your PATH setup if you have trouble building or using Visual Studio.**

To build at the command line, run `build.cmd` or `build.sh` from the solution directory.

If you get a build error similar to *The project file "(some path)\blazor\modules\jsinterop\src\Mono.WebAssembly.Interop\Mono.WebAssembly.Interop.csproj" was not found.*, it's because you didn't yet restore the Git submodules. Please see *Prerequisites* above.

## Run unit tests

Run `build.cmd /t:Test` or `build.sh /t:Test`

## Run end-to-end tests

Prerequisites:
- Install [selenium-standalone](https://www.npmjs.com/package/selenium-standalone) (requires Java 8 or 9)
  - [Open JDK9](http://jdk.java.net/java-se-ri/9)
  - `npm install -g selenium-standalone`
  - `selenium-standalone install`
- Chrome

Run `selenium-standalone start`

Run `build.cmd /t:Test /p:BlazorAllTests=true` or `build.sh /t:Test /p:BlazorAllTests=true`

## Opening in Visual Studio

Prerequisites:
- Follow the steps [here](https://github.com/aspnet/Home/wiki/Building-from-source) to set up a local copy of dotnet
- Visual Studio 2017 15.7 latest preview - [download](https://www.visualstudio.com/thank-you-downloading-visual-studio/?ch=pre&sku=Enterprise&rel=15)

When installing Visual Studio choose the following workloads:
- ASP.NET and Web Development
- Visual Studio extension development features

Before attempting to open the Blazor repo in Visual Studio, restore Git submodules by running the following command at the repo root:

    git submodule update --init --recursive

If you have problems using Visual Studio with `Blazor.sln` please refer to the [developer documentation](https://github.com/aspnet/Home/wiki/Building-from-source).

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
    <PackageReference Include="Microsoft.AspNetCore.Blazor.Browser" Version="0.3.0-preview1-10220" />
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
