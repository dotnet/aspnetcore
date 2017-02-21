// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Threading.Tasks;
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
                return new TheoryData<string>
                {
                    "ClassLibraryTagHelper",
                    "LocalTagHelper",
                };
            }
        }

        [Theory]
        [MemberData(nameof(ApplicationWithTagHelpersData))]
        public async Task Precompilation_WorksForViewsThatUseTagHelpers(string url)
        {
            // Arrange
            using (var deployer = Fixture.CreateDeployment())
            {
                var deploymentResult = deployer.Deploy();

                // Act
                var response = await Fixture.HttpClient.GetStringWithRetryAsync(
                    $"{deploymentResult.ApplicationBaseUri}Home/{url}",
                    Fixture.Logger);

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
