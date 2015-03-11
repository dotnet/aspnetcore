// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using ViewComponentWebSite;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class ViewComponentTests
    {
        private const string SiteName = nameof(ViewComponentWebSite);
        private readonly Action<IApplicationBuilder> _app = new Startup().Configure;

        public static IEnumerable<object[]> ViewViewComponents_AreRenderedCorrectlyData
        {
            get
            {
                yield return new[]
                {
                    "ViewWithAsyncComponents",
                    string.Join(Environment.NewLine,
                                       "<test-component>value-from-component value-from-view</test-component>",
                                        "ViewWithAsyncComponents InvokeAsync: hello from viewdatacomponent")
                };

                yield return new[]
                {
                    "ViewWithSyncComponents",
                    string.Join(Environment.NewLine,
                                       "<test-component>value-from-component value-from-view</test-component>",
                                        "ViewWithSyncComponents Invoke: hello from viewdatacomponent")
                };
            }
        }

        [Theory]
        [MemberData(nameof(ViewViewComponents_AreRenderedCorrectlyData))]
        public async Task ViewViewComponents_AreRenderedCorrectly(string actionName, string expected)
        {
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();

            // Act
            var body = await client.GetStringAsync("http://localhost/Home/" + actionName);

            // Assert
            Assert.Equal(expected, body.Trim());
        }

        [Fact]
        public async Task ViewComponents_SupportsValueType()
        {
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();

            // Act
            var body = await client.GetStringAsync("http://localhost/Home/ViewWithIntegerViewComponent");

            // Assert
            Assert.Equal("10", body.Trim());
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
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();

            // Act
            // https://github.com/aspnet/Mvc/issues/1354
            // The invoked ViewComponent/View has a model which is an internal type implementing Enumerable.
            // For ex - TestEnumerableObject.Select(t => t) returns WhereSelectListIterator
            var body = await client.GetStringAsync(url);

            // Assert
            Assert.Equal("<p>Hello</p><p>World</p><p>Sample</p><p>Test</p>"
                + "<p>Hello</p><p>World</p><p>" + linqQueryType + "</p><p>Test</p>", body.Trim());
        }

        [Theory]
        [InlineData("ViewComponentWebSite.Namespace1.SameName")]
        [InlineData("ViewComponentWebSite.Namespace2.SameName")]
        public async Task ViewComponents_FullName(string name)
        {
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();

            // Act
            var body = await client.GetStringAsync("http://localhost/FullName/Invoke?name=" + name);

            // Assert
            Assert.Equal(name, body.Trim());
        }

        [Fact]
        public async Task ViewComponents_ShortNameUsedForViewLookup()
        {
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();

            var name = "ViewComponentWebSite.Integer";

            // Act
            var body = await client.GetStringAsync("http://localhost/FullName/Invoke?name=" + name);

            // Assert
            Assert.Equal("17", body.Trim());
        }
    }
}
