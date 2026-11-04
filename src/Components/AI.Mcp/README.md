# Components AI MCP Apps

This Razor class library hosts standard MCP Apps using the official
`@modelcontextprotocol/ext-apps` 2.0.3 `AppBridge`. It uses
`ModelContextProtocol.Core` SDK tools, existing AI block handlers/renderers,
and an application-defined `McpContentHost`. All iframe, interop, sandbox,
and MCP bridge functionality is in this package; Components.AI is unchanged.
It does not depend on a Blazor
app renderer, private renderer payloads, or a particular model provider.

## Setup

Create one host per conversation. Keep the SDK client alive for its views.
For applications with one conversation per DI scope, register
`services.AddMcpAppHost()` and inject `McpAppHost`. Applications with
multiple conversations in a scope should construct one host per conversation.

Advertise support before creating each SDK connection. Binding a client that
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

var appHost = new McpAppHost(new McpAppHostOptions());
var tools = appHost.BindTools(client, await client.ListToolsAsync());
var agent = new UIAgent(functionInvokingChatClient, options =>
{
    options.ChatOptions = new ChatOptions { Tools = [.. tools] };
    appHost.Configure(options);
});
```

`functionInvokingChatClient` must include `FunctionInvokingChatClient`.
Original calls use its native `CurrentContext.CallContent` identity. Calling
a decorated function without that context fails **before** dispatch.
Use SDK `WithName` aliases when binding multiple servers with colliding tool
names. Connections are identified by their `McpClient` instance, not an
application-assigned ID or server name. The same client can supply multiple
bindings with distinct model-facing tool names. The supplied tools must belong to the supplied client;
the SDK does not expose an owning-client property that this library can check.

Inside the existing `MessageList`, register:

```razor
<BlockRenderer TBlock="McpAppContentBlock" Context="block">
    <McpBlock THost="Host" @key="block.Id" Host="appHost" Block="block" />
</BlockRenderer>
```

Define the application component `Host.razor`. It inherits the MCP-specific
base and owns both its markup and per-view configuration:

```razor
@inherits McpContentHost
@using System.Text.Json

@if (!IsClosed)
{
    <button @onclick="ChangeThemeAsync">Toggle theme</button>
    <button @onclick="async () => await CloseAsync()">Close</button>
    <iframe @ref="Frame"
            title="Application"
            sandbox="allow-scripts allow-same-origin"
            referrerpolicy="no-referrer"
            style="width:100%;border:0">
    </iframe>
}

@code {
    private readonly McpContentOptions _options = new()
    {
        ProxyUrl = new Uri("https://apps.example/proxy"),
        MaximumHeight = 900,
    };

    protected override McpContentOptions Options => _options;

    private string _theme = "light";

    private async Task ChangeThemeAsync()
    {
        _theme = _theme == "light" ? "dark" : "light";
        await UpdateHostContextAsync(JsonSerializer.SerializeToElement(new { theme = _theme }));
    }
}
```

Controls and presentation state belong to this particular `Host` instance.
`McpBlock` does not expose forwarding methods or need a component reference.
Closing a host cancels and tears down its own app; `IsClosed` lets the application
stop rendering that iframe without affecting other renderings of the same block.
There is no required context object or global notification mechanism.

Configure browser origins/permissions through `Options.BrowserPolicy`, and
document/message bounds and deadlines directly on `McpContentOptions`.
`AllowToolCall` and `AllowResourceRead` are per-view policies as well.
The base starts communication only after the iframe reference is captured;
it does not replace the application's `sandbox`, `allow`, or presentation.
It fetches the resource and imports the package's MCP module, which uses
`AppBridge` directly. There is no generic adapter or second protocol layer.
The protected `ModulePath`, `JS`, `OnReadyAsync`, and `OnClosedAsync` members
support application-specific browser integration and lifecycle hooks.

Existing model approval remains available: wrap returned model functions
with the existing `ApprovalRequiredAIFunction` mechanism. App-originated
calls do not create approval UI, call the model, append conversation
history, or modify `AgentContext`.

## Separate proxy application

The proxy can run in another C# application/process:

```csharp
using Microsoft.AspNetCore.Builder;

app.MapStaticAssets();
app.MapMcpAppProxy("/proxy", new Uri("https://chat.example"));
```

It must reference this RCL and serve its transitive static assets.
`hostOrigin` is the exact **browser-visible host origin**. It is not inferred
from an incoming frame or an internal server address. The host must configure
the corresponding absolute `Options.ProxyUrl` on a distinct origin. Production
origins can share a reverse proxy; no fixed port, loopback address, or
in-memory registry is required.

The official adapter sources are under `src/Interop`. The repository npm
workspace bundles `mcp-host.js` and `mcp-proxy.js` to
`Interop/dist/{Debug,Release}`. The RCL discovers, packs, and publishes these
as `_content/Microsoft.AspNetCore.Components.AI.Mcp/` assets. Do not copy
generated bundles into source `wwwroot` or ship unbundled bare imports.

## Policy and resource selection

Tool inventory is a host-bound snapshot of discovered `McpClientTool`
definitions, not a second catalog. `_meta.ui.visibility` defaults to both
`model` and `app`. Model aliases do not change app-facing original server
names. App calls can reach only app-visible tools on the originating
connection. `AllowToolCall(client, originalName)` receives that owning client
and can further deny calls. The host checks request envelopes, required tool names/resource URIs,
JSON-object arguments, routing, and configured size/lifetime limits. Missing
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

`BrowserPolicy` contains explicit host grants for exact resource,
connection, nested-frame, and base-address HTTP origins and browser feature
names (`camera`, `microphone`, `geolocation`, `clipboard-write`). Each must
also be requested by resource metadata. Ungranted/malformed requirements
fail explicitly; they are not silently expanded. External assets and
network requests work when both declared and configured. No grants are
implicit. `domain` metadata does not choose a proxy origin.

Connection grants also accept exact `ws://` and `wss://` origins for
WebSockets. Those schemes do not grant script/resource origins.

The originating UI resource is readable through the broker. Other resources
require `AllowResourceRead(client, uri)`, with the owning client passed to
the policy. Browser network grants and brokered SDK resource access are separate.

## Result retention, restoration, and lifetime

The model function calls the original SDK tool once, retains its raw
`CallToolResult`, then returns a separate model projection with protocol
`_meta` removed from the result, content blocks, and embedded resources.
Business content, structured content, and error state remain; the app receives
the untouched original metadata. Rendering only reads resources and retained results;
it never invokes the original tool to reconstruct a view.

Raw results are retained in the conversation host, up to
`MaximumRetainedCalls` (default 128), until disposal. Reaching the limit
rejects a new originating call before execution; it does not evict live
views. Same-host views can reopen retained results. Cross-host/process
history restoration has no durable raw-result store in this baseline:
`McpBlock<THost>.UnavailableContent` (or its explicit default status) is shown
instead of rerunning the tool. Dispose the host with the conversation.
Disposing a view does not dispose a shared SDK connection.

The MCP content host bounds pending requests, document/message bytes,
initialization, operations, and teardown. Cancellation never promises to
undo a completed server mutation. No automatic retry follows an uncertain
transport outcome.

## Capability boundary

The baseline supports standard initialization, ping, tool input/result and
cancellation, inline display, finite sizing, host theme/context updates,
teardown, snapshot app-visible tool listing/calls, and permitted resource
reads/listing. Resource listing aggregates the SDK's pages, filters by host
policy, and returns a complete host-visible snapshot without a cursor.
The protected `UpdateHostContextAsync` helper sends MCP Apps host-context
updates through `AppBridge.setHostContext`. Pass a JSON object containing fields
such as `theme` or `locale`; fields omitted from the update are preserved.
The example keeps theme state in `Host.razor` and sends `{ "theme": "dark" }`
or `{ "theme": "light" }`. This is a protocol payload, not a required
observable context object or cascading-state abstraction.
`MaximumHeight` configures the inline view's finite height bound (1200 CSS
pixels by default).

Fullscreen/PiP, resource templates/subscriptions/change
notifications, prompts, sampling, links, downloads, app message insertion,
and model-context updates are not advertised or implemented. Unsupported
requests fail explicitly. Server inventory refresh/reconnect requires a
new host/binding snapshot; no list-change support is claimed.

Interactive Server (including prerender-to-interactive activation) is the
initial supported deployment target. Static rendering cannot open a live
pipe. WebAssembly/Auto deployment and durable cross-process restoration
are intentionally outside this baseline.

## Repository validation

Follow the [repository build prerequisites](../../../docs/BuildFromSource.md).
The deterministic browser coverage follows the existing Components.AI
Components.Testing/MSTest/C# Playwright pattern in an AI.Mcp-owned test project.
It starts real C# MCP servers,
a separate-origin proxy, and the consumer without an external model or account.
The consumer renders an application-defined `Host : McpContentHost`.
Its `Home.razor` initializes the SDK clients and agent directly and owns their
disposal, together with a page-owned `McpAppHost`. There is no separate
conversation-session service in the test asset.
Multi-app coverage sends a user message that makes the scripted model emit two
distinct UI tool calls through the normal function-invocation pipeline. Each
resulting content block renders once; the page has no copy count or duplication mode.

From the repository root on Windows:

```powershell
npm run build --workspace @microsoft/microsoft.components.ai.mcp
npm run build --workspace @microsoft/components-ai-mcp-test-app
. .\activate.ps1
dotnet test .\src\Components\AI.Mcp\test\Microsoft.AspNetCore.Components.AI.Mcp.Tests.csproj
dotnet build .\src\Components\AI.Mcp\testassets\McpAppsHost.E2E.Tests\McpAppsHost.E2E.Tests.csproj /p:UseIisNativeAssets=false /p:E2EAppMode=publish
dotnet run --project .\src\Components\AI.Mcp\testassets\McpAppsHost.E2E.Tests\McpAppsHost.E2E.Tests.csproj --no-build -- --filter "FullyQualifiedName~McpAppHostingTests" --minimum-expected-tests 7
```

The native IIS assets are not needed for these Kestrel-hosted scenarios.
`E2EAppMode=publish` tests published payloads; the Testing targets also
support `E2EAppMode=build` for source-harness execution.
