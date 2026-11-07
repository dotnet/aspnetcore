# Microsoft.AspNetCore.Components.Mcp

This package renders Razor component forms and handles their submissions through
standard MCP Apps. Static SSR is the rendering and lifetime constraint, not a
promise of feature parity with HTTP SSR.

Each opening call, GET, or form submission owns a fresh async invocation scope.
Long-lived SDK tools and filters are catalog adapters: they resolve a fresh
operation invoker and host from this scope rather than retaining an execution
pipeline. Successful existing-tool parameter projection also runs inside the
opening invocation scope. Components, form mapping, navigation, and logical
request context use that operation's services.

The host waits for initial rendering and the named form
callback to finish, returns completed HTML, then disposes the renderer before
its services. The call completes only after async scope disposal finishes.
Internal navigation is resolved server-side to final HTML; each follow-on
logical GET renders with fresh scoped services without HTTP redirect hops.
The host does not retain a circuit, component instance, event callback,
or `EditContext` for the next operation.

## Setup

Reference the package from an ASP.NET Core application and use the C# MCP SDK:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMcpServer()
    .WithRazorComponents<FormApplication>(options =>
    {
        options.Name = "show_form";
        options.BaseUri = new Uri("https://example.com/");
    })
    .WithHttpTransport();

var app = builder.Build();
app.MapStaticAssets();
app.MapMcp("/mcp");
app.Run();
```

`BaseUri` is the application's logical routing and asset base, not the MCP
endpoint or iframe URL. It must be absolute and end in `/`. The application declares the packaged browser flavor explicitly at
`blazor.mcp.js` beneath this base.
The host must serve that asset and permit the declared resource origin.
There is no CDN or runtime Node dependency.

The authored root owns its document, loading phase, and runtime script:

```razor
@using Microsoft.AspNetCore.Components.Mcp
@inject NavigationManager Navigation

<!DOCTYPE html>
<html>
<head><base href="@Navigation.BaseUri" /></head>
<body>
    @if (Context.IsShellRendering)
    {
        <p>Loading application...</p>
    }
    else
    {
        <Routes />
    }
    <script src="blazor.mcp.js"></script>
</body>
</html>

@code {
    [CascadingParameter]
    private McpComponentRequestContext Context { get; set; } = default!;
}
```

Standard Apps hosts may read the resource before the opening tool completes.
`resources/read` renders this actual root in a fresh scope with
`IsShellRendering` set before component parameters and initialization. The root
must independently render its complete loading document without constructing
data-dependent content. Root initialization must honor this phase too; required
initial-data validation remains required in full rendering. Resource reads
never invoke the opening tool or its parameter factory and never allocate a view.

The authored script executes naturally when the host loads the resource HTML.
There is no injected runtime, extracted script tag, or fallback if it is omitted.
Declare the script outside the phase conditional. The browser retains its
already-executed script representation during initial/full document updates,
including body replacement mode, without fetching or initializing it again.
The host supplies the same scoped request context through DI and cascading
parameters; its phase is fixed for that scope and is never taken from tool data.
In ordinary HTTP scopes the context is inactive and `IsShellRendering` is false.
An application sharing its root with HTTP hosting can use `Context.IsActive` to
choose its authored `blazor.mcp.js` versus `blazor.web.js` script.

The package's static web asset base path is `/`. With an HTTP PathBase such as
`/app`, use the matching logical `BaseUri` ending in `/app/`; the relative script
then resolves to `/app/blazor.mcp.js`. The old `_content` route is not an alias.

Register `WithRazorComponents` **after** other component hosting services,
including `AddInteractiveServerComponents`. Ordinary HTTP/circuit scopes keep
their existing navigation, authentication, and form services; an MCP rendering
scope selects its own implementations.

The SDK owns protocol and transport. Default Streamable HTTP is the validated
hosting path. Compatibility with other SDK transports is not claimed.

## Forms

Use ordinary `@formname`, `EditForm.FormName`, `[SupplyParameterFromForm]`,
`FormMappingScope`, binding, and server validation. No action registry,
component type name, or form-model registration is sent by the browser.

```razor
<EditForm Model="Input" FormName="save" OnValidSubmit="Save">
    <DataAnnotationsValidator />
    <ValidationSummary />
    <InputText @bind-Value="Input.Name" />
    <button type="submit">Save</button>
</EditForm>
<p>@message</p>

@code {
    private string? message;

    [SupplyParameterFromForm]
    private InputModel Input { get; set; } = default!;

    protected override void OnInitialized()
    {
        Input ??= new();
    }

    private void Save()
    {
        message = $"Saved {Input.Name}.";
    }

    public sealed class InputModel
    {
        [System.ComponentModel.DataAnnotations.Required]
        public string Name { get; set; } = "";
    }
}
```

Form values are supplied before component initialization. The new tree must
finish its initial asynchronous work before its scope-qualified submit handler
is resolved. Missing handlers are request errors; ambiguous application handler
names are application errors.

Native successful controls and the actual submitter determine the payload.
GET creates a query string. POST supports URL-encoded and multipart forms,
including `formaction`, `formmethod`, and `formenctype` overrides.
URL-encoded file controls submit filenames; multipart controls submit file
contents, with a total 32 MiB file limit per submission. Empty unselected
multipart file controls do not create an upload. `text/plain` and non-self form
targets are not supported. The containing Apps host must allow native HTML
forms in its iframe sandbox.

The package uses the Components form mapper, not MVC model binding. Existing
`RazorComponentsServiceOptions` form-mapping limits apply. Server
`EditContext`/DataAnnotations validation and native HTML constraint validation
are supported; automatic Blazor client-validation runtime parity is not claimed.

## Existing SDK opening tools and typed initial data

An application can associate an already-registered SDK tool by name instead of
adding another opening tool:

```csharp
builder.Services.AddMcpServer()
    .WithTools<OrderTools>()
    .WithRazorComponents<OrderApplication>(
        "show_order",
        initialParametersFactory: result => new Dictionary<string, object?>
        {
            [nameof(OrderApplication.InitialOrder)] =
                result.StructuredContent!.Value.Deserialize<Order>(
                    ModelContextProtocol.McpJsonUtilities.DefaultOptions),
        },
        configure: options =>
        {
            options.BaseUri = new Uri("https://example.com/");
        })
    .WithHttpTransport();
```

The original SDK tool, schema, metadata, filters, business content, structured
content, and errors remain in the SDK catalog. A successful opening result is
copied before app metadata is appended. The parameter factory runs only after
success. Parameters must be serializable server-produced data; the host saves a
JSON snapshot and reconstructs fresh values for each subsequent GET or POST.
Neither dispatch nor resource reads rerun the opening business tool.

The app integration is inserted outside ordinary SDK call filters during options
postconfiguration. Filters registered through normal SDK configuration finish
their response enrichment or error transformation before the parameter factory
and rendering run. A result transformed into an error does not create an app
view. Custom later postconfiguration that explicitly changes filter priority can
change this ordering; the integration does not override such priority changes.

## Context and supported boundaries

`McpComponentRequestContext` exposes the actual request principal, logical
location/base URI, and cooperative cancellation token. `IsActive` is false in
ordinary HTTP scopes; other properties require an active MCP operation.
An `IHttpContextAccessor`, if present, describes the transport request, not a
logical page request. The package does not manufacture or cascade a page
`HttpContext`, issue page cookies, or apply page response headers to MCP framing.

Views hold only an opaque association, principal association, and serialized
initial data, plus cancellation/completion bookkeeping while an operation runs.
This cross-request continuation bookkeeping is separate from the scoped
execution pipeline; fresh invocation scopes do not eliminate it. Views expire
20 minutes after creation. Default modern SDK HTTP
requests do not have a stable `McpServer`/session identity. For anonymous
requests, possession of the opaque view capability is the association, not
physical-client identity. Explicit teardown closes the association, requests
cancellation, and waits for admitted operations to release their resources.
This includes completing async disposal of the invocation's services.
Cancellation cannot forcibly terminate component code that ignores its token;
the host does not dispose services while that code is still using them.
Submitted business mutations are never automatically retried.

`RazorComponentMcpOptions.PreserveDom` controls matching/permanent DOM
preservation in the rendered body; it defaults to `true`. The document head is
synchronized in either mode, keeping the application base URI continuously
available. `LogLevel` controls messages emitted by the
MCP browser flavor and defaults to `Information`. `ConfigureBrowser` is a no-op
in this host. `CacheView` renders its children on every operation without
caching.

There is no streaming rendering, session/TempData integration, arbitrary remote
`@onclick` handling, interactive render mode, JavaScript interop runtime, or
general HTTP SSR feature-parity contract. In-app GET/redirect handling supports
the form workflow; broad navigation/history/focus/enhanced-load/initializer
parity is outside this package's scope. Persistent business state belongs in
application services or storage, not component fields.

## Repository validation

Activate the repository SDK before invoking `dotnet`. On Windows:

```powershell
. .\activate.ps1
npm run build --workspace @microsoft/aspnetcore-components-mcp
npm run build --workspace @microsoft/aspnetcore-components-mcp-test-consumer
dotnet test src\Components\Mcp\test\Microsoft.AspNetCore.Components.Mcp.Tests.csproj -p:UseIisNativeAssets=false
dotnet build src\Components\Mcp\testassets\Components.McpTestApp.E2E.Tests\Components.McpTestApp.E2E.Tests.csproj -p:UseIisNativeAssets=false
dotnet run --project src\Components\Mcp\testassets\Components.McpTestApp.E2E.Tests\Components.McpTestApp.E2E.Tests.csproj --no-build -- --minimum-expected-tests 40 --report-trx
```

The dedicated E2E project follows Components.AI's test approach: MSTest and
Microsoft.Testing.Platform, assembly-owned Components.Testing `ServerFactory`,
and C# Playwright `BrowserTest` classes. Each browser case gets an isolated
context; the fixture launches the published test application. Install the pinned
Playwright browser with the generated output's `playwright.ps1 install
chromium-headless-shell` if the test reports that the executable is missing.

The standard Apps consumer uses the C# SDK over actual Streamable
HTTP and treats the private rendering data as opaque standard tool/resource
content. It requires no model, entitlement, Components.AI, or Node host process.
Playwright's browser automation driver is test tooling, not an MCP server host.
