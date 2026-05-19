# ASP.NET Core SignalR

ASP.NET Core SignalR is a library for ASP.NET Core developers that makes it incredibly simple to add real-time web functionality to your applications. What is "real-time web" functionality? It's the ability to have your server-side code push content to the connected clients as it happens, in real-time.

You can watch an introductory presentation here - [ASP.NET Core SignalR: Build 2018](https://www.youtube.com/watch?v=Lws0zOaseIM)

Documentation for ASP.NET Core SignalR can be found in the [Real-time Apps](https://learn.microsoft.com/aspnet/core/signalr/introduction) section of the ASP.NET Core Documentation site.

## Description

This folder contains the server and client implementations for SignalR.

The following contains a description of the sub-directories.

- `server/Core`: Contains the main server-side implementation of the SignalR protocol and the Hubs API.
- `server/SignalR`: Contains extensions that help make using SignalR easier.
- `server/StackExchangeRedis`: Contains a backplane implementation using StackExchange.Redis.
- `server/Specification.Tests`: Contains a set of tests for users to use when verifying custom implementations of SignalR types.
- `common/Http.Connections.Common`: Contains common types used by both the server and .NET client for the HTTP layer.
- `common/Http.Connections`: Contains the HTTP implementation layer for LongPolling, ServerSentEvents, and WebSockets on the server.
- `common/Protocols.Json`: Contains the Json Hub Protocol implementation using System.Text.Json for the server and .NET client.
- `common/Protocols.MessagePack`: Contains the MessagePack Hub Protocol implementation for the server and .NET client.
- `common/Protocols.NewtonsoftJson`: Contains the Json Hub Protocol implementation using Newtonsoft.Json for the server and .NET client.
- `common/SignalR.Common`: Contains common types used by both the server and .NET client for the SignalR layer.
- `clients/csharp`: Contains the client-side implementation of the SignalR protocol in .NET.
- `clients/java`: Contains the client-side implementation of the SignalR protocol in Java.
- `clients/ts`: Contains the client-side implementation of the SignalR protocol in TypeScript/JavaScript.

## Development Setup

### Build

By default, the build script will try to build Java and Typescript projects. If you don't want to include those, you can pass "-NoBuildJava" and "-NoBuildNodeJS" respectively to the build script to skip them. Or "--no-build-java" and "--no-build-nodejs" on MacOS or Linux.

To build this specific project from source, follow the instructions [on building the project](../../docs/BuildFromSource.md#step-3-build-the-repo).

Or for the less detailed explanation, run the following command inside this directory.
```powershell
> ./build.cmd
```

Or on MacOS or Linux:

```bash
$ ./build.sh
```

### Test

This project's tests require having "java" and "npm" on your path.

To run the tests for this project, you can [run the tests on the command line](https://github.com/dotnet/aspnetcore/blob/main/docs/BuildFromSource.md#running-tests-on-command-line) in this directory.

Or for the less detailed explanation, run the following command inside this directory.
```powershell
> ./build.cmd -t
```

Or on MacOS or Linux:

```bash
$ ./build.sh -t
```

You can also run project specific tests by running `dotnet test` in the `tests` directory next to the `src` directory of the project.

To run Java tests go to the `clients/java/signalr` folder and run `gradle test`.

To run TypeScript unit tests go to the `clients/ts` folder and run `npm run test`.
To run TypeScript EndToEnd tests go to the `clients/ts/FunctionalTests` folder and run `npm run test:inner`.

## More Information

For more information, see the [ASP.NET Core README](../../README.md).
