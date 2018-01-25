# Blazor

**An experimental web UI framework using C#/Razor and HTML, running in the browser via WebAssembly**

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

## Getting Started

We are still *very early* in this project. There isn’t yet anything you can download nor any project template you can use. Most of the planned features aren’t implemented yet. Even the parts that are already implemented aren’t yet optimized for minimal payload size. If you’re keen, you can clone the repo, build it, and run the tests.

## Contributing

There are lots of ways that you can contribute to Blazor! Read our [contributing guide](https://github.com/aspnet/Blazor/blob/dev/CONTRIBUTING.md) to learn about our development process and how to propose bug fixes and improvements.

## Still got questions?

Check out our [FAQ](https://github.com/aspnet/Blazor/wiki/FAQ) or open an [issue](https://github.com/aspnet/Blazor/issues).
