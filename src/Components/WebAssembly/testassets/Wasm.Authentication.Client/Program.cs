// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Wasm.Authentication.Client
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);

            builder.Services.AddApiAuthorization<RemoteAppState, OidcAccount>()
                .AddAccountClaimsPrincipalFactory<RemoteAppState, OidcAccount, PreferencesUserFactory>();
                .ConfigureAccessTokenOptions("ExternalAPI", options =>
                {
                    options.AllowedOrigins.Add(new Uri("https://example.com"));
                    options.TokenRequestOptions = new AccessTokenRequestOptions
                    {
                        Scopes = new[] { "Wasm.Authentication.ServerAPI" }
                    };
                });

            builder.Services.AddHttpClient("ExternalAPI", client => client.BaseAddress = new Uri("https://example.com"))
                .AddHttpMessageHandler(() => new RemoteAuthenticationServiceConfigurationMessageHandler("ExternalAPI"))
                .AddHttpMessageHandler<RemoteAuthenticationMessageHandler>();

            // Default API (Configured by default by AddApiAuthorization
            builder.Services.AddHttpClient("ServerAPI", client => client.BaseAddress = new Uri(new Uri(builder.HostEnvironment.BaseAddress).GetLeftPart(UriPartial.Authority)))
                .AddHttpMessageHandler<RemoteAuthenticationMessageHandler>();

            builder.Services.AddSingleton<StateService>();

            builder.RootComponents.Add<App>("app");

            await builder.Build().RunAsync();
        }
    }
}
