ASP.NET Security
========

AppVeyor: [![AppVeyor](https://ci.appveyor.com/api/projects/status/fujhh8n956v5ohfd/branch/dev?svg=true)](https://ci.appveyor.com/project/aspnetci/Security/branch/dev)

Travis:   [![Travis](https://travis-ci.org/aspnet/Security.svg?branch=dev)](https://travis-ci.org/aspnet/Security)

Contains the security and authorization middlewares for ASP.NET Core.

### Notes

ASP.NET Security will not include Basic Authentication middleware due to its potential insecurity and performance problems. If you host under IIS you can enable it via IIS configuration. If you require Basic Authentication middleware for testing purposes, as a shared secret authentication mechanism for server to server communication, or to use a database as a user source then please look at the samples from [leastprivilege](https://github.com/leastprivilege/BasicAuthentication.AspNet5) or [Kukkimonsuta](https://github.com/Kukkimonsuta/Odachi/tree/master/src/Odachi.AspNetCore.Authentication.Basic).


This project is part of ASP.NET Core. You can find samples, documentation and getting started instructions for ASP.NET Core at the [Home](https://github.com/aspnet/home) repo.
