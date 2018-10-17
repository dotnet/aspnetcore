WebSockets
================

AppVeyor: [![AppVeyor](https://ci.appveyor.com/api/projects/status/lk5hyg6gki03hdqe/branch/dev?svg=true)](https://ci.appveyor.com/project/aspnetci/WebSockets/branch/dev)

Travis:   [![Travis](https://travis-ci.org/aspnet/WebSockets.svg?branch=dev)](https://travis-ci.org/aspnet/WebSockets)

Contains a managed implementation of the WebSocket protocol, along with server integration components.

This project is part of ASP.NET Core. You can find samples, documentation and getting started instructions for ASP.NET Core at the [Home](https://github.com/aspnet/home) repo.


## System Requirements

This repo has a few special system requirements/prerequisites.

1. Windows IIS Express tests require IIS Express 10 and Windows 8 for WebSockets support
2. HttpListener/ASP.NET 4.6 samples require at least Windows 8
3. Autobahn Test Suite requires special installation see the README.md in test/AutobahnTestApp
