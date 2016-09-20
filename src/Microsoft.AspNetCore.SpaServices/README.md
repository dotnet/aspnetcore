# Microsoft.AspNetCore.SpaServices

If you're building an ASP.NET Core application, and want to use Angular 2, React, Knockout, or another single-page app (SPA) framework, this NuGet package contains useful infrastructure for you.

This package enables:

 * [**Server-side prerendering**](#server-side-prerendering) for *universal* (a.k.a. *isomorphic*) applications, where your Angular 2 / React / etc. components are first rendered on the server, and then transferred to the client where execution continues
 * [**Webpack middleware**](#webpack-dev-middleware) so that, during development, any webpack-built resources will be generated on demand, without you having to run webpack manually or compile files to disk
 * [**Hot module replacement**](#webpack-hot-module-replacement) so that, during development, your code and markup changes will be pushed to your browser and updated in the running application automatically, without even needing to reload the page
 * [**Routing helpers**](#routing-helper-mapspafallbackroute) for integrating server-side routing with client-side routing

Behind the scenes, it uses the [`Microsoft.AspNetCore.NodeServices`](https://github.com/aspnet/JavaScriptServices/tree/dev/src/Microsoft.AspNetCore.NodeServices) package as a fast and robust way to invoke Node.js-hosted code from ASP.NET Core at runtime.

### Requirements

* [Node.js](https://nodejs.org/en/)
  * To test this is installed and can be found, run `node -v` on a command line
  * Note: If you're deploying to an Azure web site, you don't need to do anything here - Node is already installed and available in the server environments
* [.NET Core](https://dot.net), version 1.0 RC2 or later

### Installation into existing projects

 * Add `Microsoft.AspNetCore.SpaServices` to the dependencies list in your `project.json` file
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

## Server-side prerendering

The `SpaServices` package isn't tied to any particular client-side framework, and it doesn't force you to set up your client-side application in any one particular style. So, `SpaServices` doesn't contain hard-coded logic for rendering Angular 2 / React / etc. components.

Instead, what `SpaServices` offers is ASP.NET Core APIs that know how to invoke a JavaScript function that you supply, passing through context information that you'll need for server-side prerendering, and then injects the resulting HTML string into your rendered page. In this document, you'll find examples of setting this up to render Angular 2 and React components.

### 1. Enable the asp-prerender-* tag helpers

Make sure you've installed the `Microsoft.AspNetCore.SpaServices` NuGet package and the `aspnet-prerendering` NPM package. Together these contain the server-side and client-side library code you'll need.

Now go to your `Views/_ViewImports.cshtml` file, and add the following line:

    @addTagHelper "*, Microsoft.AspNetCore.SpaServices"

### 2. Use asp-prerender-* in a view

Choose a place in one of your MVC views where you want to prerender a SPA component. For example, open `Views/Home/Index.cshtml`, and add markup like the following:

    <div id="my-spa" asp-prerender-module="ClientApp/boot-server"></div>

If you run your application now, and browse to whatever page renders the view you just edited, you should get an error similar to the following (assuming you're running in *Development* mode so you can see the error information): *Error: Cannot find module 'some/directory/ClientApp/boot-server'*. You've told the prerendering tag helper to execute code from a JavaScript module called `boot-server`, but haven't yet supplied any such module!

### 3. Supplying JavaScript code to perform prerendering

Create a JavaScript file at the path matching the `asp-prerender-module` value you specified above. In this example, that means creating a folder called `ClientApp` at the root of your project, and creating a file inside it called `boot-server.js`. Try putting the following into it:

```javascript
module.exports = function(params) {
    return new Promise(function (resolve, reject) {
        var result = '<h1>Hello world!</h1>'
            + '<p>Current time in Node is: ' + new Date() + '</p>'
            + '<p>Request path is: ' + params.location.path + '</p>'
            + '<p>Absolute URL is: ' + params.absoluteUrl + '</p>';

        resolve({ html: result });
    });
};
```

If you try running your app now, you should see the HTML snippet generated by your JavaScript getting injected into your page.

As you can see, your JavaScript code receives context information (such as the URL being requested), and returns a `Promise` so that it can asynchronously supply the markup to be injected into the page. You can put whatever logic you like here, but typically you'll want to execute a component from your Angular 2 / React / etc. application.

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
module.exports = function(params) {
    return new Promise(function (resolve, reject) {
        var result = '<h1>Hello world!</h1>'
            + '<p>Is gold user: ' + params.data.isGoldUser + '</p>'
            + '<p>Number of cookies: ' + params.data.cookies.length + '</p>';

        resolve({ html: result });
    });
};
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

Of course, rather than writing your `boot-server` module and your entire SPA in plain ES5 JavaScript, it's quite likely that you'll want to write your client-side code in TypeScript or at least ES2015 code. To enable this, you can either:

 * Set up some build tool such as Babel to transpile to ES5, and always remember to run this to generate plain ES5 `.js` files before you run your application
 * Or, more conveniently, use [webpack](https://webpack.github.io/) along with the `asp-prerender-webpack-config` attribute so that `Microsoft.AspNetCore.SpaServices` can automatically build your boot module and the SPA code that it references. Then there's no need for `.js` files even to be written to disk - the build process is all dynamic and in memory.

To enable webpack builds for your server-side prerendering, amend your MVC view to specify the location of your webpack configuration file using an `asp-prerender-webpack-config` attribute, e.g.:

    <div id="my-spa" asp-prerender-module="ClientApp/boot-server"
                     asp-prerender-webpack-config="webpack.config.js"></div>

You'll also need to install the NPM module `aspnet-webpack` if you don't have it already, e.g.:

    npm install --save aspnet-webpack

This includes webpack as well as the server-side code needed to invoke it from ASP.NET Core at runtime.

Now, assuming you have a working webpack configuration at `webpack.config.js`, your boot module and SPA code will dynamically be built using webpack.

#### Example: Configuring webpack to build TypeScript

Let's say you want to write your boot module and SPA code in TypeScript. First ensure that `aspnet-webpack` is installed, along with the libraries needed for TypeScript compilation:

    npm install --save aspnet-webpack ts-loader typescript

Next, create a file `webpack.config.js` at the root of your project, containing:

```javascript
module.exports = {
    resolve: { extensions: [ '', '.js', '.ts' ] },
    module: {
        loaders: [
            { test: /\.ts$/, loader: 'ts-loader' }
        ]
    }
};
```

This tells webpack that it should compile `.ts` files using TypeScript, and that when looking for modules by name (e.g., `boot-server`), it should also find files with `.js` and `.ts` extensions.

Now you can delete `ClientApp/boot-server.js`, and in its place, create `ClientApp/boot-server.ts`, containing the TypeScript equivalent of what you had before:

```javascript
export default function (params: any): Promise<{ html: string}> {
    return new Promise((resolve, reject) => {
        const html = `
            <h1>Hello world!</h1>
            <p>Current time in Node is: ${ new Date() }</p>
            <p>Request path is: ${ params.location.path }</p>
            <p>Absolute URL is: ${ params.absoluteUrl }</p>`;

        resolve({ html });
    });
}
```

Finally, you can tell `SpaServices` to use the Webpack environment you've just set up. In your MVC view where you use `aspnet-prerender-module`, also specify `aspnet-prerender-webpack-config`:

    <div id="my-spa" asp-prerender-module="ClientApp/boot-server"
                     asp-prerender-webpack-config="webpack.config.js"></div>

Now your `boot-server.ts` code should get executed when your ASP.NET Core page is rendered, and since it's TypeScript, it can of course reference any other TypeScript modules, which means your entire SPA can be written in TypeScript and executed on the server.

Webpack is a broad and powerful tool and can do far more than just invoke the TypeScript compiler. To learn more, see the [webpack website](https://webpack.github.io/).


### 5(a). Prerendering Angular 2 components

If you're building an Angular 2 application, you can run your components on the server inside your `boot-server.ts` file so they will be injected into the resulting web page.

First install the NPM package `angular2-universal` - this contains infrastructure for executing Angular 2 components inside Node.js:

```
npm install --save angular2-universal
```

Now you can use the [`angular2-universal` APIs](https://github.com/angular/universal) from your `boot-server.ts` TypeScript module to execute your Angular 2 component on the server. The code needed for this is fairly complex, but that's unavoidable because Angular 2 supports so many different ways of being configured, and you need to provide wiring for whatever combination of DI modules you're using.

You can find an example `boot-server.ts` that renders arbitrary Angular 2 components [here](https://github.com/aspnet/JavaScriptServices/blob/dev/templates/Angular2Spa/ClientApp/boot-server.ts). If you use this with your own application, you might need to edit the `serverBindings` array to reference any other DI services that your Angular 2 component depends on.

The easiest way to get started with Angular 2 server-side rendering on ASP.NET Core is to use the [aspnetcore-spa generator](http://blog.stevensanderson.com/2016/05/02/angular2-react-knockout-apps-on-aspnet-core/), which creates a ready-made working starting point.

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

#### Running React code on the server

Now you have React code being built using Webpack, you can enable server-side prerendering using the `aspnet-prerender-*` tag helpers as follows:

    <div id="my-spa" asp-prerender-module="ClientApp/boot-server"
                     asp-prerender-webpack-config="webpack.config.js"></div>

... along with the following boot module at `ClientApp/boot-server.jsx`:

```javascript
import * as React from 'react';
import { renderToString } from 'react-dom/server';
import { HelloMessage } from './react-app';

export default function (params) {
    return new Promise((resolve, reject) => {
        resolve({
            html: renderToString(<HelloMessage message="from the server" />)
        });
    });
}
```

Now you should find that your React app is rendered in the page even before any JavaScript is loaded in the browser (or even if JavaScript is disabled in the browser).

#### Realistic React apps and Redux

The above example is extremely simple - it doesn't use `react-router`, and it doesn't load any data asynchronously. Real applications are likely to do both of these.

For an example server-side boot module that knows how to evaluate `react-router` routes and render the correct React component, see [this example](https://github.com/aspnet/JavaScriptServices/blob/dev/templates/ReactReduxSpa/ClientApp/boot-server.tsx).

Supporting asynchronous data loading involves more considerations. Unlike Angular 2 applications that run asynchronously on the server and freely overwrite server-generated markup with client-generated markup, React strictly wants to run synchronously on the server and always produce the same markup on the server as it does on the client.

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

1. **If using Angular 2, use its native style loader to attach the styles to components**. This is extremely simple and is usually the right choice if you are using Angular 2. However it only applies to Angular 2 components, not to any other part of the host page, so sometimes you might want to combine this technique with options 2 or 3 below.

2. **Or, use Webpack's style loader to attach the styles at runtime**. The CSS markup will be included in your JavaScript bundles and will be attached to the document dynamically. This has certain benefits during development but isn't recommended in production.

3. **Or, have each build write a standalone `.css` file to disk**. At runtime, load it using a regular `<link rel='stylesheet'>` tag. This is likely to be the approach you'll want for production use (at least for non-Angular 2 applications, such as React applications) as it's the most robust and best-performing option.

If instead of LESS you prefer SASS or another CSS preprocessor, the exact same techniques should work, but of course you'll need to replace the `less-loader` with an equivalent Webpack loader for SASS or your chosen preprocessor.

#### Approach 1: Scoping styles to Angular 2 components

If you are using Angular 2, this is the easiest way to perform styling. It works with both server and client rendering, supports Hot Module Replacement, and robustly scopes styles to particular components (and optionally, their descendant elements).

This repository's Angular 2 template uses this technique to scope styles to components out of the box. It defines those styles as `.css` files. For example, its components reference `.css` files like this:

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
{ test: /\.css/, include: /ClientApp/, loader: 'raw' }
```

Now if you want to use LESS instead of plain CSS, you just need to include a LESS loader. Run the following in a command prompt at your project root:

```
npm install --save less-loader less
```

Next, add the following loader configuration to the `loaders` array in `webpack.config.js`:

```javascript
{ test: /\.less/, include: /ClientApp/, loader: 'raw!less' }
```

Notice how this chains together the `less` loader (which transforms `.less` syntax to plain CSS syntax), then the `raw` loader (which turn the result into a string literal). With this in place, you can reference `.less` files from your Angular 2 components in the obvious way:

```javascript
@Component({
    ...
    styles: [require('./somecomponent.less')]
})
export class SomeComponent { ... }
```

... and your styles will be applied in both server-side and client-side rendering.

#### Approach 2: Loading the styles using Webpack and JavaScript

This technique works with any client-side framework (not just Angular 2), and can also apply styles to the entire document rather than just individual components. It's a little simpler to set up than technique 3, plus it works flawlessly with Hot Module Replacement (HMR). The downside is that it's really only good for development time, because in production you probably don't want users to wait until JavaScript is loaded before styles are applied to the page (this would mean they'd see a 'flash of unstyled content' while the page is being loaded).

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
{ test: /\.less/, loader: 'style!css!less' }
```

This means that when you `import` or `require` a `.less` file, it should pass it first to the LESS compiler to produce CSS, then the output goes to the CSS and Style loaders that know how to attach it dynamically to the page at runtime.

That's all you need to do! Restart your site and you should see the LESS styles being applied. This technique is compatible with both source maps and Hot Module Replacement (HMR), so you can edit your `.less` files at will and see the changes appearing live in the browser.

**Scoping styles in Angular 2 components**

If you're using Angular 2, you can define styles on a per-component basis rather than just globally for your whole app. Angular then takes care of ensuring that only the intended styles are applied to each component, even if the selector names would otherwise clash. To extend the above technique to per-component styling, first install the `to-string-loader` NPM module:

```
npm install --save to-string-loader
```

Then in your `webpack.config.js`, simplify the `loader` entry for LESS files so that it just outputs `css` (without preparing it for use in a `style` tag):

```javascript
{ test: /\.less/, loader: 'css!less' }
```

Now **you must remove any direct global references to the `.less` file**, since you'll no longer be loading it globally. So if you previously loaded `mystyles.less` using an `import` or `require` statement in `boot-client.ts` or similar, remove that line.

Finally, load the LESS file scoped to a particular Angular 2 component by declaring a `styles` value for that component. For example,

```javascript
@ng.Component({
  selector: ... leave value unchanged ...,
  template: ... leave value unchanged ...,
  styles: [require('to-string!../../styles/mystyles.less')]
})
export class YourComponent {
   ... code remains here ...
}
```

Now when you reload your page, you should file that the styles in `mystyles.less` are applied, but only to the component where you attached it. It's reasonable to use this technique in production because, even though the styles now depend on JavaScript to be applied, they are only used on elements that are injected via JavaScript anyway.

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
{ test: /\.less$/, loader: extractStyles.extract('css!less') }
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

// You call to app.UseStaticFiles(); should be here
```

Also check your webpack configuration at `webpack.config.js`. Since `UseWebpackDevMiddleware` needs to know which incoming requests to intercept, make sure you've specified a `publicPath` value on your `output`, for example:

```javascript
module.exports = {
    // ... rest of your webpack config is here ...

    output: {
        path: path.join(__dirname, 'wwwroot', 'dist'),
        publicPath: '/dist',
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
npm install --save webpack-hot-middleware
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
npm install --save aspnet-webpack-react
```

Now if you edit any React component (e.g., in `.jsx` or `.tsx` files), the updated component will be injected into the running application, and will even preserve its in-memory state.

**Note**: In you webpack config, be sure that your React components are loaded using `babel-loader` (and *not* just directly using `babel` or `ts-loader`), because `babel-loader` is where the HMR instrumentation is injected. For an example of HMR for React components built with TypeScript, see the [aspnetcore-spa generator](http://blog.stevensanderson.com/2016/05/02/angular2-react-knockout-apps-on-aspnet-core/).

#### Enabling hot replacement for other module types

Webpack has built-in HMR support for various types of module, such as styles and React components as described above. But to support HMR for other code modules, you need to add a small block of code that calls `module.hot.accept` to receive the updated module and update the running application.

This is [documented in detail on the Webpack site](https://webpack.github.io/docs/hot-module-replacement.html). Or to get a working HMR-enabled ASP.NET Core site with Angular 2, React, React+Redux, or Knockout, you can use the [aspnetcore-spa generator](http://blog.stevensanderson.com/2016/05/02/angular2-react-knockout-apps-on-aspnet-core/).


## Routing helper: MapSpaFallbackRoute

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
