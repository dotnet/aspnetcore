// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace GlobalizationWasmApp
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.Services.AddLocalization();
            builder.RootComponents.Add<App>("app");

            var host = builder.Build();
            ConfigureCulture(host);

            await host.RunAsync();
        }

        private static void ConfigureCulture(WebAssemblyHost host)
        {
            var uri = new Uri(host.Services.GetService<NavigationManager>().Uri);

            var cultureName = HttpUtility.ParseQueryString(uri.Query)["dotNetCulture"];
            if (cultureName is null)
            {
                return;
            }

            var culture = new CultureInfo(cultureName);
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
        }
    }
}
