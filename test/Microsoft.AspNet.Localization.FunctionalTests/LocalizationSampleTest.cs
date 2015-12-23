// Copyright (c) .NET Foundation. All rights reserved. 
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNet.Server.Testing;
using Microsoft.AspNet.Testing.xunit;
using Xunit;

namespace Microsoft.AspNet.Localization.FunctionalTests
{
    public class LocalizationSampleTest
    {
        private static readonly string _applicationPath = Path.GetFullPath(Path.Combine("../../samples", "LocalizationSample"));
        
        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(RuntimeFlavor.Clr, "http://localhost:5070/", RuntimeArchitecture.x86)]
        [InlineData(RuntimeFlavor.CoreClr, "http://localhost:5071/", RuntimeArchitecture.x86)]
        public Task RunSite_WindowsOnly(RuntimeFlavor runtimeFlavor, string applicationBaseUrl, RuntimeArchitecture runtimeArchitecture)
        {
            var testRunner = new TestRunner(_applicationPath);
            return testRunner.RunTestAndVerifyResponseHeading(
                runtimeFlavor,
                runtimeArchitecture,
                applicationBaseUrl,
                "My/Resources",
                "fr-FR",
                "<h1>Bonjour</h1>");
        }
        
        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Windows)]
        [InlineData(RuntimeFlavor.Mono, "http://localhost:5070/", RuntimeArchitecture.x64)]
        [InlineData(RuntimeFlavor.CoreClr, "http://localhost:5071/", RuntimeArchitecture.x64)]
        public Task RunSite_NonWindowsOnly(RuntimeFlavor runtimeFlavor, string applicationBaseUrl, RuntimeArchitecture runtimeArchitecture)
        {
            var testRunner = new TestRunner(_applicationPath);
            return testRunner.RunTestAndVerifyResponseHeading(
                runtimeFlavor,
                runtimeArchitecture,
                applicationBaseUrl,
                "My/Resources",
                "fr-FR",
                "<h1>Bonjour</h1>");
        }
    }
}
