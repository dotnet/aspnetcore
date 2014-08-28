// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.TestHost;
using RazorWebSite;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class ViewEngineTests
    {
        private readonly IServiceProvider _provider = TestHelper.CreateServices("RazorWebSite");
        private readonly Action<IBuilder> _app = new Startup().Configure;

        public static IEnumerable<object[]> RazorView_ExecutesPageAndLayoutData
        {
            get
            {
                yield return new[] { "ViewWithoutLayout", @"ViewWithoutLayout-Content" };
                yield return new[]
                {
                    "ViewWithLayout",
@"<layout>

ViewWithLayout-Content
</layout>"
                };
                yield return new[]
                {
                    "ViewWithFullPath",
@"<layout>

ViewWithFullPath-content
</layout>"
                };
                yield return new[]
                {
                    "ViewWithNestedLayout",
@"<layout>

<nested-layout>
/ViewEngine/ViewWithNestedLayout

ViewWithNestedLayout-Content
</nested-layout>
</layout>"
                };
            }
        }

        [Theory]
        [MemberData("RazorView_ExecutesPageAndLayoutData")]
        public async Task RazorView_ExecutesPageAndLayout(string actionName, string expected)
        {
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var body = await client.GetStringAsync("http://localhost/ViewEngine/" + actionName);

            // Assert
            Assert.Equal(expected, body.Trim());
        }

        [Fact]
        public async Task RazorView_ExecutesPartialPagesWithCorrectContext()
        {
            var expected =
@"<partial>98052

</partial>
test-value";
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var body = await client.GetStringAsync("http://localhost/ViewEngine/ViewWithPartial");

            // Assert
            Assert.Equal(expected, body.Trim());
        }

        [Fact]
        public async Task RazorView_PassesViewContextBetweenViewAndLayout()
        {
            var expected =
@"<title>Page title</title>

partial-content
component-content";
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var body = await client.GetStringAsync("http://localhost/ViewEngine/ViewPassesViewDataToLayout");

            // Assert
            Assert.Equal(expected, body.Trim());
        }
    }
}