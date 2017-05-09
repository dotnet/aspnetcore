# JavaScriptServices

AppVeyor: [![AppVeyor](https://ci.appveyor.com/api/projects/status/gprilrckx116vc9m/branch/dev?svg=true)](https://ci.appveyor.com/project/aspnetci/javascriptservices/branch/dev)

This project is part of ASP.NET Core. You can find samples, documentation and getting started instructions for ASP.NET Core at the [Home](https://github.com/aspnet/home) repo.

## What is this?

`JavaScriptServices` is a set of client-side technologies for ASP.NET Core. It provides infrastructure that you'll find useful if you:

-  Use Angular 2 / React / Vue / Aurelia / Knockout / etc.
-  Build your client-side resources using Webpack.
-  Execute JavaScript on the server at runtime.

Read [Building Single Page Applications on ASP.NET Core with JavaScriptServices](https://blogs.msdn.microsoft.com/webdev/2017/02/14/building-single-page-applications-on-asp-net-core-with-javascriptservices/) for more details.

This repo contains:

 * A set of NuGet/NPM packages that implement functionality for:
   * Invoking arbitrary NPM packages at runtime from .NET code ([docs](https://github.com/aspnet/JavaScriptServices/tree/dev/src/Microsoft.AspNetCore.NodeServices#simple-usage-example))
   * Server-side prerendering of SPA components ([docs](https://github.com/aspnet/JavaScriptServices/tree/dev/src/Microsoft.AspNetCore.SpaServices#server-side-prerendering))
   * Webpack dev middleware ([docs](https://github.com/aspnet/JavaScriptServices/tree/dev/src/Microsoft.AspNetCore.SpaServices#webpack-dev-middleware))
   * Hot module replacement (HMR) ([docs](https://github.com/aspnet/JavaScriptServices/tree/dev/src/Microsoft.AspNetCore.SpaServices#webpack-hot-module-replacement))
   * Server-side and client-side routing integration ([docs](https://github.com/aspnet/JavaScriptServices/tree/dev/src/Microsoft.AspNetCore.SpaServices#routing-helper-mapspafallbackroute))
   * Server-side and client-side validation integration
   * "Lazy loading" for Knockout apps
 * A Yeoman generator that creates preconfigured app starting points ([guide](http://blog.stevensanderson.com/2016/05/02/angular2-react-knockout-apps-on-aspnet-core/))
 * Samples and docs

It's cross-platform (Windows, Linux, or macOS) and works with .NET Core 1.0.1 or later.

## Creating new applications

If you want to build a brand-new ASP.NET Core app that uses Angular 2 / React / Knockout on the client, consider starting with the `aspnetcore-spa` generator. This lets you choose your client-side framework. It generates a starting point that includes applicable features such as Webpack dev middleware, server-side prerendering, and efficient production builds. It's much easier than configuring everything to work together manually!

To do this, install Yeoman and these generator templates:

    npm install -g yo generator-aspnetcore-spa

Generate your new application starting point:

    cd some-empty-directory
    yo aspnetcore-spa

Once the generator has run and restored all the dependencies, you can start up your new ASP.NET Core SPA:

    dotnet run 

For a more detailed walkthrough, see [getting started with the `aspnetcore-spa` generator](http://blog.stevensanderson.com/2016/05/02/angular2-react-knockout-apps-on-aspnet-core/).

## Adding to existing applications

If you have an existing ASP.NET Core application, or if you just want to use the underlying JavaScriptServices packages directly, you can install these packages using NuGet and NPM:

 * `Microsoft.AspNetCore.NodeServices`
   * This provides a fast and robust way for .NET code to run JavaScript on the server inside a Node.js environment. You can use this to consume arbitrary functionality from NPM packages at runtime in your ASP.NET Core app.
   * Most applications developers don't need to use this directly, but you can do so if you want to implement your own functionality that involves calling Node.js code from .NET at runtime.
   * Find [documentation and usage examples here](https://github.com/aspnet/JavaScriptServices/tree/dev/src/Microsoft.AspNetCore.NodeServices#microsoftaspnetcorenodeservices).
 * `Microsoft.AspNetCore.SpaServices`
   * This provides infrastructure that's generally useful when building Single Page Applications (SPAs) with technologies such as Angular 2 or React (for example, server-side prerendering and webpack middleware). Internally, it uses the `NodeServices` package to implement its features.
   * Find [documentation and usage examples here](https://github.com/aspnet/JavaScriptServices/tree/dev/src/Microsoft.AspNetCore.SpaServices#microsoftaspnetcorespaservices).
 * `Microsoft.AspNetCore.AngularServices`
   * This builds on the `SpaServices` package and includes features specific to Angular 2. Currently, this includes validation helpers.
   * The code is [here](https://github.com/aspnet/JavaScriptServices/tree/dev/src/Microsoft.AspNetCore.AngularServices). You'll find a usage example for [the validation helper here](https://github.com/aspnet/JavaScriptServices/blob/dev/samples/angular/MusicStore/wwwroot/ng-app/components/admin/album-edit/album-edit.ts).

There was previously a `Microsoft.AspNetCore.ReactServices` but this is not currently needed - all applicable functionality is in `Microsoft.AspNetCore.SpaServices`, because it's sufficiently general. We might add a new `Microsoft.AspNetCore.ReactServices` package in the future if new React-specific requirements emerge.

If you want to build a helper library for some other SPA framework, you can do so by taking a dependency on `Microsoft.AspNetCore.SpaServices` and wrapping its functionality in whatever way is most useful for your SPA framework.

## Samples and templates

Inside this repo, [the `templates` directory](https://github.com/aspnet/JavaScriptServices/tree/dev/templates) contains the application starting points that the `aspnetcore-spa` generator emits. You can clone this repo and run those applications directly. But it's easier to [use the Yeoman tool to run the generator](http://blog.stevensanderson.com/2016/05/02/angular2-react-knockout-apps-on-aspnet-core/).

The [`samples` directory](https://github.com/aspnet/JavaScriptServices/tree/dev/samples) contains examples of:

- Using the JavaScript services family of packages with Angular 2 and React.
- A standalone `NodeServices` usage for runtime code transpilation and image processing.

**To run the samples:**

 * Clone this repo
 * At the repo's root directory (the one containing `src`, `samples`, etc.), run `dotnet restore`
 * Change directory to the sample you want to run (for example, `cd samples/angular/MusicStore`)
 * Restore Node dependencies by running `npm install`
   * If you're trying to run the Angular 2 "Music Store" sample, then also run `gulp` (which you need to have installed globally). None of the other samples require this.
 * Run the application (`dotnet run`)
 * Browse to [http://localhost:5000](http://localhost:5000)

## Contributing

If you're interested in contributing to the various packages, samples, and project templates in this repo, that's great! You can run the code in this repo just by:

 * Cloning the repo
 * Running `dotnet restore` at the repo root dir
 * Going to whatever sample or template you want to run (for example, `cd templates/Angular2Spa`)
 * Restoring NPM dependencies (run `npm install`)
 * Launching it (`dotnet run`)

If you're planning to submit a pull request, and if it's more than a trivial fix (for example, for a typo), it's usually a good idea first to file an issue describing what you're proposing to do and how it will work. Then you can find out if it's likely that such a pull request will be accepted, and how it fits into wider ongoing plans.
