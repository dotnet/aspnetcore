// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Xunit;

namespace Microsoft.AspNetCore.Localization.FunctionalTests
{
    public class LocalizationSampleTest
    {
        private static readonly string _applicationPath = Path.Combine("samples", "LocalizationSample");

        [Fact]
        public Task RunSite()
        {
            var testRunner = new TestRunner(_applicationPath);

            return testRunner.RunTestAndVerifyResponseHeading(
                RuntimeArchitecture.x64,
                "My/Resources",
                "fr-FR",
                "<h1>Bonjour</h1>");
        }
    }
}
