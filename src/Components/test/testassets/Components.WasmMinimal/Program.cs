// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using Components.TestServer.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.WebAssembly.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services.AddSingleton<AsyncOperationService>();
builder.Services.AddCascadingAuthenticationState();

var additionalClaim = DefaultWebAssemblyJSRuntime.Instance.Invoke<string>("getQueryParam", "additionalClaim");

builder.Services.AddAuthenticationStateDeserialization(options =>
{
    var originalCallback = options.DeserializationCallback;
    options.DeserializationCallback = async authenticationStateData =>
    {
        var authenticationState = await originalCallback(authenticationStateData);
        if (!string.IsNullOrEmpty(additionalClaim))
        {
            authenticationState.User.Identities.First().AddClaim(new Claim("additional-claim", additionalClaim));
        }
        return authenticationState;
    };
});

await builder.Build().RunAsync();
