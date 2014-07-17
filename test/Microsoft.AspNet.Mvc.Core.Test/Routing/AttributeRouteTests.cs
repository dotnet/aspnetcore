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
    public class AttributeRouteTests
    {
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

            // There is a record for IsEnabled and one for WriteCore.
            Assert.Equal(2, sink.Writes.Count);

            var enabled = sink.Writes[0];
            Assert.Equal(typeof(AttributeRoute).FullName, enabled.LoggerName);
            Assert.Equal("AttributeRoute.RouteAsync", enabled.Scope);
            Assert.Null(enabled.State);

            var write = sink.Writes[1];
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

            // There is a record for IsEnabled and one for WriteCore.
            Assert.Equal(2, sink.Writes.Count);

            var enabled = sink.Writes[0];
            Assert.Equal(typeof(AttributeRoute).FullName, enabled.LoggerName);
            Assert.Equal("AttributeRoute.RouteAsync", enabled.Scope);
            Assert.Null(enabled.State);

            var write = sink.Writes[1];
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

            var context = CreateVirtualPathContext(new { action = "Index", id = 5}, new { controller = "Store" });

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

        private static VirtualPathContext CreateVirtualPathContext(object values, object ambientValues = null)
        {
            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext.Setup(h => h.RequestServices.GetService(typeof(ILoggerFactory)))
                .Returns(NullLoggerFactory.Instance);

            return new VirtualPathContext(
                mockHttpContext.Object, 
                new RouteValueDictionary(ambientValues), 
                new RouteValueDictionary(values));
        }

        private static AttributeRouteLinkGenerationEntry CreateGenerationEntry(string template, object requiredValues)
        {
            var constraintResolver = CreateConstraintResolver();

            var entry = new AttributeRouteLinkGenerationEntry();
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
            entry.Precedence = AttributeRoutePrecedence.Compute(entry.Template);
            entry.RequiredLinkValues = new RouteValueDictionary(requiredValues);
            entry.RouteGroup = template;

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

        private static DefaultInlineConstraintResolver CreateConstraintResolver()
        {
            var services = Mock.Of<IServiceProvider>();

            var options = new RouteOptions();
            var optionsMock = new Mock<IOptionsAccessor<RouteOptions>>();
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

                return Task.FromResult<object>(null);
            }
        }
    }
}