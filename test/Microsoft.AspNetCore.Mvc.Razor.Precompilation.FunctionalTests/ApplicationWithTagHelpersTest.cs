// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Precompilation
{
    public class TagHelperTest : IClassFixture<TagHelperTest.ApplicationWithTagHelpersFixture>
    {
        public TagHelperTest(ApplicationWithTagHelpersFixture fixture)
        {
            Fixture = fixture;
        }

        public ApplicationTestFixture Fixture { get; }

        public static IEnumerable<object[]> ApplicationWithTagHelpersData
        {
            get
            {
                var urls = new[]
                {
                    "ClassLibraryTagHelper",
                    "LocalTagHelper",
                    "NuGetPackageTagHelper",
                };

                return Enumerable.Zip(urls, RuntimeFlavors.SupportedFlavors, (a, b) => new object[] { a, b });
            }
        }

        [Theory]
        [MemberData(nameof(ApplicationWithTagHelpersData))]
        public async Task Precompilation_WorksForViewsThatUseTagHelpers(string url, RuntimeFlavor flavor)
        {
            // Arrange
            using (var deployer = Fixture.CreateDeployment(flavor))
            {
                var deploymentResult = deployer.Deploy();
                var httpClient = new HttpClient()
                {
                    BaseAddress = new Uri(deploymentResult.ApplicationBaseUri)
                };

                // Act
                var response = await httpClient.GetStringWithRetryAsync($"Home/{url}", Fixture.Logger);

                // Assert
                TestEmbeddedResource.AssertContent($"ApplicationWithTagHelpers.Home.{url}.txt", response);
            }
        }

        public class ApplicationWithTagHelpersFixture : ApplicationTestFixture
        {
            public ApplicationWithTagHelpersFixture()
                : base("ApplicationWithTagHelpers")
            {
            }

            protected override void Restore()
            {
                RestoreProject(Path.GetFullPath(Path.Combine(ApplicationPath, "..", "ClassLibraryTagHelper")));
                base.Restore();
            }
        }
    }
}
