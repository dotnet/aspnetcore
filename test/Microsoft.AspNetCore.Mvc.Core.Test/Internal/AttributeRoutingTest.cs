// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text.Encodings.Web;
#if NETCOREAPP1_0
using System.Reflection;
#endif
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Tree;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.WebEncoders.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    public class AttributeRoutingTest
    {
        [Fact]
        public async Task AttributeRouting_SyntaxErrorInTemplate()
        {
            // Arrange
            var action = CreateAction("InvalidTemplate", "{a/dkfk}");

            var expectedMessage =
                "The following errors occurred with attribute routing information:" + Environment.NewLine +
                Environment.NewLine +
                "For action: 'InvalidTemplate'" + Environment.NewLine +
                "Error: The route parameter name 'a/dkfk' is invalid. Route parameter names must be non-empty and " +
                "cannot contain these characters: '{', '}', '/'. The '?' character marks a parameter as optional, " +
                "and can occur only at the end of the parameter. The '*' character marks a parameter as catch-all, " +
                "and can occur only at the start of the parameter." + Environment.NewLine +
                "Parameter name: routeTemplate";

            var handler = CreateRouter();
            var services = CreateServices(action);

            var route = AttributeRouting.CreateAttributeMegaRoute(handler, services);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await route.RouteAsync(new RouteContext(new DefaultHttpContext()));
            });

            Assert.Equal(expectedMessage, ex.Message);
        }

        [Fact]
        public async Task AttributeRouting_DisallowedParameter()
        {
            // Arrange
            var action = CreateAction("DisallowedParameter", "{foo}/{action}");
            action.RouteValueDefaults.Add("foo", "bleh");

            var expectedMessage =
                "The following errors occurred with attribute routing information:" + Environment.NewLine +
                Environment.NewLine +
                "For action: 'DisallowedParameter'" + Environment.NewLine +
                "Error: The attribute route '{foo}/{action}' cannot contain a parameter named '{foo}'. " +
                "Use '[foo]' in the route template to insert the value 'bleh'.";

            var handler = CreateRouter();
            var services = CreateServices(action);

            var route = AttributeRouting.CreateAttributeMegaRoute(handler, services);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await route.RouteAsync(new RouteContext(new DefaultHttpContext()));
            });

            Assert.Equal(expectedMessage, ex.Message);
        }

        [Fact]
        public async Task AttributeRouting_MultipleErrors()
        {
            // Arrange
            var action1 = CreateAction("DisallowedParameter1", "{foo}/{action}");
            action1.RouteValueDefaults.Add("foo", "bleh");

            var action2 = CreateAction("DisallowedParameter2", "cool/{action}");
            action2.RouteValueDefaults.Add("action", "hey");

            var expectedMessage =
                "The following errors occurred with attribute routing information:" + Environment.NewLine +
                Environment.NewLine +
                "For action: 'DisallowedParameter1'" + Environment.NewLine +
                "Error: The attribute route '{foo}/{action}' cannot contain a parameter named '{foo}'. " +
                "Use '[foo]' in the route template to insert the value 'bleh'." + Environment.NewLine +
                Environment.NewLine +
                "For action: 'DisallowedParameter2'" + Environment.NewLine +
                "Error: The attribute route 'cool/{action}' cannot contain a parameter named '{action}'. " +
                "Use '[action]' in the route template to insert the value 'hey'.";

            var handler = CreateRouter();
            var services = CreateServices(action1, action2);

            var route = AttributeRouting.CreateAttributeMegaRoute(handler, services);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await route.RouteAsync(new RouteContext(new DefaultHttpContext()));
            });

            Assert.Equal(expectedMessage, ex.Message);
        }

        [Fact]
        public async Task AttributeRouting_WithControllerActionDescriptor()
        {
            // Arrange
            var controllerType = typeof(HomeController);
            var actionMethod = controllerType.GetMethod("Index");

            var action = new ControllerActionDescriptor();
            action.DisplayName = "Microsoft.AspNetCore.Mvc.Routing.AttributeRoutingTest+HomeController.Index";
            action.MethodInfo = actionMethod;
            action.RouteConstraints = new List<RouteDataActionConstraint>()
            {
                new RouteDataActionConstraint(TreeRouter.RouteGroupKey, "group"),
            };
            action.AttributeRouteInfo = new AttributeRouteInfo();
            action.AttributeRouteInfo.Template = "{controller}/{action}";

            action.RouteValueDefaults.Add("controller", "Home");
            action.RouteValueDefaults.Add("action", "Index");

            var expectedMessage =
                "The following errors occurred with attribute routing information:" + Environment.NewLine +
                Environment.NewLine +
                "For action: 'Microsoft.AspNetCore.Mvc.Routing.AttributeRoutingTest+HomeController.Index'" + Environment.NewLine +
                "Error: The attribute route '{controller}/{action}' cannot contain a parameter named '{controller}'. " +
                "Use '[controller]' in the route template to insert the value 'Home'.";

            var handler = CreateRouter();
            var services = CreateServices(action);

            var route = AttributeRouting.CreateAttributeMegaRoute(handler, services);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await route.RouteAsync(new RouteContext(new DefaultHttpContext()));
            });

            Assert.Equal(expectedMessage, ex.Message);
        }

        private static ActionDescriptor CreateAction(string displayName, string template)
        {
            return new DisplayNameActionDescriptor()
            {
                DisplayName = displayName,
                RouteConstraints = new List<RouteDataActionConstraint>()
                {
                    new RouteDataActionConstraint(TreeRouter.RouteGroupKey, "whatever"),
                },
                AttributeRouteInfo = new AttributeRouteInfo { Template = template },
            };
        }

        private static IRouter CreateRouter()
        {
            return Mock.Of<IRouter>();
        }

        private static IServiceProvider CreateServices(params ActionDescriptor[] actions)
        {
            var collection = new ActionDescriptorCollection(actions, version: 0);

            var actionDescriptorProvider = new Mock<IActionDescriptorCollectionProvider>();
            actionDescriptorProvider
                .Setup(a => a.ActionDescriptors)
                .Returns(collection);

            var routeOptions = new Mock<IOptions<RouteOptions>>();
            routeOptions
                .SetupGet(o => o.Value)
                .Returns(new RouteOptions());

            var services = new ServiceCollection()
                .AddSingleton<IInlineConstraintResolver>(new DefaultInlineConstraintResolver(routeOptions.Object))
                .AddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>()
                .AddSingleton<UrlEncoder>(new UrlTestEncoder());

            services.AddRouting();

            return services.AddSingleton(actionDescriptorProvider.Object)
                .AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance)
                .BuildServiceProvider();
        }

        private class DisplayNameActionDescriptor : ActionDescriptor
        {
        }

        private class HomeController
        {
            public void Index() { }
        }
    }
}
