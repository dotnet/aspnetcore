// Copyright (c) .NET Foundation. All rights reserved. 
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Testing;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.AspNetCore.Localization.FunctionalTests
{
    public class LocalizationTest
    {
        private static readonly string _applicationPath = Path.Combine("test", "LocalizationWebsite");
        
        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(RuntimeFlavor.Clr, "http://localhost:5070/", RuntimeArchitecture.x64)]
        [InlineData(RuntimeFlavor.CoreClr, "http://localhost:5071/", RuntimeArchitecture.x64)]
        public Task Localization_ResourcesInFolder_ReturnLocalizedValue_Windows(
            RuntimeFlavor runtimeFlavor,
            string applicationBaseUrl,
            RuntimeArchitecture runtimeArchitecture)
        {
            var testRunner = new TestRunner(_applicationPath);
            return testRunner.RunTestAndVerifyResponse(
                runtimeFlavor,
                runtimeArchitecture,
                applicationBaseUrl,
                "ResourcesInFolder",
                "fr-FR",
                "Bonjour from StartupResourcesInFolder Bonjour from Test in resources folder Bonjour from Customer in resources folder");
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(RuntimeFlavor.Clr, "http://localhost:5070/", RuntimeArchitecture.x64)]
        [InlineData(RuntimeFlavor.CoreClr, "http://localhost:5071/", RuntimeArchitecture.x64)]
        public Task Localization_ResourcesInFolder_ReturnLocalizedValue_WithCultureFallback_Windows(
            RuntimeFlavor runtimeFlavor,
            string applicationBaseUrl,
            RuntimeArchitecture runtimeArchitecture)
        {
            var testRunner = new TestRunner(_applicationPath);
            return testRunner.RunTestAndVerifyResponse(
                runtimeFlavor,
                runtimeArchitecture,
                applicationBaseUrl,
                "ResourcesInFolder",
                "fr-FR-test",
                "Bonjour from StartupResourcesInFolder Bonjour from Test in resources folder Bonjour from Customer in resources folder");
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(RuntimeFlavor.Clr, "http://localhost:5070/", RuntimeArchitecture.x64)]
        [InlineData(RuntimeFlavor.CoreClr, "http://localhost:5071/", RuntimeArchitecture.x64)]
        public Task Localization_ResourcesInFolder_ReturnNonLocalizedValue_CultureHierarchyTooDeep_Windows(
            RuntimeFlavor runtimeFlavor,
            string applicationBaseUrl,
            RuntimeArchitecture runtimeArchitecture)
        {
            var testRunner = new TestRunner(_applicationPath);
            return testRunner.RunTestAndVerifyResponse(
                runtimeFlavor,
                runtimeArchitecture,
                applicationBaseUrl,
                "ResourcesInFolder",
                "fr-FR-test-again-too-deep-to-work",
                "Hello Hello Hello");
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Windows)]
        [FrameworkSkipCondition(RuntimeFrameworks.CoreCLR)]
        public Task Localization_ResourcesInFolder_ReturnLocalizedValue_Clr()
        {
            var testRunner = new TestRunner(_applicationPath);
            return testRunner.RunTestAndVerifyResponse(
                RuntimeFlavor.Clr,
                RuntimeArchitecture.x64,
                "http://localhost:5072",
                "ResourcesInFolder",
                "fr-FR",
                "Bonjour from StartupResourcesInFolder Bonjour from Test in resources folder Bonjour from Customer in resources folder");
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Windows)]
        [FrameworkSkipCondition(RuntimeFrameworks.CoreCLR)]
        public Task Localization_ResourcesInFolder_ReturnLocalizedValue_WithCultureFallback_Clr()
        {
            var testRunner = new TestRunner(_applicationPath);
            return testRunner.RunTestAndVerifyResponse(
                RuntimeFlavor.Clr,
                RuntimeArchitecture.x64,
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
            var testRunner = new TestRunner(_applicationPath);
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
            var testRunner = new TestRunner(_applicationPath);
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
        [InlineData(RuntimeFlavor.Clr, "http://localhost:5074/", RuntimeArchitecture.x64)]
        [InlineData(RuntimeFlavor.CoreClr, "http://localhost:5075/", RuntimeArchitecture.x64)]
        public Task Localization_ResourcesAtRootFolder_ReturnLocalizedValue_Windows(
            RuntimeFlavor runtimeFlavor,
            string applicationBaseUrl,
            RuntimeArchitecture runtimeArchitecture)
        {
            var testRunner = new TestRunner(_applicationPath);
            return testRunner.RunTestAndVerifyResponse(
                runtimeFlavor,
                runtimeArchitecture,
                applicationBaseUrl,
                "ResourcesAtRootFolder",
                "fr-FR",
                "Bonjour from StartupResourcesAtRootFolder Bonjour from Test in root folder Bonjour from Customer in Models folder");
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Windows)]
        [FrameworkSkipCondition(RuntimeFrameworks.CoreCLR)]
        public Task Localization_ResourcesAtRootFolder_ReturnLocalizedValue_Clr()
        {
            var testRunner = new TestRunner(_applicationPath);
            return testRunner.RunTestAndVerifyResponse(
                RuntimeFlavor.Clr,
                RuntimeArchitecture.x64,
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
            var testRunner = new TestRunner(_applicationPath);
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
