// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Routing;
using Microsoft.AspNet.Routing.Template;
using Microsoft.AspNet.Mvc.Logging;
using Microsoft.Framework.OptionsModel;
using Microsoft.Framework.Logging;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Routing
{
    public class AttributeRouteTest
    {
        [Theory]
        [InlineData("template/5", "template/{parameter:int}")]
        [InlineData("template/5", "template/{parameter}")]
        [InlineData("template/5", "template/{*parameter:int}")]
        [InlineData("template/5", "template/{*parameter}")]
        [InlineData("template/{parameter:int}", "template/{parameter}")]
        [InlineData("template/{parameter:int}", "template/{*parameter:int}")]
        [InlineData("template/{parameter:int}", "template/{*parameter}")]
        [InlineData("template/{parameter}", "template/{*parameter:int}")]
        [InlineData("template/{parameter}", "template/{*parameter}")]
        [InlineData("template/{*parameter:int}", "template/{*parameter}")]
        public async Task AttributeRoute_RouteAsync_RespectsPrecedence(
            string firstTemplate,
            string secondTemplate)
        {
            // Arrange
            var expectedRouteGroup = string.Format("{0}&&{1}", 0, firstTemplate);

            // We need to force the creation of a closure in order to avoid an issue with Moq and Roslyn.
            var numberOfCalls = 0;
            Action<RouteContext> callBack = ctx => { ctx.IsHandled = true; numberOfCalls++; };

            var next = new Mock<IRouter>();
            next.Setup(r => r.RouteAsync(It.IsAny<RouteContext>()))
                            .Callback(callBack)
                            .Returns(Task.FromResult(true))
                            .Verifiable();

            var firstRoute = CreateMatchingEntry(next.Object, firstTemplate, order: 0);
            var secondRoute = CreateMatchingEntry(next.Object, secondTemplate, order: 0);

            // We setup the route entries in reverse order of precedence to ensure that when we
            // try to route the request, the route with a higher precedence gets tried first.
            var matchingRoutes = new[] { secondRoute, firstRoute };

            var linkGenerationEntries = Enumerable.Empty<AttributeRouteLinkGenerationEntry>();

            var route = new AttributeRoute(next.Object, matchingRoutes, linkGenerationEntries, NullLoggerFactory.Instance);

            var context = CreateRouteContext("/template/5");

            // Act
            await route.RouteAsync(context);

            // Assert
            Assert.Equal(expectedRouteGroup, context.RouteData.Values["test_route_group"]);
        }

        [Theory]
        [InlineData("template/5", "template/{parameter:int}")]
        [InlineData("template/5", "template/{parameter}")]
        [InlineData("template/5", "template/{*parameter:int}")]
        [InlineData("template/5", "template/{*parameter}")]
        [InlineData("template/{parameter:int}", "template/{parameter}")]
        [InlineData("template/{parameter:int}", "template/{*parameter:int}")]
        [InlineData("template/{parameter:int}", "template/{*parameter}")]
        [InlineData("template/{parameter}", "template/{*parameter:int}")]
        [InlineData("template/{parameter}", "template/{*parameter}")]
        [InlineData("template/{*parameter:int}", "template/{*parameter}")]
        public async Task AttributeRoute_RouteAsync_RespectsOrderOverPrecedence(
            string firstTemplate,
            string secondTemplate)
        {
            // Arrange
            var expectedRouteGroup = string.Format("{0}&&{1}", 0, secondTemplate);

            // We need to force the creation of a closure in order to avoid an issue with Moq and Roslyn.
            var numberOfCalls = 0;
            Action<RouteContext> callBack = ctx => { ctx.IsHandled = true; numberOfCalls++; };

            var next = new Mock<IRouter>();
            next.Setup(r => r.RouteAsync(It.IsAny<RouteContext>()))
                            .Callback(callBack)
                            .Returns(Task.FromResult(true))
                            .Verifiable();

            var firstRoute = CreateMatchingEntry(next.Object, firstTemplate, order: 1);
            var secondRoute = CreateMatchingEntry(next.Object, secondTemplate, order: 0);

            // We setup the route entries with a lower relative order and higher relative precedence
            // first to ensure that when we try to route the request, the route with the higher
            // relative order gets tried first.
            var matchingRoutes = new[] { firstRoute, secondRoute };

            var linkGenerationEntries = Enumerable.Empty<AttributeRouteLinkGenerationEntry>();

            var route = new AttributeRoute(next.Object, matchingRoutes, linkGenerationEntries, NullLoggerFactory.Instance);

            var context = CreateRouteContext("/template/5");

            // Act
            await route.RouteAsync(context);

            // Assert
            Assert.Equal(expectedRouteGroup, context.RouteData.Values["test_route_group"]);
        }

        [Theory]
        [InlineData("template/5")]
        [InlineData("template/{parameter:int}")]
        [InlineData("template/{parameter}")]
        [InlineData("template/{*parameter:int}")]
        [InlineData("template/{*parameter}")]
        public async Task AttributeRoute_RouteAsync_RespectsOrder(string template)
        {
            // Arrange
            var expectedRouteGroup = string.Format("{0}&&{1}", 0, template);

            // We need to force the creation of a closure in order to avoid an issue with Moq and Roslyn.
            var numberOfCalls = 0;
            Action<RouteContext> callBack = ctx => { ctx.IsHandled = true; numberOfCalls++; };

            var next = new Mock<IRouter>();
            next.Setup(r => r.RouteAsync(It.IsAny<RouteContext>()))
                            .Callback(callBack)
                            .Returns(Task.FromResult(true))
                            .Verifiable();

            var firstRoute = CreateMatchingEntry(next.Object, template, order: 1);
            var secondRoute = CreateMatchingEntry(next.Object, template, order: 0);

            // We setup the route entries with a lower relative order first to ensure that when
            // we try to route the request, the route with the higher relative order gets tried first.
            var matchingRoutes = new[] { firstRoute, secondRoute };

            var linkGenerationEntries = Enumerable.Empty<AttributeRouteLinkGenerationEntry>();

            var route = new AttributeRoute(next.Object, matchingRoutes, linkGenerationEntries, NullLoggerFactory.Instance);

            var context = CreateRouteContext("/template/5");

            // Act
            await route.RouteAsync(context);

            // Assert
            Assert.Equal(expectedRouteGroup, context.RouteData.Values["test_route_group"]);
        }

        [Theory]
        [InlineData("template/{first:int}", "template/{second:int}")]
        [InlineData("template/{first}", "template/{second}")]
        [InlineData("template/{*first:int}", "template/{*second:int}")]
        [InlineData("template/{*first}", "template/{*second}")]
        public async Task AttributeRoute_RouteAsync_EnsuresStableOrdering(string first, string second)
        {
            // Arrange
            var expectedRouteGroup = string.Format("{0}&&{1}", 0, first);

            // We need to force the creation of a closure in order to avoid an issue with Moq and Roslyn.
            var numberOfCalls = 0;
            Action<RouteContext> callBack = ctx => { ctx.IsHandled = true; numberOfCalls++; };

            var next = new Mock<IRouter>();
            next.Setup(r => r.RouteAsync(It.IsAny<RouteContext>()))
                            .Callback(callBack)
                            .Returns(Task.FromResult(true))
                            .Verifiable();

            var secondRouter = new Mock<IRouter>(MockBehavior.Strict);

            var firstRoute = CreateMatchingEntry(next.Object, first, order: 0);
            var secondRoute = CreateMatchingEntry(next.Object, second, order: 0);

            // We setup the route entries with a lower relative template order first to ensure that when
            // we try to route the request, the route with the higher template order gets tried first.
            var matchingRoutes = new[] { secondRoute, firstRoute };

            var linkGenerationEntries = Enumerable.Empty<AttributeRouteLinkGenerationEntry>();

            var route = new AttributeRoute(next.Object, matchingRoutes, linkGenerationEntries, NullLoggerFactory.Instance);

            var context = CreateRouteContext("/template/5");

            // Act
            await route.RouteAsync(context);

            // Assert
            Assert.Equal(expectedRouteGroup, context.RouteData.Values["test_route_group"]);
        }

        [Theory]
        [InlineData("template/5", "template/{parameter:int}")]
        [InlineData("template/5", "template/{parameter}")]
        [InlineData("template/5", "template/{*parameter:int}")]
        [InlineData("template/5", "template/{*parameter}")]
        [InlineData("template/{parameter:int}", "template/{parameter}")]
        [InlineData("template/{parameter:int}", "template/{*parameter:int}")]
        [InlineData("template/{parameter:int}", "template/{*parameter}")]
        [InlineData("template/{parameter}", "template/{*parameter:int}")]
        [InlineData("template/{parameter}", "template/{*parameter}")]
        [InlineData("template/{*parameter:int}", "template/{*parameter}")]
        public void AttributeRoute_GenerateLink_RespectsPrecedence(string firstTemplate, string secondTemplate)
        {
            // Arrange
            var expectedGroup = CreateRouteGroup(0, firstTemplate);

            string selectedGroup = null;

            var next = new Mock<IRouter>();
            next.Setup(n => n.GetVirtualPath(It.IsAny<VirtualPathContext>())).Callback<VirtualPathContext>(ctx =>
            {
                selectedGroup = (string)ctx.ProvidedValues[AttributeRouting.RouteGroupKey];
                ctx.IsBound = true;
            })
            .Returns((string)null);

            var matchingRoutes = Enumerable.Empty<AttributeRouteMatchingEntry>();

            var firstEntry = CreateGenerationEntry(firstTemplate, requiredValues: null);
            var secondEntry = CreateGenerationEntry(secondTemplate, requiredValues: null, order: 0);

            // We setup the route entries in reverse order of precedence to ensure that when we
            // try to generate a link, the route with a higher precedence gets tried first.
            var linkGenerationEntries = new[] { secondEntry, firstEntry };

            var route = new AttributeRoute(next.Object, matchingRoutes, linkGenerationEntries, NullLoggerFactory.Instance);

            var context = CreateVirtualPathContext(values: null, ambientValues: new { parameter = 5 });

            // Act
            string result = route.GetVirtualPath(context);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("template/5", result);
            Assert.Equal(expectedGroup, selectedGroup);
        }

        [Theory]
        [InlineData("template/5", "template/{parameter:int}")]
        [InlineData("template/5", "template/{parameter}")]
        [InlineData("template/5", "template/{*parameter:int}")]
        [InlineData("template/5", "template/{*parameter}")]
        [InlineData("template/{parameter:int}", "template/{parameter}")]
        [InlineData("template/{parameter:int}", "template/{*parameter:int}")]
        [InlineData("template/{parameter:int}", "template/{*parameter}")]
        [InlineData("template/{parameter}", "template/{*parameter:int}")]
        [InlineData("template/{parameter}", "template/{*parameter}")]
        [InlineData("template/{*parameter:int}", "template/{*parameter}")]
        public void AttributeRoute_GenerateLink_RespectsOrderOverPrecedence(string firstTemplate, string secondTemplate)
        {
            // Arrange
            var selectedGroup = CreateRouteGroup(0, secondTemplate);

            string firstRouteGroupSelected = null;
            var next = new Mock<IRouter>();
            next.Setup(n => n.GetVirtualPath(It.IsAny<VirtualPathContext>())).Callback<VirtualPathContext>(ctx =>
            {
                firstRouteGroupSelected = (string)ctx.ProvidedValues[AttributeRouting.RouteGroupKey];
                ctx.IsBound = true;
            })
            .Returns((string)null);

            var matchingRoutes = Enumerable.Empty<AttributeRouteMatchingEntry>();

            var firstRoute = CreateGenerationEntry(firstTemplate, requiredValues: null, order: 1);
            var secondRoute = CreateGenerationEntry(secondTemplate, requiredValues: null, order: 0);

            // We setup the route entries with a lower relative order and higher relative precedence
            // first to ensure that when we try to generate a link, the route with the higher
            // relative order gets tried first.
            var linkGenerationEntries = new[] { firstRoute, secondRoute };

            var route = new AttributeRoute(next.Object, matchingRoutes, linkGenerationEntries, NullLoggerFactory.Instance);

            var context = CreateVirtualPathContext(null, ambientValues: new { parameter = 5 });

            // Act
            string result = route.GetVirtualPath(context);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("template/5", result);
            Assert.Equal(selectedGroup, firstRouteGroupSelected);
        }

        [Theory]
        [InlineData("template/5", "template/5")]
        [InlineData("template/{first:int}", "template/{second:int}")]
        [InlineData("template/{first}", "template/{second}")]
        [InlineData("template/{*first:int}", "template/{*second:int}")]
        [InlineData("template/{*first}", "template/{*second}")]
        public void AttributeRoute_GenerateLink_RespectsOrder(string firstTemplate, string secondTemplate)
        {
            // Arrange
            var expectedGroup = CreateRouteGroup(0, secondTemplate);

            var next = new Mock<IRouter>();
            string selectedGroup = null;
            next.Setup(n => n.GetVirtualPath(It.IsAny<VirtualPathContext>())).Callback<VirtualPathContext>(ctx =>
            {
                selectedGroup = (string)ctx.ProvidedValues[AttributeRouting.RouteGroupKey];
                ctx.IsBound = true;
            })
            .Returns((string)null);

            var matchingRoutes = Enumerable.Empty<AttributeRouteMatchingEntry>();

            var firstRoute = CreateGenerationEntry(firstTemplate, requiredValues: null, order: 1);
            var secondRoute = CreateGenerationEntry(secondTemplate, requiredValues: null, order: 0);

            // We setup the route entries with a lower relative order first to ensure that when
            // we try to generate a link, the route with the higher relative order gets tried first.
            var linkGenerationEntries = new[] { firstRoute, secondRoute };

            var route = new AttributeRoute(next.Object, matchingRoutes, linkGenerationEntries, NullLoggerFactory.Instance);

            var context = CreateVirtualPathContext(values: null, ambientValues: new { first = 5, second = 5 });

            // Act
            string result = route.GetVirtualPath(context);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("template/5", result);
            Assert.Equal(expectedGroup, selectedGroup);
        }

        [Theory]
        [InlineData("first/5", "second/5")]
        [InlineData("first/{first:int}", "second/{second:int}")]
        [InlineData("first/{first}", "second/{second}")]
        [InlineData("first/{*first:int}", "second/{*second:int}")]
        [InlineData("first/{*first}", "second/{*second}")]
        public void AttributeRoute_GenerateLink_EnsuresStableOrder(string firstTemplate, string secondTemplate)
        {
            // Arrange
            var expectedGroup = CreateRouteGroup(0, firstTemplate);

            var next = new Mock<IRouter>();
            string selectedGroup = null;
            next.Setup(n => n.GetVirtualPath(It.IsAny<VirtualPathContext>())).Callback<VirtualPathContext>(ctx =>
            {
                selectedGroup = (string)ctx.ProvidedValues[AttributeRouting.RouteGroupKey];
                ctx.IsBound = true;
            })
            .Returns((string)null);

            var matchingRoutes = Enumerable.Empty<AttributeRouteMatchingEntry>();

            var firstRoute = CreateGenerationEntry(firstTemplate, requiredValues: null, order: 0);
            var secondRoute = CreateGenerationEntry(secondTemplate, requiredValues: null, order: 0);

            // We setup the route entries with a lower relative template order first to ensure that when
            // we try to generate a link, the route with the higher template order gets tried first.
            var linkGenerationEntries = new[] { secondRoute, firstRoute };

            var route = new AttributeRoute(next.Object, matchingRoutes, linkGenerationEntries, NullLoggerFactory.Instance);

            var context = CreateVirtualPathContext(values: null, ambientValues: new { first = 5, second = 5 });

            // Act
            string result = route.GetVirtualPath(context);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("first/5", result);
            Assert.Equal(expectedGroup, selectedGroup);
        }

        public static IEnumerable<object[]> NamedEntriesWithDifferentTemplates
        {
            get
            {
                var data = new TheoryData<IEnumerable<AttributeRouteLinkGenerationEntry>>();
                data.Add(new[]
                {
                        CreateGenerationEntry("template", null, 0, "NamedEntry"),
                        CreateGenerationEntry("otherTemplate", null, 0, "NamedEntry"),
                        CreateGenerationEntry("anotherTemplate", null, 0, "NamedEntry")
                });

                // Default values for parameters are taken into account by comparing the templates.
                data.Add(new[]
                {
                        CreateGenerationEntry("template/{parameter=0}", null, 0, "NamedEntry"),
                        CreateGenerationEntry("template/{parameter=1}", null, 0, "NamedEntry"),
                        CreateGenerationEntry("template/{parameter=2}", null, 0, "NamedEntry")
                });

                // Names for entries are compared ignoring casing.
                data.Add(new[]
                {
                        CreateGenerationEntry("template/{*parameter:int=0}", null, 0, "NamedEntry"),
                        CreateGenerationEntry("template/{*parameter:int=1}", null, 0, "NAMEDENTRY"),
                        CreateGenerationEntry("template/{*parameter:int=2}", null, 0, "namedentry")
                });
                return data;
            }
        }

        [Theory]
        [MemberData(nameof(AttributeRouteTest.NamedEntriesWithDifferentTemplates))]
        public void AttributeRoute_CreateAttributeRoute_ThrowsIfDifferentEntriesHaveTheSameName(
            IEnumerable<AttributeRouteLinkGenerationEntry> namedEntries)
        {
            // Arrange
            string expectedExceptionMessage = "Two or more routes named 'NamedEntry' have different templates." +
                Environment.NewLine +
                "Parameter name: linkGenerationEntries";

            var next = new Mock<IRouter>().Object;

            var matchingEntries = Enumerable.Empty<AttributeRouteMatchingEntry>();

            // Act
            var exception = Assert.Throws<ArgumentException>(
                "linkGenerationEntries",
                () => new AttributeRoute(
                    next,
                    matchingEntries,
                    namedEntries,
                    NullLoggerFactory.Instance));

            Assert.Equal(expectedExceptionMessage, exception.Message, StringComparer.OrdinalIgnoreCase);
        }

        public static IEnumerable<object[]> NamedEntriesWithTheSameTemplate
        {
            get
            {
                var data = new TheoryData<IEnumerable<AttributeRouteLinkGenerationEntry>>();

                data.Add(new[]
                {
                        CreateGenerationEntry("template", null, 0, "NamedEntry"),
                        CreateGenerationEntry("template", null, 1, "NamedEntry"),
                        CreateGenerationEntry("template", null, 2, "NamedEntry")
                });

                // Templates are compared ignoring casing.
                data.Add(new[]
                {
                        CreateGenerationEntry("template", null, 0, "NamedEntry"),
                        CreateGenerationEntry("Template", null, 1, "NamedEntry"),
                        CreateGenerationEntry("TEMPLATE", null, 2, "NamedEntry")
                });

                data.Add(new[]
                {
                        CreateGenerationEntry("template/{parameter=0}", null, 0, "NamedEntry"),
                        CreateGenerationEntry("template/{parameter=0}", null, 1, "NamedEntry"),
                        CreateGenerationEntry("template/{parameter=0}", null, 2, "NamedEntry")
                });

                return data;
            }
        }

        [Theory]
        [MemberData(nameof(AttributeRouteTest.NamedEntriesWithTheSameTemplate))]
        public void AttributeRoute_GeneratesLink_ForMultipleNamedEntriesWithTheSameTemplate(
            IEnumerable<AttributeRouteLinkGenerationEntry> namedEntries)
        {
            // Arrange
            var expectedLink = namedEntries.First().Template.Parameters.Any() ? "template/5" : "template";

            var expectedGroup = "0&" + namedEntries.First().TemplateText;
            string selectedGroup = null;
            var next = new Mock<IRouter>();
            next.Setup(s => s.GetVirtualPath(It.IsAny<VirtualPathContext>()))
                .Callback<VirtualPathContext>(vpc =>
                {
                    vpc.IsBound = true;
                    selectedGroup = (string)vpc.ProvidedValues[AttributeRouting.RouteGroupKey];
                });

            var matchingEntries = Enumerable.Empty<AttributeRouteMatchingEntry>();

            var route = new AttributeRoute(
                    next.Object,
                    matchingEntries,
                    namedEntries,
                    NullLoggerFactory.Instance);

            var ambientValues = namedEntries.First().Template.Parameters.Any() ? new { parameter = 5 } : null;

            var context = CreateVirtualPathContext(values: null, ambientValues: ambientValues, name: "NamedEntry");

            // Act
            var result = route.GetVirtualPath(context);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedGroup, selectedGroup);
            Assert.Equal(expectedLink, result);
        }

        [Fact]
        public void AttributeRoute_GenerateLink_WithName()
        {
            // Arrange
            string selectedGroup = null;
            var next = new Mock<IRouter>();
            next.Setup(s => s.GetVirtualPath(It.IsAny<VirtualPathContext>()))
                .Callback<VirtualPathContext>(vpc =>
                {
                    vpc.IsBound = true;
                    selectedGroup = (string)vpc.ProvidedValues[AttributeRouting.RouteGroupKey];
                });

            var namedEntry = CreateGenerationEntry("named", requiredValues: null, order: 1, name: "NamedRoute");
            var unnamedEntry = CreateGenerationEntry("unnamed", requiredValues: null, order: 0);

            // The named route has a lower order which will ensure that we aren't trying the route as
            // if it were an unnamed route.
            var linkGenerationEntries = new[] { namedEntry, unnamedEntry };

            var matchingEntries = Enumerable.Empty<AttributeRouteMatchingEntry>();

            var route = new AttributeRoute(next.Object, matchingEntries, linkGenerationEntries, NullLoggerFactory.Instance);

            var context = CreateVirtualPathContext(values: null, ambientValues: null, name: "NamedRoute");

            // Act
            var result = route.GetVirtualPath(context);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("1&named", selectedGroup);
            Assert.Equal("named", result);
        }

        [Fact]
        public void AttributeRoute_DoesNotGenerateLink_IfThereIsNoRouteForAGivenName()
        {
            // Arrange
            string selectedGroup = null;
            var next = new Mock<IRouter>();
            next.Setup(s => s.GetVirtualPath(It.IsAny<VirtualPathContext>()))
                .Callback<VirtualPathContext>(vpc =>
                {
                    vpc.IsBound = true;
                    selectedGroup = (string)vpc.ProvidedValues[AttributeRouting.RouteGroupKey];
                });

            var namedEntry = CreateGenerationEntry("named", requiredValues: null, order: 1, name: "NamedRoute");

            // Add an unnamed entry to ensure we don't fall back to generating a link for an unnamed route.
            var unnamedEntry = CreateGenerationEntry("unnamed", requiredValues: null, order: 0);

            // The named route has a lower order which will ensure that we aren't trying the route as
            // if it were an unnamed route.
            var linkGenerationEntries = new[] { namedEntry, unnamedEntry };

            var matchingEntries = Enumerable.Empty<AttributeRouteMatchingEntry>();

            var route = new AttributeRoute(next.Object, matchingEntries, linkGenerationEntries, NullLoggerFactory.Instance);

            var context = CreateVirtualPathContext(values: null, ambientValues: null, name: "NonExistingNamedRoute");

            // Act
            var result = route.GetVirtualPath(context);

            // Assert
            Assert.Null(result);
        }

        [Theory]
        [InlineData("template/{parameter:int}", null)]
        [InlineData("template/{parameter:int}", "NaN")]
        [InlineData("template/{parameter}", null)]
        [InlineData("template/{*parameter:int}", null)]
        [InlineData("template/{*parameter:int}", "NaN")]
        public void AttributeRoute_DoesNotGenerateLink_IfValuesDoNotMatchNamedEntry(string template, string value)
        {
            // Arrange
            string selectedGroup = null;
            var next = new Mock<IRouter>();
            next.Setup(s => s.GetVirtualPath(It.IsAny<VirtualPathContext>()))
                .Callback<VirtualPathContext>(vpc =>
                {
                    vpc.IsBound = true;
                    selectedGroup = (string)vpc.ProvidedValues[AttributeRouting.RouteGroupKey];
                });

            var namedEntry = CreateGenerationEntry(template, requiredValues: null, order: 1, name: "NamedRoute");

            // Add an unnamed entry to ensure we don't fall back to generating a link for an unnamed route.
            var unnamedEntry = CreateGenerationEntry("unnamed", requiredValues: null, order: 0);

            // The named route has a lower order which will ensure that we aren't trying the route as
            // if it were an unnamed route.
            var linkGenerationEntries = new[] { namedEntry, unnamedEntry };

            var matchingEntries = Enumerable.Empty<AttributeRouteMatchingEntry>();

            var route = new AttributeRoute(next.Object, matchingEntries, linkGenerationEntries, NullLoggerFactory.Instance);

            var ambientValues = value == null ? null : new { parameter = value };

            var context = CreateVirtualPathContext(values: null, ambientValues: ambientValues, name: "NamedRoute");

            // Act
            var result = route.GetVirtualPath(context);

            // Assert
            Assert.Null(result);
        }

        [Theory]
        [InlineData("template/{parameter:int}", "5")]
        [InlineData("template/{parameter}", "5")]
        [InlineData("template/{*parameter:int}", "5")]
        [InlineData("template/{*parameter}", "5")]
        public void AttributeRoute_GeneratesLink_IfValuesMatchNamedEntry(string template, string value)
        {
            // Arrange
            string selectedGroup = null;
            var next = new Mock<IRouter>();
            next.Setup(s => s.GetVirtualPath(It.IsAny<VirtualPathContext>()))
                .Callback<VirtualPathContext>(vpc =>
                {
                    vpc.IsBound = true;
                    selectedGroup = (string)vpc.ProvidedValues[AttributeRouting.RouteGroupKey];
                });

            var namedEntry = CreateGenerationEntry(template, requiredValues: null, order: 1, name: "NamedRoute");

            // Add an unnamed entry to ensure we don't fall back to generating a link for an unnamed route.
            var unnamedEntry = CreateGenerationEntry("unnamed", requiredValues: null, order: 0);

            // The named route has a lower order which will ensure that we aren't trying the route as
            // if it were an unnamed route.
            var linkGenerationEntries = new[] { namedEntry, unnamedEntry };

            var matchingEntries = Enumerable.Empty<AttributeRouteMatchingEntry>();

            var route = new AttributeRoute(next.Object, matchingEntries, linkGenerationEntries, NullLoggerFactory.Instance);

            var ambientValues = value == null ? null : new { parameter = value };

            var context = CreateVirtualPathContext(values: null, ambientValues: ambientValues, name: "NamedRoute");

            // Act
            var result = route.GetVirtualPath(context);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(string.Format("1&{0}", template), selectedGroup);
            Assert.Equal("template/5", result);
        }

        [Fact]
        public async void AttributeRoute_RouteAsyncHandled_LogsCorrectValues()
        {
            // Arrange
            var sink = new TestSink();
            var loggerFactory = new TestLoggerFactory(sink);

            var entry = CreateMatchingEntry("api/Store");
            var route = CreateRoutingAttributeRoute(loggerFactory, entry);

            var context = CreateRouteContext("/api/Store");

            // Act
            await route.RouteAsync(context);

            // Assert
            Assert.Equal(1, sink.Scopes.Count);
            var scope = sink.Scopes[0];
            Assert.Equal(typeof(AttributeRoute).FullName, scope.LoggerName);
            Assert.Equal("AttributeRoute.RouteAsync", scope.Scope);

            Assert.Equal(1, sink.Writes.Count);

            var write = sink.Writes[0];
            Assert.Equal(typeof(AttributeRoute).FullName, write.LoggerName);
            Assert.Equal("AttributeRoute.RouteAsync", write.Scope);
            var values = Assert.IsType<AttributeRouteRouteAsyncValues>(write.State);
            Assert.Equal("AttributeRoute.RouteAsync", values.Name);
            Assert.Equal(true, values.Handled);
        }

        [Fact]
        public async void AttributeRoute_RouteAsyncNotHandled_LogsCorrectValues()
        {
            // Arrange
            var sink = new TestSink();
            var loggerFactory = new TestLoggerFactory(sink);

            var entry = CreateMatchingEntry("api/Store");
            var route = CreateRoutingAttributeRoute(loggerFactory, entry);

            var context = CreateRouteContext("/");

            // Act
            await route.RouteAsync(context);

            // Assert
            Assert.Equal(1, sink.Scopes.Count);
            var scope = sink.Scopes[0];
            Assert.Equal(typeof(AttributeRoute).FullName, scope.LoggerName);
            Assert.Equal("AttributeRoute.RouteAsync", scope.Scope);

            Assert.Equal(1, sink.Writes.Count);

            var write = sink.Writes[0];
            Assert.Equal(typeof(AttributeRoute).FullName, write.LoggerName);
            Assert.Equal("AttributeRoute.RouteAsync", write.Scope);
            var values = Assert.IsType<AttributeRouteRouteAsyncValues>(write.State);
            Assert.Equal("AttributeRoute.RouteAsync", values.Name);
            Assert.Equal(false, values.Handled);
        }

        [Fact]
        public void AttributeRoute_GenerateLink_NoRequiredValues()
        {
            // Arrange
            var entry = CreateGenerationEntry("api/Store", new { });
            var route = CreateAttributeRoute(entry);

            var context = CreateVirtualPathContext(new { });

            // Act
            var path = route.GetVirtualPath(context);

            // Assert
            Assert.Equal("api/Store", path);
        }

        [Fact]
        public void AttributeRoute_GenerateLink_Match()
        {
            // Arrange
            var entry = CreateGenerationEntry("api/Store", new { action = "Index", controller = "Store" });
            var route = CreateAttributeRoute(entry);

            var context = CreateVirtualPathContext(new { action = "Index", controller = "Store" });

            // Act
            var path = route.GetVirtualPath(context);

            // Assert
            Assert.Equal("api/Store", path);
        }

        [Fact]
        public void AttributeRoute_GenerateLink_NoMatch()
        {
            // Arrange
            var entry = CreateGenerationEntry("api/Store", new { action = "Details", controller = "Store" });
            var route = CreateAttributeRoute(entry);

            var context = CreateVirtualPathContext(new { action = "Index", controller = "Store" });

            // Act
            var path = route.GetVirtualPath(context);

            // Assert
            Assert.Null(path);
        }

        [Fact]
        public void AttributeRoute_GenerateLink_Match_WithAmbientValues()
        {
            // Arrange
            var entry = CreateGenerationEntry("api/Store", new { action = "Index", controller = "Store" });
            var route = CreateAttributeRoute(entry);

            var context = CreateVirtualPathContext(new { }, new { action = "Index", controller = "Store" });

            // Act
            var path = route.GetVirtualPath(context);

            // Assert
            Assert.Equal("api/Store", path);
        }

        [Fact]
        public void AttributeRoute_GenerateLink_Match_WithParameters()
        {
            // Arrange
            var entry = CreateGenerationEntry("api/Store/{action}", new { action = "Index", controller = "Store" });
            var route = CreateAttributeRoute(entry);

            var context = CreateVirtualPathContext(new { action = "Index", controller = "Store" });

            // Act
            var path = route.GetVirtualPath(context);

            // Assert
            Assert.Equal("api/Store/Index", path);
        }

        [Fact]
        public void AttributeRoute_GenerateLink_Match_WithMoreParameters()
        {
            // Arrange
            var entry = CreateGenerationEntry(
                "api/{area}/dosomething/{controller}/{action}",
                new { action = "Index", controller = "Store", area = "AwesomeCo" });

            var expectedValues = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                { "area", "AwesomeCo" },
                { "controller", "Store" },
                { "action", "Index" },
                { AttributeRouting.RouteGroupKey, entry.RouteGroup },
            };

            var next = new StubRouter();
            var route = CreateAttributeRoute(next, entry);

            var context = CreateVirtualPathContext(
                new { action = "Index", controller = "Store" },
                new { area = "AwesomeCo" });

            // Act
            var path = route.GetVirtualPath(context);

            // Assert
            Assert.Equal("api/AwesomeCo/dosomething/Store/Index", path);
            Assert.Equal(expectedValues, next.GenerationContext.ProvidedValues);
        }

        [Fact]
        public void AttributeRoute_GenerateLink_Match_WithDefault()
        {
            // Arrange
            var entry = CreateGenerationEntry("api/Store/{action=Index}", new { action = "Index", controller = "Store" });
            var route = CreateAttributeRoute(entry);

            var context = CreateVirtualPathContext(new { action = "Index", controller = "Store" });

            // Act
            var path = route.GetVirtualPath(context);

            // Assert
            Assert.Equal("api/Store", path);
        }

        [Fact]
        public void AttributeRoute_GenerateLink_Match_WithConstraint()
        {
            // Arrange
            var entry = CreateGenerationEntry("api/Store/{action}/{id:int}", new { action = "Index", controller = "Store" });

            var expectedValues = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                { "action", "Index" },
                { "id", 5 },
                { AttributeRouting.RouteGroupKey, entry.RouteGroup  },
            };

            var next = new StubRouter();
            var route = CreateAttributeRoute(next, entry);

            var context = CreateVirtualPathContext(new { action = "Index", controller = "Store", id = 5 });

            // Act
            var path = route.GetVirtualPath(context);

            // Assert
            Assert.Equal("api/Store/Index/5", path);
            Assert.Equal(expectedValues, next.GenerationContext.ProvidedValues);
        }

        [Fact]
        public void AttributeRoute_GenerateLink_NoMatch_WithConstraint()
        {
            // Arrange
            var entry = CreateGenerationEntry("api/Store/{action}/{id:int}", new { action = "Index", controller = "Store" });
            var route = CreateAttributeRoute(entry);

            var expectedValues = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                { "id", "5" },
                { AttributeRouting.RouteGroupKey, entry.RouteGroup  },
            };

            var next = new StubRouter();
            var context = CreateVirtualPathContext(new { action = "Index", controller = "Store", id = "heyyyy" });

            // Act
            var path = route.GetVirtualPath(context);

            // Assert
            Assert.Null(path);
        }

        [Fact]
        public void AttributeRoute_GenerateLink_Match_WithMixedAmbientValues()
        {
            // Arrange
            var entry = CreateGenerationEntry("api/Store", new { action = "Index", controller = "Store" });
            var route = CreateAttributeRoute(entry);

            var context = CreateVirtualPathContext(new { action = "Index" }, new { controller = "Store" });

            // Act
            var path = route.GetVirtualPath(context);

            // Assert
            Assert.Equal("api/Store", path);
        }

        [Fact]
        public void AttributeRoute_GenerateLink_Match_WithQueryString()
        {
            // Arrange
            var entry = CreateGenerationEntry("api/Store", new { action = "Index", controller = "Store" });
            var route = CreateAttributeRoute(entry);

            var context = CreateVirtualPathContext(new { action = "Index", id = 5 }, new { controller = "Store" });

            // Act
            var path = route.GetVirtualPath(context);

            // Assert
            Assert.Equal("api/Store?id=5", path);
        }

        [Fact]
        public void AttributeRoute_GenerateLink_ForwardsRouteGroup()
        {
            // Arrange
            var entry = CreateGenerationEntry("api/Store", new { action = "Index", controller = "Store" });

            var expectedValues = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                { AttributeRouting.RouteGroupKey, entry.RouteGroup },
            };

            var next = new StubRouter();
            var route = CreateAttributeRoute(next, entry);

            var context = CreateVirtualPathContext(new { action = "Index", controller = "Store" });

            // Act
            var path = route.GetVirtualPath(context);

            // Assert
            Assert.Equal(expectedValues, next.GenerationContext.ProvidedValues);
        }

        [Fact]
        public void AttributeRoute_GenerateLink_RejectedByFirstRoute()
        {
            // Arrange
            var entry1 = CreateGenerationEntry("api/Store", new { action = "Index", controller = "Store" });
            var entry2 = CreateGenerationEntry("api2/{controller}", new { action = "Index", controller = "Blog" });

            var route = CreateAttributeRoute(entry1, entry2);

            var context = CreateVirtualPathContext(new { action = "Index", controller = "Blog" });

            // Act
            var path = route.GetVirtualPath(context);

            // Assert
            Assert.Equal("api2/Blog", path);
        }

        [Fact]
        public void AttributeRoute_GenerateLink_RejectedByHandler()
        {
            // Arrange
            var entry1 = CreateGenerationEntry("api/Store", new { action = "Edit", controller = "Store" });
            var entry2 = CreateGenerationEntry("api2/{controller}", new { action = "Edit", controller = "Store" });

            var next = new StubRouter();

            var callCount = 0;
            next.GenerationDelegate = (VirtualPathContext c) =>
            {
                // Reject entry 1.
                callCount++;
                return !c.ProvidedValues.Contains(new KeyValuePair<string, object>(
                    AttributeRouting.RouteGroupKey,
                    entry1.RouteGroup));
            };

            var route = CreateAttributeRoute(next, entry1, entry2);

            var context = CreateVirtualPathContext(new { action = "Edit", controller = "Store" });

            // Act
            var path = route.GetVirtualPath(context);

            // Assert
            Assert.Equal("api2/Store", path);
            Assert.Equal(2, callCount);
        }

        [Fact]
        public void AttributeRoute_GenerateLink_ToArea()
        {
            // Arrange
            var entry1 = CreateGenerationEntry("Help/Store", new { area = "Help", action = "Edit", controller = "Store" });
            entry1.Precedence = 1;

            var entry2 = CreateGenerationEntry("Store", new { area = (string)null, action = "Edit", controller = "Store" });
            entry2.Precedence = 2;

            var next = new StubRouter();

            var route = CreateAttributeRoute(next, entry1, entry2);

            var context = CreateVirtualPathContext(new { area = "Help", action = "Edit", controller = "Store" });

            // Act
            var path = route.GetVirtualPath(context);

            // Assert
            Assert.Equal("Help/Store", path);
        }

        [Fact]
        public void AttributeRoute_GenerateLink_ToArea_PredecedenceReversed()
        {
            // Arrange
            var entry1 = CreateGenerationEntry("Help/Store", new { area = "Help", action = "Edit", controller = "Store" });
            entry1.Precedence = 2;

            var entry2 = CreateGenerationEntry("Store", new { area = (string)null, action = "Edit", controller = "Store" });
            entry2.Precedence = 1;

            var next = new StubRouter();

            var route = CreateAttributeRoute(next, entry1, entry2);

            var context = CreateVirtualPathContext(new { area = "Help", action = "Edit", controller = "Store" });

            // Act
            var path = route.GetVirtualPath(context);

            // Assert
            Assert.Equal("Help/Store", path);
        }

        [Fact]
        public void AttributeRoute_GenerateLink_ToArea_WithAmbientValues()
        {
            // Arrange
            var entry1 = CreateGenerationEntry("Help/Store", new { area = "Help", action = "Edit", controller = "Store" });
            entry1.Precedence = 1;

            var entry2 = CreateGenerationEntry("Store", new { area = (string)null, action = "Edit", controller = "Store" });
            entry2.Precedence = 2;

            var next = new StubRouter();

            var route = CreateAttributeRoute(next, entry1, entry2);

            var context = CreateVirtualPathContext(
                values: new { action = "Edit", controller = "Store" },
                ambientValues: new { area = "Help" });

            // Act
            var path = route.GetVirtualPath(context);

            // Assert
            Assert.Equal("Help/Store", path);
        }

        [Fact]
        public void AttributeRoute_GenerateLink_OutOfArea_IgnoresAmbientValue()
        {
            // Arrange
            var entry1 = CreateGenerationEntry("Help/Store", new { area = "Help", action = "Edit", controller = "Store" });
            entry1.Precedence = 1;

            var entry2 = CreateGenerationEntry("Store", new { area = (string)null, action = "Edit", controller = "Store" });
            entry2.Precedence = 2;

            var next = new StubRouter();

            var route = CreateAttributeRoute(next, entry1, entry2);

            var context = CreateVirtualPathContext(
                values: new { action = "Edit", controller = "Store" },
                ambientValues: new { area = "Blog" });

            // Act
            var path = route.GetVirtualPath(context);

            // Assert
            Assert.Equal("Store", path);
        }

        private static RouteContext CreateRouteContext(string requestPath)
        {
            var request = new Mock<HttpRequest>(MockBehavior.Strict);
            request.SetupGet(r => r.Path).Returns(new PathString(requestPath));

            var context = new Mock<HttpContext>(MockBehavior.Strict);
            context.Setup(m => m.RequestServices.GetService(typeof(ILoggerFactory)))
                .Returns(NullLoggerFactory.Instance);

            context.SetupGet(c => c.Request).Returns(request.Object);

            return new RouteContext(context.Object);
        }

        private static VirtualPathContext CreateVirtualPathContext(
            object values,
            object ambientValues = null,
            string name = null)
        {
            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext.Setup(h => h.RequestServices.GetService(typeof(ILoggerFactory)))
                .Returns(NullLoggerFactory.Instance);

            return new VirtualPathContext(
                mockHttpContext.Object,
                new RouteValueDictionary(ambientValues),
                new RouteValueDictionary(values),
                name);
        }

        private static AttributeRouteMatchingEntry CreateMatchingEntry(IRouter router, string template, int order)
        {
            var constraintResolver = CreateConstraintResolver();

            var routeTemplate = TemplateParser.Parse(template, constraintResolver);

            var entry = new AttributeRouteMatchingEntry();
            entry.Route = new TemplateRoute(router, template, constraintResolver);
            entry.Precedence = AttributeRoutePrecedence.Compute(routeTemplate);
            entry.Order = order;

            string routeGroup = string.Format("{0}&&{1}", order, template);
            entry.Route.Defaults.Add("test_route_group", routeGroup);

            return entry;
        }

        private static AttributeRouteLinkGenerationEntry CreateGenerationEntry(
            string template,
            object requiredValues,
            int order = 0,
            string name = null)
        {
            var constraintResolver = CreateConstraintResolver();

            var entry = new AttributeRouteLinkGenerationEntry();
            entry.TemplateText = template;
            entry.Template = TemplateParser.Parse(template, constraintResolver);

            var defaults = entry.Template.Parameters
                .Where(p => p.DefaultValue != null)
                .ToDictionary(p => p.Name, p => p.DefaultValue);

            var constraints = entry.Template.Parameters
                .Where(p => p.InlineConstraint != null)
                .ToDictionary(p => p.Name, p => p.InlineConstraint);

            entry.Constraints = constraints;
            entry.Defaults = defaults;
            entry.Binder = new TemplateBinder(entry.Template, defaults);
            entry.Order = order;
            entry.Precedence = AttributeRoutePrecedence.Compute(entry.Template);
            entry.RequiredLinkValues = new RouteValueDictionary(requiredValues);
            entry.RouteGroup = CreateRouteGroup(order, template);
            entry.Name = name;
            return entry;
        }

        private AttributeRouteMatchingEntry CreateMatchingEntry(string template)
        {
            var mockConstraint = new Mock<IRouteConstraint>();
            mockConstraint.Setup(c => c.Match(
                It.IsAny<HttpContext>(),
                It.IsAny<IRouter>(),
                It.IsAny<string>(),
                It.IsAny<IDictionary<string, object>>(),
                It.IsAny<RouteDirection>()))
            .Returns(true);

            var mockConstraintResolver = new Mock<IInlineConstraintResolver>();
            mockConstraintResolver.Setup(r => r.ResolveConstraint(
                It.IsAny<string>()))
            .Returns(mockConstraint.Object);

            var entry = new AttributeRouteMatchingEntry()
            {
                Route = new TemplateRoute(new StubRouter(), template, mockConstraintResolver.Object)
            };

            return entry;
        }

        private static string CreateRouteGroup(int order, string template)
        {
            return string.Format("{0}&{1}", order, template);
        }

        private static DefaultInlineConstraintResolver CreateConstraintResolver()
        {
            var services = Mock.Of<IServiceProvider>();

            var options = new RouteOptions();
            var optionsMock = new Mock<IOptions<RouteOptions>>();
            optionsMock.SetupGet(o => o.Options).Returns(options);

            return new DefaultInlineConstraintResolver(services, optionsMock.Object);
        }

        private static AttributeRoute CreateAttributeRoute(AttributeRouteLinkGenerationEntry entry)
        {
            return CreateAttributeRoute(new StubRouter(), entry);
        }

        private static AttributeRoute CreateAttributeRoute(IRouter next, AttributeRouteLinkGenerationEntry entry)
        {
            return CreateAttributeRoute(next, new[] { entry });
        }

        private static AttributeRoute CreateAttributeRoute(params AttributeRouteLinkGenerationEntry[] entries)
        {
            return CreateAttributeRoute(new StubRouter(), entries);
        }

        private static AttributeRoute CreateAttributeRoute(IRouter next, params AttributeRouteLinkGenerationEntry[] entries)
        {
            return new AttributeRoute(
                next,
                Enumerable.Empty<AttributeRouteMatchingEntry>(),
                entries,
                NullLoggerFactory.Instance);
        }

        private static AttributeRoute CreateRoutingAttributeRoute(ILoggerFactory loggerFactory = null, params AttributeRouteMatchingEntry[] entries)
        {
            loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;

            return new AttributeRoute(
                new StubRouter(),
                entries,
                Enumerable.Empty<AttributeRouteLinkGenerationEntry>(),
                loggerFactory);
        }

        private class StubRouter : IRouter
        {
            public VirtualPathContext GenerationContext { get; set; }

            public Func<VirtualPathContext, bool> GenerationDelegate { get; set; }

            public RouteContext MatchingContext { get; set; }

            public Func<RouteContext, bool> MatchingDelegate { get; set; }

            public string GetVirtualPath(VirtualPathContext context)
            {
                GenerationContext = context;

                if (GenerationDelegate == null)
                {
                    context.IsBound = true;
                }
                else
                {
                    context.IsBound = GenerationDelegate(context);
                }

                return null;
            }

            public Task RouteAsync(RouteContext context)
            {
                if (MatchingDelegate == null)
                {
                    context.IsHandled = true;
                }
                else
                {
                    context.IsHandled = MatchingDelegate(context);
                }

                return Task.FromResult(true);
            }
        }
    }
}