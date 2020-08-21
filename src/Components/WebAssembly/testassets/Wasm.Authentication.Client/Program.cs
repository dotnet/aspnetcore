// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Wasm.Authentication.Client
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);

            builder.Services.AddApiAuthorization<RemoteAppState, OidcAccount>()
                .AddAccountClaimsPrincipalFactory<RemoteAppState, OidcAccount, PreferencesUserFactory>();

            builder.Services.AddHttpClient<WeatherForecastClient>(client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress))
            .AddHttpMessageHandler<BaseAddressAuthorizationMessageHandler>();

            ConfigureCommonServices(builder.Services);

            builder.RootComponents.Add<App>("app");

            await builder.Build().RunAsync();
        }

        public static void ConfigureCommonServices(IServiceCollection services)
        {
            services.TryAddScoped<SignOutSessionStateManager>();

            services.AddSingleton<StateService>();
        }
    }
}
