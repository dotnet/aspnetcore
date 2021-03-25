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
- [Metadata/](Metadata/): Contains ASP.NET Core metadata.
- [Owin/](Owin/): Contains components for running OWIN middleware in an ASP.NET Core application, and to run ASP.NET Core middleware in an OWIN application.
- [Routing/](Routing/): Contains middleware for routing requests to application logic and for generating links.
- [Routing.Abstractions/](Routing.Abstractions/): Contains abstractions for routing requests to application logic and for generating links.
- [WebUtilities/](WebUtilities/): Contains utilities, for working with forms, multipart messages, and query strings.
- [samples/](samples/): Contains samples.

## Development Setup

### Build

To build this specific project from source, follow the instructions [on building a subset of the code](../../docs/BuildFromSource.md#building-a-subset-of-the-code).

### Test

To run the tests for this project, [run the tests on the command line](../../docs/BuildFromSource.md#running-tests-on-command-line) in this directory.

## More Information

For more information, see the [ASP.NET Core README](../../README.md).
