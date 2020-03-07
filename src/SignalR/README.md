ASP.NET Core SignalR
====================

ASP.NET Core SignalR is a library for ASP.NET Core developers that makes it incredibly simple to add real-time web functionality to your applications. What is "real-time web" functionality? It's the ability to have your server-side code push content to the connected clients as it happens, in real-time.

You can watch an introductory presentation here - [ASP.NET Core SignalR: Build 2018](https://www.youtube.com/watch?v=Lws0zOaseIM)

## Documentation

Documentation for ASP.NET Core SignalR can be found in the [Real-time Apps](https://docs.microsoft.com/aspnet/core/signalr/introduction) section of the ASP.NET Core Documentation site.

## TypeScript Version

If you are encountering TypeScript definition issues with SignalR, please ensure you are using the latest version of TypeScript to compile your application. If the issue occurs in the latest TypeScript, please let us know.

When in doubt, check the version of TypeScript referenced by our [package.json](clients/ts/package.json) file. That version is the minimum TypeScript version expected to work with SignalR.

## Packages

You can install the latest released JavaScript client from npm with the following command:

```bash
npm install @microsoft/signalr
```

The `@microsoft/signalr` package (and it's dependencies) require NPM 5.6.0 or higher.

**NOTE:** Previous versions of the SignalR client were named `@aspnet/signalr` or `@aspnet/signalr-client`.

**IMPORTANT:** When using preview builds, you should always ensure you are using the same version of both the JavaScript client and the Server. The version numbers should align as they are produced in the same build process.

The CI build publishes the latest dev version of the JavaScript client to our dev npm registry as @microsoft/signalr. You can install the module as follows:

- Create an .npmrc file with the following line:
  `@microsoft:registry=https://dotnet.myget.org/f/aspnetcore-dev/npm/`
- Run:
  `npm install @microsoft/signalr`

Alternatively, if you don't want to create the .npmrc file run the following commands:
```
npm install @microsoft/signalr --registry https://dotnet.myget.org/f/aspnetcore-dev/npm/
```

We also have a MsgPack protocol library which is installed via:

```bash
npm install @microsoft/signalr-protocol-msgpack
```

## Deploying

Once you've installed the NPM modules, they will be located in the `node_modules/@microsoft/signalr` and `node_modules/@microsoft/signalr-protocol-msgpack` folders. If you are building a NodeJS application or using an ECMAScript module loader/bundler (such as [webpack](https://webpack.js.org)), you can load them directly. If you are building a browser application without using a module bundler, you can find UMD-compatible bundles in the `dist/browser` folder; minified versions are provided as well. Simply copy these to your project as appropriate and use a build task to keep them up-to-date.
