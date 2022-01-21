// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Microsoft.AspNetCore.Mvc.Routing;

public class UrlHelperTest : UrlHelperTestBase
{
    protected override IServiceProvider CreateServices()
    {
        var services = GetCommonServices();
        return services.BuildServiceProvider();
    }

    protected override IUrlHelper CreateUrlHelper(string appRoot, string host, string protocol)
    {
        var services = CreateServices();
        var httpContext = CreateHttpContext(services, appRoot, host, protocol);
        var actionContext = CreateActionContext(httpContext);
        var defaultRoutes = GetDefaultRoutes(services);
        actionContext.RouteData.Routers.Add(defaultRoutes);
        return new UrlHelper(actionContext);
    }

    protected override IUrlHelper CreateUrlHelperWithDefaultRoutes(
        string appRoot,
        string host,
        string protocol,
        string routeName,
        string template)
    {
        var services = CreateServices();
        var httpContext = CreateHttpContext(services, appRoot, host, protocol);
        var actionContext = CreateActionContext(httpContext);
        var router = GetDefaultRoutes(services, routeName, template);
        actionContext.RouteData.Routers.Add(router);
        return CreateUrlHelper(actionContext);
    }

    protected override IUrlHelper CreateUrlHelper(ActionContext actionContext)
    {
        return new UrlHelper(actionContext);
    }

    protected override IUrlHelper CreateUrlHelperWithDefaultRoutes(string appRoot, string host, string protocol)
    {
        var services = CreateServices();
        var context = CreateHttpContext(services, appRoot, host, protocol);

        var router = GetDefaultRoutes(services);
        var actionContext = CreateActionContext(context);
        actionContext.RouteData.Routers.Add(router);

        return CreateUrlHelper(actionContext);
    }

    protected override IUrlHelper CreateUrlHelper(
        string appRoot,
        string host,
        string protocol,
        string routeName,
        string template,
        object defaults,
        object requiredValues)
    {
        var services = CreateServices();
        var routeBuilder = CreateRouteBuilder(services);
        routeBuilder.MapRoute(
            routeName,
            template,
            defaults);
        var router = routeBuilder.Build();
        var httpContext = CreateHttpContext(services, appRoot, host, protocol);
        var actionContext = CreateActionContext(httpContext);
        actionContext.RouteData.Routers.Add(router);
        return CreateUrlHelper(actionContext);
    }

    private static IRouter GetDefaultRoutes(IServiceProvider services)
    {
        return GetDefaultRoutes(services, "mockRoute", "/mockTemplate");
    }

    private static IRouter GetDefaultRoutes(
        IServiceProvider services,
        string mockRouteName,
        string mockTemplateValue)
    {
        var routeBuilder = CreateRouteBuilder(services);

        var target = new Mock<IRouter>(MockBehavior.Strict);
        target
            .Setup(router => router.GetVirtualPath(It.IsAny<VirtualPathContext>()))
            .Returns<VirtualPathContext>(context => null);
        routeBuilder.DefaultHandler = target.Object;

        routeBuilder.MapRoute(
            "OrdersApi",
            "api/orders/{id}",
            new RouteValueDictionary(new { controller = "Orders", action = "GetById" }));

        routeBuilder.MapRoute(
            string.Empty,
            "{controller}/{action}/{id}",
            new RouteValueDictionary(new { id = "defaultid" }));

        routeBuilder.MapRoute(
            "namedroute",
            "named/{controller}/{action}/{id}",
            new RouteValueDictionary(new { id = "defaultid" }));

        var mockHttpRoute = new Mock<IRouter>();
        mockHttpRoute
            .Setup(mock => mock.GetVirtualPath(It.Is<VirtualPathContext>(c => string.Equals(c.RouteName, mockRouteName))))
            .Returns(new VirtualPathData(mockHttpRoute.Object, mockTemplateValue));

        routeBuilder.Routes.Add(mockHttpRoute.Object);
        return routeBuilder.Build();
    }

    private static IRouteBuilder CreateRouteBuilder(IServiceProvider services)
    {
        var app = new Mock<IApplicationBuilder>();
        app
            .SetupGet(a => a.ApplicationServices)
            .Returns(services);

        return new RouteBuilder(app.Object)
        {
            DefaultHandler = new PassThroughRouter(),
        };
    }

    private class PassThroughRouter : IRouter
    {
        public VirtualPathData GetVirtualPath(VirtualPathContext context)
        {
            return null;
        }

        public Task RouteAsync(RouteContext context)
        {
            context.Handler = (c) => Task.FromResult(0);
            return Task.FromResult(false);
        }
    }
}
