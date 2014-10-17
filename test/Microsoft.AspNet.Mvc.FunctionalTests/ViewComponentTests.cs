// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.TestHost;
using ViewComponentWebSite;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class ViewComponentTests
    {
        private readonly IServiceProvider _provider = TestHelper.CreateServices("ViewComponentWebSite");
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
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var body = await client.GetStringAsync("http://localhost/Home/" + actionName);

            // Assert
            Assert.Equal(expected, body.Trim());
        }

        [Fact]
        public async Task ViewComponents_SupportsValueType()
        {
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var body = await client.GetStringAsync("http://localhost/Home/ViewWithIntegerViewComponent");

            // Assert
            Assert.Equal("10", body.Trim());
        }
    }
}
