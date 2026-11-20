// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Net.Http;
using System.Runtime.InteropServices.JavaScript;
using System.Security.Claims;
using System.Web;
using Components.TestServer.Services;
using Components.WasmMinimal;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
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

await using var host = builder.Build();
var navigationManager = host.Services.GetRequiredService<NavigationManager>();
var activityLinksTestId = HttpUtility.ParseQueryString(new Uri(navigationManager.Uri).Query)["activity-links-test-id"];
using var activityLinksClient = activityLinksTestId is { Length: > 0 }
    ? new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) }
    : null;
using var activityLinksTracer = activityLinksClient is not null && activityLinksTestId is { Length: > 0 }
    ? Sdk.CreateTracerProviderBuilder()
        .AddSource("Microsoft.AspNetCore.Components")
        .AddProcessor(new SimpleActivityExportProcessor(
            new ActivityLinksWebAssemblyExporter(activityLinksClient, activityLinksTestId)))
        .Build()
    : null;

await host.RunAsync();

internal static partial class JSImports
{
    [JSImport("globalThis.getQueryParam")]
    public static partial string GetQueryParam(string name);
}
