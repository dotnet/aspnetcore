// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using RazorWebSite;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class RazorViewLocationSpecificationTest
    {
        private const string BaseUrl = "http://localhost/ViewNameSpecification_Home/";
        private const string SiteName = nameof(RazorWebSite);
        private readonly Action<IApplicationBuilder> _app = new Startup().Configure;

        [Theory]
        [InlineData("LayoutSpecifiedWithPartialPathInViewStart")]
        [InlineData("LayoutSpecifiedWithPartialPathInViewStart_ForViewSpecifiedWithAppRelativePath")]
        [InlineData("LayoutSpecifiedWithPartialPathInViewStart_ForViewSpecifiedWithPartialName")]
        [InlineData("LayoutSpecifiedWithPartialPathInViewStart_ForViewSpecifiedWithAppRelativePathWithExtension")]
        public async Task PartialLayoutPaths_SpecifiedInViewStarts_GetResolvedByViewEngine(string action)
        {
            var expected =
@"<layout>
_ViewStart that specifies partial Layout
</layout>";
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();

            // Act
            var body = await client.GetStringAsync(BaseUrl + action);

            // Assert
            Assert.Equal(expected, body.Trim());
        }

        [Theory]
        [InlineData("LayoutSpecifiedWithPartialPathInPage")]
        [InlineData("LayoutSpecifiedWithPartialPathInPageWithPartialPath")]
        [InlineData("LayoutSpecifiedWithPartialPathInPageWithAppRelativePath")]
        [InlineData("LayoutSpecifiedWithPartialPathInPageWithAppRelativePathWithExtension")]
        public async Task PartialLayoutPaths_SpecifiedInPage_GetResolvedByViewEngine(string actionName)
        {
            var expected =
@"<non-shared>
Layout specified in page
</non-shared>";
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();

            // Act
            var body = await client.GetStringAsync(BaseUrl + actionName);

            // Assert
            Assert.Equal(expected, body.Trim());
        }

        [Theory]
        [InlineData("LayoutSpecifiedWithNonPartialPath")]
        [InlineData("LayoutSpecifiedWithNonPartialPathWithExtension")]
        public async Task NonPartialLayoutPaths_GetResolvedByViewEngine(string actionName)
        {
            var expected =
@"<non-shared>
Page With Non Partial Layout
</non-shared>";
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();

            // Act
            var body = await client.GetStringAsync(BaseUrl + actionName);

            // Assert
            Assert.Equal(expected, body.Trim());
        }

        [Theory]
        [InlineData("ViewWithPartial_SpecifiedWithPartialName")]
        [InlineData("ViewWithPartial_SpecifiedWithAbsoluteName")]
        [InlineData("ViewWithPartial_SpecifiedWithAbsoluteNameAndExtension")]
        public async Task PartialsCanBeSpecifiedWithPartialPath(string actionName)
        {
            var expected =
@"<layout>

Non Shared Partial

</layout>";
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();

            // Act
            var body = await client.GetStringAsync(BaseUrl + actionName);

            // Assert
            Assert.Equal(expected, body.Trim());
        }
    }
}