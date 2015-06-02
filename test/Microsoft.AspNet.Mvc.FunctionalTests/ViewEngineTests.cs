// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Net.Http.Headers;
using RazorWebSite;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class ViewEngineTests
    {
        private const string SiteName = nameof(RazorWebSite);
        private static readonly Assembly _assembly = typeof(ViewEngineTests).GetTypeInfo().Assembly;

        private readonly Action<IApplicationBuilder> _app = new Startup().Configure;
        private readonly Action<IServiceCollection> _configureServices = new Startup().ConfigureServices;

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

                yield return new[]
                {
                    "ViewWithDataFromController",
                    "<h1>hello from controller</h1>"
                };
            }
        }

        [Theory]
        [MemberData(nameof(RazorView_ExecutesPageAndLayoutData))]
        public async Task RazorView_ExecutesPageAndLayout(string actionName, string expected)
        {
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Act
            var body = await client.GetStringAsync("http://localhost/ViewEngine/" + actionName);

            // Assert
            Assert.Equal(expected, body.Trim());
        }

        [Fact]
        public async Task RazorView_ExecutesPartialPagesWithCorrectContext()
        {
            var expected = string.Join(Environment.NewLine,
                                       "<partial>98052",
                                       "",
                                       "</partial>",
                                       "<partial2>98052",
                                       "",
                                       "</partial2>",
                                       "test-value");
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Act
            var body = await client.GetStringAsync("http://localhost/ViewEngine/ViewWithPartial");

            // Assert
            Assert.Equal(expected, body.Trim());
        }

        [Fact]
        public async Task RazorView_DoesNotThrow_PartialViewWithEnumerableModel()
        {
            // Arrange
            var expected = "HelloWorld";
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Act
            var body = await client.GetStringAsync(
                "http://localhost/ViewEngine/ViewWithPartialTakingModelFromIEnumerable");

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
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Act
            var body = await client.GetStringAsync("http://localhost/ViewEngine/ViewPassesViewDataToLayout");

            // Assert
            Assert.Equal(expected, body.Trim());
        }

        public static IEnumerable<object[]> RazorViewEngine_UsesAllExpandedPathsToLookForViewsData
        {
            get
            {
                var expected1 = string.Join(Environment.NewLine,
                                            "expander-index",
                                            "gb-partial");
                yield return new[] { "en-GB", expected1 };

                var expected2 = string.Join(Environment.NewLine,
                                            "fr-index",
                                            "fr-partial");
                yield return new[] { "fr", expected2 };

                var expected3 = string.Join(Environment.NewLine,
                                            "expander-index",
                                            "expander-partial");
                yield return new[] { "na", expected3 };
            }
        }

        [Theory]
        [MemberData(nameof(RazorViewEngine_UsesAllExpandedPathsToLookForViewsData))]
        public async Task RazorViewEngine_UsesViewExpandersForViewsAndPartials(string value, string expected)
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();
            var cultureCookie = "c=" + value + "|uic=" + value;
            client.DefaultRequestHeaders.Add(
                "Cookie",
                new CookieHeaderValue("ASPNET_CULTURE", cultureCookie).ToString());

            // Act
            var body = await client.GetStringAsync("http://localhost/TemplateExpander");

            // Assert
            Assert.Equal(expected, body.Trim());
        }

        public static TheoryData ViewLocationExpanders_PassesInIsPartialToViewLocationExpanderContextData
        {
            get
            {
                return new TheoryData<string, string>
                {
                    { "Index", "<expander-view><shared-views>/Shared-Views/ExpanderViews/_ExpanderPartial.cshtml</shared-views></expander-view>" },
                    { "Partial", "<shared-views>/Shared-Views/ExpanderViews/_ExpanderPartial.cshtml</shared-views>" }
                };
            }
        }

        [Theory]
        [MemberData(nameof(ViewLocationExpanders_PassesInIsPartialToViewLocationExpanderContextData))]
        public async Task ViewLocationExpanders_PassesInIsPartialToViewLocationExpanderContext(string action, string expected)
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Act
            var body = await client.GetStringAsync($"http://localhost/ExpanderViews/{action}");

            // Assert
            Assert.Equal(expected, body.Trim());
        }

        public static IEnumerable<object[]> RazorViewEngine_RendersPartialViewsData
        {
            get
            {
                yield return new[]
                {
                    "ViewWithoutLayout", "ViewWithoutLayout-Content"
                };
                yield return new[]
                {
                    "PartialViewWithNamePassedIn",
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
/PartialViewEngine/ViewWithNestedLayout

ViewWithNestedLayout-Content
</nested-layout>
</layout>"
                };
                yield return new[]
                {
                    "PartialWithDataFromController", "<h1>hello from controller</h1>"
                };
                yield return new[]
                {
                    "PartialWithModel", string.Join(Environment.NewLine,
                                                    "my name is judge",
                                                    "<partial>98052",
                                                    "</partial>")
                };
            }
        }

        [Theory]
        [MemberData(nameof(RazorViewEngine_RendersPartialViewsData))]
        public async Task RazorViewEngine_RendersPartialViews(string actionName, string expected)
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Act
            var body = await client.GetStringAsync("http://localhost/PartialViewEngine/" + actionName);

            // Assert
            Assert.Equal(expected, body.Trim());
        }

        [Fact]
        public async Task LayoutValueIsPassedBetweenNestedViewStarts()
        {
            // Arrange
            var expected = string.Join(Environment.NewLine,
                                       "<title>viewstart-value</title>",
                                       "",
                                       "~/Views/NestedViewStarts/NestedViewStarts/Layout.cshtml",
                                       "index-content");
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Act
            var body = await client.GetStringAsync("http://localhost/NestedViewStarts");

            // Assert
            Assert.Equal(expected, body.Trim());
        }

        public static IEnumerable<object[]> RazorViewEngine_UsesExpandersForLayoutsData
        {
            get
            {
                var expected1 =
 @"<language-layout>
View With Layout
</language-layout>";

                yield return new[] { "en-GB", expected1 };
                yield return new[] { "na", expected1 };

                var expected2 =
 @"<fr-language-layout>
View With Layout
</fr-language-layout>";
                yield return new[] { "fr", expected2 };

            }
        }

        [Theory]
        [MemberData(nameof(RazorViewEngine_UsesExpandersForLayoutsData))]
        public async Task RazorViewEngine_UsesExpandersForLayouts(string value, string expected)
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();
            var cultureCookie = "c=" + value + "|uic=" + value;
            client.DefaultRequestHeaders.Add(
                "Cookie",
                new CookieHeaderValue("ASPNET_CULTURE", cultureCookie).ToString());

            // Act
            var body = await client.GetStringAsync("http://localhost/TemplateExpander/ViewWithLayout");

            // Assert
            Assert.Equal(expected, body.Trim());
        }

        [Fact]
        public async Task ViewStartsCanUseDirectivesInjectedFromParentGlobals()
        {
            // Arrange
            var expected =
@"<view-start>Hello Controller-Person</view-start>
<page>Hello Controller-Person</page>";
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();
            var target = "http://localhost/NestedViewImports";

            // Act
            var body = await client.GetStringAsync(target);

            // Assert
            Assert.Equal(expected, body.Trim());
        }

        [Fact]
        public async Task ViewComponentsExecuteLayout()
        {
            // Arrange
            var expected =
@"<title>View With Component With Layout</title>

Page Content
<component-title>ViewComponent With Title</component-title>
<component-body>
Component With Layout</component-body>";
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Act
            var body = await client.GetStringAsync("http://localhost/ViewEngine/ViewWithComponentThatHasLayout");

            // Assert
            Assert.Equal(expected, body.Trim());
        }

        [Fact]
        public async Task ViewComponentsDoNotExecuteViewStarts()
        {
            // Arrange
            var expected = @"<page-content>ViewComponent With ViewStart</page-content>";
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Act
            var body = await client.GetStringAsync("http://localhost/ViewEngine/ViewWithComponentThatHasViewStart");

            // Assert
            Assert.Equal(expected, body.Trim());
        }

        [Fact]
        public async Task PartialDoNotExecuteViewStarts()
        {
            // Arrange
            var expected = "Partial that does not specify Layout";
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Act
            var body = await client.GetStringAsync("http://localhost/PartialsWithLayout/PartialDoesNotExecuteViewStarts");

            // Assert
            Assert.Equal(expected, body.Trim());
        }

        [Fact]
        public async Task PartialsRenderedViaRenderPartialAsync_CanRenderLayouts()
        {
            // Arrange
            var expected =
@"<layout-for-viewstart-with-layout><layout-for-viewstart-with-layout>
Partial that specifies Layout
</layout-for-viewstart-with-layout>Partial that does not specify Layout
</layout-for-viewstart-with-layout>";
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Act
            var body = await client.GetStringAsync("http://localhost/PartialsWithLayout/PartialsRenderedViaRenderPartial");

            // Assert
            Assert.Equal(expected, body.Trim());
        }

        [Fact]
        public async Task PartialsRenderedViaPartialAsync_CanRenderLayouts()
        {
            // Arrange
            var expected =
@"<layout-for-viewstart-with-layout><layout-for-viewstart-with-layout>
Partial that specifies Layout
</layout-for-viewstart-with-layout>
Partial that does not specify Layout
</layout-for-viewstart-with-layout>";
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Act
            var body = await client.GetStringAsync("http://localhost/PartialsWithLayout/PartialsRenderedViaPartialAsync");

            // Assert
            Assert.Equal(expected, body.Trim());
        }

        [Fact]
        public async Task RazorView_SetsViewPathAndExecutingPagePath()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();
            var outputFile = "compiler/resources/ViewEngineController.ViewWithPaths.txt";
            var expectedContent = await ResourceFile.ReadResourceAsync(_assembly, outputFile, sourceFile: false);

            // Act
            var responseContent = await client.GetStringAsync("http://localhost/ViewWithPaths");

            // Assert
            responseContent = responseContent.Trim();
#if GENERATE_BASELINES
            ResourceFile.UpdateFile(_assembly, outputFile, expectedContent, responseContent);
#else
            Assert.Equal(expectedContent, responseContent);
#endif
        }
    }
}
