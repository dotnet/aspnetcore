# Blazor

**An experimental web UI framework using C#/Razor and HTML, running in the browser via WebAssembly**

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

We are still *very early* in this project. There isn't yet anything you can download nor any project template you can use. Most of the planned features aren't implemented yet. Even the parts that are already implemented aren't yet optimized for minimal payload size. If you're keen, you can clone the repo, build it, and run the samples and tests.

## Build

Prerequisites:
- [.NET Core SDK](https://dot.net/core) (>2.1.4)
- [Node.js](https://nodejs.org/) (>8.3)

Run `dotnet build` from the solution directory.

## Run tests

Run `dotnet test test/<dir>/<project>.Test.csproj`

## Run end-to-end tests

Prerequisites:
- Install [selenium-standalone](https://www.npmjs.com/package/selenium-standalone) (requires Java 8 or later)
  - `npm install -g selenium-standalone`
  - `selenium-standalone install`
- Chrome

Run `selenium-standalone start`

Run `dotnet test test\Microsoft.AspNetCore.Blazor.E2ETest\Microsoft.AspNetCore.Blazor.E2ETest.csproj`

## Run all tests

Install prerequisites for E2E tests

Run `dotnet test test\AllTests.proj`

## Contributing

There are lots of ways that you can contribute to Blazor! Read our [contributing guide](https://github.com/aspnet/Blazor/blob/dev/CONTRIBUTING.md) to learn about our development process and how to propose bug fixes and improvements.

## Still got questions?

Check out our [FAQ](https://github.com/aspnet/Blazor/wiki/FAQ) or open an [issue](https://github.com/aspnet/Blazor/issues).
