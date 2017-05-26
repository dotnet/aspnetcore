// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.ViewCompilation
{
    public class ApplicationWithParseErrorsTest
    {
        public static TheoryData SupportedFlavorsTheoryData => RuntimeFlavors.SupportedFlavorsTheoryData;

        [ConditionalTheory]
        [MemberData(nameof(SupportedFlavorsTheoryData))]
        public async Task PublishingPrintsParseErrors(RuntimeFlavor flavor)
        {
            // Arrange
            var applicationPath = ApplicationPaths.GetTestAppDirectory("ApplicationWithParseErrors");
            var indexPath = Path.Combine(applicationPath, "Views", "Home", "Index.cshtml");
            var viewImportsPath = Path.Combine(applicationPath, "Views", "Home", "About.cshtml");
            var expectedErrors = new[]
            {
                indexPath + " (0): The code block is missing a closing \"}\" character.  Make sure you have a matching \"}\" character for all the \"{\" characters within this block, and that none of the \"}\" characters are being interpreted as markup.",
                viewImportsPath + " (1): A space or line break was encountered after the \"@\" character.  Only valid identifiers, keywords, comments, \"(\" and \"{\" are valid at the start of a code block and they must occur immediately following \"@\" with no space in between.",

            };
            var testSink = new TestSink();
            var deploymentParameters = ApplicationTestFixture.GetDeploymentParameters(applicationPath, flavor);
            var loggerFactory = new TestLoggerFactory(testSink, enabled: true);
            using (var deployer = ApplicationDeployerFactory.Create(deploymentParameters, loggerFactory))
            {
                // Act
                await Assert.ThrowsAsync<Exception>(() => deployer.DeployAsync());

                // Assert
                var logs = testSink.Writes.Select(w => w.State.ToString().Trim()).ToList();
                foreach (var expectedError in expectedErrors)
                {
                    Assert.Contains(logs, log => log.Contains(expectedError));
                }
            }
        }
    }
}
