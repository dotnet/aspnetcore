// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using BlazorMcpProof;
using Components.McpTestApp.Components;
using Components.McpTestApp.Hosting;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using System.Text.Json.Nodes;

var builder = WebApplication.CreateBuilder(args);
AppContext.SetSwitch("Microsoft.AspNetCore.Components.Endpoints.NavigationManager.DisableThrowNavigationException",
    builder.Configuration.GetValue("McpTest:DisableThrowNavigationException", false));
var configuredUrl = builder.Configuration["urls"]?.Split(';', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()
    ?? throw new InvalidOperationException("The MCP test app requires an explicit listener URL.");
var baseUri = new Uri(configuredUrl.TrimEnd('/') + "/");
builder.Services.AddRazorComponents(options => options.DisableClientValidation = true);
builder.Services.AddSingleton<McpFormEffects>();
builder.Services.AddScoped<McpScopeProbe>();
builder.Services.AddSingleton<McpTestConsumer>();
builder.Services.AddSingleton<FormActionGate>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped(provider =>
{
    var context = provider.GetRequiredService<Microsoft.AspNetCore.Components.Mcp.McpComponentRequestContext>();
    var token = context.IsActive ? context.CancellationToken
        : provider.GetRequiredService<IHttpContextAccessor>().HttpContext?.RequestAborted ?? CancellationToken.None;
    return new FormOperationCancellation(token);
});
var initialState = new InitialFormState();
builder.Services.AddSingleton(initialState);
var toolMetadata = new JsonObject { ["businessTool"] = "preserved" };
builder.Services.PostConfigure<McpServerOptions>(options =>
{
    foreach (var tool in options.ToolCollection!.Where(tool => tool.ProtocolTool.Name is "open_initial_form" or "unassociated_initial_form"))
    {
        tool.ProtocolTool.Meta = toolMetadata;
    }
});
builder.Services.AddMcpServer().WithTools<InitialDataTools>()
    .WithRequestFilters(filters => filters.AddCallToolFilter(next => async (request, cancellationToken) =>
    {
        var result = await next(request, cancellationToken);
        if (request.Params?.Name == "open_initial_form" && result.IsError != true)
        {
            result.Meta ??= new JsonObject();
            result.Meta["existingFilter"] = "preserved";
            var label = result.StructuredContent?.GetProperty("label").GetString();
            if (label == "filter-enrichment")
            {
                result.StructuredContent = JsonSerializer.SerializeToElement(
                    new InitialFormData { Label = "enriched-by-filter", Count = 73 }, McpJsonUtilities.DefaultOptions);
            }
            else if (label == "filter-error")
            {
                result.IsError = true;
                result.Content = [new ModelContextProtocol.Protocol.TextContentBlock { Text = "Rejected by the existing SDK filter." }];
            }
        }

        return result;
    }))
    .WithRazorComponents<App>(options =>
{
    options.Name = "component_form";
    options.BaseUri = baseUri;
    options.InitialPath = "mcp-form?case=mcp";
    options.PreserveDom = builder.Configuration.GetValue("McpTest:PreserveDom", true);
    options.LogLevel = builder.Configuration.GetValue("McpTest:LogLevel", LogLevel.Information);
})
    .WithRazorComponents<TypedApp>("open_initial_form", result => new Dictionary<string, object?>
    {
        [nameof(TypedApp.InitialData)] = initialState.Project(result),
    }, options =>
    {
        options.Name = "typed_form";
        options.BaseUri = baseUri;
        options.InitialPath = "mcp-typed";
    })
    .WithHttpTransport();

builder.Services.AddMcpServer().WithRazorComponents<NoScriptApp>(options =>
{
    options.Name = "no_script";
    options.BaseUri = baseUri;
});

var app = builder.Build();
var consumer = app.Services.GetRequiredService<McpTestConsumer>();
var sandboxBuilder = WebApplication.CreateSlimBuilder();
var sandbox = sandboxBuilder.Build();
sandbox.Urls.Clear();
sandbox.Urls.Add("http://127.0.0.1:0");
sandbox.MapGet("/sandbox/{id:guid}", (Guid id) =>
{
    var connection = consumer.Get(id);
    var hostOrigin = JsonSerializer.Serialize(connection.HostOrigin);
    return Results.Text("""
        <!DOCTYPE html><html><body style="margin:0">
        <script>
        const hostOrigin = HOST_ORIGIN;
        let inner;
        addEventListener("message", event => {
          if (event.source === parent && event.origin === hostOrigin) {
            const message = event.data;
            if (message?.method === "ui/notifications/sandbox-resource-ready") {
              const domains = message.params.csp?.resourceDomains ?? [];
              const bases = message.params.csp?.baseUriDomains ?? [];
              const policy = `default-src 'none'; script-src 'unsafe-inline' ${domains.join(" ")}; style-src 'unsafe-inline' ${domains.join(" ")}; img-src data: blob: ${domains.join(" ")}; font-src ${domains.join(" ")}; base-uri ${bases.join(" ") || "'self'"}; frame-src 'none'`;
              inner = document.createElement("iframe");
              inner.id = "app-frame";
              inner.setAttribute("sandbox", "allow-scripts allow-same-origin allow-forms");
              inner.style.cssText = "border:0;width:100%;height:900px";
              const parsed = new DOMParser().parseFromString(message.params.html, "text/html");
              const csp = parsed.createElement("meta");
              csp.httpEquiv = "Content-Security-Policy";
              csp.content = policy;
              parsed.head.prepend(csp);
              inner.srcdoc = "<!DOCTYPE html>" + parsed.documentElement.outerHTML;
              document.body.append(inner);
            } else if (inner && !message?.method?.startsWith("ui/notifications/sandbox-")) {
              inner.contentWindow.postMessage(message, "*");
            }
          } else if (inner && event.source === inner.contentWindow && !event.data?.method?.startsWith("ui/notifications/sandbox-")) {
            parent.postMessage(event.data, hostOrigin);
          }
        });
        parent.postMessage({jsonrpc:"2.0",method:"ui/notifications/sandbox-proxy-ready",params:{}}, hostOrigin);
        </script></body></html>
        """.Replace("HOST_ORIGIN", hostOrigin, StringComparison.Ordinal), "text/html");
});
await sandbox.StartAsync();
var sandboxUri = sandbox.Urls.Single();

app.UseAntiforgery();
app.MapStaticAssets();
app.MapMcp("/mcp");
app.MapRazorComponents<App>();
app.MapGet("/host", () => Results.Text("""
    <!DOCTYPE html><html><head><link rel="icon" href="data:,"></head><body>
    <h1>Standard MCP Apps test consumer</h1>
    <button id="open">Open application</button>
    <button id="close">Close application</button>
    <p id="host-status">Not opened</p>
    <div id="view"></div>
    <script src="/mcp-consumer.js" type="module"></script>
    </body></html>
    """, "text/html"));
app.MapPost("/consumer/open", async (HttpContext context, bool? resultFirst, string? tool, string? label, int? count) =>
{
    var producer = builder.Configuration["McpProducerEndpoint"] is { } configuredProducer ? new Uri(configuredProducer) : new Uri(baseUri, "mcp");
    var arguments = tool == "open_initial_form"
        ? new Dictionary<string, object?> { ["input"] = new InitialToolArguments
        {
            Label = label ?? throw new InvalidOperationException("The typed test consumer requires an explicit label."),
            Count = count ?? throw new InvalidOperationException("The typed test consumer requires an explicit count."),
        } }
        : new Dictionary<string, object?>();
    var opened = await consumer.OpenAsync(producer, baseUri.GetLeftPart(UriPartial.Authority), resultFirst == true, tool ?? "component_form", arguments, context.RequestAborted);
    return Results.Json(new { opened.Id, opened.Tool, opened.Resource, opened.Result, opened.Arguments, sandboxUri }, McpJsonUtilities.DefaultOptions);
});
app.MapPost("/consumer/{id:guid}/rpc", async (Guid id, McpTestConsumer.BrokerRequest request, HttpContext context) =>
    Results.Json(await consumer.ForwardAsync(id, request, context.RequestAborted), McpJsonUtilities.DefaultOptions));
app.MapDelete("/consumer/{id:guid}", async (Guid id) =>
{
    await consumer.CloseAsync(id);
    return Results.Ok();
});
app.MapGet("/observations/{caseId}", (string caseId, McpFormEffects effects) =>
{
    var state = effects.Read(caseId);
    return new { state.Count, state.Value, state.ComponentsCreated, state.ComponentsDisposed, state.ScopesDisposed };
});
app.MapPost("/test/reset/{caseId}", (string caseId, McpFormEffects effects) =>
{
    effects.Reset(caseId);
    return Results.NoContent();
});
app.MapGet("/test/initial-observations", () => initialState.Read());
app.MapPost("/test/reset-initial", () =>
{
    initialState.Reset();
    return Results.NoContent();
});
app.MapGet("/test/gate/{caseId}", (string caseId, FormActionGate gate) => gate.Read(caseId));
app.MapPost("/test/gate/{caseId}/release", (string caseId, FormActionGate gate) =>
{
    gate.Release(caseId);
    return Results.NoContent();
});

try
{
    await app.RunAsync();
}
finally
{
    await sandbox.StopAsync();
    await sandbox.DisposeAsync();
}

public sealed class McpTestAppMarker;
