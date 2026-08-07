// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddOidcAuthentication(options =>
{
    options.ProviderOptions.Authority = $"{builder.HostEnvironment.BaseAddress}oidc";
    options.ProviderOptions.ClientId = "s6BhdRkqt3";
    options.ProviderOptions.ResponseType = "code";
    options.ProviderOptions.TokenStorage = RemoteAuthenticationTokenStorage.LocalStorage;
});

await builder.Build().RunAsync();
