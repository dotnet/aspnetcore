// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Microsoft.AspNetCore.Mvc.Routing;

public class AttributeRoutingTest
{
    [Fact]
    [ReplaceCulture]
    public async Task AttributeRouting_SyntaxErrorInTemplate()
    {
        // Arrange
        var value = "a/dkfk";
        var action = CreateAction("InvalidTemplate", "{" + value + "}");

        var expectedMessage =
            "The following errors occurred with attribute routing information:" + Environment.NewLine +
            Environment.NewLine +
            "For action: 'InvalidTemplate'" + Environment.NewLine +
            "Error: The route parameter name 'a/dkfk' is invalid. Route parameter names must be non-empty and " +
            "cannot contain these characters: '{', '}', '/'. The '?' character marks a parameter as optional, " +
            "and can occur only at the end of the parameter. The '*' character marks a parameter as catch-all, " +
            "and can occur only at the start of the parameter. (Parameter 'routeTemplate')";

        var services = CreateServices(action);

        var route = AttributeRouting.CreateAttributeMegaRoute(services);
        var routeContext = new RouteContext(new DefaultHttpContext());

        // Act & Assert
        var ex = await Assert.ThrowsAsync<RouteCreationException>(() => route.RouteAsync(routeContext));

        Assert.Equal(expectedMessage, ex.Message);
    }

    [Fact]
    public async Task AttributeRouting_DisallowedParameter()
    {
        // Arrange
        var action = CreateAction("DisallowedParameter", "{foo}/{action}");
        action.RouteValues.Add("foo", "bleh");

        var expectedMessage =
            "The following errors occurred with attribute routing information:" + Environment.NewLine +
            Environment.NewLine +
            "For action: 'DisallowedParameter'" + Environment.NewLine +
            "Error: The attribute route '{foo}/{action}' cannot contain a parameter named '{foo}'. " +
            "Use '[foo]' in the route template to insert the value 'bleh'.";

        var services = CreateServices(action);

        var route = AttributeRouting.CreateAttributeMegaRoute(services);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<RouteCreationException>(async () =>
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
        action1.RouteValues.Add("foo", "bleh");

        var action2 = CreateAction("DisallowedParameter2", "cool/{action}");
        action2.RouteValues.Add("action", "hey");

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

        var services = CreateServices(action1, action2);

        var route = AttributeRouting.CreateAttributeMegaRoute(services);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<RouteCreationException>(async () =>
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
        action.RouteValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "controller", "Home" },
                { "action", "Index" },
            };
        action.AttributeRouteInfo = new AttributeRouteInfo();
        action.AttributeRouteInfo.Template = "{controller}/{action}";

        var expectedMessage =
            "The following errors occurred with attribute routing information:" + Environment.NewLine +
            Environment.NewLine +
            "For action: 'Microsoft.AspNetCore.Mvc.Routing.AttributeRoutingTest+HomeController.Index'" + Environment.NewLine +
            "Error: The attribute route '{controller}/{action}' cannot contain a parameter named '{controller}'. " +
            "Use '[controller]' in the route template to insert the value 'Home'.";

        var services = CreateServices(action);

        var route = AttributeRouting.CreateAttributeMegaRoute(services);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<RouteCreationException>(async () =>
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
            RouteValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            AttributeRouteInfo = new AttributeRouteInfo { Template = template },
        };
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

        var inlineConstraintResolver = new DefaultInlineConstraintResolver(routeOptions.Object, Mock.Of<IServiceProvider>());

        var services = new ServiceCollection()
            .AddSingleton<IInlineConstraintResolver>(inlineConstraintResolver);

        services.AddRouting();
        services.AddOptions();
        services.AddLogging();

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
