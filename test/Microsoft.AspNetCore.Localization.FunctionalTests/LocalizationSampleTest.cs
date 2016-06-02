// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Testing;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.AspNetCore.Localization.FunctionalTests
{
    public class LocalizationSampleTest
    {
        private static readonly string _applicationPath =  Path.Combine("samples", "LocalizationSample");

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(RuntimeFlavor.Clr, "http://localhost:5080/", RuntimeArchitecture.x64)]
        [InlineData(RuntimeFlavor.CoreClr, "http://localhost:5081/", RuntimeArchitecture.x64)]
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
        //[InlineData(RuntimeFlavor.Clr, "http://localhost:5080/", RuntimeArchitecture.x64)] // Disabled due to https://github.com/dotnet/corefx/issues/9012
        [InlineData(RuntimeFlavor.CoreClr, "http://localhost:5081/", RuntimeArchitecture.x64)]
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
