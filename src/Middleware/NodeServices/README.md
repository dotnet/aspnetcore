# Microsoft.AspNetCore.NodeServices

This NuGet package provides a fast and robust way to invoke Node.js code from a .NET application (typically ASP.NET Core web apps). You can use this whenever you want to use Node/NPM-supplied functionality at runtime in ASP.NET. For example,

 * Executing arbitrary JavaScript
 * Runtime integration with JavaScript build or packaging tools, e.g., transpiling code via Babel
 * Using of NPM modules for image resizing, audio compression, language recognition, etc.
 * Calling third-party services that supply Node-based APIs but don't yet ship native .NET ones

It is the underlying mechanism supporting the following packages:

 * [`Microsoft.AspNetCore.SpaServices`](/src/Middleware/SpaServices/) - builds on NodeServices, adding functionality commonly used in Single Page Applications, such as server-side prerendering, webpack middleware, and integration between server-side and client-side routing.

### Requirements

* [Node.js](https://nodejs.org/en/)
  * To test this is installed and can be found, run `node -v` on a command line
  * Note: If you're deploying to an Azure web site, you don't need to do anything here - Node is already installed and available in the server environments
* [.NET](https://dot.net)
  * For .NET Core (e.g., ASP.NET Core apps), you need at least 1.0 RC2
  * For .NET Framework, you need at least version 4.5.1.

### Installation

For .NET Core apps:

 * Add `Microsoft.AspNetCore.NodeServices` to the dependencies list in your `project.json` file
 * Run `dotnet restore` (or if you use Visual Studio, just wait a moment - it will restore dependencies automatically)

For .NET Framework apps:

 * `nuget install Microsoft.AspNetCore.NodeServices`

### Do you just want to build an ASP.NET Core app with Angular / React / Knockout / etc.?

In that case, you don't need to use NodeServices directly (or install it manually). You can either:

* **Recommended:** Use the `aspnetcore-spa` Yeoman generator to get a ready-to-go starting point using your choice of client-side framework. [Instructions here.](http://blog.stevensanderson.com/2016/05/02/angular2-react-knockout-apps-on-aspnet-core/)
* Or set up your ASP.NET Core and client-side Angular/React/KO/etc. app manually, and then use the [`Microsoft.AspNetCore.SpaServices`](/src/Middleware/SpaServices/) package to add features like server-side prerendering or Webpack middleware. But really, at least try using the `aspnetcore-spa` generator first.

# Simple usage example

## For ASP.NET Core apps

.NET Core has a built-in dependency injection (DI) system. NodeServices is designed to work with this, so you don't have to manage the creation or disposal of instances.

Enable NodeServices in your application by first adding the following to your `ConfigureServices` method in `Startup.cs`:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // ... all your existing configuration is here ...

    // Enable Node Services
    services.AddNodeServices();
}
```

Now you can receive an instance of `NodeServices` as an action method parameter to any MVC action, and then use it to make calls into Node.js code, e.g.:

```csharp
public async Task<IActionResult> MyAction([FromServices] INodeServices nodeServices)
{
    var result = await nodeServices.InvokeAsync<int>("./addNumbers", 1, 2);
    return Content("1 + 2 = " + result);
}
```

Of course, you also need to supply the Node.js code you want to invoke. Create a file called `addNumbers.js` at the root of your ASP.NET Core application, and add the following code:

```javascript
module.exports = function (callback, first, second) {
    var result = first + second;
    callback(/* error */ null, result);
};
```

As you can see, the exported JavaScript function will receive the arguments you pass from .NET (as long as they are JSON-serializable), along with a Node-style callback you can use to send back a result or error when you are ready.

When the `InvokeAsync<T>` method receives the result back from Node, the result will be JSON-deserialized to whatever generic type you specified when calling `InvokeAsync<T>` (e.g., above, that type is `int`). If `InvokeAsync<T>` receives an error from your Node code, it will throw an exception describing that error.

If you want to put `addNumber.js` inside a subfolder rather than the root of your app, then also amend the path in the `_nodeServices.Invoke` call to match that path.

## For non-ASP.NET apps

In other types of .NET Core app, where you don't have ASP.NET supplying an `IServiceCollection` to you, you'll need to instantiate your own DI container. For example, add a reference to the .NET package `Microsoft.Extensions.DependencyInjection`, and then you can construct an `IServiceCollection`, then register NodeServices as usual:

```csharp
var services = new ServiceCollection();
services.AddNodeServices(options => {
    // Set any properties that you want on 'options' here
});
```

Now you can ask it to supply the shared `INodeServices` instance:

```csharp
var serviceProvider = services.BuildServiceProvider();
var nodeServices = serviceProvider.GetRequiredService<INodeServices>();
```

Or, if you want to obtain a separate (non-shared) `INodeServices` instance:

```csharp
var options = new NodeServicesOptions(serviceProvider) { /* Assign/override any other options here */ };
var nodeServices = NodeServicesFactory.CreateNodeServices(options);
```

Besides this, the usage is the same as described for ASP.NET above, so you can now call `nodeServices.InvokeAsync<T>(...)` etc.

You can dispose the `nodeServices` object whenever you are done with it (and it will shut down the associated Node.js instance), but because these instances are expensive to create, you should whenever possible retain and reuse instances. Don't dispose the shared instance returned from `serviceProvider.GetRequiredService` (except perhaps if you know your application is shutting down, although .NET's finalizers will dispose it anyway if the shutdown is graceful).

NodeServices instances are thread-safe - you can call `InvokeAsync<T>` simultaneously from multiple threads. Also, they are smart enough to detect if the associated Node instance has died and will automatically start a new Node instance if needed.

# API Reference

### AddNodeServices

**Signatures:**

```csharp
AddNodeServices()
AddNodeServices(Action<NodeServicesOptions> setupAction)
```

This is an extension method on `IServiceCollection`. It registers NodeServices with ASP.NET Core's DI system. Typically you should call this from the `ConfigureServices` method in your `Startup.cs` file.

To access this extension method, you'll need to add the following namespace import to the top of your file, if it isn't already there:

```csharp
using Microsoft.Extensions.DependencyInjection;
```

**Examples**

Using default options:

```csharp
services.AddNodeServices();
```

Or, specifying options:

```csharp
services.AddNodeServices(options =>
{
    options.WatchFileExtensions = new[] { ".coffee", ".sass" };
    // ... etc. - see other properties below
});
```

**Parameters**

 * `setupAction` - type: `Action<NodeServicesOptions>`
   * Optional. If not specified, defaults will be used.
   * Properties on `NodeServicesOptions`:
     * `HostingModel` - an `NodeHostingModel` enum value. See: [hosting models](#hosting-models)
     * `ProjectPath` - if specified, controls the working directory used when launching Node instances. This affects, for example, the location that `require` statements resolve relative paths against. If not specified, your application root directory is used.
     * `WatchFileExtensions` - if specified, the launched Node instance will watch for changes to any files with these extensions, and auto-restarts when any are changed. The default array includes `.js`, `.jsx`, `.ts`, `.tsx`, `.json`, and `.html`.

**Return type**: None. But once you've done this, you can get `NodeServices` instances out of ASP.NET's DI system. Typically it will be a singleton instance.

### CreateNodeServices

**Signature:**

```csharp
CreateNodeServices(NodeServicesOptions options)
```

Supplies a new (non-shared) instance of `NodeServices`.

**Example**

```csharp
var options = new NodeServicesOptions(serviceProvider); // Obtains default options from DI config
var nodeServices = NodeServicesFactory.CreateNodeServices(options);
```

**Parameters**
 * `options` - type: `NodeServicesOptions`.
   * Configures the returned `NodeServices` instance.
   * Properties:
     * `HostingModel` - an `NodeHostingModel` enum value. See: [hosting models](#hosting-models)
     * `ProjectPath` - if specified, controls the working directory used when launching Node instances. This affects, for example, the location that `require` statements resolve relative paths against. If not specified, your application root directory is used.
     * `WatchFileExtensions` - if specified, the launched Node instance will watch for changes to any files with these extension, and auto-restarts when any are changed.

**Return type:** `NodeServices`

If you create a `NodeServices` instance this way, you can also dispose it (call `nodeServiceInstance.Dispose();`) and it will shut down the associated Node instance. But because these instances are expensive to create, you should whenever possible retain and reuse your `NodeServices` object. They are thread-safe - you can call `nodeServiceInstance.InvokeAsync<T>(...)` simultaneously from multiple threads.

### InvokeAsync&lt;T&gt;

**Signature:**

```csharp
InvokeAsync<T>(string moduleName, params object[] args)
```

Asynchronously calls a JavaScript function and returns the result, or throws an exception if the result was an error.

**Example 1: Getting a JSON-serializable object from Node (the most common use case)**

```csharp
var result = await myNodeServicesInstance.InvokeAsync<TranspilerResult>(
    "./Node/transpile",
    pathOfSomeFileToBeTranspiled);
```

... where `TranspilerResult` might be defined as follows:

```csharp
public class TranspilerResult
{
    public string Code { get; set; }
    public string[] Warnings { get; set; }
}
```

... and the corresponding JavaScript module (in `Node/transpile.js`) could be implemented as follows:

```javascript
module.exports = function (callback, filePath) {
    // Invoke some external transpiler (e.g., an NPM module) then:
    callback(null, {
        code: theTranspiledCodeAsAString,
        warnings: someArrayOfStrings
    });
};
```

**Example 2: Getting a stream of binary data from Node**

```csharp
var imageStream = await myNodeServicesInstance.InvokeAsync<Stream>(
    "./Node/resizeImage",
    fullImagePath,
    width,
    height);

// In an MVC action method, you can pipe the result to the response as follows
return File(imageStream, someContentType);
```

... where the corresponding JavaScript module (in `Node/resizeImage.js`) could be implemented as follows:

```javascript
var sharp = require('sharp'); // A popular image manipulation package on NPM

module.exports = function(result, physicalPath, maxWidth, maxHeight) {
    // Invoke the 'sharp' NPM module, and have it pipe the resulting image data back to .NET
    sharp(physicalPath)
        .resize(maxWidth || null, maxHeight || null)
        .pipe(result.stream);
}
```

**Parameters**

* `moduleName` - type: `string`
  * The name of a JavaScript module that Node.js must be able to resolve by calling `require(moduleName)`. This can be a relative path such as `"./Some/Directory/mymodule"`. If you don't specify the `.js` filename extension, Node.js will infer it anyway.
* `params`
  * Any set of JSON-serializable objects you want to pass to the exported JavaScript function

**Return type:** `T`, which must be:

 * A JSON-serializable .NET type, if your JavaScript code uses the `callback(error, result)` pattern to return an object, as in example 1 above
 * Or, the type `System.IO.Stream`, if your JavaScript code writes data to the `result.stream` object (which is a [Node `Duplex` stream](https://nodejs.org/api/stream.html#stream_class_stream_duplex)), as in example 2 above

### InvokeExportAsync&lt;T&gt;

**Signature**

```csharp
InvokeExportAsync<T>(string moduleName, string exportName, params object[] args)
```

This is exactly the same as `InvokeAsync<T>`, except that it also takes an `exportName` parameter. You can use this if you want your JavaScript module to export more than one function.

**Example**

```csharp
var someString = await myNodeServicesInstance.InvokeExportAsync<string>(
    "./Node/myNodeApis",
    "getMeAString");

var someStringInFrench = await myNodeServicesInstance.InvokeExportAsync<string>(
    "./Node/myNodeApis",
    "convertLanguage"
    someString,
    "fr-FR");
```

... where  the corresponding JavaScript module (in `Node/myNodeApis.js`) could be implemented as follows:

```javascript
module.exports = {

    getMeAString: function (callback) {
        callback(null, 'Here is a string');
    },

    convertLanguage: function (callback, sourceString, targetLanguage) {
        // Implementation detail left as an exercise for the reader
        doMachineTranslation(sourceString, targetLanguage, function(error, result) {
            callback(error, result);
        });
    }

};
```

**Parameters, return type, etc.** For all other details, see the docs for [`InvokeAsync<T>`](#invokeasynct)

## Hosting models

NodeServices has a pluggable hosting/transport mechanism, because it is an abstraction over various possible ways to invoke Node.js from .NET. This allows more high-level facilities (e.g., for Angular prerendering) to be agnostic to the details of launching Node and communicating with it - those high-level facilities can just trust that *somehow* we can invoke code in Node for them.

Using this abstraction, we could run Node inside the .NET process, in a separate process on the same machine, or even on a different machine altogether. At the time of writing, all the built-in hosting mechanisms work by launching Node as a separate process on the same machine as your .NET code.

**What about Edge.js?**

[Edge.js](http://tjanczuk.github.io/edge/#/) hosts Node.js inside a .NET process, or vice-versa, and lets you interoperate between the two.

NodeServices is not meant to compete with Edge.js. Instead, NodeServices is an abstraction over all possible ways to invoke Node from .NET. Eventually we may offer an in-process Node hosting mechanism via Edge.js, without you needing to change your higher-level code. This can be done when Edge.js supports hosting Node in cross-platform .NET Core processes ([discussion](https://github.com/tjanczuk/edge/issues/279)).

**What about VroomJS?**

People have asked about using [VroomJS](https://github.com/fogzot/vroomjs) as a hosting mechanism. We don't currently plan to implement that, because Vroom only supplies a V8 runtime environment, not a complete Node environment. The difference is that, with a true Node environment, *all* NPM modules and Node code will work exactly as expected, whereas in a Vroom environment, code will only work if it doesn't use any Node primitives, which rules out large portions of the NPM landscape.

### Custom hosting models

If you implement a custom hosting model (by implementing `INodeInstance`), then you can cause it to be used by populating `NodeInstanceFactory` on your options:

```csharp
services.AddNodeServices(options =>
{
    options.NodeInstanceFactory = () => new MyCustomNodeInstance();
});
```
