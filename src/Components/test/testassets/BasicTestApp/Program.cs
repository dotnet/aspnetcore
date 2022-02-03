// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Net.Http;
using System.Web;
using BasicTestApp.AuthTest;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.WebAssembly.Services;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.JSInterop;

namespace BasicTestApp;

public class Program
{
    public static async Task Main(string[] args)
    {
        await SimulateErrorsIfNeededForTest();

        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        builder.RootComponents.Add<HeadOutlet>("head::after");
        builder.RootComponents.Add<Index>("root");
        builder.RootComponents.RegisterForJavaScript<DynamicallyAddedRootComponent>("my-dynamic-root-component");
        builder.RootComponents.RegisterForJavaScript<JavaScriptRootComponentParameterTypes>(
            "component-with-many-parameters",
            javaScriptInitializer: "myJsRootComponentInitializers.testInitializer");

        builder.Services.AddSingleton(new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
        builder.Services.AddSingleton<AuthenticationStateProvider, ServerAuthenticationStateProvider>();
        builder.Services.AddAuthorizationCore(options =>
        {
            options.AddPolicy("NameMustStartWithB", policy =>
                policy.RequireAssertion(ctx => ctx.User.Identity.Name?.StartsWith('B') ?? false));
        });

        builder.Services.AddScoped<PreserveStateService>();
        builder.Services.AddTransient<FormsTest.ValidationComponentDI.SaladChef>();

        builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));

        builder.Logging.Services.AddSingleton<ILoggerProvider, PrependMessageLoggerProvider>(s =>
            new PrependMessageLoggerProvider(builder.Configuration["Logging:PrependMessage:Message"], s.GetService<IJSRuntime>()));

        var host = builder.Build();
        ConfigureCulture(host);

        await host.RunAsync();
    }

    private static void ConfigureCulture(WebAssemblyHost host)
    {
        // In the absence of a specified value, we want the culture to be en-US so that the tests for bind can work consistently.
        var culture = new CultureInfo("en-US");

        Uri uri = null;
        try
        {
            uri = new Uri(host.Services.GetService<NavigationManager>().Uri);
        }
        catch (ArgumentException)
        {
            // Some of our tests set this application up incorrectly so that querying NavigationManager.Uri throws.
        }

        if (uri != null && HttpUtility.ParseQueryString(uri.Query)["culture"] is string cultureName)
        {
            culture = new CultureInfo(cultureName);
        }

        // CultureInfo.CurrentCulture is async-scoped and will not affect the culture in sibling scopes.
        // Use CultureInfo.DefaultThreadCurrentCulture instead to modify the application's default scope.
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
    }

    // Supports E2E tests in StartupErrorNotificationTest
    private static async Task SimulateErrorsIfNeededForTest()
    {
        var currentUrl = DefaultWebAssemblyJSRuntime.Instance.Invoke<string>("getCurrentUrl");
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
