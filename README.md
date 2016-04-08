NodeServices
========

This project is part of ASP.NET Core. You can find samples, documentation and getting started instructions for ASP.NET Core at the [Home](https://github.com/aspnet/home) repo.

## What is this?

This repo hosts sources for the `Microsoft.AspNetCore.AngularServices` and `Microsoft.AspNetCore.ReactServices` packages, along with samples and the underlying `Microsoft.AspNetCore.NodeServices project`.

#### `Microsoft.AspNetCore.AngularServices`

This package provides facilities for developers building Angular 2 applications on ASP.NET.

Most notably, this includes **server-side prerendering**. You can build a "universal" (sometimes called "isomorphic") single-page application that renders its initial HTML on the server, and then continues execution on the client. Benefits:
 * Massively improves application delivery and startup time (often reducing from 5-10+ seconds to 100ms or so, especially on high-latency networks or low-CPU-speed clients)
 * Enables search engine crawlers to explore your SPA
 * Ensures that users don't wait for any 'loading' UI when they first hit your application.

A sample is included in this repo.

We are also working with the Angular team to add support for other client+server features such as cache priming, so that the client-side SPA code does not need to wait for an initial set of ajax requests to complete - the necessary data can be bundled with the initial page. Another possible future feature would be helpers to emit a JSON representation of C# class model metadata, so some validation rules can transparently apply both on the server and the client.

#### `Microsoft.AspNetCore.ReactServices`

This package provides similar facilities for React applications on ASP.NET.

This includes **server-side prerendering** to support "universal" (sometimes called "isomorphic") single-page applications as described above. A sample is included in this repo.

We are open to adding other client+server features that will make React developers more productive on ASP.NET. Please let us know if you have specific feature proposals.

#### *Your favourite JavaScript framework goes here*

Although we have finite resources and are currently focused on adding Angular 2 and React support, the architecture here is designed so that you can build your own server-side support for other client-side libraries and frameworks.

The underlying `Microsoft.AspNetCore.NodeServices` package is a general-purpose way for ASP.NET applications (or .NET applications more generally) to interoperate with code running inside Node.js. That's how `AngularServices`/`ReactServices` server-side rendering works - those packages transparently spin up Node.js instances that can perform the server-side rendering. Any code that runs inside Node can efficiently be invoked from .NET via this package, which takes care of starting and stopping Node instances and manages the communication between .NET and Node.

## Using AngularServices/ReactServices in your own projects

Currently it's a little early for production use in real projects, because for example AngularServices uses [angular-universal](https://github.com/angular/universal), which is still in prerelease. Some important features are either not implemented or are not yet stable. The React support is more solid.

If you're a keen early-adopter type, you can infer usage from the samples. Let us know how you get on :)

## Trying the samples

To get started,

1. Ensure you have [installed the latest stable version of ASP.NET Core](https://www.asp.net/vnext). Instructions are available for [Windows](http://docs.asp.net/en/latest/getting-started/installing-on-windows.html), [Mac](http://docs.asp.net/en/latest/getting-started/installing-on-mac.html), and [Linux](http://docs.asp.net/en/latest/getting-started/installing-on-linux.html).
2. Ensure you have [installed a recent version of Node.js](https://nodejs.org/en/). To check this works, open a console prompt, and type `node -v`. It should print a version number.
3. Ensure you have installed `gulp` globally. You can check if it's there by running `gulp -v`. If you need to install it:

   ```
   npm install -g gulp
   ```

3. Clone this repository:

   ```
   git clone https://github.com/aspnet/NodeServices.git
   ```

**Using Visual Studio on Windows**

1. Open the solution file, `NodeServices.sln`, in Visual Studio.
2. Wait for it to finish fetching and installing dependencies.
3. If you get the error `'reactivex/rxjs' is not in the npm registry`, then your Visual Studio installation's version of the NPM tool is out of date. You will need to restore NPM dependencies manually from a command prompt (e.g., `cd samples\angular\MusicStore` then `npm install`).
4. Select a sample and run it. For example, right-click on the `MusicStore` project in Solution Explorer and choose `Set as startup project`. Then press `Ctrl+F5` to launch it.

Note that to run the React example, you'll also need to run `webpack` from the `samples\react\ReactGrid` directory (having first installed webpack if you don't yet have it - `npm install -g webpack`).

**Using dnx on Windows/Mac/Linux**

1. Ensure you are using a suitable .NET runtime. Currently, this project is tested with version `1.0.0-rc1-final` on `coreclr`:

   ```
   dnvm use 1.0.0-rc1-final -r coreclr
   ```

2. In the solution root directory (`NodeServices` - i.e., the directory that contains `NodeServices.sln`), restore the .NET dependencies:


   ```
   cd NodeServices
   dnu restore
   ```

3. Change directory to whichever sample you want to run, then restore the Node dependencies. For example:

   ```
   cd samples/angular/MusicStore/
   npm install
   ```

4. Where applicable, build the project. For example, the Angular example uses Gulp, so you'll need to execute `gulp`, whereas the React example uses Webpack, so you'll need to execute `webpack`. The ES2015 example does not need to be built.

   If you don't already have it, install the applicable build tool first (e.g., `npm install -g webpack`).

5. Run the project (and wait until it displays the message `Application started`)

  ```
  dnx web
  ```

6. Browse to [`http://localhost:5000/`](http://localhost:5000/)

