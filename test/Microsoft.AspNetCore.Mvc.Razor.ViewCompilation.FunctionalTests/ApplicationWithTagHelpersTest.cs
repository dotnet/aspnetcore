// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.ViewCompilation
{
    public class TagHelperTest : IClassFixture<TagHelperTest.ApplicationWithTagHelpersFixture>
    {
        public TagHelperTest(ApplicationWithTagHelpersFixture fixture)
        {
            Fixture = fixture;
        }

        public ApplicationTestFixture Fixture { get; }

        public static TheoryData ApplicationWithTagHelpersData
        {
            get
            {
                var urls = new[]
                {
                    "ClassLibraryTagHelper",
                    "LocalTagHelper",
                };

                var data = new TheoryData<string, RuntimeFlavor>();
                foreach (var runtimeFlavor in RuntimeFlavors.SupportedFlavors)
                {
                    foreach(var url in urls)
                    {
                        data.Add(url, runtimeFlavor);
                    }
                }

                return data;
            }
        }

        [Theory]
        [MemberData(nameof(ApplicationWithTagHelpersData))]
        public async Task Precompilation_WorksForViewsThatUseTagHelpers(string url, RuntimeFlavor flavor)
        {
            // Arrange
            Fixture.CreateDeployment(flavor);

            // Act
            var response = await Fixture.HttpClient.GetStringWithRetryAsync(
                $"Home/{url}",
                Fixture.Logger);

            // Assert
            TestEmbeddedResource.AssertContent($"ApplicationWithTagHelpers.Home.{url}.txt", response);
        }

        public class ApplicationWithTagHelpersFixture : ApplicationTestFixture
        {
            public ApplicationWithTagHelpersFixture()
                : base("ApplicationWithTagHelpers")
            {
            }
        }
    }
}
