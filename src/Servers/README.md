# ASP.NET Core Servers

ASP.NET Core Servers contains all servers that can be used in ASP.NET Core by default. These include:

- [Kestrel](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel), our cross-platform web server that is included and enabled by default in ASP.NET Core.
- [IIS Server/ASP.NET Core Module](https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/iis/), a flexible secure managed Web Server to be hosted with IIS on Windows.
- [HTTP.sys](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/servers/httpsys), a web server that uses the [Windows Hypertext Transfer Protocol Stack](https://docs.microsoft.com/en-us/iis/get-started/introduction-to-iis/introduction-to-iis-architecture#hypertext-transfer-protocol-stack-httpsys).

## Description

This folder contains all servers implementations related abstractions for ASP.NET Core.

- `Kestrel`: Contains the implementation of the Kestrel Web Server.
- `IIS`: Cotnains all code for the IIS Web Server and ASP.NET Core Module.
- `HttpSys`: Contains all code for the HTTP.sys Web Server.
- `Connections.Abstractions`: A set of abstractions for creating and using Connections; used in Kestrel.

## More Information

For more information, see the [ASP.NET Core README](../../README.md).
