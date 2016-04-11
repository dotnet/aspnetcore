// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.AspNetCore.Routing.Internal;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.AspNetCore.Routing.Tree;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.WebEncoders.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    public class AttributeRouteTest
    {
        private static readonly RequestDelegate NullHandler = (c) => Task.FromResult(0);

        // This test verifies that AttributeRoute can respond to changes in the AD collection. It does this
        // by running a successful request, then removing that action and verifying the next route isn't
        // successful.
        [Fact]
        public async Task AttributeRoute_UsesUpdatedActionDescriptors()
        {
            // Arrange
            var handler = CreateHandler();

            var actions = new List<ActionDescriptor>()
            {
                new ActionDescriptor()
                {
                    AttributeRouteInfo = new AttributeRouteInfo()
                    {
                        Template = "api/Blog/{id}"
                    },
                    RouteConstraints = new List<RouteDataActionConstraint>()
                    {
                        new RouteDataActionConstraint(TreeRouter.RouteGroupKey, "1"),
                    },
                },
                new ActionDescriptor()
                {
                    AttributeRouteInfo = new AttributeRouteInfo()
                    {
                        Template = "api/Store/Buy/{id}"
                    },
                    RouteConstraints = new List<RouteDataActionConstraint>()
                    {
                        new RouteDataActionConstraint(TreeRouter.RouteGroupKey, "2"),
                    },
                },
            };

            var actionDescriptorProvider = CreateActionDescriptorProvider(actions);
            var route = CreateRoute(handler.Object, actionDescriptorProvider.Object);

            var requestServices = new Mock<IServiceProvider>(MockBehavior.Strict);
            requestServices
                .Setup(s => s.GetService(typeof(ILoggerFactory)))
                .Returns(NullLoggerFactory.Instance);

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Path = new PathString("/api/Store/Buy/5");
            httpContext.RequestServices = requestServices.Object;

            var context = new RouteContext(httpContext);

            // Act 1
            await route.RouteAsync(context);

            // Assert 1
            Assert.NotNull(context.Handler);
            Assert.Equal("5", context.RouteData.Values["id"]);
            Assert.Equal("2", context.RouteData.Values[TreeRouter.RouteGroupKey]);

            handler.Verify(h => h.RouteAsync(It.IsAny<RouteContext>()), Times.Once());

            // Arrange 2 - remove the action and update the collection
            actions.RemoveAt(1);
            actionDescriptorProvider
                .SetupGet(ad => ad.ActionDescriptors)
                .Returns(new ActionDescriptorCollection(actions, version: 2));

            context = new RouteContext(httpContext);

            // Act 2
            await route.RouteAsync(context);

            // Assert 2
            Assert.Null(context.Handler);
            Assert.Empty(context.RouteData.Values);

            handler.Verify(h => h.RouteAsync(It.IsAny<RouteContext>()), Times.Once());
        }

        [Fact]
        public void AttributeRoute_GetEntries_CreatesLinkGenerationEntry()
        {
            // Arrange
            var actions = new List<ActionDescriptor>()
            {
                new ActionDescriptor()
                {
                    AttributeRouteInfo = new AttributeRouteInfo()
                    {
                        Template = "api/Blog/{id}",
                        Name = "BLOG_INDEX",
                        Order = 17,
                    },
                    RouteConstraints = new List<RouteDataActionConstraint>()
                    {
                        new RouteDataActionConstraint(TreeRouter.RouteGroupKey, "1"),
                    },
                    RouteValueDefaults = new Dictionary<string, object>()
                    {
                        { "controller", "Blog" },
                        { "action", "Index" },
                    },
                },
            };

            var actionDescriptorProvider = CreateActionDescriptorProvider(actions);

            var route = CreateRoute(CreateHandler().Object, actionDescriptorProvider.Object);

            // Act
            var entries = route.GetEntries(actionDescriptorProvider.Object.ActionDescriptors);

            // Assert
            Assert.Collection(
                entries.LinkGenerationEntries,
                e =>
                {
                    Assert.NotNull(e.Binder);
                    Assert.Empty(e.Constraints);
                    Assert.Empty(e.Defaults);
                    Assert.Equal(RoutePrecedence.ComputeGenerated(e.Template), e.GenerationPrecedence);
                    Assert.Equal("BLOG_INDEX", e.Name);
                    Assert.Equal(17, e.Order);
                    Assert.Equal(actions[0].RouteValueDefaults, e.RequiredLinkValues);
                    Assert.Equal("1", e.RouteGroup);
                    Assert.Equal("api/Blog/{id}", e.Template.TemplateText);
                });
        }

        [Fact]
        public void AttributeRoute_GetEntries_CreatesLinkGenerationEntry_WithConstraint()
        {
            // Arrange
            var actions = new List<ActionDescriptor>()
            {
                new ActionDescriptor()
                {
                    AttributeRouteInfo = new AttributeRouteInfo()
                    {
                        Template = "api/Blog/{id:int}",
                        Name = "BLOG_INDEX",
                        Order = 17,
                    },
                    RouteConstraints = new List<RouteDataActionConstraint>()
                    {
                        new RouteDataActionConstraint(TreeRouter.RouteGroupKey, "1"),
                    },
                    RouteValueDefaults = new Dictionary<string, object>()
                    {
                        { "controller", "Blog" },
                        { "action", "Index" },
                    },
                },
            };

            var actionDescriptorProvider = CreateActionDescriptorProvider(actions);

            var route = CreateRoute(CreateHandler().Object, actionDescriptorProvider.Object);

            // Act
            var entries = route.GetEntries(actionDescriptorProvider.Object.ActionDescriptors);

            // Assert
            Assert.Collection(
                entries.LinkGenerationEntries,
                e =>
                {
                    Assert.NotNull(e.Binder);
                    Assert.Single(e.Constraints, kvp => kvp.Key == "id");
                    Assert.Empty(e.Defaults);
                    Assert.Equal(RoutePrecedence.ComputeGenerated(e.Template), e.GenerationPrecedence);
                    Assert.Equal("BLOG_INDEX", e.Name);
                    Assert.Equal(17, e.Order);
                    Assert.Equal(actions[0].RouteValueDefaults, e.RequiredLinkValues);
                    Assert.Equal("1", e.RouteGroup);
                    Assert.Equal("api/Blog/{id:int}", e.Template.TemplateText);
                });
        }

        [Fact]
        public void AttributeRoute_GetEntries_CreatesLinkGenerationEntry_WithDefault()
        {
            // Arrange
            var actions = new List<ActionDescriptor>()
            {
                new ActionDescriptor()
                {
                    AttributeRouteInfo = new AttributeRouteInfo()
                    {
                        Template = "api/Blog/{*slug=hello}",
                        Name = "BLOG_INDEX",
                        Order = 17,
                    },
                    RouteConstraints = new List<RouteDataActionConstraint>()
                    {
                        new RouteDataActionConstraint(TreeRouter.RouteGroupKey, "1"),
                    },
                    RouteValueDefaults = new Dictionary<string, object>()
                    {
                        { "controller", "Blog" },
                        { "action", "Index" },
                    },
                },
            };

            var actionDescriptorProvider = CreateActionDescriptorProvider(actions);

            var route = CreateRoute(CreateHandler().Object, actionDescriptorProvider.Object);

            // Act
            var entries = route.GetEntries(actionDescriptorProvider.Object.ActionDescriptors);

            // Assert
            Assert.Collection(
                entries.LinkGenerationEntries,
                e =>
                {
                    Assert.NotNull(e.Binder);
                    Assert.Empty(e.Constraints);
                    Assert.Equal(new RouteValueDictionary(new { slug = "hello" }), e.Defaults);
                    Assert.Equal(RoutePrecedence.ComputeGenerated(e.Template), e.GenerationPrecedence);
                    Assert.Equal("BLOG_INDEX", e.Name);
                    Assert.Equal(17, e.Order);
                    Assert.Equal(actions[0].RouteValueDefaults, e.RequiredLinkValues);
                    Assert.Equal("1", e.RouteGroup);
                    Assert.Equal("api/Blog/{*slug=hello}", e.Template.TemplateText);
                });
        }

        // These actions seem like duplicates, but this is a real case that can happen where two different
        // actions define the same route info. Link generation happens based on the action name + controller
        // name.
        [Fact]
        public void AttributeRoute_GetEntries_CreatesLinkGenerationEntry_ForEachAction()
        {
            // Arrange
            var actions = new List<ActionDescriptor>()
            {
                new ActionDescriptor()
                {
                    AttributeRouteInfo = new AttributeRouteInfo()
                    {
                        Template = "api/Blog/{id}",
                        Name = "BLOG_INDEX",
                        Order = 17,
                    },
                    RouteConstraints = new List<RouteDataActionConstraint>()
                    {
                        new RouteDataActionConstraint(TreeRouter.RouteGroupKey, "1"),
                    },
                    RouteValueDefaults = new Dictionary<string, object>()
                    {
                        { "controller", "Blog" },
                        { "action", "Index" },
                    },
                },
                new ActionDescriptor()
                {
                    AttributeRouteInfo = new AttributeRouteInfo()
                    {
                        Template = "api/Blog/{id}",
                        Name = "BLOG_INDEX",
                        Order = 17,
                    },
                    RouteConstraints = new List<RouteDataActionConstraint>()
                    {
                        new RouteDataActionConstraint(TreeRouter.RouteGroupKey, "1"),
                    },
                    RouteValueDefaults = new Dictionary<string, object>()
                    {
                        { "controller", "Blog" },
                        { "action", "Index2" },
                    },
                },
            };

            var actionDescriptorProvider = CreateActionDescriptorProvider(actions);

            var route = CreateRoute(CreateHandler().Object, actionDescriptorProvider.Object);

            // Act
            var entries = route.GetEntries(actionDescriptorProvider.Object.ActionDescriptors);

            // Assert
            Assert.Collection(
                entries.LinkGenerationEntries,
                e =>
                {
                    Assert.NotNull(e.Binder);
                    Assert.Empty(e.Constraints);
                    Assert.Empty(e.Defaults);
                    Assert.Equal(RoutePrecedence.ComputeGenerated(e.Template), e.GenerationPrecedence);
                    Assert.Equal("BLOG_INDEX", e.Name);
                    Assert.Equal(17, e.Order);
                    Assert.Equal(actions[0].RouteValueDefaults, e.RequiredLinkValues);
                    Assert.Equal("1", e.RouteGroup);
                    Assert.Equal("api/Blog/{id}", e.Template.TemplateText);
                },
                e =>
                {
                    Assert.NotNull(e.Binder);
                    Assert.Empty(e.Constraints);
                    Assert.Empty(e.Defaults);
                    Assert.Equal(RoutePrecedence.ComputeGenerated(e.Template), e.GenerationPrecedence);
                    Assert.Equal("BLOG_INDEX", e.Name);
                    Assert.Equal(17, e.Order);
                    Assert.Equal(actions[1].RouteValueDefaults, e.RequiredLinkValues);
                    Assert.Equal("1", e.RouteGroup);
                    Assert.Equal("api/Blog/{id}", e.Template.TemplateText);
                });
        }

        [Fact]
        public void AttributeRoute_GetEntries_CreatesMatchingEntry()
        {
            // Arrange
            var actions = new List<ActionDescriptor>()
            {
                new ActionDescriptor()
                {
                    AttributeRouteInfo = new AttributeRouteInfo()
                    {
                        Template = "api/Blog/{id}",
                        Name = "BLOG_INDEX",
                        Order = 17,
                    },
                    RouteConstraints = new List<RouteDataActionConstraint>()
                    {
                        new RouteDataActionConstraint(TreeRouter.RouteGroupKey, "1"),
                    },
                    RouteValueDefaults = new Dictionary<string, object>()
                    {
                        { "controller", "Blog" },
                        { "action", "Index" },
                    },
                },
            };

            var actionDescriptorProvider = CreateActionDescriptorProvider(actions);

            var handler = CreateHandler().Object;
            var route = CreateRoute(handler, actionDescriptorProvider.Object);

            // Act
            var entries = route.GetEntries(actionDescriptorProvider.Object.ActionDescriptors);

            // Assert
            Assert.Collection(
                entries.MatchingEntries,
                e =>
                {
                    Assert.Empty(e.Constraints);
                    Assert.Equal(17, e.Order);
                    Assert.Equal(RoutePrecedence.ComputeMatched(e.RouteTemplate), e.Precedence);
                    Assert.Equal("BLOG_INDEX", e.RouteName);
                    Assert.Equal("api/Blog/{id}", e.RouteTemplate.TemplateText);
                    Assert.Same(handler, e.Target);
                    Assert.Collection(
                        e.TemplateMatcher.Defaults.OrderBy(kvp => kvp.Key),
                        kvp => Assert.Equal(new KeyValuePair<string, object>(TreeRouter.RouteGroupKey, "1"), kvp));
                    Assert.Same(e.RouteTemplate, e.TemplateMatcher.Template);
                });
        }

        [Fact]
        public void AttributeRoute_GetEntries_CreatesMatchingEntry_WithConstraint()
        {
            // Arrange
            var actions = new List<ActionDescriptor>()
            {
                new ActionDescriptor()
                {
                    AttributeRouteInfo = new AttributeRouteInfo()
                    {
                        Template = "api/Blog/{id:int}",
                        Name = "BLOG_INDEX",
                        Order = 17,
                    },
                    RouteConstraints = new List<RouteDataActionConstraint>()
                    {
                        new RouteDataActionConstraint(TreeRouter.RouteGroupKey, "1"),
                    },
                    RouteValueDefaults = new Dictionary<string, object>()
                    {
                        { "controller", "Blog" },
                        { "action", "Index" },
                    },
                },
            };

            var actionDescriptorProvider = CreateActionDescriptorProvider(actions);

            var handler = CreateHandler().Object;
            var route = CreateRoute(handler, actionDescriptorProvider.Object);

            // Act
            var entries = route.GetEntries(actionDescriptorProvider.Object.ActionDescriptors);

            // Assert
            Assert.Collection(
                entries.MatchingEntries,
                e =>
                {
                    Assert.Single(e.Constraints, kvp => kvp.Key == "id");
                    Assert.Equal(17, e.Order);
                    Assert.Equal(RoutePrecedence.ComputeMatched(e.RouteTemplate), e.Precedence);
                    Assert.Equal("BLOG_INDEX", e.RouteName);
                    Assert.Equal("api/Blog/{id:int}", e.RouteTemplate.TemplateText);
                    Assert.Same(handler, e.Target);
                    Assert.Collection(
                        e.TemplateMatcher.Defaults.OrderBy(kvp => kvp.Key),
                        kvp => Assert.Equal(new KeyValuePair<string, object>(TreeRouter.RouteGroupKey, "1"), kvp));
                    Assert.Same(e.RouteTemplate, e.TemplateMatcher.Template);
                });
        }

        [Fact]
        public void AttributeRoute_GetEntries_CreatesMatchingEntry_WithDefault()
        {
            // Arrange
            var actions = new List<ActionDescriptor>()
            {
                new ActionDescriptor()
                {
                    AttributeRouteInfo = new AttributeRouteInfo()
                    {
                        Template = "api/Blog/{*slug=hello}",
                        Name = "BLOG_INDEX",
                        Order = 17,
                    },
                    RouteConstraints = new List<RouteDataActionConstraint>()
                    {
                        new RouteDataActionConstraint(TreeRouter.RouteGroupKey, "1"),
                    },
                    RouteValueDefaults = new Dictionary<string, object>()
                    {
                        { "controller", "Blog" },
                        { "action", "Index" },
                    },
                },
            };

            var actionDescriptorProvider = CreateActionDescriptorProvider(actions);

            var handler = CreateHandler().Object;
            var route = CreateRoute(handler, actionDescriptorProvider.Object);

            // Act
            var entries = route.GetEntries(actionDescriptorProvider.Object.ActionDescriptors);

            // Assert
            Assert.Collection(
                entries.MatchingEntries,
                e =>
                {
                    Assert.Empty(e.Constraints);
                    Assert.Equal(17, e.Order);
                    Assert.Equal(RoutePrecedence.ComputeMatched(e.RouteTemplate), e.Precedence);
                    Assert.Equal("BLOG_INDEX", e.RouteName);
                    Assert.Equal("api/Blog/{*slug=hello}", e.RouteTemplate.TemplateText);
                    Assert.Same(handler, e.Target);
                    Assert.Collection(
                        e.TemplateMatcher.Defaults.OrderBy(kvp => kvp.Key),
                        kvp => Assert.Equal(new KeyValuePair<string, object>(TreeRouter.RouteGroupKey, "1"), kvp),
                        kvp => Assert.Equal(new KeyValuePair<string, object>("slug", "hello"), kvp));
                    Assert.Same(e.RouteTemplate, e.TemplateMatcher.Template);
                });
        }

        // These actions seem like duplicates, but this is a real case that can happen where two different
        // actions define the same route info. Link generation happens based on the action name + controller
        // name.
        [Fact]
        public void AttributeRoute_GetEntries_CreatesMatchingEntry_CombinesLikeActions()
        {
            // Arrange
            var actions = new List<ActionDescriptor>()
            {
                new ActionDescriptor()
                {
                    AttributeRouteInfo = new AttributeRouteInfo()
                    {
                        Template = "api/Blog/{id}",
                        Name = "BLOG_INDEX",
                        Order = 17,
                    },
                    RouteConstraints = new List<RouteDataActionConstraint>()
                    {
                        new RouteDataActionConstraint(TreeRouter.RouteGroupKey, "1"),
                    },
                    RouteValueDefaults = new Dictionary<string, object>()
                    {
                        { "controller", "Blog" },
                        { "action", "Index" },
                    },
                },
                new ActionDescriptor()
                {
                    AttributeRouteInfo = new AttributeRouteInfo()
                    {
                        Template = "api/Blog/{id}",
                        Name = "BLOG_INDEX",
                        Order = 17,
                    },
                    RouteConstraints = new List<RouteDataActionConstraint>()
                    {
                        new RouteDataActionConstraint(TreeRouter.RouteGroupKey, "1"),
                    },
                    RouteValueDefaults = new Dictionary<string, object>()
                    {
                        { "controller", "Blog" },
                        { "action", "Index2" },
                    },
                },
            };

            var actionDescriptorProvider = CreateActionDescriptorProvider(actions);

            var handler = CreateHandler().Object;
            var route = CreateRoute(handler, actionDescriptorProvider.Object);

            // Act
            var entries = route.GetEntries(actionDescriptorProvider.Object.ActionDescriptors);

            // Assert
            Assert.Collection(
                entries.MatchingEntries,
                e =>
                {
                    Assert.Empty(e.Constraints);
                    Assert.Equal(17, e.Order);
                    Assert.Equal(RoutePrecedence.ComputeMatched(e.RouteTemplate), e.Precedence);
                    Assert.Equal("BLOG_INDEX", e.RouteName);
                    Assert.Equal("api/Blog/{id}", e.RouteTemplate.TemplateText);
                    Assert.Same(handler, e.Target);
                    Assert.Collection(
                        e.TemplateMatcher.Defaults.OrderBy(kvp => kvp.Key),
                        kvp => Assert.Equal(new KeyValuePair<string, object>(TreeRouter.RouteGroupKey, "1"), kvp));
                    Assert.Same(e.RouteTemplate, e.TemplateMatcher.Template);
                });
        }

        private static Mock<IRouter> CreateHandler()
        {
            var handler = new Mock<IRouter>(MockBehavior.Strict);
            handler
                .Setup(h => h.RouteAsync(It.IsAny<RouteContext>()))
                .Callback<RouteContext>(c => c.Handler = NullHandler)
                .Returns(TaskCache.CompletedTask)
                .Verifiable();
            return handler;
        }

        private static Mock<IActionDescriptorCollectionProvider> CreateActionDescriptorProvider(
            IReadOnlyList<ActionDescriptor> actions)
        {
            var actionDescriptorProvider = new Mock<IActionDescriptorCollectionProvider>(MockBehavior.Strict);
            actionDescriptorProvider
                .SetupGet(ad => ad.ActionDescriptors)
                .Returns(new ActionDescriptorCollection(actions, version: 1));

            return actionDescriptorProvider;
        }

        private static AttributeRoute CreateRoute(
            IRouter handler, 
            IActionDescriptorCollectionProvider actionDescriptorProvider)
        {
            var constraintResolver = new Mock<IInlineConstraintResolver>();
            constraintResolver
                .Setup(c => c.ResolveConstraint("int"))
                .Returns(new IntRouteConstraint());

            var policy = new UriBuilderContextPooledObjectPolicy(new UrlTestEncoder());
            var pool = new DefaultObjectPool<UriBuildingContext>(policy);

            var route = new AttributeRoute(
                handler,
                actionDescriptorProvider,
                constraintResolver.Object,
                pool,
                new UrlTestEncoder(),
                NullLoggerFactory.Instance);

            return route;
        }
    }
}
