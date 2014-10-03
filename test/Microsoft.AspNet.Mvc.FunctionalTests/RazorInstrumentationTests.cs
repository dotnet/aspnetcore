// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.TestHost;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;
using RazorInstrumentationWebSite;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class RazorInstrumentationTests
    {
        private readonly IServiceProvider _services = TestHelper.CreateServices("RazorInstrumentationWebsite");
        private readonly Action<IApplicationBuilder> _app = new Startup().Configure;
        private readonly string _expected = string.Join(Environment.NewLine,
                                                        @"<div>",
                                                        @"2147483647",
                                                        "",
                                                        @"viewstart-content",
                                                        @"<p class=""Hello world"">",
                                                        @"page-content",
                                                        @"</p>",
                                                        @"</div>");
        private readonly IEnumerable<Tuple<int, int, bool>> _expectedLineMappings = new[]
        {
            Tuple.Create(93, 2, true),
            Tuple.Create(96, 16, false),
            Tuple.Create(112, 2, true),
            Tuple.Create(0, 2, true),
            Tuple.Create(2, 8, true),
            Tuple.Create(10, 16, false),
            Tuple.Create(26, 1, true),
            Tuple.Create(27, 21, true),
            Tuple.Create(0, 7, true),
            Tuple.Create(8, 12, false),
            Tuple.Create(20, 2, true),
            Tuple.Create(23, 12, false),
            Tuple.Create(35, 8, true),
        };

        public static IEnumerable<object[]> ActionNames
        {
            get
            {
                yield return new[] { "FullPath" };
                yield return new[] { "ViewDiscoveryPath" };
            }
        }

        [Theory]
        [MemberData(nameof(ActionNames))]
        public async Task ViewsAreServedWithoutInstrumentationByDefault(string actionName)
        {
            // Arrange
            var context = new TestPageExecutionContext();
            var services = GetServiceProvider(context);
            var server = TestServer.Create(services, _app);
            var client = server.CreateClient();

            // Act
            var body = await client.GetStringAsync("http://localhost/Home/" + actionName);

            // Assert
            Assert.Equal(_expected, body.Trim());
            Assert.Empty(context.Values);
        }

        [Theory]
        [MemberData(nameof(ActionNames))]
        public async Task ViewsAreInstrumentedWhenPageExecutionListenerFeatureIsEnabled(string actionName)
        {
            // Arrange
            var context = new TestPageExecutionContext();
            var services = GetServiceProvider(context);
            var server = TestServer.Create(services, _app);
            var client = server.CreateClient();
            client.DefaultRequestHeaders.Add("ENABLE-RAZOR-INSTRUMENTATION", "true");

            // Act
            var body = await client.GetStringAsync("http://localhost/Home/" + actionName);

            // Assert
            Assert.Equal(_expected, body.Trim());
            Assert.Equal(_expectedLineMappings, context.Values);
        }

        [Theory]
        [MemberData(nameof(ActionNames))]
        public async Task ViewsCanSwitchFromRegularToInstrumented(string actionName)
        {
            // Arrange - 1
            var context = new TestPageExecutionContext();
            var services = GetServiceProvider(context);
            var server = TestServer.Create(services, _app);
            var client = server.CreateClient();

            // Act - 1
            var body = await client.GetStringAsync("http://localhost/Home/" + actionName);

            // Assert - 1
            Assert.Equal(_expected, body.Trim());
            Assert.Empty(context.Values);

            // Arrange - 2
            client.DefaultRequestHeaders.Add("ENABLE-RAZOR-INSTRUMENTATION", "true");

            // Act - 2
            body = await client.GetStringAsync("http://localhost/Home/" + actionName);

            // Assert - 2
            Assert.Equal(_expected, body.Trim());
            Assert.Equal(_expectedLineMappings, context.Values);
        }

        [Fact]
        public async Task SwitchingFromNonInstrumentedToInstrumentedWorksForLayoutAndViewStarts()
        {
            // Arrange - 1
            var context = new TestPageExecutionContext();
            var services = GetServiceProvider(context);
            var server = TestServer.Create(services, _app);
            var client = server.CreateClient();

            // Act - 1
            var body = await client.GetStringAsync("http://localhost/Home/FullPath");

            // Assert - 1
            Assert.Equal(_expected, body.Trim());
            Assert.Empty(context.Values);

            // Arrange - 2
            client.DefaultRequestHeaders.Add("ENABLE-RAZOR-INSTRUMENTATION", "true");

            // Act - 2
            body = await client.GetStringAsync("http://localhost/Home/ViewDiscoveryPath");

            // Assert - 2
            Assert.Equal(_expected, body.Trim());
            Assert.Equal(_expectedLineMappings, context.Values);
        }

        private IServiceProvider GetServiceProvider(TestPageExecutionContext pageExecutionContext)
        {
            var services = new ServiceCollection();
            services.AddInstance(pageExecutionContext);
            return services.BuildServiceProvider(_services);
        }
    }
}