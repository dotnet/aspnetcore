// Copyright (c) .NET Foundation. All rights reserved. 
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Testing;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.AspNetCore.Localization.FunctionalTests
{
    public class LocalizationTest
    {
        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(RuntimeFlavor.Clr, "http://localhost:5070/", RuntimeArchitecture.x86)]
        [InlineData(RuntimeFlavor.CoreClr, "http://localhost:5071/", RuntimeArchitecture.x86)]
        public Task Localization_ResourcesInFolder_ReturnLocalizedValue_Windows(
            RuntimeFlavor runtimeFlavor,
            string applicationBaseUrl,
            RuntimeArchitecture runtimeArchitechture)
        {
            var testRunner = new TestRunner();
            return testRunner.RunTestAndVerifyResponse(
                runtimeFlavor,
                runtimeArchitechture,
                applicationBaseUrl,
                "ResourcesInFolder",
                "fr-FR",
                "Bonjour from StartupResourcesInFolder Bonjour from Test in resources folder Bonjour from Customer in resources folder");
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(RuntimeFlavor.Clr, "http://localhost:5070/", RuntimeArchitecture.x86)]
        [InlineData(RuntimeFlavor.CoreClr, "http://localhost:5071/", RuntimeArchitecture.x86)]
        public Task Localization_ResourcesInFolder_ReturnLocalizedValue_WithCultureFallback_Windows(
            RuntimeFlavor runtimeFlavor,
            string applicationBaseUrl,
            RuntimeArchitecture runtimeArchitechture)
        {
            var testRunner = new TestRunner();
            return testRunner.RunTestAndVerifyResponse(
                runtimeFlavor,
                runtimeArchitechture,
                applicationBaseUrl,
                "ResourcesInFolder",
                "fr-FR-test",
                "Bonjour from StartupResourcesInFolder Bonjour from Test in resources folder Bonjour from Customer in resources folder");
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(RuntimeFlavor.Clr, "http://localhost:5070/", RuntimeArchitecture.x86)]
        [InlineData(RuntimeFlavor.CoreClr, "http://localhost:5071/", RuntimeArchitecture.x86)]
        public Task Localization_ResourcesInFolder_ReturnNonLocalizedValue_CultureHierarchyTooDeep_Windows(
            RuntimeFlavor runtimeFlavor,
            string applicationBaseUrl,
            RuntimeArchitecture runtimeArchitechture)
        {
            var testRunner = new TestRunner();
            return testRunner.RunTestAndVerifyResponse(
                runtimeFlavor,
                runtimeArchitechture,
                applicationBaseUrl,
                "ResourcesInFolder",
                "fr-FR-test-again-too-deep-to-work",
                "Hello Hello Hello");
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Windows)]
        [FrameworkSkipCondition(RuntimeFrameworks.CoreCLR)]
        public Task Localization_ResourcesInFolder_ReturnLocalizedValue_Mono()
        {
            var testRunner = new TestRunner();
            return testRunner.RunTestAndVerifyResponse(
                RuntimeFlavor.Mono,
                RuntimeArchitecture.x86,
                "http://localhost:5072",
                "ResourcesInFolder",
                "fr-FR",
                "Bonjour from StartupResourcesInFolder Bonjour from Test in resources folder Bonjour from Customer in resources folder");
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Windows)]
        [FrameworkSkipCondition(RuntimeFrameworks.CoreCLR)]
        public Task Localization_ResourcesInFolder_ReturnLocalizedValue_WithCultureFallback_Mono()
        {
            var testRunner = new TestRunner();
            return testRunner.RunTestAndVerifyResponse(
                RuntimeFlavor.Mono,
                RuntimeArchitecture.x86,
                "http://localhost:5072",
                "ResourcesInFolder",
                "fr-FR-test",
                "Bonjour from StartupResourcesInFolder Bonjour from Test in resources folder Bonjour from Customer in resources folder");
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Windows)]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        public Task Localization_ResourcesInFolder_ReturnLocalizedValue_CoreCLR_NonWindows()
        {
            var testRunner = new TestRunner();
            return testRunner.RunTestAndVerifyResponse(
                RuntimeFlavor.CoreClr,
                RuntimeArchitecture.x64,
                "http://localhost:5073/",
                "ResourcesInFolder",
                "fr-FR",
                "Bonjour from StartupResourcesInFolder Bonjour from Test in resources folder Bonjour from Customer in resources folder");
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Windows)]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        public Task Localization_ResourcesInFolder_ReturnLocalizedValue_WithCultureFallback_CoreCLR_NonWindows()
        {
            var testRunner = new TestRunner();
            return testRunner.RunTestAndVerifyResponse(
                RuntimeFlavor.CoreClr,
                RuntimeArchitecture.x64,
                "http://localhost:5073/",
                "ResourcesInFolder",
                "fr-FR-test",
                "Bonjour from StartupResourcesInFolder Bonjour from Test in resources folder Bonjour from Customer in resources folder");
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(RuntimeFlavor.Clr, "http://localhost:5074/", RuntimeArchitecture.x86)]
        [InlineData(RuntimeFlavor.CoreClr, "http://localhost:5075/", RuntimeArchitecture.x86)]
        public Task Localization_ResourcesAtRootFolder_ReturnLocalizedValue_Windows(
            RuntimeFlavor runtimeFlavor,
            string applicationBaseUrl,
            RuntimeArchitecture runtimeArchitechture)
        {
            var testRunner = new TestRunner();
            return testRunner.RunTestAndVerifyResponse(
                runtimeFlavor,
                runtimeArchitechture,
                applicationBaseUrl,
                "ResourcesAtRootFolder",
                "fr-FR",
                "Bonjour from StartupResourcesAtRootFolder Bonjour from Test in root folder Bonjour from Customer in Models folder");
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Windows)]
        [FrameworkSkipCondition(RuntimeFrameworks.CoreCLR)]
        public Task Localization_ResourcesAtRootFolder_ReturnLocalizedValue_Mono()
        {
            var testRunner = new TestRunner();
            return testRunner.RunTestAndVerifyResponse(
                RuntimeFlavor.Mono,
                RuntimeArchitecture.x86,
                "http://localhost:5076",
                "ResourcesAtRootFolder",
                "fr-FR",
                "Bonjour from StartupResourcesAtRootFolder Bonjour from Test in root folder Bonjour from Customer in Models folder");
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Windows)]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        public Task Localization_ResourcesAtRootFolder_ReturnLocalizedValue_CoreCLR_NonWindows()
        {
            var testRunner = new TestRunner();
            return testRunner.RunTestAndVerifyResponse(
                RuntimeFlavor.CoreClr,
                RuntimeArchitecture.x64,
                "http://localhost:5077/",
                "ResourcesAtRootFolder",
                "fr-FR",
                "Bonjour from StartupResourcesAtRootFolder Bonjour from Test in root folder Bonjour from Customer in Models folder");
        }
    }
}
