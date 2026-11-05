# Components AI MCP Apps

This Razor class library hosts standard MCP Apps using the official
`@modelcontextprotocol/ext-apps` 2.0.3 `AppBridge`. It uses
`ModelContextProtocol.Core` SDK tools, existing AI block handlers/renderers,
and an application-defined `McpContentHost`. All iframe, interop, sandbox,
and MCP bridge functionality is in this package; Components.AI is unchanged.
It does not depend on a Blazor
app renderer, private renderer payloads, or a particular model provider.

## Setup

App recognition and call/result pairing use the ordinary
`FunctionInvocationContentBlock` pipeline. There is no MCP-specific block type,
mapping handler, or handler-registration step.

The application owns the SDK client and keeps it alive for its views. Tools run
through the ordinary SDK/function-invocation path, without wrappers, result
stripping, a separate invocation lifecycle or a conversation-level result registry.

Advertise support before creating each SDK connection. Using a client that
has already connected does not retroactively negotiate the Apps extension.

```csharp
using Microsoft.AspNetCore.Components.AI.Mcp;
using ModelContextProtocol.Client;

var clientOptions = new McpClientOptions();
clientOptions.EnableMcpApps();
var client = await McpClient.CreateAsync(transport, clientOptions);
```

```csharp
using Microsoft.AspNetCore.Components.AI.Mcp;
using Microsoft.Extensions.AI;
using System.Linq;

var sdkTools = (await client.ListToolsAsync()).ToArray();
var modelTools = sdkTools.Where(IsModelVisible).Cast<AITool>().ToList();
var agent = new UIAgent(functionInvokingChatClient, options =>
{
    options.ChatOptions = new ChatOptions { Tools = modelTools };
});

bool IsModelVisible(McpClientTool tool)
{
    var visibility = tool.ProtocolTool.Meta?["ui"]?["visibility"];
    return visibility is null ||
        visibility.AsArray().Any(value => value?.GetValue<string>() == "model");
}
```

`functionInvokingChatClient` must include `FunctionInvokingChatClient`.
Use SDK `WithName` aliases when binding multiple servers with colliding tool
names. Connections are identified by their `McpClient` instance, not an
application-assigned ID or server name. The supplied tools must belong to the supplied client;
the SDK does not expose an owning-client property that this library can check.

Inside the existing `MessageList`, register:

```razor
<BlockRenderer TBlock="FunctionInvocationContentBlock"
               When="IsMcpApp"
               Context="block">
    <McpBlock THost="Host" @key="block.Id"
              Client="client" Tools="sdkTools"
              Block="block" />
</BlockRenderer>
```

The predicate looks up the model-facing name, including any SDK `WithName`
alias, in that same cached snapshot:

```csharp
bool IsMcpApp(FunctionInvocationContentBlock block)
{
    var metadata = sdkTools.SingleOrDefault(tool => tool.Name == block.ToolName)?.ProtocolTool.Meta;
    return metadata?["ui"]?["resourceUri"] is not null ||
        metadata?["ui/resourceUri"] is not null;
}
```

`McpBlock` cascades its explicit `Client` internally, only around its `THost`.
The base content host receives the client through a cascading parameter and the
SDK snapshot through `Tools`. Application markup does not cascade a client around
the conversation. Rendering does not rediscover tools; fresh server definitions
would lose application-assigned model aliases.

Define the application component `Host.razor`. It inherits the MCP-specific
base and owns its iframe URL, attributes, layout, and local controls:

```razor
@inherits McpContentHost

@if (!IsClosed)
{
    <button @onclick="async () => await CloseAsync()">Close</button>
    <iframe @ref="Frame"
            src="https://localhost:5002/mcp-sandbox"
            title="Application"
            sandbox="allow-scripts allow-same-origin"
            referrerpolicy="no-referrer"
            style="display:block;width:100%;height:600px;border:0">
    </iframe>
}

```

Controls and presentation state belong to this particular `Host` instance.
`McpBlock` does not expose forwarding methods or need a component reference.
Closing a host cancels and tears down its own app; `IsClosed` lets the application
stop rendering that iframe without affecting other renderings of the same block.
There is no required context object or global notification mechanism.

Static/prerendered `McpBlock` renders no host or iframe and performs no interop.
After interactive activation it imports the shared host module before rendering
`THost`. The module captures early proxy readiness by source window and origin;
attachment never navigates or replaces the application's iframe.
The base fetches the resource and uses `AppBridge` directly. There is no public
options bag, browser-policy model, generic adapter, or second protocol layer.
The protected `JS`, `OnReadyAsync`, and `OnClosedAsync` members support additional
browser integration and lifecycle hooks. Optional theme controls can be enabled
in `OnReadyAsync`; the primary hosting markup needs no readiness flag.

Existing model approval remains available: wrap the model-visible SDK functions
with the existing `ApprovalRequiredAIFunction` mechanism. App-originated
calls do not create approval UI, call the model, append conversation
history, or modify `AgentContext`.

## Static sandbox in the same application

`Sandbox.razor` customizes only the body of a standalone static-SSR document:

```razor
@inherits McpSandboxBase

<iframe id="mcp-app"
        title="@FrameTitle"
        sandbox="allow-scripts"
        referrerpolicy="no-referrer">
</iframe>

@code {
    [Parameter]
    public string FrameTitle { get; set; } = "Application content";
}
```

The concrete base supplies the document shell, sizing, and shared module bootstrap.
The Razor-generated body runs in a render-tree region. It must contain exactly one
iframe with ID `mcp-app`; missing or invalid target markup fails explicitly.
Do not add `@page`, a layout, scripts, or an interactive render mode.

Configure two listeners in one application process, with endpoint host restrictions:

```csharp
using Microsoft.AspNetCore.Builder;

var builder = WebApplication.CreateBuilder(args);
var hostOrigin = new Uri("https://localhost:5001");
var sandboxOrigin = new Uri("https://localhost:5002");
builder.WebHost.UseUrls(hostOrigin.AbsoluteUri, sandboxOrigin.AbsoluteUri);
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
var app = builder.Build();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapMcpAppSandbox<Sandbox>("/mcp-sandbox", hostOrigin, sandboxOrigin,
    parameters: new Dictionary<string, object?>
    {
        [nameof(Sandbox.FrameTitle)] = "Application content"
    });
app.MapRazorComponents<App>().AddInteractiveServerRenderMode()
    .RequireHost("localhost:5001");
app.Run();
```

`MapMcpAppSandbox` creates a nonstreaming `RazorComponentResult` with HTML content
type and `Cache-Control: no-store`, without using the app router or layout.
It restricts requests to the configured sandbox scheme, host, and effective port.
It snapshots additional parameters and supplies the reserved `HostOrigin`
parameter itself. It derives effective origins from the supplied deployment
addresses and enforces the protocol's distinct-origin requirement without imposing
additional address-format restrictions. Developers provide the correct deployment
values; the sandbox component renders the supplied parent origin without validating
or normalizing it. The helper does not configure
listeners or TLS certificates; a suitable development certificate is required for
the HTTPS example.

Use `MapMcpAppSandbox<McpSandboxBase>` instead when the default body is sufficient.
No origin-only subclass is needed. The same application serves static assets on
both listeners. `hostOrigin` is the exact browser-visible parent origin, not an
internal address inferred from the request.

The official adapter sources are under `src/Interop`. The repository npm
workspace bundles `mcp-host.js` and `mcp-sandbox.js` to
`Interop/dist/{Debug,Release}`. The RCL discovers, packs, and publishes these
as `_content/Microsoft.AspNetCore.Components.AI.Mcp/` assets. Do not copy
generated bundles into source `wwwroot` or ship unbundled bare imports.

## Policy and resource selection

Tool inventory is the application-supplied snapshot of discovered `McpClientTool`
definitions, not a second catalog. `_meta.ui.visibility` defaults to both
`model` and `app`. Model aliases do not change app-facing original server
names. App calls can reach only app-visible tools on the originating
connection. The host checks SDK JSON-RPC request envelopes, required tool names/resource URIs,
JSON-object arguments, and routing. Missing
or wrong-typed request parameters return the standard invalid-params error.
The owning MCP server validates each tool's JSON Schema and business
arguments. The host does not interpret or restrict tool schemas, including
references, conditional/dependent schemas, or standard numeric forms.

UI resources use `_meta.ui.resourceUri` (with the legacy `ui/resourceUri`
fallback), an absolute `ui://` URI, and MIME type
`text/html;profile=mcp-app`. One matching HTML content item must be returned;
additional nonmatching items are ignored and multiple matching items fail.
Both text and strict UTF-8 blob resources are supported. A UI resource need
not be listed. Content-item `_meta.ui` takes precedence over resource-listing
metadata, which supplies the fallback.

Resource `ui.csp` and `ui.permissions` declarations pass through as JSON, without
a duplicate C# policy model or experimental Apps dependency. Sandbox JS applies
the document CSP before loading the existing inner iframe. Resource/network
origins must be declared; omitted or empty network/resource/frame lists grant
none. Base addresses default to the same origin. Connection declarations also
support WebSocket origins; resource declarations support wildcard subdomains.
Malformed declarations fail explicitly.

The SDK's `buildAllowAttribute` translates optional permission declarations only
when the application has not fixed the inner iframe's `allow` attribute.
Application-owned inner and outer attributes remain unchanged; declarations do
not override those restrictions or browser policy. `domain` metadata does not
provision an origin, and `prefersBorder` remains a host presentation preference.

Brokered resource access remains limited to the originating UI resource, including
resource listing. Browser network declarations do not expand SDK resource access.
Custom request-policy callbacks are not part of this simplified API.

## SDK results, restoration, and lifetime

The normal function-invocation block is the source of truth for the call, arguments,
identity, result and invocation exception. SDK return values reach the chat pipeline
unchanged. No metadata or structured content is stripped and no separate app-only
result is retained.

The iframe initializes and receives tool input while the result is still pending.
Once ready, the host rechecks the latest `Block.Result` and pushes it through the
official bridge, once per iframe session. Results available before, during or after
initialization use the same path; rerendering does not duplicate delivery.

At this delivery boundary, complete MCP JSON envelopes are preserved. Single or
multiple converted AI content items use their SDK-preserved individual MCP content
blocks where available. Unsupported result shapes generate a diagnostic and an
app cancellation notification, not an empty success result. Complete MCP error
results keep `isError`; invocation exceptions produce cancellation notifications.

The ambient `AgentContext` supplies actual turn failure/cancellation state when
no result is produced. Cancelling a turn clears its response blocks; the host
observes that lifecycle rather than assuming a cancelled result exists. Outside
that conversation scope, the application owns pending-block/view lifetime.

Restoration delivers whatever supported result is already in the ordinary block.
There is no registry lookup and no tool replay to recover data. The SDK's
informational invocation exception is not serialized by its standard serializer.
Disposing a view does not dispose the application-owned client.

The library does not impose document/message byte limits, pending-operation caps,
or initialization, operation, and teardown deadlines. Caller/view-lifetime
cancellation remains in effect; SDK and platform defaults are unchanged.
Metadata interpretation and result preparation are private host implementation,
using SDK metadata/content types directly. The document is a private nested struct
carrying HTML and optional JSON declarations, not another application-facing concept.
The cancellation adapter uses Core SDK JSON-RPC types and serialization,
not custom request/response/error models. Cancellation never promises to
undo a completed server mutation. No automatic retry follows an uncertain
transport outcome.

## Capability boundary

The baseline supports standard initialization, ping, tool input/result and
cancellation, inline display, fixed host-owned sizing, host theme/context updates,
teardown, snapshot app-visible tool listing/calls, and permitted resource
reads/listing. Resource listing aggregates the SDK's pages, filters to the selected
app resource, and returns a complete snapshot without a cursor.
The protected `UpdateHostContextAsync` helper sends MCP Apps host-context
updates through `AppBridge.setHostContext`. Pass a JSON object containing fields
such as `theme` or `locale`; fields omitted from the update are preserved.
The example keeps theme state in `Host.razor` and sends `{ "theme": "dark" }`
or `{ "theme": "light" }`. This is a protocol payload, not a required
observable context object or cascading-state abstraction.
Host context reports the actual fixed iframe width and height and tracks layout
changes. App size notifications do not overwrite application sizing or impose a
hidden height cap. Flexible app-driven sizing is outside this baseline.

Fullscreen/PiP, resource templates/subscriptions/change
notifications, prompts, sampling, links, downloads, app message insertion,
and model-context updates are not advertised or implemented. Unsupported
requests fail explicitly. Server inventory refresh/reconnect requires a
new client/tool snapshot and a remounted view; no list-change support is claimed.

Interactive Server (including prerender-to-interactive activation) is the
initial supported deployment target. Static rendering cannot open a live
pipe. WebAssembly/Auto deployment and verification of cross-process restoration
are intentionally outside this baseline.

## Repository validation

Follow the [repository build prerequisites](../../../docs/BuildFromSource.md).
The deterministic browser coverage follows the existing Components.AI
Components.Testing/MSTest/C# Playwright pattern in an AI.Mcp-owned test project.
It starts real C# MCP servers,
a consumer listening on both the chat and sandbox origins, without an external
model or account or a separate proxy process.
The consumer renders an application-defined `Host : McpContentHost`.
Its `Home.razor` initializes the SDK clients and agent directly and owns their
disposal. There is no separate conversation-session service or MCP coordinator.
Multi-app coverage sends a user message that makes the scripted model emit two
distinct UI tool calls through the normal function-invocation pipeline. Each
resulting content block renders once; the page has no copy count or duplication mode.
Coverage also exercises ordinary non-app invocations, aliased client scopes,
single/multiple SDK-converted contents, original results delayed until an app save,
MCP error results, original-call transport failure, turn cancellation without a
result block, and exactly-once app delivery across rerenders. Additional cases
exercise payloads above the former 2 MiB limit, 33 concurrent app requests,
and initialization lasting beyond the former 30-second deadline. The large-payload
test host explicitly adjusts its SignalR receive setting so a platform default
does not hide the library's behavior; the library itself does not change it.

From the repository root on Windows:

```powershell
npm run build --workspace @microsoft/microsoft.components.ai.mcp
npm run build --workspace @microsoft/components-ai-mcp-test-app
. .\activate.ps1
dotnet test .\src\Components\AI.Mcp\test\Microsoft.AspNetCore.Components.AI.Mcp.Tests.csproj
dotnet build .\src\Components\AI.Mcp\testassets\McpAppsHost.E2E.Tests\McpAppsHost.E2E.Tests.csproj /p:UseIisNativeAssets=false /p:E2EAppMode=publish
dotnet run --project .\src\Components\AI.Mcp\testassets\McpAppsHost.E2E.Tests\McpAppsHost.E2E.Tests.csproj --no-build -- --filter "FullyQualifiedName~McpAppHostingTests" --minimum-expected-tests 24
```

The native IIS assets are not needed for these Kestrel-hosted scenarios.
`E2EAppMode=publish` tests published payloads; the Testing targets also
support `E2EAppMode=build` for source-harness execution.
