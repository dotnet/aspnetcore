// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if ASPNET50
using Microsoft.AspNet.Routing;
using Microsoft.Framework.OptionsModel;
using Moq;
using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.AspNet.Mvc.Routing
{
    public class AttributeRoutingTest
    {
        [Fact]
        public void AttributeRouting_SyntaxErrorInTemplate()
        {
            // Arrange
            var action = CreateAction("InvalidTemplate", "{a/dkfk}");

            var expectedMessage =
                "The following errors occurred with attribute routing information:" + Environment.NewLine +
                Environment.NewLine +
                "For action: 'InvalidTemplate'" + Environment.NewLine +
                "Error: There is an incomplete parameter in the route template. " +
                "Check that each '{' character has a matching '}' character." + Environment.NewLine +
                "Parameter name: routeTemplate";

            var router = CreateRouter();
            var services = CreateServices(action);

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(
                () => { AttributeRouting.CreateAttributeMegaRoute(router, services); });

            Assert.Equal(expectedMessage, ex.Message);
        }

        [Fact]
        public void AttributeRouting_DisallowedParameter()
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

            var router = CreateRouter();
            var services = CreateServices(action);

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(
                () => { AttributeRouting.CreateAttributeMegaRoute(router, services); });

            Assert.Equal(expectedMessage, ex.Message);
        }

        [Fact]
        public void AttributeRouting_MultipleErrors()
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

            var router = CreateRouter();
            var services = CreateServices(action1, action2);

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(
                () => { AttributeRouting.CreateAttributeMegaRoute(router, services); });

            Assert.Equal(expectedMessage, ex.Message);
        }

        [Fact]
        public void AttributeRouting_WithReflectedActionDescriptor()
        {
            // Arrange
            var controllerType = typeof(HomeController);
            var actionMethod = controllerType.GetMethod("Index");

            var action = new ControllerActionDescriptor();
            action.DisplayName = "Microsoft.AspNet.Mvc.Routing.AttributeRoutingTest+HomeController.Index";
            action.MethodInfo = actionMethod;
            action.RouteConstraints = new List<RouteDataActionConstraint>()
            {
                new RouteDataActionConstraint(AttributeRouting.RouteGroupKey, "group"),
            };
            action.AttributeRouteInfo = new AttributeRouteInfo();
            action.AttributeRouteInfo.Template = "{controller}/{action}";

            action.RouteValueDefaults.Add("controller", "Home");
            action.RouteValueDefaults.Add("action", "Index");

            var expectedMessage =
                "The following errors occurred with attribute routing information:" + Environment.NewLine +
                Environment.NewLine +
                "For action: 'Microsoft.AspNet.Mvc.Routing.AttributeRoutingTest+HomeController.Index'" + Environment.NewLine +
                "Error: The attribute route '{controller}/{action}' cannot contain a parameter named '{controller}'. " +
                "Use '[controller]' in the route template to insert the value 'Home'.";

            var router = CreateRouter();
            var services = CreateServices(action);

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(
                () => { AttributeRouting.CreateAttributeMegaRoute(router, services); });

            Assert.Equal(expectedMessage, ex.Message);
        }

        private static ActionDescriptor CreateAction(string displayName, string template)
        {
            return new DisplayNameActionDescriptor()
            {
                DisplayName = displayName,
                RouteConstraints = new List<RouteDataActionConstraint>()
                {
                    new RouteDataActionConstraint(AttributeRouting.RouteGroupKey, "whatever"),
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
            var collection = new ActionDescriptorsCollection(actions, version: 0);

            var actionDescriptorProvider = new Mock<IActionDescriptorsCollectionProvider>();
            actionDescriptorProvider
                .Setup(a => a.ActionDescriptors)
                .Returns(collection);

            var services = new Mock<IServiceProvider>();
            services
                .Setup(s => s.GetService(typeof(IActionDescriptorsCollectionProvider)))
                .Returns(actionDescriptorProvider.Object);

            var routeOptions = new Mock<IOptions<RouteOptions>>();
            routeOptions
                .SetupGet(o => o.Options)
                .Returns(new RouteOptions());

            services
                .Setup(s => s.GetService(typeof(IInlineConstraintResolver)))
                .Returns(new DefaultInlineConstraintResolver(services.Object, routeOptions.Object));

            return services.Object;
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
#endif
