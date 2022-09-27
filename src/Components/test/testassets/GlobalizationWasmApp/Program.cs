// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Web;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace GlobalizationWasmApp;

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
