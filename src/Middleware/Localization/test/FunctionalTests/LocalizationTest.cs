// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using LocalizationWebsite;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Xunit;

namespace Microsoft.AspNetCore.Localization.FunctionalTests
{
    public class LocalizationTest
    {
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
                "zh",
                "您好 (zh) from StartupResourcesInFolder Hello Hello Hello");
        }

        [Fact]
        public Task Localization_ResourcesInFolder_ReturnLocalizedValue_WithCultureFallback_ToRootCulture()
        {
            return RunTest(
                typeof(StartupResourcesInFolder),
                "ar-YE",
                "مرحبا from StartupResourcesInFolder Hello Hello Hello");
        }

        [Fact]
        public Task Localization_ResourcesInFolder_ReturnNonLocalizedValue_WithCultureFallback_ToCultureHierarchy()
        {
            return RunTest(
                typeof(StartupResourcesInFolder),
                "zh-TW",
                "您好 (zh-Hant) from StartupResourcesInFolder Hello Hello Hello");
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
        public Task Localization_ResourcesInFolder_ReturnLocalizedValue_WithoutFallBackToParentCultures()
        {
            return RunTest(
                typeof(StartupResourcesInFolderWithoutFallBackToParentCultures),
                "zh-Hant",
                "您好 (zh-Hant) from StartupResourcesInFolderWithoutFallBackToParentCultures");
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
            var webHostBuilder = new WebHostBuilder().UseStartup(startupType);
            var testHost = new TestServer(webHostBuilder);

            var client = testHost.CreateClient();
            var request = new HttpRequestMessage();
            var cookieValue = $"c={culture}|uic={culture}";
            request.Headers.Add("Cookie", $"{CookieRequestCultureProvider.DefaultCookieName}={cookieValue}");

            var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expected, await response.Content.ReadAsStringAsync());
    }
    }
}
