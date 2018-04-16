# Blazor

**An experimental .NET web framework using C#/Razor and HTML that runs in the browser via WebAssembly**

[![Gitter](https://badges.gitter.im/aspnet/Blazor.svg)](https://gitter.im/aspnet/Blazor?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge)

Blazor is a .NET web framework that runs in any browser. You author Blazor apps using C#/Razor and HTML.

Blazor uses only the latest web standards. No plugins or transpilation needed. It runs in the browser on a real .NET runtime ([Mono](http://www.mono-project.com/news/2017/08/09/hello-webassembly/)) implemented in [WebAssembly](http://webassembly.org) that executes normal .NET assemblies. It works in older browsers too by falling back to an [asm.js](http://asmjs.org/) based .NET runtime.

Blazor will have all the features of a modern web framework, including: 
- A component model for building composable UI 
- Routing 
- Layouts 
- Forms and validation 
- Dependency injection 
- JavaScript interop 
- Live reloading in the browser during development 
- Server-side rendering 
- Full .NET debugging both in browsers and in the IDE
- Rich IntelliSense and tooling
- Ability to run on older (non-WebAssembly) browsers via asm.js
- Publishing and app size trimming 

> Note: Blazor is an *experimental* project. It's not (yet) a committed product. This is to allow time to fully investigate the technical issues associated with running .NET in the browser and to ensure we can build something that developers love and can be productive with. During this experimental phase, we expect to engage deeply with early Blazor adopters like you to hear your feedback and suggestions.

To see Blazor in action, check out [Steve Sanderson's prototype demo at NDC Oslo](https://www.youtube.com/watch?v=MiLAE6HMr10&feature=youtu.be&t=31m45s) last year. You can also try out a [simple live Blazor app](https://blazor-demo.github.io/).

## Getting Started

To get setup with Blazor:

1. Install the [.NET Core 2.1 Preview 1 SDK](https://www.microsoft.com/net/download/dotnet-core/sdk-2.1.300-preview1).
1. Install the latest *preview* of [Visual Studio 2017 (15.7)](https://www.visualstudio.com/vs/preview) with the *ASP.NET and web development* workload.
   - *Note:* You can install Visual Studio previews side-by-side with an existing Visual Studio installation without impacting your existing development environment.
1. Install the [ASP.NET Core Blazor Language Services extension](https://go.microsoft.com/fwlink/?linkid=870389) from the Visual Studio Marketplace.

You're now ready to start building web apps with Blazor! To build your first Blazor web app check out our [getting started guide](https://go.microsoft.com/fwlink/?linkid=870449).

## Building the Repo

Prerequisites:
- [Node.js](https://nodejs.org/) (>8.3)

The Blazor repository uses the same set of build tools as the other ASP.NET Core projects. The [developer documention](https://github.com/aspnet/Home/wiki/Building-from-source) for building is the authoritative guide. **Please read this document and check your PATH setup if you have trouble building or using Visual Studio**

To build at the command line, run `build.cmd` or `build.sh` from the solution directory.

## Run unit tests

Run `build.cmd /t:Test` or `build.sh /t:Test`

## Run end-to-end tests

Prerequisites:
- Install [selenium-standalone](https://www.npmjs.com/package/selenium-standalone) (requires Java 8 or later)
  - `npm install -g selenium-standalone`
  - `selenium-standalone install`
- Chrome

Run `selenium-standalone start`

Run `build.cmd /t:Test /p:BlazorAllTests=true` or `build.sh /t:Test /p:BlazorAllTests=true`

## Opening in Visual Studio

Prerequisites:
- Follow the steps [here](https://github.com/aspnet/Home/wiki/Building-from-source) to set up a local copy of dotnet
- Visual Studio 2017 15.7 latest preview - [download](https://www.visualstudio.com/thank-you-downloading-visual-studio/?ch=pre&sku=Enterprise&rel=15)

We recommend getting the latest preview version of Visual Studio and updating it frequently. The Blazor
editing experience in Visual Studio depends on  new features of the Razor language tooling and
will be updated frequently.

When installing Visual Studio choose the following workloads:
- ASP.NET and Web Development
- Visual Studio extension development features

If you have problems using Visual Studio with `Blazor.sln` please refer to the [developer documention](https://github.com/aspnet/Home/wiki/Building-from-source).

## Developing the Blazor VS Tooling

To do local development of the Blazor tooling experience in VS, select the `Microsoft.VisualStudio.BlazorExtension`
project and launch the debugger.

The Blazor Visual Studio tooling will build as part of the command line build when on Windows.

## Using CI Builds of Blazor

To use a nightly or developer CI build of the Blazor package, ensure that you have the Blazor package feed configured, and update your package version numbers. You should use developer builds only with the expectation that things will break and change without any sort of announcment.

Update your projects to include the Blazor developer feed (`https://dotnet.myget.org/f/blazor-dev/api/v3/index.json`) and ASP.NET Core developer feed (`https://dotnet.myget.org/F/dotnet-core/api/v3/index.json`). You can do this in a project file with MSBuild:
```
    <RestoreSources>
      $(RestoreSources);
      https://api.nuget.org/v3/index.json;
      https://dotnet.myget.org/F/dotnet-core/api/v3/index.json;
      https://dotnet.myget.org/f/blazor-dev/api/v3/index.json;
    </RestoreSources>
```

Or in a NuGet.config in the same director as the solution file:

```
<?xml version="1.0" encoding="utf-8"?>
<configuration>
 <packageSources>
    <clear />
    <add key="blazor" value="https://dotnet.myget.org/f/blazor-dev/api/v3/index.json" />
    <add key="aspnet" value="dotnet.myget.org/F/dotnet-core/api/v3/index.json" />
    <add key="nuget" value="https://api.nuget.org/v3/index.json" />
 </packageSources>
</configuration>
```

You can browse https://dotnet.myget.org/gallery/blazor-dev to find the current versions of packages. We recommend picking a specific version of the packages and using it across your projects. 

```
  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Razor.Design" Version="2.1.0-preview2-final" PrivateAssets="all" />
    <PackageReference Include="Microsoft.AspNetCore.Blazor.Browser" Version="0.3.0-preview1-10220" />
    <PackageReference Include="Microsoft.AspNetCore.Blazor.Build" Version="0.3.0-preview1-10220" />
    <DotNetCliToolReference Include="Microsoft.AspNetCore.Blazor.Cli" Version="0.3.0-preview1-10220" />
  </ItemGroup>
```


## Contributing

There are lots of ways that you can contribute to Blazor! Read our [contributing guide](https://github.com/aspnet/Blazor/blob/dev/CONTRIBUTING.md) to learn about our development process and how to propose bug fixes and improvements.

## Still got questions?

Check out our [FAQ](https://github.com/aspnet/Blazor/wiki/FAQ) or open an [issue](https://github.com/aspnet/Blazor/issues).
