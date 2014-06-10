// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Routing;
using Microsoft.AspNet.Routing.Template;
using Microsoft.Framework.OptionsModel;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Routing
{
    public class AttributeRouteTests
    {
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

        private static VirtualPathContext CreateVirtualPathContext(object values, object ambientValues = null)
        {
            var httpContext = Mock.Of<HttpContext>();

            return new VirtualPathContext(
                httpContext, 
                new RouteValueDictionary(ambientValues), 
                new RouteValueDictionary(values));
        }

        private static AttributeRouteGenerationEntry CreateGenerationEntry(string template, object requiredValues)
        {
            var constraintResolver = CreateConstraintResolver();

            var entry = new AttributeRouteGenerationEntry();
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

        private static DefaultInlineConstraintResolver CreateConstraintResolver()
        {
            var services = Mock.Of<IServiceProvider>();

            var options = new RouteOptions();
            var optionsMock = new Mock<IOptionsAccessor<RouteOptions>>();
            optionsMock.SetupGet(o => o.Options).Returns(options);

            return new DefaultInlineConstraintResolver(services, optionsMock.Object);
        }

        private static AttributeRoute CreateAttributeRoute(AttributeRouteGenerationEntry entry)
        {
            return CreateAttributeRoute(new StubRouter(), entry);
        }

        private static AttributeRoute CreateAttributeRoute(IRouter next, AttributeRouteGenerationEntry entry)
        {
            return CreateAttributeRoute(next, new[] { entry });
        }

        private static AttributeRoute CreateAttributeRoute(params AttributeRouteGenerationEntry[] entries)
        {
            return CreateAttributeRoute(new StubRouter(), entries);
        }

        private static AttributeRoute CreateAttributeRoute(IRouter next, params AttributeRouteGenerationEntry[] entries)
        {
            return new AttributeRoute(
                next,
                Enumerable.Empty<AttributeRouteMatchingEntry>(),
                entries);
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