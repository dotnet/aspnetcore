// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Net.Http;
using System.Runtime.InteropServices.JavaScript;
using System.Security.Claims;
using Components.TestServer.Services;
using Components.WasmMinimal;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using OpenTelemetry;
using OpenTelemetry.Trace;
using TestContentPackage;
using TestContentPackage.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

var enUs = new CultureInfo("en-US");
CultureInfo.DefaultThreadCurrentCulture = enUs;
CultureInfo.DefaultThreadCurrentUICulture = enUs;
builder.Services.AddSingleton<AsyncOperationService>();
builder.Services.AddSingleton<InteractiveWebAssemblyService>();
builder.Services.AddSingleton<InteractiveAutoService>();
builder.Services.AddSingleton<InteractiveServerService>();

// Register custom serializer for persistent component state
builder.Services.AddSingleton<PersistentComponentStateSerializer<int>, CustomIntSerializer>();

builder.Services.AddCascadingAuthenticationState();

if (JSImports.GetQueryParam("activity-links-test-id") is { Length: > 0 } activityLinksTestId)
{
    var client = new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };
    builder.Services.AddOpenTelemetry().WithTracing(tracing => tracing
        .AddSource("Microsoft.AspNetCore.Components")
        .AddProcessor(new SimpleActivityExportProcessor(
            new ActivityLinksWebAssemblyExporter(client, activityLinksTestId))));
}

builder.Services.AddAuthenticationStateDeserialization(options =>
{
    var originalCallback = options.DeserializationCallback;
    options.DeserializationCallback = async authenticationStateData =>
    {
        var authenticationState = await originalCallback(authenticationStateData);
        var identity = authenticationState.User.Identities.First();
        if (identity.IsAuthenticated)
        {
            var additionalClaim = JSImports.GetQueryParam("additionalClaim");
            if (!string.IsNullOrEmpty(additionalClaim))
            {
                identity.AddClaim(new Claim("additional-claim", additionalClaim));
            }
        }
        return authenticationState;
    };
});

await builder.Build().RunAsync();

internal static partial class JSImports
{
    [JSImport("globalThis.getQueryParam")]
    public static partial string GetQueryParam(string name);
}
