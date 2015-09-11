// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class ViewComponentTests : IClassFixture<MvcTestFixture<ViewComponentWebSite.Startup>>
    {
        public ViewComponentTests(MvcTestFixture<ViewComponentWebSite.Startup> fixture)
        {
            Client = fixture.Client;
        }

        public HttpClient Client { get; }

        public static IEnumerable<object[]> ViewViewComponents_AreRenderedCorrectlyData
        {
            get
            {
                yield return new[]
                {
                    "ViewWithAsyncComponents",
                    @"<test-component>value-from-component value-from-view</test-component>
ViewWithAsyncComponents InvokeAsync: hello from viewdatacomponent"
                };

                yield return new[]
                {
                    "ViewWithSyncComponents",
                    @"<test-component>value-from-component value-from-view</test-component>
ViewWithSyncComponents Invoke: hello from viewdatacomponent"
                };
            }
        }

        [Theory]
        [MemberData(nameof(ViewViewComponents_AreRenderedCorrectlyData))]
        public async Task ViewViewComponents_AreRenderedCorrectly(string actionName, string expected)
        {
            // Arrange & Act
            var body = await Client.GetStringAsync("http://localhost/Home/" + actionName);

            // Assert
            Assert.Equal(expected, body.Trim(), ignoreLineEndingDifferences: true);
        }

        [Fact]
        public async Task ViewComponents_SupportsValueType()
        {
            // Arrange & Act
            var body = await Client.GetStringAsync("http://localhost/Home/ViewWithIntegerViewComponent");

            // Assert
            Assert.Equal("10", body.Trim());
        }

        [Fact]
        public async Task ViewComponents_InvokeWithViewComponentResult()
        {
            // Arrange & Act
            var body = await Client.GetStringAsync("http://localhost/ViewComponentResult/Invoke?number=31");

            // Assert
            Assert.Equal("31", body.Trim());
        }

        [Theory]
        [InlineData("http://localhost/Home/ViewComponentWithEnumerableModelUsingWhere", "Where")]
        [InlineData("http://localhost/Home/ViewComponentWithEnumerableModelUsingSelect", "Select")]
        [InlineData("http://localhost/Home/ViewComponentWithEnumerableModelUsingSelectMany", "SelectMany")]
        [InlineData("http://localhost/Home/ViewComponentWithEnumerableModelUsingTake", "Take")]
        [InlineData("http://localhost/Home/ViewComponentWithEnumerableModelUsingTakeWhile", "TakeWhile")]
        [InlineData("http://localhost/Home/ViewComponentWithEnumerableModelUsingUnion", "Union")]
        public async Task ViewComponents_SupportsEnumerableModel(string url, string linqQueryType)
        {
            // Arrange & Act
            // https://github.com/aspnet/Mvc/issues/1354
            // The invoked ViewComponent/View has a model which is an internal type implementing Enumerable.
            // For ex - TestEnumerableObject.Select(t => t) returns WhereSelectListIterator
            var body = await Client.GetStringAsync(url);

            // Assert
            Assert.Equal("<p>Hello</p><p>World</p><p>Sample</p><p>Test</p>"
                + "<p>Hello</p><p>World</p><p>" + linqQueryType + "</p><p>Test</p>", body.Trim());
        }

        [Theory]
        [InlineData("ViewComponentWebSite.Namespace1.SameName")]
        [InlineData("ViewComponentWebSite.Namespace2.SameName")]
        public async Task ViewComponents_FullName(string name)
        {
            // Arrange & Act
            var body = await Client.GetStringAsync("http://localhost/FullName/Invoke?name=" + name);

            // Assert
            Assert.Equal(name, body.Trim());
        }

        [Fact]
        public async Task ViewComponents_ShortNameUsedForViewLookup()
        {
            // Arrange
            var name = "ViewComponentWebSite.Integer";

            // Act
            var body = await Client.GetStringAsync("http://localhost/FullName/Invoke?name=" + name);

            // Assert
            Assert.Equal("17", body.Trim());
        }
    }
}
