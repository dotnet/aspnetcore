// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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

            //builder.Services.AddApiAuthorization<RemoteAppState, OidcAccount>()
            builder.Services.AddOidcAuthentication<RemoteAppState, OidcAccount>(options =>
            {
                options.ProviderOptions.Authority = "https://localhost:5001";
                options.ProviderOptions.ClientId = "Wasm.Authentication.Client";
                options.ProviderOptions.ResponseType = "code";
                options.ProviderOptions.DefaultScopes.Clear();
                options.ProviderOptions.DefaultScopes.Add("openid");
                options.ProviderOptions.DefaultScopes.Add("profile");
            })
                .AddUserFactory<RemoteAppState, OidcAccount, PreferencesUserFactory>();

            builder.Services.AddSingleton<StateService>();

            builder.RootComponents.Add<App>("app");

            await builder.Build().RunAsync();
        }
    }
}
