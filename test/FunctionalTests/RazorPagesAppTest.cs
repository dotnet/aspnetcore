// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace FunctionalTests
{
    public class RazorPagesAppTest : IClassFixture<RazorPagesAppTest.TestFixture>
    {
        public RazorPagesAppTest(TestFixture fixture)
        {
            Fixture = fixture;
        }

        public ApplicationTestFixture Fixture { get; }

        public static TheoryData SupportedFlavorsTheoryData => RuntimeFlavors.SupportedFlavorsTheoryData;

        [ConditionalTheory]
        [MemberData(nameof(SupportedFlavorsTheoryData))]
        public async Task Precompilation_WorksForIndexPage_UsingFolderName(RuntimeFlavor flavor)
        {
            // Arrange
            using (var deployment = await Fixture.CreateDeploymentAsync(flavor))
            {
                // Act
                var response = await deployment.HttpClient.GetStringWithRetryAsync(
                "/",
                Fixture.Logger);

                // Assert
                TestEmbeddedResource.AssertContent("RazorPages.Index.txt", response);
            }
        }

        [ConditionalTheory]
        [MemberData(nameof(SupportedFlavorsTheoryData))]
        public async Task Precompilation_WorksForIndexPage_UsingFileName(RuntimeFlavor flavor)
        {
            // Arrange
            using (var deployment = await Fixture.CreateDeploymentAsync(flavor))
            {
                // Act
                var response = await deployment.HttpClient.GetStringWithRetryAsync(
                "/Index",
                Fixture.Logger);

                // Assert
                TestEmbeddedResource.AssertContent("RazorPages.Index.txt", response);
            }
        }

        [ConditionalTheory]
        [MemberData(nameof(SupportedFlavorsTheoryData))]
        public async Task Precompilation_WorksForPageWithModel(RuntimeFlavor flavor)
        {
            // Arrange
            using (var deployment = await Fixture.CreateDeploymentAsync(flavor))
            {
                // Act
                var response = await deployment.HttpClient.GetStringWithRetryAsync(
                "/PageWithModel?person=Dan",
                Fixture.Logger);

                // Assert
                TestEmbeddedResource.AssertContent("RazorPages.PageWithModel.txt", response);
            }
        }

        [ConditionalTheory]
        [MemberData(nameof(SupportedFlavorsTheoryData))]
        public async Task Precompilation_WorksForPageWithRoute(RuntimeFlavor flavor)
        {
            // Arrange
            using (var deployment = await Fixture.CreateDeploymentAsync(flavor))
            {
                // Act
                var response = await deployment.HttpClient.GetStringWithRetryAsync(
                "/PageWithRoute/Dan",
                Fixture.Logger);

                // Assert
                TestEmbeddedResource.AssertContent("RazorPages.PageWithRoute.txt", response);
            }
        }

        [ConditionalTheory]
        [MemberData(nameof(SupportedFlavorsTheoryData))]
        public async Task Precompilation_WorksForPageInNestedFolder(RuntimeFlavor flavor)
        {
            // Arrange
            using (var deployment = await Fixture.CreateDeploymentAsync(flavor))
            {
                // Act
                var response = await deployment.HttpClient.GetStringWithRetryAsync(
                "/Nested1/Nested2/PageWithTagHelper",
                Fixture.Logger);

                // Assert
                TestEmbeddedResource.AssertContent("RazorPages.Nested1.Nested2.PageWithTagHelper.txt", response);
            }
        }

        [ConditionalTheory]
        [MemberData(nameof(SupportedFlavorsTheoryData))]
        public async Task Precompilation_WorksWithPageConventions(RuntimeFlavor flavor)
        {
            // Arrange
            using (var deployment = await Fixture.CreateDeploymentAsync(flavor))
            {
                // Act
                var response = await RetryHelper.RetryRequest(
                () => deployment.HttpClient.GetAsync("/Auth/Index"),
                Fixture.Logger,
                retryCount: 5);

                // Assert
                Assert.Equal("/Login?ReturnUrl=%2FAuth%2FIndex", response.RequestMessage.RequestUri.PathAndQuery);
            }
        }

        public class TestFixture : ApplicationTestFixture
        {
            public TestFixture()
                : base("RazorPagesApp")
            {
            }
        }
    }
}
