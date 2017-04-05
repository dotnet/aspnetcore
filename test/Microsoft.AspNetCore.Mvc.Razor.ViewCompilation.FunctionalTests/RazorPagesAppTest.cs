// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.ViewCompilation
{
    public class RazorPagesAppTest : IClassFixture<RazorPagesAppTest.TestFixture>
    {
        public RazorPagesAppTest(TestFixture fixture)
        {
            Fixture = fixture;
        }

        public ApplicationTestFixture Fixture { get; }

        [Fact]
        public async Task Precompilation_WorksForIndexPage_UsingFolderName()
        {
            // Act
            var response = await Fixture.HttpClient.GetStringWithRetryAsync(
                "/",
                Fixture.Logger);

            // Assert
            TestEmbeddedResource.AssertContent("RazorPages.Index.txt", response);
        }

        [Fact]
        public async Task Precompilation_WorksForIndexPage_UsingFileName()
        {
            // Act
            var response = await Fixture.HttpClient.GetStringWithRetryAsync(
                "/Index",
                Fixture.Logger);

            // Assert
            TestEmbeddedResource.AssertContent("RazorPages.Index.txt", response);
        }

        [Fact]
        public async Task Precompilation_WorksForPageWithModel()
        {
            // Act
            var response = await Fixture.HttpClient.GetStringWithRetryAsync(
                "/PageWithModel?person=Dan",
                Fixture.Logger);

            // Assert
            TestEmbeddedResource.AssertContent("RazorPages.PageWithModel.txt", response);
        }

        [Fact]
        public async Task Precompilation_WorksForPageWithRoute()
        {
            // Act
            var response = await Fixture.HttpClient.GetStringWithRetryAsync(
                "/PageWithRoute/Dan",
                Fixture.Logger);

            // Assert
            TestEmbeddedResource.AssertContent("RazorPages.PageWithRoute.txt", response);
        }

        [Fact]
        public async Task Precompilation_WorksForPageInNestedFolder()
        {
            // Act
            var response = await Fixture.HttpClient.GetStringWithRetryAsync(
                "/Nested1/Nested2/PageWithTagHelper",
                Fixture.Logger);

            // Assert
            TestEmbeddedResource.AssertContent("RazorPages.Nested1.Nested2.PageWithTagHelper.txt", response);
        }

        [Fact]
        public async Task Precompilation_WorksWithPageConventions()
        {
            // Act
            var response = await RetryHelper.RetryRequest(
                () => Fixture.HttpClient.GetAsync("/Auth/Index"),
                Fixture.Logger,
                retryCount: 5);

            // Assert
            Assert.Equal("/Login?ReturnUrl=%2FAuth%2FIndex", response.RequestMessage.RequestUri.PathAndQuery);
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
