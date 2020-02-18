// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using BasicTestApp.AuthTest;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Mono.WebAssembly.Interop;

namespace BasicTestApp
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            await SimulateErrorsIfNeededForTest();

            // We want the culture to be en-US so that the tests for bind can work consistently.
            CultureInfo.CurrentCulture = new CultureInfo("en-US");

            var builder = WebAssemblyHostBuilder.CreateDefault(args);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Create("WEBASSEMBLY")))
            {
                // Needed because the test server runs on a different port than the client app,
                // and we want to test sending/receiving cookies under this config
                WebAssemblyHttpMessageHandlerOptions.DefaultCredentials = FetchCredentialsOption.Include;
            }

            builder.RootComponents.Add<Index>("root");

            builder.Services.AddSingleton<AuthenticationStateProvider, ServerAuthenticationStateProvider>();
            builder.Services.AddAuthorizationCore(options =>
            {
                options.AddPolicy("NameMustStartWithB", policy =>
                    policy.RequireAssertion(ctx => ctx.User.Identity.Name?.StartsWith("B") ?? false));
            });

            await builder.Build().RunAsync();
        }

        // Supports E2E tests in StartupErrorNotificationTest
        private static async Task SimulateErrorsIfNeededForTest()
        {
            var currentUrl = new MonoWebAssemblyJSRuntime().Invoke<string>("getCurrentUrl");
            if (currentUrl.Contains("error=sync"))
            {
                throw new InvalidTimeZoneException("This is a synchronous startup exception");
            }

            await Task.Yield();

            if (currentUrl.Contains("error=async"))
            {
                throw new InvalidTimeZoneException("This is an asynchronous startup exception");
            }
        }
    }
}
