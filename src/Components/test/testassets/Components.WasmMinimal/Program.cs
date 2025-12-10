// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices.JavaScript;
using System.Security.Claims;
using Components.TestServer.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using TestContentPackage;
using TestContentPackage.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
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

await builder.Build().RunAsync();

internal static partial class JSImports
{
    [JSImport("globalThis.getQueryParam")]
    public static partial string GetQueryParam(string name);
}
