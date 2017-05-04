// Copyright (c) .NET Foundation. All rights reserved. 
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.AspNetCore.Localization.FunctionalTests
{
    public class LocalizationTest
    {
        private static readonly string _applicationPath = Path.Combine("test", "LocalizationWebsite");

        [Fact]
        public Task Localization_CustomCulture()
        {
            var testRunner = new TestRunner(_applicationPath);

            return testRunner.RunTestAndVerifyResponse(
                RuntimeArchitecture.x64,
                "CustomCulturePreserved",
                "en-US",
                "kr10.00");
        }

        [Fact]
        public Task Localization_ResourcesInClassLibrary_ReturnLocalizedValue()
        {
            var testRunner = new TestRunner(_applicationPath);

            return testRunner.RunTestAndVerifyResponse(
                RuntimeArchitecture.x64,
                "ResourcesInClassLibrary",
                "fr-FR",
                "Bonjour from ResourcesClassLibraryNoAttribute Bonjour from ResourcesClassLibraryNoAttribute Bonjour from ResourcesClassLibraryWithAttribute Bonjour from ResourcesClassLibraryWithAttribute");
        }

        [Fact]
        public Task Localization_ResourcesInFolder_ReturnLocalizedValue()
        {
            var testRunner = new TestRunner(_applicationPath);

            return testRunner.RunTestAndVerifyResponse(
                RuntimeArchitecture.x64,
                "ResourcesInFolder",
                "fr-FR",
                "Bonjour from StartupResourcesInFolder Bonjour from Test in resources folder Bonjour from Customer in resources folder Hello");
        }

        [Fact]
        public Task Localization_ResourcesInFolder_ReturnLocalizedValue_WithCultureFallback()
        {
            var testRunner = new TestRunner(_applicationPath);

            return testRunner.RunTestAndVerifyResponse(
                RuntimeArchitecture.x64,
                "ResourcesInFolder",
                "fr-FR-test",
                "Bonjour from StartupResourcesInFolder Bonjour from Test in resources folder Bonjour from Customer in resources folder Hello");
        }

        [Fact]
        public Task Localization_ResourcesInFolder_ReturnNonLocalizedValue_CultureHierarchyTooDeep()
        {
            var testRunner = new TestRunner(_applicationPath);

            return testRunner.RunTestAndVerifyResponse(
                RuntimeArchitecture.x64,
                "ResourcesInFolder",
                "fr-FR-test-again-too-deep-to-work",
                "Hello Hello Hello Hello");
        }

        [Fact]
        public Task Localization_ResourcesAtRootFolder_ReturnLocalizedValue()
        {
            var testRunner = new TestRunner(_applicationPath);

            return testRunner.RunTestAndVerifyResponse(
                RuntimeArchitecture.x64,
                "ResourcesAtRootFolder",
                "fr-FR",
                "Bonjour from StartupResourcesAtRootFolder Bonjour from Test in root folder Bonjour from Customer in Models folder");
        }
    }
}
