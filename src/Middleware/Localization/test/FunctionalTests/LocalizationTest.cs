// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using LocalizationWebsite;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Localization.FunctionalTests;

public class LocalizationTest
{
    [Fact]
    public Task Localization_ContentLanguageHeader()
    {
        return RunTest(
            typeof(StartupContentLanguageHeader),
            "ar-YE",
            "True ar-YE");
    }

    [Fact]
    public Task Localization_CustomCulture()
    {
        return RunTest(
            typeof(StartupCustomCulturePreserved),
            "en-US",
            "kr10.00");
    }

    [Fact]
    public Task Localization_GetAllStrings()
    {
        return RunTest(
            typeof(StartupGetAllStrings),
            "fr-FR",
            "1 Bonjour from Customer in resources folder");
    }

    [Fact]
    public Task Localization_ResourcesInClassLibrary_ReturnLocalizedValue()
    {
        return RunTest(
            typeof(StartupResourcesInClassLibrary),
            "fr-FR",
            "Bonjour from ResourcesClassLibraryNoAttribute Bonjour from ResourcesClassLibraryNoAttribute Bonjour from ResourcesClassLibraryWithAttribute Bonjour from ResourcesClassLibraryWithAttribute");
    }

    [Fact]
    public Task Localization_ResourcesInFolder_ReturnLocalizedValue()
    {
        return RunTest(
            typeof(StartupResourcesInFolder),
            "fr-FR",
            "Bonjour from StartupResourcesInFolder Bonjour from Test in resources folder Bonjour from Customer in resources folder Hello");
    }

    [Fact]
    public Task Localization_ResourcesInFolder_ReturnLocalizedValue_WithCultureFallback()
    {
        return RunTest(
            typeof(StartupResourcesInFolder),
            "fr-FR-test",
            "Bonjour from StartupResourcesInFolder Bonjour from Test in resources folder Bonjour from Customer in resources folder Hello");
    }

    [Fact]
    public Task Localization_ResourcesInFolder_ReturnNonLocalizedValue_CultureHierarchyTooDeep()
    {
        return RunTest(
            typeof(StartupResourcesInFolder),
            "fr-FR-test-again-too-deep-to-work",
            "Hello Hello Hello Hello");
    }

    [Fact]
    public Task Localization_ResourcesAtRootFolder_ReturnLocalizedValue()
    {
        return RunTest(
            typeof(StartupResourcesAtRootFolder),
            "fr-FR",
            "Bonjour from StartupResourcesAtRootFolder Bonjour from Test in root folder Bonjour from Customer in Models folder");
    }

    [Fact]
    public Task Localization_BuilderAPIs()
    {
        return RunTest(
            typeof(StartupBuilderAPIs),
            "ar-YE",
            "Hello");
    }

    private async Task RunTest(Type startupType, string culture, string expected)
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .UseStartup(startupType);
            }).Build();

        await host.StartAsync();

        var testHost = host.GetTestServer();

        var client = testHost.CreateClient();
        var request = new HttpRequestMessage();
        var cookieValue = $"c={culture}|uic={culture}";
        request.Headers.Add("Cookie", $"{CookieRequestCultureProvider.DefaultCookieName}={cookieValue}");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(expected, await response.Content.ReadAsStringAsync());
    }
}
