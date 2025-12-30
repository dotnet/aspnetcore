# Http

Http is a collection of HTTP abstractions used in ASP.NET Core, such as `HttpContext`, `HttpRequest`, `HttpResponse` and `RequestDelegate`.

It also includes `Endpoint Routing` and `WebUtilities`.

## Description

The following contains a description of each sub-directory in the `Http` directory.

- [Authentication.Abstractions/](Authentication.Abstractions/): Contains common types used by the various authentication components.
- [Authentication.Core/](Authentication.Core/): Contains common types used by the various authentication middleware components.
- [Headers/](Headers/): Contains headers and header parser implementations.
- [Http/](Http/): Contains default HTTP feature implementations.
- [Http.Abstractions/](Http.Abstractions/): Contains HTTP object model for HTTP requests and responses and also common extension methods for registering middleware in an IApplicationBuilder.
- [Http.Extensions/](Http.Extensions/): Contains common extension methods for HTTP abstractions, HTTP headers, HTTP request/response, and session state.
- [Http.Features/](Http.Features/): Contains HTTP feature interface definitions.
- [Http.Results/](Http.Results/): Contains implementations of `IResult` and related types.
- [Metadata/](Metadata/): Contains ASP.NET Core metadata.
- [Owin/](Owin/): Contains components for running OWIN middleware in an ASP.NET Core application, and to run ASP.NET Core middleware in an OWIN application.
- [Routing/](Routing/): Contains middleware for routing requests to application logic and for generating links.
- [Routing.Abstractions/](Routing.Abstractions/): Contains abstractions for routing requests to application logic and for generating links.
- [WebUtilities/](WebUtilities/): Contains utilities, for working with forms, multipart messages, and query strings.
- [samples/](samples/): Contains samples.

## Development Setup

To run the code generation using [T4 Text Templates](https://learn.microsoft.com/visualstudio/modeling/code-generation-and-t4-text-templates), you can use an IDE that supports it (eg. Visual Studio or JetBrains Rider) or install the cross-platform open-source dotnet tool [Mono/T4](https://github.com/mono/t4).

```powershell
> dotnet tool install -g dotnet-t4
> t4 Http.Results\ResultsCache.StatusCodes.tt
```

### Build

To build this specific project from source, follow the instructions [on building the project](../../docs/BuildFromSource.md#step-3-build-the-repo).

### Test

To run the tests for this project, [run the tests on the command line](../../docs/BuildFromSource.md#running-tests-on-command-line) in this directory.

## More Information

For more information, see the [ASP.NET Core README](../../README.md).
