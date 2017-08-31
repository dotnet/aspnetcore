ASP.NET Core SignalR
========

AppVeyor: [![AppVeyor](https://ci.appveyor.com/api/projects/status/80sq517n7peiaxi9/branch/dev?svg=true)](https://ci.appveyor.com/project/aspnetci/signalr/branch/dev)

Travis:   [![Travis](https://travis-ci.org/aspnet/SignalR.svg?branch=dev)](https://travis-ci.org/aspnet/SignalR)

ASP.NET Core SignalR is a new library for ASP.NET Core developers that makes it incredibly simple to add real-time web functionality to your applications. What is "real-time web" functionality? It's the ability to have your server-side code push content to the connected clients as it happens, in real-time.

You can watch an introductory presentation here - [Introducing ASP.NET Core Sockets](https://vimeo.com/204078084).

This project is part of ASP.NET Core. You can find samples, documentation and getting started instructions for ASP.NET Core at the [Home](https://github.com/aspnet/home) repo.

The JavaScript client is being published to our dev npm registry as @aspnet/signalr-client. The module contains a browserfied version of the client. You can install the module as follows:

- Create an .npmrc file with the following line:
  `@aspnet:registry=https://dotnet.myget.org/f/aspnetcore-ci-dev/npm/`
- Run:
  `npm install @aspnet/signalr-client`

Alternatively, if you don't want to create the .npmrc file run the following commands:
```
npm install msgpack5
npm install @aspnet/signalr-client --registry https://dotnet.myget.org/f/aspnetcore-ci-dev/npm/
```
