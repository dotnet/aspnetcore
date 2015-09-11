// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.Framework.DependencyInjection;
using RazorPageExecutionInstrumentationWebSite;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class RazorPageExecutionInstrumentationTest
    {
        private const string SiteName = nameof(RazorPageExecutionInstrumentationWebSite);
        private readonly Action<IApplicationBuilder> _app = new Startup().Configure;
        private readonly Action<IServiceCollection> _configureServices = new Startup().ConfigureServices;

        public static IEnumerable<object[]> InstrumentationData
        {
            get
            {
                var expected = @"<div>
2147483647
viewstart-content
<p class=""Hello world"">
page-content
</p>
</div>";

                var expectedLineMappings = new[]
                {
                    Tuple.Create(92, 16, false),
                    Tuple.Create(108, 1, true),
                    Tuple.Create(0, 2, true),
                    Tuple.Create(2, 8, true),
                    Tuple.Create(10, 16, false),
                    Tuple.Create(26, 1, true),
                    Tuple.Create(27, 19, true),
                    Tuple.Create(0, 6, true),
                    Tuple.Create(7, 12, false),
                    Tuple.Create(19, 1, true),
                    Tuple.Create(21, 12, false),
                    Tuple.Create(33, 7, true),
                };

                yield return new object[] { "FullPath", expected, expectedLineMappings };
                yield return new object[] { "ViewDiscoveryPath", expected, expectedLineMappings };

                var expected2 = @"<div>
2147483647
viewstart-content
view-with-partial-content
<p class=""class"">partial-content</p>
<p class=""class"">partial-content</p>
</div>";
                var expectedLineMappings2 = new[]
                {
                    Tuple.Create(92, 16, false),
                    Tuple.Create(108, 1, true),
                    Tuple.Create(0, 26, true),
                    Tuple.Create(27, 39, false),
                    // Html.PartialAsync()
                    Tuple.Create(28, 2, true),
                    Tuple.Create(30, 8, true),
                    Tuple.Create(38, 4, false),
                    Tuple.Create(42, 1, true),
                    Tuple.Create(43, 20, true),
                    Tuple.Create(66, 1, true),
                    // Html.RenderPartial()
                    Tuple.Create(28, 2, true),
                    Tuple.Create(30, 8, true),
                    Tuple.Create(38, 4, false),
                    Tuple.Create(42, 1, true),
                    Tuple.Create(43, 20, true),
                    Tuple.Create(0, 6, true),
                    Tuple.Create(7, 12, false),
                    Tuple.Create(19, 1, true),
                    Tuple.Create(21, 12, false),
                    Tuple.Create(33, 7, true)
                };
                yield return new object[] { "ViewWithPartial", expected2, expectedLineMappings2 };
            }
        }

        [Theory]
        [MemberData(nameof(InstrumentationData))]
        public async Task ViewsAreServedWithoutInstrumentationByDefault(
            string actionName,
            string expected,
            IEnumerable<Tuple<int, int, bool>> ignored)
        {
            // Arrange
            var context = new TestPageExecutionContext();
            var server = TestHelper.CreateServer(_app, SiteName, services =>
            {
                services.AddInstance(context);
                _configureServices(services);
            });
            var client = server.CreateClient();

            // Act
            var body = await client.GetStringAsync("http://localhost/Home/" + actionName);

            // Assert
            Assert.Equal(expected, body.Trim(), ignoreLineEndingDifferences: true);
            Assert.Empty(context.Values);
        }

        [Theory]
        [MemberData(nameof(InstrumentationData))]
        public async Task ViewsAreInstrumentedWhenPageExecutionListenerFeatureIsEnabled(
            string actionName,
            string expected,
            IEnumerable<Tuple<int, int, bool>> expectedLineMappings)
        {
            // Arrange
            var context = new TestPageExecutionContext();
            var server = TestHelper.CreateServer(_app, SiteName, services =>
            {
                services.AddInstance(context);
                _configureServices(services);
            });
            var client = server.CreateClient();
            client.DefaultRequestHeaders.Add("ENABLE-RAZOR-INSTRUMENTATION", "true");

            // Act
            var body = await client.GetStringAsync("http://localhost/Home/" + actionName);

            // Assert
            Assert.Equal(expected, body.Trim(), ignoreLineEndingDifferences: true);
            Assert.Equal(expectedLineMappings, context.Values);
        }

        [Theory]
        [MemberData(nameof(InstrumentationData))]
        public async Task ViewsCanSwitchFromRegularToInstrumented(
            string actionName,
            string expected,
            IEnumerable<Tuple<int, int, bool>> expectedLineMappings)
        {
            // Arrange - 1
            var context = new TestPageExecutionContext();
            var server = TestHelper.CreateServer(_app, SiteName, services =>
            {
                services.AddInstance(context);
                _configureServices(services);
            });
            var client = server.CreateClient();

            // Act - 1
            var body = await client.GetStringAsync("http://localhost/Home/" + actionName);

            // Assert - 1
            Assert.Equal(expected, body.Trim(), ignoreLineEndingDifferences: true);
            Assert.Empty(context.Values);

            // Arrange - 2
            client.DefaultRequestHeaders.Add("ENABLE-RAZOR-INSTRUMENTATION", "true");

            // Act - 2
            body = await client.GetStringAsync("http://localhost/Home/" + actionName);

            // Assert - 2
            Assert.Equal(expected, body.Trim(), ignoreLineEndingDifferences: true);
            Assert.Equal(expectedLineMappings, context.Values);
        }

        [Fact]
        public async Task SwitchingFromNonInstrumentedToInstrumentedWorksForLayoutAndViewStarts()
        {
            // Arrange - 1
            var expectedLineMappings = new[]
            {
                Tuple.Create(92, 16, false),
                Tuple.Create(108, 1, true),
                Tuple.Create(0, 2, true),
                Tuple.Create(2, 8, true),
                Tuple.Create(10, 16, false),
                Tuple.Create(26, 1, true),
                Tuple.Create(27, 19, true),
                Tuple.Create(0, 6, true),
                Tuple.Create(7, 12, false),
                Tuple.Create(19, 1, true),
                Tuple.Create(21, 12, false),
                Tuple.Create(33, 7, true),
            };
            var context = new TestPageExecutionContext();
            var server = TestHelper.CreateServer(_app, SiteName, services =>
            {
                services.AddInstance(context);
                _configureServices(services);
            });
            var client = server.CreateClient();

            // Act - 1
            var body = await client.GetStringAsync("http://localhost/Home/FullPath");

            // Assert - 1
            Assert.Empty(context.Values);

            // Arrange - 2
            client.DefaultRequestHeaders.Add("ENABLE-RAZOR-INSTRUMENTATION", "true");

            // Act - 2
            body = await client.GetStringAsync("http://localhost/Home/ViewDiscoveryPath");

            // Assert - 2
            Assert.Equal(expectedLineMappings, context.Values);
        }
    }
}