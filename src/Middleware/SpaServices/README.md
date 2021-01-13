# Microsoft.AspNetCore.SpaServices

If you're building an ASP.NET Core application, and want to use Angular, React, Knockout, or another single-page app (SPA) framework, this NuGet package contains useful infrastructure for you.

This package enables:

 * [**Server-side prerendering**](#server-side-prerendering) for *universal* (a.k.a. *isomorphic*) applications, where your Angular / React / etc. components are first rendered on the server, and then transferred to the client where execution continues
 * [**Webpack middleware**](#webpack-dev-middleware) so that, during development, any webpack-built resources will be generated on demand, without you having to run webpack manually or compile files to disk
 * [**Hot module replacement**](#webpack-hot-module-replacement) so that, during development, your code and markup changes will be pushed to your browser and updated in the running application automatically, without even needing to reload the page
 * [**Routing helpers**](#routing-helper-mapspafallbackroute) for integrating server-side routing with client-side routing

Behind the scenes, it uses the [`Microsoft.AspNetCore.NodeServices`](src/Middleware/NodeServices) package as a fast and robust way to invoke Node.js-hosted code from ASP.NET Core at runtime.

### Requirements

* [Node.js](https://nodejs.org/en/)
  * To test this is installed and can be found, run `node -v` on a command line
  * Note: If you're deploying to an Azure web site, you don't need to do anything here - Node is already installed and available in the server environments
* [.NET Core](https://dot.net), version 1.0 RC2 or later

### Installation into existing projects

 * Install the `Microsoft.AspNetCore.SpaServices` NuGet package
 * Run `dotnet restore` (or if you use Visual Studio, just wait a moment - it will restore dependencies automatically)
 * Install supporting NPM packages for the features you'll be using:
   * For **server-side prerendering**, install `aspnet-prerendering`
   * For **server-side prerendering with Webpack build support**, also install `aspnet-webpack`
   * For **webpack dev middleware**, install `aspnet-webpack`
   * For **webpack dev middleware with hot module replacement**, also install `webpack-hot-middleware`
   * For **webpack dev middleware with React hot module replacement**, also install `aspnet-webpack-react`

   For example, run `npm install --save aspnet-prerendering aspnet-webpack` to install `aspnet-prerendering` and `aspnet-webpack`.


### Creating entirely new projects

If you're starting from scratch, you might prefer to use the `aspnetcore-spa` Yeoman generator to get a ready-to-go starting point using your choice of client-side framework. This includes `Microsoft.AspNetCore.SpaServices` along with everything configured for webpack middleware, server-side prerendering, etc.

See: [Getting started with the aspnetcore-spa generator](http://blog.stevensanderson.com/2016/05/02/angular2-react-knockout-apps-on-aspnet-core/)

Also, if you want to debug projects created with the aspnetcore-spa generator, see [Debugging your projects](#debugging-your-projects)

## Server-side prerendering

The `SpaServices` package isn't tied to any particular client-side framework, and it doesn't force you to set up your client-side application in any one particular style. So, `SpaServices` doesn't contain hard-coded logic for rendering Angular / React / etc. components.

Instead, what `SpaServices` offers is ASP.NET Core APIs that know how to invoke a JavaScript function that you supply, passing through context information that you'll need for server-side prerendering, and then injects the resulting HTML string into your rendered page. In this document, you'll find examples of setting this up to render Angular and React components.

### 1. Enable the asp-prerender-* tag helpers

Make sure you've installed into your project:

 * The `Microsoft.AspNetCore.SpaServices` NuGet package, version 1.1.0-* or later
 * The `aspnet-prerendering` NPM package, version 2.0.1 or later

Together these contain the server-side and client-side library code you'll need. Now go to your `Views/_ViewImports.cshtml` file, and add the following line:

    @addTagHelper "*, Microsoft.AspNetCore.SpaServices"

### 2. Use asp-prerender-* in a view

Choose a place in one of your MVC views where you want to prerender a SPA component. For example, open `Views/Home/Index.cshtml`, and add markup like the following:

    <div id="my-spa" asp-prerender-module="ClientApp/boot-server"></div>

If you run your application now, and browse to whatever page renders the view you just edited, you should get an error similar to the following (assuming you're running in *Development* mode so you can see the error information): *Error: Cannot find module 'some/directory/ClientApp/boot-server'*. You've told the prerendering tag helper to execute code from a JavaScript module called `boot-server`, but haven't yet supplied any such module!

### 3. Supplying JavaScript code to perform prerendering

Create a JavaScript file at the path matching the `asp-prerender-module` value you specified above. In this example, that means creating a folder called `ClientApp` at the root of your project, and creating a file inside it called `boot-server.js`. Try putting the following into it:

```javascript
var prerendering = require('aspnet-prerendering');

module.exports = prerendering.createServerRenderer(function(params) {
    return new Promise(function (resolve, reject) {
        var result = '<h1>Hello world!</h1>'
            + '<p>Current time in Node is: ' + new Date() + '</p>'
            + '<p>Request path is: ' + params.location.path + '</p>'
            + '<p>Absolute URL is: ' + params.absoluteUrl + '</p>';

        resolve({ html: result });
    });
});
```

If you try running your app now, you should see the HTML snippet generated by your JavaScript getting injected into your page.

As you can see, your JavaScript code receives context information (such as the URL being requested), and returns a `Promise` so that it can asynchronously supply the markup to be injected into the page. You can put whatever logic you like here, but typically you'll want to execute a component from your Angular / React / etc. application.

**Passing data from .NET code into JavaScript code**

If you want to supply additional data to the JavaScript function that performs your prerendering, you can use the `asp-prerender-data` attribute. You can give any value as long as it's JSON-serializable. Bear in mind that it will be serialized and sent as part of the remote procedure call (RPC) to Node.js, so avoid trying to pass massive amounts of data.

For example, in your `cshtml`,

    <div id="my-spa" asp-prerender-module="ClientApp/boot-server"
                     asp-prerender-data="new {
                        IsGoldUser = true,
                        Cookies = ViewContext.HttpContext.Request.Cookies
                     }"></div>

Now in your JavaScript prerendering function, you can access this data by reading `params.data`, e.g.:

```javascript
var prerendering = require('aspnet-prerendering');

module.exports = prerendering.createServerRenderer(function(params) {
    return new Promise(function (resolve, reject) {
        var result = '<h1>Hello world!</h1>'
            + '<p>Is gold user: ' + params.data.isGoldUser + '</p>'
            + '<p>Number of cookies: ' + params.data.cookies.length + '</p>';

        resolve({ html: result });
    });
});
```

Notice that the property names are received in JavaScript-style casing (e.g., `isGoldUser`) even though they were sent in C#-style casing (e.g., `IsGoldUser`). This is because of how the JSON serialization is configured by default.

**Passing data from server-side to client-side code**

If, as well as returning HTML, you also want to pass some contextual data from your server-side code to your client-side code, you can supply a `globals` object alongside the initial `html`, e.g.:

```javascript
resolve({
    html: result,
    globals: {
        albumsList: someDataHere,
        userData: someMoreDataHere
    }
});
```

When the `aspnet-prerender-*` tag helper emits this result into the document, as well as injecting the `html` string, it will also emit code that populates `window.albumsList` and `window.userData` with JSON-serialized copies of the objects you passed.

This can be useful if, for example, you want to avoid loading the same data twice (once on the server and once on the client).

### 4. Enabling webpack build tooling

Of course, rather than writing your `boot-server` module and your entire SPA in plain ES5 JavaScript, it's quite likely that you'll want to write your client-side code in TypeScript or at least ES2015 code. To enable this, you need to set up a build system.

#### Example: Configuring Webpack to build TypeScript

Let's say you want to write your boot module and SPA code in TypeScript, and build it using Webpack. First ensure that `webpack` is installed, along with the libraries needed for TypeScript compilation:

    npm install -g webpack
    npm install --save ts-loader typescript

Next, create a file `webpack.config.js` at the root of your project, containing:

```javascript
var path = require('path');

module.exports = {
    entry: { 'main-server': './ClientApp/boot-server.ts' },
    resolve: { extensions: [ '', '.js', '.ts' ] },
    output: {
        path: path.join(__dirname, './ClientApp/dist'),
        filename: '[name].js',
        libraryTarget: 'commonjs'
    },
    module: {
        loaders: [
            { test: /\.ts$/, loader: 'ts-loader' }
        ]
    },
    target: 'node',
    devtool: 'inline-source-map'
};
```

This tells webpack that it should compile `.ts` files using TypeScript, and that when looking for modules by name (e.g., `boot-server`), it should also find files with `.js` and `.ts` extensions.

If you don't already have a `tsconfig.json` file at the root of your project, add one now. Make sure your `tsconfig.json` includes `"es6"` in its `"lib"` array so that TypeScript knows about intrinsics such as `Promise`. Here's an example `tsconfig.json`:

```json
{
  "compilerOptions": {
    "moduleResolution": "node",
    "target": "es5",
    "sourceMap": true,
    "lib": [ "es6", "dom" ]
  },
  "exclude": [ "bin", "node_modules" ]
}
```

Now you can delete `ClientApp/boot-server.js`, and in its place, create `ClientApp/boot-server.ts`, containing the TypeScript equivalent of what you had before:

```javascript
import { createServerRenderer } from 'aspnet-prerendering';

export default createServerRenderer(params => {
    return new Promise((resolve, reject) => {
        const html = `
            <h1>Hello world!</h1>
            <p>Current time in Node is: ${ new Date() }</p>
            <p>Request path is: ${ params.location.path }</p>
            <p>Absolute URL is: ${ params.absoluteUrl }</p>`;

        resolve({ html });
    });
});
```

Finally, run `webpack` on the command line to build `ClientApp/dist/main-server.js`. Then you can tell `SpaServices` to use that file for server-side prerendering. In your MVC view where you use `aspnet-prerender-module`, update the attribute value:

    <div id="my-spa" asp-prerender-module="ClientApp/dist/main-server"></div>

Webpack is a broad and powerful tool and can do far more than just invoke the TypeScript compiler. To learn more, see the [webpack website](https://webpack.github.io/).


### 5(a). Prerendering Angular components

If you're building an Angular application, you can run your components on the server inside your `boot-server.ts` file so they will be injected into the resulting web page.

First install the NPM package `angular2-universal` - this contains infrastructure for executing Angular components inside Node.js:

```
npm install --save angular2-universal
```

Now you can use the [`angular2-universal` APIs](https://github.com/angular/universal) from your `boot-server.ts` TypeScript module to execute your Angular component on the server. The code needed for this is fairly complex, but that's unavoidable because Angular supports so many different ways of being configured, and you need to provide wiring for whatever combination of DI modules you're using.

The easiest way to get started with Angular server-side rendering on ASP.NET Core is to use the [aspnetcore-spa generator](http://blog.stevensanderson.com/2016/05/02/angular2-react-knockout-apps-on-aspnet-core/), which creates a ready-made working starting point.

### 5(b). Prerendering React components

React components can be executed synchronously on the server quite easily, although asynchronous execution is tricker as described below.

#### Setting up client-side React code

Let's say you want to write a React component in ES2015 code. You might install the NPM modules `react react-dom babel-loader babel-preset-react babel-preset-es2015`, and then prepare Webpack to build `.jsx` files by creating `webpack.config.js` in your project root, containing:

```javascript
var path = require('path');

module.exports = {
    resolve: { extensions: [ '', '.js', '.jsx' ] },
    module: {
        loaders: [
            { test: /\.jsx?$/, loader: 'babel-loader' }
        ]
    },
    entry: {
        main: ['./ClientApp/react-app.jsx'],
    },
    output: {
        path: path.join(__dirname, 'wwwroot', 'dist'),
        filename: '[name].js'
    },
};
```

You will also need a `.babelrc` file in your project root, containing:

```javascript
{
    "presets": ["es2015", "react"]
}
```

This is enough to be able to build ES2015 `.jsx` files via Webpack. Now you could implement a simple React component, for example the following at `ClientApp/react-app.jsx`:

```javascript
import * as React from 'react';

export class HelloMessage extends React.Component
{
    render() {
        return <h1>Hello {this.props.message}!</h1>;
    }
}
```

... and the following code to run it in a browser at `ClientApp/boot-client.jsx`:

```javascript
import * as React from 'react';
import * as ReactDOM from 'react-dom';
import { HelloMessage } from './react-app';

ReactDOM.render(<HelloMessage message="World" />, document.getElementById('my-spa'));
```

At this stage, run `webpack` on the command line to build `wwwroot/dist/main.js`. Or, to avoid having to do this manually, you could use the `SpaServices` package to [enable Webpack dev middleware](#webpack-dev-middleware).

You can now run your React code on the client by adding the following to one of your MVC views:

    <div id="my-spa"></div>
    <script src="/dist/main.js"></script>

If you want to enable server-side prerendering too, follow the same process as described under [server-side prerendering](#server-side-prerendering).

#### Realistic React apps and Redux

The above example is extremely simple - it doesn't use `react-router`, and it doesn't load any data asynchronously. Real applications are likely to do both of these.

Supporting asynchronous data loading involves more considerations. Unlike Angular applications that run asynchronously on the server and freely overwrite server-generated markup with client-generated markup, React strictly wants to run synchronously on the server and always produce the same markup on the server as it does on the client.

To make this work, you most likely need some way to know in advance what data your React components will need to use, load it separately from those components, and have some way of transferring information about the loaded data from server to client. If you try to implement this in a generalized way, you'll end up reinventing something like the Flux/Redux pattern.

To avoid inventing your own incomplete version of Flux/Redux, you probably should just use [Redux](https://github.com/reactjs/redux). This is at first a very unfamiliar and tricky-looking abstraction, but does solve all the problems around server-side execution of React apps. To get a working starting point for an ASP.NET Core site with React+Redux on the client (and server-side prerendering), see the [aspnetcore-spa generator](http://blog.stevensanderson.com/2016/05/02/angular2-react-knockout-apps-on-aspnet-core/).

## Webpack dev middleware

If you're using webpack, the webpack dev middleware feature included in `Microsoft.AspNetCore.SpaServices` will streamline your development process. It intercepts requests that would match files built by webpack, and dynamically builds those files on demand. They don't need to be written to disk - they are just held in memory and served directly to the browser.

Benefits:

 * You don't have to run `webpack` manually or set up any file watchers
 * The browser is always guaranteed to receive up-to-date built output
 * The built artifacts are normally served instantly or at least extremely quickly, because internally, an instance of `webpack` stays active and has partial compilation states pre-cached in memory

It lets you work as if the browser natively understands whatever file types you are working with (e.g., TypeScript, SASS), because it's as if there's no build process to wait for.

### Example: A simple Webpack setup that builds TypeScript

**Note:** If you already have Webpack in your project, then you can skip this section.

As a simple example, here's how you can set up Webpack to build TypeScript files. First install the relevant NPM packages by executing this from the root directory of your project:

```
npm install --save typescript ts-loader
```

And if you don't already have it, you'll find it useful to install the `webpack` command-line tool:

```
npm install -g webpack
```

Now add a Webpack configuration file. Create `webpack.config.js` in the root of your project, containing the following:

```javascript
module.exports = {
    resolve: {
        // For modules referenced with no filename extension, Webpack will consider these extensions
        extensions: [ '', '.js', '.ts' ]
    },
    module: {
        loaders: [
            // This example only configures Webpack to load .ts files. You can also drop in loaders
            // for other file types, e.g., .coffee, .sass, .jsx, ...
            { test: /\.ts$/, loader: 'ts-loader' }
        ]
    },
    entry: {
        // The loader will follow all chains of reference from this entry point...
        main: ['./ClientApp/MyApp.ts']
    },
    output: {
        // ... and emit the built result in this location
        path: __dirname + '/wwwroot/dist',
        filename: '[name].js'
    },
};
```

Now you can put some TypeScript code (minimally, just `console.log('Hello');`) at `ClientApp/MyApp.ts` and then run `webpack` from the command line to build it (and everything it references). The output will be placed in `wwwroot/dist`, so you can load and run it in a browser by adding the following to one of your views (e.g., `Views\Home\Index.cshtml`):

    <script src="/dist/main.js"></script>

The Webpack loader, `ts-loader`, follows all chains of reference from `MyApp.ts` and will compile all referenced TypeScript code into your output. If you want, you can create a [`tsconfig.json` file](https://www.typescriptlang.org/docs/handbook/tsconfig-json.html) to control things like whether source maps will be included in the output. If you add other Webpack loaders to your `webpack.config.js`, you can even reference things like SASS from your TypeScript, and then it will get built to CSS and loaded automatically.

So that's enough to build TypeScript. Here's where webpack dev middleware comes in to auto-build your code whenever needed (so you don't need any file watchers or to run `webpack` manually), and optionally hot module replacement (HMR) to push your changes automatically from code editor to browser without even reloading the page.

### Example: A simple Webpack setup that builds LESS

Following on from the preceding example that builds TypeScript, you could extend your Webpack configuration further to support building LESS. There are three major approaches to doing this:

1. **If using Angular, use its native style loader to attach the styles to components**. This is extremely simple and is usually the right choice if you are using Angular. However it only applies to Angular components, not to any other part of the host page, so sometimes you might want to combine this technique with options 2 or 3 below.

2. **Or, use Webpack's style loader to attach the styles at runtime**. The CSS markup will be included in your JavaScript bundles and will be attached to the document dynamically. This has certain benefits during development but isn't recommended in production.

3. **Or, have each build write a standalone `.css` file to disk**. At runtime, load it using a regular `<link rel='stylesheet'>` tag. This is likely to be the approach you'll want for production use (at least for non-Angular applications, such as React applications) as it's the most robust and best-performing option.

If instead of LESS you prefer SASS or another CSS preprocessor, the exact same techniques should work, but of course you'll need to replace the `less-loader` with an equivalent Webpack loader for SASS or your chosen preprocessor.

#### Approach 1: Scoping styles to Angular components

If you are using Angular, this is the easiest way to perform styling. It works with both server and client rendering, supports Hot Module Replacement, and robustly scopes styles to particular components (and optionally, their descendant elements).

This repository's Angular template uses this technique to scope styles to components out of the box. It defines those styles as `.css` files. For example, its components reference `.css` files like this:

```javascript
@Component({
    ...
    styles: [require('./somecomponent.css')]
})
export class SomeComponent { ... }
```

To make this work, the template has Webpack configured to inject the contents of the `.css` file as a string literal in the built file. Here's the configuration that enables this:

```javascript
// This goes into webpack.config.js, in the module loaders array:
{ test: /\.css/, include: /ClientApp/, loader: 'raw-loader' }
```

Now if you want to use LESS instead of plain CSS, you just need to include a LESS loader. Run the following in a command prompt at your project root:

```
npm install --save less-loader less
```

Next, add the following loader configuration to the `loaders` array in `webpack.config.js`:

```javascript
{ test: /\.less/, include: /ClientApp/, loader: 'raw-loader!less-loader' }
```

Notice how this chains together with `less-loader` (which transforms `.less` syntax to plain CSS syntax), then the `raw` loader (which turn the result into a string literal). With this in place, you can reference `.less` files from your Angular components in the obvious way:

```javascript
@Component({
    ...
    styles: [require('./somecomponent.less')]
})
export class SomeComponent { ... }
```

... and your styles will be applied in both server-side and client-side rendering.

#### Approach 2: Loading the styles using Webpack and JavaScript

This technique works with any client-side framework (not just Angular), and can also apply styles to the entire document rather than just individual components. It's a little simpler to set up than technique 3, plus it works flawlessly with Hot Module Replacement (HMR). The downside is that it's really only good for development time, because in production you probably don't want users to wait until JavaScript is loaded before styles are applied to the page (this would mean they'd see a 'flash of unstyled content' while the page is being loaded).

First create a `.less` file in your project. For example, create a file at `ClientApp/styles/mystyles.less` containing:

```less
@base: #f938ab;

h1 {
  color: @base;
}
```

Reference this file from an `import` or `require` statement in one of your JavaScript or TypeScript files. For example, if you've got a `boot-client.ts` file, add the following near the top:

```javascript
import './styles/mystyles.less';
```

If you try to run the Webpack compiler now (e.g., via `webpack` on the command line), you'll get an error saying it doesn't know how to build `.less` files. So, it's time to install a Webpack loader for LESS (plus related NPM modules). In a command prompt at your project's root directory, run:

```
npm install --save less-loader less
```

Finally, tell Webpack to use this whenever it encounters a `.less` file. In `webpack.config.js`, add to the `loaders` array:

```
{ test: /\.less/, loader: 'style-loader!css-loader!less-loader' }
```

This means that when you `import` or `require` a `.less` file, it should pass it first to the LESS compiler to produce CSS, then the output goes to the CSS and Style loaders that know how to attach it dynamically to the page at runtime.

That's all you need to do! Restart your site and you should see the LESS styles being applied. This technique is compatible with both source maps and Hot Module Replacement (HMR), so you can edit your `.less` files at will and see the changes appearing live in the browser.

#### Approach 3: Building LESS to CSS files on disk

This technique takes a little more work to set up than technique 2, and lacks compatibility with HMR. But it's much better for production use if your styles are applied to the whole page (not just elements constructed via JavaScript), because it loads the CSS independently of JavaScript.

First add a `.less` file into your project. For example, create a file at `ClientApp/styles/mystyles.less` containing:

```less
@base: #f938ab;

h1 {
  color: @base;
}
```

Reference this file from an `import` or `require` statement in one of your JavaScript or TypeScript files. For example, if you've got a `boot-client.ts` file, add the following near the top:

```javascript
import './styles/mystyles.less';
```

If you try to run the Webpack compiler now (e.g., via `webpack` on the command line), you'll get an error saying it doesn't know how to build `.less` files. So, it's time to install a Webpack loader for LESS (plus related NPM modules). In a command prompt at your project's root directory, run:

```
npm install --save less less-loader extract-text-webpack-plugin
```

Next, you can extend your Webpack configuration to handle `.less` files. In `webpack.config.js`, at the top, add:

```javascript
var extractStyles = new (require('extract-text-webpack-plugin'))('mystyles.css');
```

This creates a plugin instance that will output text to a file called `mystyles.css`. You can now compile `.less` files and emit the resulting CSS text into that file. To do so, add the following to the `loaders` array in your Webpack configuration:

```javascript
{ test: /\.less$/, loader: extractStyles.extract('css-loader!less-loader') }
```

This tells Webpack that, whenever it finds a `.less` file, it should use the LESS loader to produce CSS, and then feed that CSS into the `extractStyles` object which you've already configured to write a file on disk called `mystyles.css`. Finally, for this to actually work, you need to include `extractStyles` in the list of active plugins. Just add that object to the `plugins` array in your Webpack config, e.g.:

```javascript
plugins: [
    extractStyles,
    ... leave any other plugins here ...
]
```

If you run `webpack` on the command line now, you should now find that it emits a new file at `dist/mystyles.css`. You can make browsers load this file simply by adding a regular `<link>` tag. For example, in `Views/Shared/_Layout.cshtml`, add:

```html
<link rel="stylesheet" href="~/dist/mystyles.css" asp-append-version="true" />
```

**Note:** This technique (writing the built `.css` file to disk) is ideal for production use. But note that, at development time, *it does not support Hot Module Replacement (HMR)*. You will need to reload the page each time you edit your `.less` file. This is a known limitation of `extract-text-webpack-plugin`. If you have constructive opinions on how this can be improved, see the [discussion here](https://github.com/webpack/extract-text-webpack-plugin/issues/30).

### Enabling webpack dev middleware

First install the `Microsoft.AspNetCore.SpaServices` NuGet package and the `aspnet-webpack` NPM package, then go to your `Startup.cs` file, and **before your call to `UseStaticFiles`**, add the following:

```csharp
if (env.IsDevelopment()) {
    app.UseWebpackDevMiddleware();
}

// Your call to app.UseStaticFiles(); should be here
```

Also check your webpack configuration at `webpack.config.js`. Since `UseWebpackDevMiddleware` needs to know which incoming requests to intercept, make sure you've specified a `publicPath` value on your `output`, for example:

```javascript
module.exports = {
    // ... rest of your webpack config is here ...

    output: {
        path: path.join(__dirname, 'wwwroot', 'dist'),
        publicPath: '/dist/',
        filename: '[name].js'
    },
};
```

Now, assuming you're running in [development mode](https://docs.asp.net/en/latest/fundamentals/environments.html), any requests for files under `/dist` will be intercepted and served using Webpack dev middleware.

**This is for development time only, not for production use (hence the `env.IsDevelopment()` check in the code above).** While you could technically remove that check and serve your content in production through the webpack middleware, it's hard to think of a good reason for doing so. For best performance, it makes sense to prebuild your client-side resources so they can be served directly from disk with no build middleware. If you use the [aspnetcore-spa generator](http://blog.stevensanderson.com/2016/05/02/angular2-react-knockout-apps-on-aspnet-core/), you'll get a site that produces optimised static builds for production, while also supporting webpack dev middleware at development time.

## Webpack Hot Module Replacement

For an even more streamlined development experience, you can enhance webpack dev middleware by enabling Hot Module Replacement (HMR) support. This watches for any changes you make to source files on disk (e.g., `.ts`/`.html`/`.sass`/etc. files), and automatically rebuilds them and pushes the result into your browser window, without even needing to reload the page.

This is *not* the same as a simple live-reload mechanism. It does not reload the page; it replaces code or markup directly in place. This is better, because it does not interfere with any state your SPA might have in memory, or any debugging session you have in progress.

Typically, when you change a source file, the effects appear in your local browser window in under 2 seconds, even when your overall application is large. This is superbly productive, especially in multi-monitor setups. If you cause a build error (e.g., a syntax error), details of the error will appear in your browser window. When you fix it, your application will reappear, without having lost its in-memory state.

### Enabling Hot Module Replacement

First ensure you already have a working Webpack dev middleware setup. Then, install the `webpack-hot-middleware` NPM module:

```
npm install --save-dev webpack-hot-middleware
```

At the top of your `Startup.cs` file, add the following namespace reference:

```csharp
using Microsoft.AspNetCore.SpaServices.Webpack;
```

Now amend your call to `UseWebpackDevMiddleware` as follows:

```csharp
app.UseWebpackDevMiddleware(new WebpackDevMiddlewareOptions {
    HotModuleReplacement = true
});
```

Also, to work around a temporary issue in `SpaServices`, you must ensure that your Webpack config includes a `plugins` array, even if it's empty. For example, in `webpack.config.js`:

```javascript
module.exports = {
    // ... rest of your webpack config is here ...

    plugins: [
        // Put webpack plugins here if needed, or leave it as an empty array if not
    ]
};
```

Now when you load your application in a browser, you should see a message like the following in your browser console:

```
[HMR] connected
```

If you edit any of your source files that get built by webpack, the result will automatically be pushed into the browser. As for what the browser does with these updates - that's a matter of how you configure it - see below.

**Note for TypeScript + Visual Studio users**

If you want HMR to work correctly with TypeScript, and you use Visual Studio on Windows as an IDE (but not VS Code), then you will need to make a further configuration change. In your `.csproj` file, in one of the `<PropertyGroup>` elements, add this:

    <TypeScriptCompileBlocked>true</TypeScriptCompileBlocked>

This is necessary because otherwise, Visual Studio will try to auto-compile TypeScript files as you save changes to them. That default auto-compilation behavior is unhelpful in projects where you have a proper build system (e.g., Webpack), because VS doesn't know about your build system and would emit `.js` files in the wrong locations, which would in turn cause problems with your real build or deployment mechanisms.

#### Enabling hot replacement for React components

Webpack has built-in support for updating React components in place. To enable this, amend your `UseWebpackDevMiddleware` call further as follows:

```csharp
app.UseWebpackDevMiddleware(new WebpackDevMiddlewareOptions {
    HotModuleReplacement = true,
    ReactHotModuleReplacement = true
});
```

Also, install the NPM module `aspnet-webpack-react`, e.g.:

```
npm install --save-dev aspnet-webpack-react
```

Now if you edit any React component (e.g., in `.jsx` or `.tsx` files), the updated component will be injected into the running application, and will even preserve its in-memory state.

**Note**: In you webpack config, be sure that your React components are loaded using `babel-loader` (and *not* just directly using `babel` or `ts-loader`), because `babel-loader` is where the HMR instrumentation is injected. For an example of HMR for React components built with TypeScript, see the [aspnetcore-spa generator](http://blog.stevensanderson.com/2016/05/02/angular2-react-knockout-apps-on-aspnet-core/).

#### Enabling hot replacement for other module types

Webpack has built-in HMR support for various types of module, such as styles and React components as described above. But to support HMR for other code modules, you need to add a small block of code that calls `module.hot.accept` to receive the updated module and update the running application.

This is [documented in detail on the Webpack site](https://webpack.github.io/docs/hot-module-replacement.html). Or to get a working HMR-enabled ASP.NET Core site with Angular, React, React+Redux, or Knockout, you can use the [aspnetcore-spa generator](http://blog.stevensanderson.com/2016/05/02/angular2-react-knockout-apps-on-aspnet-core/).

#### Passing options to the Webpack Hot Middleware client

You can configure the [Webpack Hot Middleware client](https://github.com/glenjamin/webpack-hot-middleware#client)
by using the `HotModuleReplacementClientOptions` property on `WebpackDevMiddlewareOptions`:

```csharp
app.UseWebpackDevMiddleware(new WebpackDevMiddlewareOptions {
    HotModuleReplacement = true,
    HotModuleReplacementClientOptions = new Dictionary<string, string> {
        { "reload", "true" },
    },
});
```

For the list of available options, please see [Webpack Hot Middleware docs](https://github.com/glenjamin/webpack-hot-middleware#client).

**Note**: The `path` option cannot be overridden this way - it is controlled by the `HotModuleReplacementEndpoint` setting.

## Routing helper: MapSpaFallbackRoute

*Note: this functionality has been superseded by `endpoints.MapFallbackToFile(...)` provided by endpoint routing.
`MapFallbackToFile` behaves similarly to `MapSpaFallbackRoute`.*

In most single-page applications, you'll want client-side routing as well as your server-side routing. Most of the time, the two routing systems work independently without interfering. However, there is one case where things get challenging: identifying 404s.

If a request arrives for `/some/page`, and it doesn't match any server-side route, it's likely that you want to return HTML that starts up your client-side application, which probably understands the route `/some/page`. But if a request arrives for `/images/user-512.png`, and it doesn't match any server-side route or static file, it's **not** likely that your client-side application would handle it - you probably want to return a 404.

To help distinguish between these cases, the `Microsoft.AspNetCore.SpaServices` NuGet package includes a routing helper, `MapSpaFallbackRoute`. For example, in your `Startup.cs` file's `Configure` method, you might add:

```csharp
    app.UseStaticFiles();

    app.UseMvc(routes =>
    {
        routes.MapRoute(
            name: "default",
            template: "{controller=Home}/{action=Index}/{id?}");

        routes.MapSpaFallbackRoute(
            name: "spa-fallback",
            defaults: new { controller = "Home", action = "Index" });
    });
```

Since `UseStaticFiles` goes first, any requests that actually match physical files under `wwwroot` will be handled by serving that static file.

Since the default server-side MVC route goes next, any requests that match existing controller/action pairs will be handled by invoking that action.

Then, since `MapSpaFallbackRoute` is last, any other requests **that don't appear to be for static files** will be served by invoking the `Index` action on `HomeController`. This action's view should serve your client-side application code, allowing the client-side routing system to handle whatever URL has been requested.

Any requests that do appear to be for static files (i.e., those that end with filename extensions), will *not* be handled by `MapSpaFallbackRoute`, and so will end up as 404s.

This is not a perfect solution to the problem of identifying 404s, because for example `MapSpaFallbackRoute` will not match requests for `/users/albert.einstein`, because it appears to contain a filename extension (`.einstein`). If you need your SPA to handle routes like that, then don't use `MapSpaFallbackRoute` - just use a regular MVC catch-all route. But then beware that requests for unknown static files will result in your client-side app being rendered.

## Debugging your projects

How to attach and use a debugger depends on what code you want to debug. For details, see:

 * [How to debug your C# code that runs on the server](#debugging-your-c-code-that-runs-on-the-server)
 * How to debug your JavaScript/TypeScript code:
   * ... [when it's running in a browser](#debugging-your-javascripttypescript-code-when-its-running-in-a-browser)
   * ... [when it's running on the server](#debugging-your-javascripttypescript-code-when-it-runs-on-the-server) (i.e., via `asp-prerender` or NodeSevices)

### Debugging your C# code that runs on the server

You can use any .NET debugger, for example Visual Studio's C# debugger or [Visual Studio Code's C# debugger](https://code.visualstudio.com/Docs/editor/debugging).

### Debugging your JavaScript/TypeScript code when it's running in a browser

**The absolute most reliable way of debugging your client-side code is to use your browser's built-in debugger.** This is much easier to make work than debugging via an IDE, plus it offers much richer insight into what's going on than your IDE will do (for example, you'll be able to inspect the DOM and capture performance profiles as well as just set breakpoints and step through code).

If you're unfamiliar with your browser's debugging tools, then take the time to get familiar with them. You will become more productive.

#### Using your browser's built-in debugging tools

##### Using Chrome's developer tools for debugging

In Chrome, with your application running in the browser, [open the developer tools](https://developer.chrome.com/devtools#access). You can now find your code:

 * In the developer tools *Sources* tab, expand folders in the hierarchy pane on the left to find the file you want
 * Or, press `ctrl`+`o` (on Windows) or `cmd`+`o` on Mac, then start to type name name of the file you want to open (e.g., `counter.component.ts`)

With source maps enabled (which is the case in the project templates in this repo), you'll be able to see your original TypeScript source code, set breakpoints on it, etc.

##### Using Internet Explorer/Edge's developer tools (F12) for debugging

In Internet Explorer or Edge, with your application running in the browser, open the F12 developer tools by pressing `F12`. You can now find your code:

 * In the F12 tools *Debugger* tab, expand folders in the hierarchy pane on the left to find the file you want
 * Or, press `ctrl`+`o`, then start to type name name of the file you want to open (e.g., `counter.component.ts`)

With source maps enabled (which is the case in the project templates in this repo), you'll be able to see your original TypeScript source code, set breakpoints on it, etc.

##### Using Firefox's developer tools for debugging

In Firefox, with your application running in the browser, open the developer tools by pressing `F12`. You can now find your code:

 * In the developer tools *Debugger* tab, expand folders in the hierarchy pane titled *Sources* towards the bottom to find the file you want
 * Or, press `ctrl`+`o` (on Windows) or `cmd`+`o` on Mac, then start to type name name of the file you want to open (e.g., `counter.component.ts`)

With source maps enabled (which is the case in the project templates in this repo), you'll be able to see your original TypeScript source code, set breakpoints on it, etc.

##### How browser-based debugging interacts with Hot Module Replacement (HMR)

If you're using HMR, then each time you modify a file, the Webpack dev middleware restarts your client-side application, adding a new version of each affected module, without reloading the page. This can be confusing during debugging, because any breakpoints set on the old version of the code will still be there, but they will no longer get hit, because the old version of the module is no longer in use.

You have two options to get breakpoints that will be hit as expected:

 * **Reload the page** (e.g., by pressing `F5`). Then your existing breakpoints will be applied to the new version of the module. This is obviously the easiest solution.
 * Or, if you don't want to reload the page, you can **set new breakpoints on the new version of the module**. To do this, look in your browser's debug tools' list of source files, and identify the newly-injected copy of the module you want to debug. It will typically have a suffix on its URL such as `?4a2c`, and may appear in a new top-level hierarchy entry called `webpack://`. Set a breakpoint in the newly-injected module, and it will be hit as expected as your application runs.

#### Using Visual Studio Code's "Debugger for Chrome" extension

If you're using Visual Studio Code and Chrome, you can set breakpoints directly on your TypeScript source code in the IDE. To do this:

1. Install VS Code's [*Debugger for Chrome* extension](https://marketplace.visualstudio.com/items?itemName=msjsdiag.debugger-for-chrome)
2. Ensure your application server has started and can be reached with a browser (for example, run `dotnet watch run`)
3. In VS Code, open its *Debug* view (on Windows/Linux, press `ctrl`+`shift`+`d`; on Mac, press `cmd`+`shift`+`d`).
4. Press the cog icon and when prompted to *Select environment*, choose `Chrome`. VS Code will create a `launch.json` file for you. This describes how the debugger and browser should be launched.
5. Edit your new `.vscode/launch.json` file to specify the correct `url` and `webRoot` for your application. If you're using the project templates in this repo, then the values you probably want are:
     * For `url`, put `"http://localhost:5000"` (but of course, change this if you're using a different port)
     * For `port`, put `5000` (or your custom port number if applicable)
     * For `workspace` in **both** configurations, put `"${workspaceRoot}/wwwroot"`
       * This tells the debugger how URLs within your application correspond to files in your VS Code workspace. By default, ASP.NET Core projects treat `wwwroot` as the root directory for publicly-served files, so `http://localhost:5000/dist/myfile.js` corresponds to `<yourprojectroot>/wwwroot/dist/myfile.js`. VS Code doesn't know about `wwwroot` unless you tell it.
       * **Important:** If your VS Code window's workspace root is not the same as your ASP.NET Core project root (for example, if VS Code is opened at a higher-level directory to show both your ASP.NET Core project plus other peer-level directories), then you will need to amend `workspace` correspondingly (e.g., to `"${workspaceRoot}/SomeDir/MyAspNetProject/wwwroot"`).
6. Start the debugger:
   * While still on the *Debug* view, from the dropdown near the top-left, choose "*Launch Chrome against localhost, with sourcemaps*".
   * Press the *Play* icon. Your application will launch in Chrome.
     * If it does nothing for a while, then eventually gives the error *Cannot connect to runtime process*, that's because you already have an instance of Chrome running. Close it first, then try again.
7. Finally, you can now set and hit breakpoints in your TypeScript code in VS Code.

For more information about VS Code's built-in debugging facilities, [see its documentation](https://code.visualstudio.com/Docs/editor/debugging).

Caveats:

 * The debugging interface between VS Code and Chrome occasionally has issues. If you're unable to set or hit breakpoints, or if you try to set a breakpoint but it appears in the wrong place, you may need to stop and restart the debugger (and often, the whole Chrome process).
 * If you're using Hot Module Replacement (HMR), then whenever you edit a file, the breakpoints in it will no longer hit. This is because HMR loads a new version of the module into the browser, so the old code no longer runs. To fix this, you must:
   * Reload the page in Chrome (e.g., by pressing `F5`)
   * **Then** (and only then), remove and re-add the breakpoint in VS Code. It will now be attached to the current version of your module. Alternatively, stop and restart debugging altogether.
 * If you prefer, you can use "*Attach to Chrome, with sourcemaps*" instead of launching a new Chrome instance, but this is a bit trickier: you must first start Chrome using the command-line option `--remote-debugging-port=9222`, and you must ensure there are no other tabs opened (otherwise, it might try to connect to the wrong one).


#### Using Visual Studio's built-in debugger for Internet Explorer

If you're using Visual Studio on Windows, and are running your app in Internet Explorer 11 (not Edge!), then you can use VS's built-in debugger rather than Interner Explorer's F12 tools if you prefer. To do this:

 1. In Internet Explorer, [enable script debugging](https://msdn.microsoft.com/en-us/library/ms241741\(v=vs.100\).aspx)
 2. In Visual Studio, [set the default "*Browse with*" option](http://stackoverflow.com/a/31959053) to Internet Explorer
 3. In Visual Studio, press F5 to launch your application with the debugger in Internet Explorer.
    * When the page has loaded in the browser, you'll be able to set and hit breakpoints in your TypeScript source files in Visual Studio.

Caveats:

 * If you're using Hot Module Replacement, you'll need to stop and restart the debugger any time you change a source file. VS's IE debugger does not recognise that source files might change while the debugging session is in progress.
 * Realistically, you are not going to be as productive using this approach to debugging as you would be if you used your browser's built-in debugging tools. The browser's built-in debugging tools are far more effective: they are always available (you don't have to have launched your application in a special way), they better handle HMR, and they don't make your application very slow to launch.

## Debugging your JavaScript/TypeScript code when it runs on the server

When you're using NodeServices or the server-side prerendering feature included in the project templates in this repo, your JavaScript/TypeScript code will execute on the server in a background instance of Node.js. You can enable debugging via [V8 Inspector Integration](https://nodejs.org/api/debugger.html#debugger_v8_inspector_integration_for_node_js) on that Node.js instance. Here's how to do it.

First, in your `Startup.cs` file, in the `ConfigureServices` method, add the following:

```
services.AddNodeServices(options => {
    options.LaunchWithDebugging = true;
    options.DebuggingPort = 9229;
});
```

Now, run your application from that command line (e.g., `dotnet run`). Then in a browser visit one of your pages that causes server-side JS to execute.

In the console, you should see all the normal trace messages appear, plus among them will be:

```
warn: Microsoft.AspNetCore.NodeServices[0]
      Debugger listening on port 9229.
warn: Microsoft.AspNetCore.NodeServices[0]
      Warning: This is an experimental feature and could change at any time.
warn: Microsoft.AspNetCore.NodeServices[0]
      To start debugging, open the following URL in Chrome:
warn: Microsoft.AspNetCore.NodeServices[0]
          chrome-devtools://devtools/bundled/inspector.html?experiments=true&v8only=true&ws=127.0.0.1:9229/xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
```

As per instructions open the URL in Chrome. Alternatively you can go to the `Sources` tab of the Dev Tools (at http://localhost:5000) and connect to the Node instance under `Threads` in the right sidebar.

By expanding the `webpack://` entry in the sidebar, you'll be able to find your original source code (it's using source maps), and then set breakpoints in it. When you re-run your app in another browser window, your breakpoints will be hit, then you can debug the server-side execution just like you'd debug client-side execution. It looks like this:

![screenshot from 2017-03-25 13-33-26](https://cloud.githubusercontent.com/assets/1596280/24324604/ab888a7e-115f-11e7-89d1-1586acf5e35c.png)

