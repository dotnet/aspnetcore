// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddOidcAuthentication(options =>
{
    options.ProviderOptions.Authority = $"{builder.HostEnvironment.BaseAddress}oidc";
    options.ProviderOptions.ClientId = "s6BhdRkqt3";
    options.ProviderOptions.ResponseType = "code";
});

await builder.Build().RunAsync();
