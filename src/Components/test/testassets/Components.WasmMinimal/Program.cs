// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Runtime.InteropServices.JavaScript;
using System.Security.Claims;
using Components.WasmMinimal;
using Components.TestServer.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;
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
builder.Services.AddSingleton<StartupNavigationState>();

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

var host = builder.Build();
if (new Uri(host.Services.GetRequiredService<NavigationManager>().Uri).Query.Contains("delay-webassembly-startup", StringComparison.Ordinal))
{
    host.Services.GetRequiredService<NavigationManager>().LocationChanged += host.Services.GetRequiredService<StartupNavigationState>().OnLocationChanged;
    await host.Services.GetRequiredService<IJSRuntime>().InvokeVoidAsync("webAssemblyStartup.wait");
}

await host.RunAsync();

internal static partial class JSImports
{
    [JSImport("globalThis.getQueryParam")]
    public static partial string GetQueryParam(string name);
}
