# ASP.NET Core issue area ownership

Choose exactly one area from this reference. API names, types, source paths, packages, and
the behavior being reported are stronger signals than broad product terms.

## `area-networking`

Kestrel, HTTP.sys, IIS server integration, HTTP/2, HTTP/3, QUIC, WebSockets, HTTP
abstractions, connection management, and `System.IO.Pipelines`.

- Code: `src/Servers/`, core `src/Http/` abstractions/features/headers/web utilities,
  `src/Middleware/WebSockets/`, `src/Hosting/Server.Abstractions/`, `src/HttpClientFactory/`
- Signals: `KestrelServerOptions`, `ListenOptions`, `HttpSysOptions`, `ConnectionContext`,
  `PipeReader`, `PipeWriter`, `IDuplexPipe`, TLS, port binding, protocol errors

## `area-blazor`

Blazor and Razor Components, WebAssembly, render modes, circuits, component forms,
QuickGrid, custom elements, and JS interop.

- Code: `src/Components/` (Components, Web, WebAssembly, Server, WebView, Endpoints),
  `src/Components/Forms/`, `src/Components/QuickGrid/`,
  `src/Components/CustomElements/`, plus `src/JSInterop/`
- Signals: `ComponentBase`, `.razor`, `RenderFragment`, `EventCallback`, `IJSRuntime`,
  `InteractiveServer`, `InteractiveWebAssembly`, `InteractiveAuto`, circuits, prerendering

Identity template markup implemented as `.razor` files remains `area-identity` unless the
defect is in Blazor component or runtime behavior.

## `area-auth`

Authentication and authorization, cookie authentication, bearer tokens, JWT, OAuth,
OpenID Connect, schemes, claims, policies, challenge, forbid, sign-in, and sign-out.

- Code: `src/Security/Authentication/`, `src/Security/Authorization/`,
  `src/Http/Authentication.*`, `src/Components/Authorization/`
- Signals: `AddAuthentication`, `AddAuthorization`, `JwtBearerOptions`,
  `OpenIdConnectOptions`, `IAuthorizationService`, `[Authorize]`

## `area-identity`

ASP.NET Core Identity, user and role management, Identity providers, Identity UI, and
Identity scaffolding or template markup.

- Code: `src/Identity/`
- Signals: `UserManager<TUser>`, `SignInManager<TUser>`, `IdentityUser`,
  `MapIdentityApi<TUser>`, passwords, 2FA, external login, passkeys

Identity UI scaffolding and generated or project-template `Components/Account` pages
belong here even though they use `.razor`. Use `area-blazor` only for a component/runtime
defect rather than the Identity template consuming it.

## `area-mvc`

MVC controllers and actions, model binding, formatters, filters, content negotiation,
ApiExplorer, and Razor Pages page-model logic.

- Code: `src/Mvc/`, `src/Html.Abstractions/`
- Signals: `Controller`, `[ApiController]`, action filters, `IInputFormatter`,
  `ActionResult`, `PageModel`, model binding and validation

## `area-minimal`

Minimal APIs, route-handler binding, endpoint filters, HTTP results, problem details, and
runtime OpenAPI document generation.

- Code: `src/Http/Http.Results/`, `src/OpenApi/` runtime services
- Signals: `MapGet`, `MapPost`, `Results.*`, `TypedResults.*`, `IEndpointFilter`,
  `OpenApiSchemaService`, `Microsoft.AspNetCore.OpenApi.*`

The `dotnet-openapi` CLI and build-time generation belong to `area-commandlinetools`.

## `area-middleware`

CORS, diagnostics, static files and assets, session, response compression and caching,
output caching, rate limiting, HTTP logging, forwarded headers, URL rewrite, and other
request-pipeline middleware.

- Code: `src/Middleware/`, `src/StaticAssets/`, `src/Caching/`
- Signals: `UseCors`, `UseStaticFiles`, `UseSession`, `UseOutputCaching`,
  `UseRateLimiter`, `UseForwardedHeaders`, middleware pipeline behavior

Pipe-level I/O and connection handling remain `area-networking`.

## `area-signalr`

SignalR clients and servers, hubs, hub protocols, transports, reconnect, groups, streaming,
scale-out, and Redis backplanes.

- Code: `src/SignalR/`
- Signals: `Hub`, `HubConnection`, `IHubContext<T>`, real-time messaging, MessagePack,
  Server-Sent Events, long polling

SignalR remains this area when it uses WebSockets.

## `area-routing`

Endpoint routing, route matching and patterns, route constraints, URL and link generation,
route values, and endpoint metadata.

- Code: `src/Http/Routing/`, `src/Http/Routing.Abstractions/`, `src/Http/Metadata/`
- Signals: `LinkGenerator`, `EndpointDataSource`, `IRouteConstraint`, route templates,
  catch-all routes

## `area-dataprotection`

Data Protection APIs, protect/unprotect operations, key management, key rings, rotation,
XML repositories, purpose strings, and key storage providers.

- Code: `src/DataProtection/`
- Signals: `IDataProtector`, `IDataProtectionProvider`, `IKeyManager`,
  `AddDataProtection`, `PersistKeysTo*`

## `area-hosting`

Host and web-host builders, `WebApplication`, startup, host configuration, server
addresses, hosting startups, Windows Services, and Azure App Service hosting integration.

- Code: `src/Hosting/`, `src/DefaultBuilder/`, hosting portions of `src/Azure/`
- Signals: `WebApplicationBuilder`, `IWebHostBuilder`, `UseStartup<T>`,
  `ASPNETCORE_URLS`, `launchSettings.json`

## `area-commandlinetools`

ASP.NET Core CLI tools and their packaging: dev certificates, user secrets, user JWTs,
SQL cache, `dotnet-openapi`, build-time API-description tooling, template infrastructure,
and installers.

- Code: `src/Tools/`, `src/Tools/Microsoft.dotnet-openapi/`,
  `src/ProjectTemplates/` template infrastructure, `src/Installers/`
- Signals: `dotnet dev-certs`, `dotnet user-secrets`, `dotnet user-jwts`,
  `dotnet-openapi`, template engine, packaging, installation, scaffolding infrastructure

For content or assets emitted by a web template, choose the area owning the generated
output. Shared layouts, CSS, JavaScript, and UI libraries used across Razor Pages, MVC,
and Blazor templates belong to `area-ui-rendering`. Blazor-specific generated behavior
belongs to `area-blazor`; Identity scaffolding or template markup belongs to
`area-identity`.

## `area-grpc`

ASP.NET Core gRPC wire-up, JSON transcoding, gRPC Swagger, gRPC-Web integration, and
interop. The main gRPC implementation is owned by `grpc/grpc-dotnet`.

- Code: `src/Grpc/`
- Signals: `AddGrpc`, `MapGrpcService<T>`, `AddGrpcJsonTranscoding`,
  `AddGrpcSwagger`, `.proto`, interceptors, unary or streaming calls

## `area-healthchecks`

Health-check endpoints, services, publishers, liveness and readiness, and health status.

- Code: `src/HealthChecks/`, `src/Middleware/HealthChecks/`
- Signals: `IHealthCheck`, `IHealthCheckPublisher`, `HealthCheckService`,
  `AddHealthChecks`, `MapHealthChecks`

## `area-security`

Security hardening owned by ASP.NET Core, antiforgery, cookie policy, CSRF/XSRF
protection, SameSite and secure-cookie policy, and HTTPS enforcement policy.

- Code: `src/Antiforgery/`, `src/Security/CookiePolicy/`
- Signals: `IAntiforgery`, `AntiforgeryOptions`, `RequireAntiforgeryTokenAttribute`,
  `CookiePolicyOptions`

Authentication and authorization remain `area-auth`; Identity user management remains
`area-identity`.

## `area-ui-rendering`

MVC Views, Razor Pages rendering and templates, Razor syntax and compilation, TagHelpers,
HTML helpers, view components, shared web-template layouts, and shared rendered UI assets.

- Code: `src/Razor/`
- Signals: `.cshtml`, `ViewResult`, `IHtmlHelper`, `TagHelper`, `ViewComponent`, layouts,
  partial views, Bootstrap/CSS/JavaScript shared by web templates

Identity template markup belongs to `area-identity`.

## `area-perf`

Performance regressions, performance optimization, benchmarks, and performance
infrastructure across the repository.

- Signals: throughput, latency, RPS, allocation, `BenchmarkDotNet`, Crank, Bombardier
- Code: area-specific `perf/` or `benchmarks/` directories

## `area-infrastructure`

Repository build system, CI/CD, shared framework and targeting-pack construction, source
build plumbing, test infrastructure, packaging, and installers.

- Code: `eng/`, `src/Framework/`, `src/BuildAfterTargetingPack/`, `src/Testing/`,
  `src/Installers/`, `*.props`, `*.targets`
- Signals: MSBuild, Arcade, CI pipelines, source build, shared framework, targeting packs

## `area-unified-build`

The `dotnet/dotnet` VMR, unified build, and source-build integration specific to unified
build.

- Code: `src/SiteExtensions/` where it participates in unified build
- Signals: VMR, `dotnet/dotnet`, unified build, source-build integration

## Priority rules

- Pipe-level I/O, connections, Kestrel configuration, protocol errors, and TLS:
  `area-networking`, not `area-middleware`.
- `Hub` or `HubConnection`: `area-signalr`, even over WebSockets.
- `ComponentBase`, `.razor`, render modes, or JS interop: `area-blazor`, except for
  Identity scaffolding/template markup.
- `.cshtml`, TagHelpers, view compilation, or `ViewResult`: `area-ui-rendering`.
- `MapGet`, `MapPost`, `Results.*`, or endpoint filters: `area-minimal`.
- `[ApiController]`, controllers, or action filters: `area-mvc`.
- `[Authorize]`, authentication schemes, JWT, or OAuth: `area-auth`.
- `UserManager`, `SignInManager`, or Identity scaffolding/template markup:
  `area-identity`.
- Shared template layouts, Bootstrap/CSS/JavaScript, and rendered UI assets:
  `area-ui-rendering`, not `area-commandlinetools`.
- Route templates, constraints, or `LinkGenerator`: `area-routing`.
- `IDataProtector` or key management: `area-dataprotection`.
- Build failures, `eng/`, packages, or CI: `area-infrastructure`.
- Runtime OpenAPI services: `area-minimal`; `dotnet-openapi` and build-time generation:
  `area-commandlinetools`.
