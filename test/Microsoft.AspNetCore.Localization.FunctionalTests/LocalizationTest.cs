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
        public Task Localization_CustomCulture_AllOS()
        {
            var testRunner = new TestRunner(_applicationPath);

            return testRunner.RunTestAndVerifyResponse(
                RuntimeFlavor.CoreClr,
                RuntimeArchitecture.x64,
                "CustomCulturePreserved",
                "en-US",
                "kr10.00");
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        public Task Localization_CustomCulture_Windows()
        {
            var testRunner = new TestRunner(_applicationPath);

            return testRunner.RunTestAndVerifyResponse(
                RuntimeFlavor.Clr,
                RuntimeArchitecture.x64,
                "CustomCulturePreserved",
                "en-US",
                "kr10.00");
        }

        [Fact]
        public Task Localization_ResourcesInClassLibrary_ReturnLocalizedValue_AllOS()
        {
            var testRunner = new TestRunner(_applicationPath);

            return testRunner.RunTestAndVerifyResponse(
                RuntimeFlavor.CoreClr,
                RuntimeArchitecture.x64,
                "ResourcesInClassLibrary",
                "fr-FR",
                "Bonjour from ResourcesClassLibraryNoAttribute Bonjour from ResourcesClassLibraryNoAttribute Bonjour from ResourcesClassLibraryWithAttribute Bonjour from ResourcesClassLibraryWithAttribute");
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        public Task Localization_ResourcesInClassLibrary_ReturnLocalizedValue_Windows()
        {
            var testRunner = new TestRunner(_applicationPath);

            return testRunner.RunTestAndVerifyResponse(
                RuntimeFlavor.Clr,
                RuntimeArchitecture.x64,
                "ResourcesInClassLibrary",
                "fr-FR",
                "Bonjour from ResourcesClassLibraryNoAttribute Bonjour from ResourcesClassLibraryNoAttribute Bonjour from ResourcesClassLibraryWithAttribute Bonjour from ResourcesClassLibraryWithAttribute");
        }

        [Fact]
        public Task Localization_ResourcesInFolder_ReturnLocalizedValue_AllOS()
        {
            var testRunner = new TestRunner(_applicationPath);

            return testRunner.RunTestAndVerifyResponse(
                RuntimeFlavor.CoreClr,
                RuntimeArchitecture.x64,
                "ResourcesInFolder",
                "fr-FR",
                "Bonjour from StartupResourcesInFolder Bonjour from Test in resources folder Bonjour from Customer in resources folder Hello");
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        public Task Localization_ResourcesInFolder_ReturnLocalizedValue_Windows()
        {
            var testRunner = new TestRunner(_applicationPath);

            return testRunner.RunTestAndVerifyResponse(
                RuntimeFlavor.Clr,
                RuntimeArchitecture.x64,
                "ResourcesInFolder",
                "fr-FR",
                "Bonjour from StartupResourcesInFolder Bonjour from Test in resources folder Bonjour from Customer in resources folder Hello");
        }

        [Fact]
        public Task Localization_ResourcesInFolder_ReturnLocalizedValue_WithCultureFallback_AllOS()
        {
            var testRunner = new TestRunner(_applicationPath);

            return testRunner.RunTestAndVerifyResponse(
                RuntimeFlavor.CoreClr,
                RuntimeArchitecture.x64,
                "ResourcesInFolder",
                "fr-FR-test",
                "Bonjour from StartupResourcesInFolder Bonjour from Test in resources folder Bonjour from Customer in resources folder Hello");
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        public Task Localization_ResourcesInFolder_ReturnLocalizedValue_WithCultureFallback_Windows()
        {
            var testRunner = new TestRunner(_applicationPath);

            return testRunner.RunTestAndVerifyResponse(
                RuntimeFlavor.Clr,
                RuntimeArchitecture.x64,
                "ResourcesInFolder",
                "fr-FR-test",
                "Bonjour from StartupResourcesInFolder Bonjour from Test in resources folder Bonjour from Customer in resources folder Hello");
        }

        [Fact]
        public Task Localization_ResourcesInFolder_ReturnNonLocalizedValue_CultureHierarchyTooDeep_AllOS()
        {
            var testRunner = new TestRunner(_applicationPath);

            return testRunner.RunTestAndVerifyResponse(
                RuntimeFlavor.CoreClr,
                RuntimeArchitecture.x64,
                "ResourcesInFolder",
                "fr-FR-test-again-too-deep-to-work",
                "Hello Hello Hello Hello");
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        public Task Localization_ResourcesInFolder_ReturnNonLocalizedValue_CultureHierarchyTooDeep_Windows()
        {
            var testRunner = new TestRunner(_applicationPath);

            return testRunner.RunTestAndVerifyResponse(
                RuntimeFlavor.Clr,
                RuntimeArchitecture.x64,
                "ResourcesInFolder",
                "fr-FR-test-again-too-deep-to-work",
                "Hello Hello Hello Hello");
        }

        [Fact]
        public Task Localization_ResourcesAtRootFolder_ReturnLocalizedValue_AllOS()
        {
            var testRunner = new TestRunner(_applicationPath);

            return testRunner.RunTestAndVerifyResponse(
                RuntimeFlavor.CoreClr,
                RuntimeArchitecture.x64,
                "ResourcesAtRootFolder",
                "fr-FR",
                "Bonjour from StartupResourcesAtRootFolder Bonjour from Test in root folder Bonjour from Customer in Models folder");
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        public Task Localization_ResourcesAtRootFolder_ReturnLocalizedValue_Windows()
        {
            var testRunner = new TestRunner(_applicationPath);

            return testRunner.RunTestAndVerifyResponse(
                RuntimeFlavor.Clr,
                RuntimeArchitecture.x64,
                "ResourcesAtRootFolder",
                "fr-FR",
                "Bonjour from StartupResourcesAtRootFolder Bonjour from Test in root folder Bonjour from Customer in Models folder");
        }
    }
}
