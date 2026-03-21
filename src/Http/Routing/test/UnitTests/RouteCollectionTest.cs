// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Microsoft.AspNetCore.Routing;

public class RouteCollectionTest
{
    private static readonly RequestDelegate NullHandler = (c) => Task.CompletedTask;

    [Theory]
    [InlineData(@"Home/Index/23", "/home/index/23", true, false)]
    [InlineData(@"Home/Index/23", "/Home/Index/23", false, false)]
    [InlineData(@"Home/Index/23", "/home/index/23/", true, true)]
    [InlineData(@"Home/Index/23", "/Home/Index/23/", false, true)]
    [InlineData(@"Home/Index/23?Param1=ABC&Param2=Xyz", "/Home/Index/23/?Param1=ABC&Param2=Xyz", false, true)]
    [InlineData(@"Home/Index/23?Param1=ABC&Param2=Xyz", "/Home/Index/23?Param1=ABC&Param2=Xyz", false, false)]
    [InlineData(@"Home/Index/23?Param1=ABC&Param2=Xyz", "/home/index/23/?Param1=ABC&Param2=Xyz", true, true)]
    [InlineData(@"Home/Index/23#Param1=ABC&Param2=Xyz", "/Home/Index/23/#Param1=ABC&Param2=Xyz", false, true)]
    [InlineData(@"Home/Index/23#Param1=ABC&Param2=Xyz", "/home/index/23#Param1=ABC&Param2=Xyz", true, false)]
    [InlineData(@"Home/Index/23/?Param1=ABC&Param2=Xyz", "/home/index/23/?Param1=ABC&Param2=Xyz", true, true)]
    [InlineData(@"Home/Index/23/#Param1=ABC&Param2=Xyz", "/home/index/23/#Param1=ABC&Param2=Xyz", true, false)]
    public void GetVirtualPath_CanLowerCaseUrls_And_AppendTrailingSlash_BasedOnOptions(
        string returnUrl,
        string expectedUrl,
        bool lowercaseUrls,
        bool appendTrailingSlash)
    {
        // Arrange
        var target = new Mock<IRouter>(MockBehavior.Strict);
        target
            .Setup(e => e.GetVirtualPath(It.IsAny<VirtualPathContext>()))
            .Returns(new VirtualPathData(target.Object, returnUrl));

        var routeCollection = new RouteCollection();
        routeCollection.Add(target.Object);
        var virtualPathContext = CreateVirtualPathContext(
            options: GetRouteOptions(
                lowerCaseUrls: lowercaseUrls,
                appendTrailingSlash: appendTrailingSlash));

        // Act
        var pathData = routeCollection.GetVirtualPath(virtualPathContext);

        // Assert
        Assert.Equal(expectedUrl, pathData.VirtualPath);
        Assert.Same(target.Object, pathData.Router);
        Assert.Empty(pathData.DataTokens);
    }

    [Theory]
    [InlineData(@"\u0130", @"/\u0130", true)]
    [InlineData(@"\u0049", @"/\u0049", true)]
    [InlineData(@"�ino", @"/�ino", true)]
    public void GetVirtualPath_DoesntLowerCaseUrls_Invariant(
        string returnUrl,
        string lowercaseUrl,
        bool lowercaseUrls)
    {
        // Arrange
        var target = new Mock<IRouter>(MockBehavior.Strict);
        target
            .Setup(e => e.GetVirtualPath(It.IsAny<VirtualPathContext>()))
            .Returns(new VirtualPathData(target.Object, returnUrl));

        var routeCollection = new RouteCollection();
        routeCollection.Add(target.Object);
        var virtualPathContext = CreateVirtualPathContext(options: GetRouteOptions(lowercaseUrls));

        // Act
        var pathData = routeCollection.GetVirtualPath(virtualPathContext);

        // Assert
        Assert.Equal(lowercaseUrl, pathData.VirtualPath);
        Assert.Same(target.Object, pathData.Router);
        Assert.Empty(pathData.DataTokens);
    }

    [Theory]
    [InlineData(@"Home/Index/23?Param1=ABC&Param2=Xyz", "/Home/Index/23?Param1=ABC&Param2=Xyz", false, true, false)]
    [InlineData(@"Home/Index/23?Param1=ABC&Param2=Xyz", "/Home/Index/23?Param1=ABC&Param2=Xyz", false, false, false)]
    [InlineData(@"Home/Index/23?Param1=ABC&Param2=Xyz", "/home/index/23/?param1=abc&param2=xyz", true, true, true)]
    [InlineData(@"Home/Index/23#Param1=ABC&Param2=Xyz", "/Home/Index/23/#Param1=ABC&Param2=Xyz", false, true, true)]
    [InlineData(@"Home/Index/23#Param1=ABC&Param2=Xyz", "/home/index/23#Param1=ABC&Param2=Xyz", true, false, false)]
    [InlineData(@"Home/Index/23/?Param1=ABC&Param2=Xyz", "/home/index/23/?param1=abc&param2=xyz", true, true, true)]
    [InlineData(@"Home/Index/23/#Param1=ABC&Param2=Xyz", "/home/index/23/#Param1=ABC&Param2=Xyz", true, false, true)]
    [InlineData(@"Home/Index/23/#Param1=ABC&Param2=Xyz", "/home/index/23/#param1=abc&param2=xyz", true, true, true)]
    public void GetVirtualPath_CanLowerCaseUrls_QueryStrings_BasedOnOptions(
        string returnUrl,
        string expectedUrl,
        bool lowercaseUrls,
        bool lowercaseQueryStrings, bool appendTrailingSlash)
    {
        // Arrange
        var target = new Mock<IRouter>(MockBehavior.Strict);
        target
            .Setup(e => e.GetVirtualPath(It.IsAny<VirtualPathContext>()))
            .Returns(new VirtualPathData(target.Object, returnUrl));

        var routeCollection = new RouteCollection();
        routeCollection.Add(target.Object);
        var virtualPathContext = CreateVirtualPathContext(
            options: GetRouteOptions(
                lowerCaseUrls: lowercaseUrls,
                lowercaseQueryStrings: lowercaseQueryStrings,
                appendTrailingSlash: appendTrailingSlash));

        // Act
        var pathData = routeCollection.GetVirtualPath(virtualPathContext);

        // Assert
        Assert.Equal(expectedUrl, pathData.VirtualPath);
        Assert.Same(target.Object, pathData.Router);
        Assert.Empty(pathData.DataTokens);
    }

    [Theory]
    [MemberData(nameof(DataTokensTestData))]
    public void GetVirtualPath_ReturnsDataTokens(RouteValueDictionary dataTokens, string routerName)
    {
        // Arrange
        var virtualPath = "/TestVirtualPath";

        var pathContextValues = new RouteValueDictionary { { "controller", virtualPath } };

        var pathContext = CreateVirtualPathContext(
            pathContextValues,
            GetRouteOptions(),
            routerName);

        var route = CreateTemplateRoute("{controller}", routerName, dataTokens);
        var routeCollection = new RouteCollection();
        routeCollection.Add(route);

        var expectedDataTokens = dataTokens ?? new RouteValueDictionary();

        // Act
        var pathData = routeCollection.GetVirtualPath(pathContext);

        // Assert
        Assert.NotNull(pathData);
        Assert.Same(route, pathData.Router);

        Assert.Equal(virtualPath, pathData.VirtualPath);

        Assert.Equal(expectedDataTokens.Count, pathData.DataTokens.Count);
        foreach (var dataToken in expectedDataTokens)
        {
            Assert.True(pathData.DataTokens.ContainsKey(dataToken.Key));
            Assert.Equal(dataToken.Value, pathData.DataTokens[dataToken.Key]);
        }
    }

    [Fact]
    public async Task RouteAsync_FirstMatches()
    {
        // Arrange
        var routes = new RouteCollection();

        var route1 = CreateRoute(accept: true);
        routes.Add(route1.Object);

        var route2 = CreateRoute(accept: false);
        routes.Add(route2.Object);

        var context = CreateRouteContext("/Cool");

        // Act
        await routes.RouteAsync(context);

        // Assert
        route1.Verify(e => e.RouteAsync(It.IsAny<RouteContext>()), Times.Exactly(1));
        route2.Verify(e => e.RouteAsync(It.IsAny<RouteContext>()), Times.Exactly(0));
        Assert.NotNull(context.Handler);

        Assert.Single(context.RouteData.Routers);
        Assert.Same(route1.Object, context.RouteData.Routers[0]);
    }

    [Fact]
    public async Task RouteAsync_SecondMatches()
    {
        // Arrange

        var routes = new RouteCollection();
        var route1 = CreateRoute(accept: false);
        routes.Add(route1.Object);

        var route2 = CreateRoute(accept: true);
        routes.Add(route2.Object);

        var context = CreateRouteContext("/Cool");

        // Act
        await routes.RouteAsync(context);

        // Assert
        route1.Verify(e => e.RouteAsync(It.IsAny<RouteContext>()), Times.Exactly(1));
        route2.Verify(e => e.RouteAsync(It.IsAny<RouteContext>()), Times.Exactly(1));
        Assert.NotNull(context.Handler);

        Assert.Single(context.RouteData.Routers);
        Assert.Same(route2.Object, context.RouteData.Routers[0]);
    }

    [Fact]
    public async Task RouteAsync_NoMatch()
    {
        // Arrange
        var routes = new RouteCollection();
        var route1 = CreateRoute(accept: false);
        routes.Add(route1.Object);

        var route2 = CreateRoute(accept: false);
        routes.Add(route2.Object);

        var context = CreateRouteContext("/Cool");

        // Act
        await routes.RouteAsync(context);

        // Assert
        route1.Verify(e => e.RouteAsync(It.IsAny<RouteContext>()), Times.Exactly(1));
        route2.Verify(e => e.RouteAsync(It.IsAny<RouteContext>()), Times.Exactly(1));
        Assert.Null(context.Handler);

        Assert.Empty(context.RouteData.Routers);
    }

    [Theory]
    [InlineData(false, "/RouteName")]
    [InlineData(true, "/routename")]
    public void NamedRouteTests_GetNamedRoute_ReturnsValue(bool lowercaseUrls, string expectedUrl)
    {
        // Arrange
        var routeCollection = GetNestedRouteCollection(new string[] { "Route1", "Route2", "RouteName", "Route3" });
        var virtualPathContext = CreateVirtualPathContext(
            routeName: "RouteName",
            options: GetRouteOptions(lowercaseUrls));

        // Act
        var pathData = routeCollection.GetVirtualPath(virtualPathContext);

        // Assert
        Assert.Equal(expectedUrl, pathData.VirtualPath);
        var namedRouter = Assert.IsAssignableFrom<INamedRouter>(pathData.Router);
        Assert.Equal(virtualPathContext.RouteName, namedRouter.Name);
        Assert.Empty(pathData.DataTokens);
    }

    [Fact]
    public void NamedRouteTests_GetNamedRoute_RouteNotFound()
    {
        // Arrange
        var routeCollection = GetNestedRouteCollection(new string[] { "Route1", "Route2", "Route3" });
        var virtualPathContext = CreateVirtualPathContext("NonExistantRoute");

        // Act
        var stringVirtualPath = routeCollection.GetVirtualPath(virtualPathContext);

        // Assert
        Assert.Null(stringVirtualPath);
    }

    [Fact]
    public void NamedRouteTests_GetNamedRoute_AmbiguousRoutesInCollection_DoesNotThrowForUnambiguousRoute()
    {
        // Arrange
        var routeCollection = GetNestedRouteCollection(new string[] { "Route1", "Route2", "Route3", "Route4" });

        // Add Duplicate route.
        routeCollection.Add(CreateNamedRoute("Route3"));
        var virtualPathContext = CreateVirtualPathContext(routeName: "Route1", options: GetRouteOptions(true));

        // Act
        var pathData = routeCollection.GetVirtualPath(virtualPathContext);

        // Assert
        Assert.Equal("/route1", pathData.VirtualPath);
        var namedRouter = Assert.IsAssignableFrom<INamedRouter>(pathData.Router);
        Assert.Equal("Route1", namedRouter.Name);
        Assert.Empty(pathData.DataTokens);
    }

    [Fact]
    public void NamedRouteTests_GetNamedRoute_AmbiguousRoutesInCollection_ThrowsForAmbiguousRoute()
    {
        // Arrange
        var ambiguousRoute = "ambiguousRoute";
        var routeCollection = GetNestedRouteCollection(new string[] { "Route1", "Route2", ambiguousRoute, "Route4" });

        // Add Duplicate route.
        routeCollection.Add(CreateNamedRoute(ambiguousRoute));
        var virtualPathContext = CreateVirtualPathContext(routeName: ambiguousRoute, options: GetRouteOptions());

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => routeCollection.GetVirtualPath(virtualPathContext));
        Assert.Equal(
            "The supplied route name 'ambiguousRoute' is ambiguous and matched more than one route.",
            ex.Message);
    }

    [Fact]
    public void GetVirtualPath_AmbiguousRoutes_RequiresRouteValueValidation_Error()
    {
        // Arrange
        var namedRoute = CreateNamedRoute("Ambiguous", accept: false);

        var routeCollection = new RouteCollection();
        routeCollection.Add(namedRoute);

        var innerRouteCollection = new RouteCollection();
        innerRouteCollection.Add(namedRoute);
        routeCollection.Add(innerRouteCollection);

        var virtualPathContext = CreateVirtualPathContext("Ambiguous");

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => routeCollection.GetVirtualPath(virtualPathContext));
        Assert.Equal("The supplied route name 'Ambiguous' is ambiguous and matched more than one route.", ex.Message);
    }

    // "Integration" tests for RouteCollection

    public static IEnumerable<object[]> IntegrationTestData
    {
        get
        {
            yield return new object[] {
                    "{controller}/{action}",
                    new RouteValueDictionary { { "controller", "Home" }, { "action", "Index" } },
                    "/home/index",
                    true };

            yield return new object[] {
                    "{controller}/{action}/",
                    new RouteValueDictionary { { "controller", "Home" }, { "action", "Index" } },
                    "/Home/Index",
                    false };

            yield return new object[] {
                    "api/{action}/",
                    new RouteValueDictionary { { "action", "Create" } },
                    "/api/create",
                    true };

            yield return new object[] {
                    "api/{action}/{id}",
                    new RouteValueDictionary {
                        { "action", "Create" },
                        { "id", "23" },
                        { "Param1", "Value1" },
                        { "Param2", "Value2" } },
                    "/api/create/23?Param1=Value1&Param2=Value2",
                    true };

            yield return new object[] {
                    "api/{action}/{id}",
                    new RouteValueDictionary {
                        { "action", "Create" },
                        { "id", "23" },
                        { "Param1", "Value1" },
                        { "Param2", "Value2" } },
                    "/api/Create/23?Param1=Value1&Param2=Value2",
                    false };
        }
    }

    [Theory]
    [MemberData(nameof(IntegrationTestData))]
    public void GetVirtualPath_Success(
        string template,
        RouteValueDictionary values,
        string expectedUrl,
        bool lowercaseUrls)
    {
        // Arrange
        var routeCollection = new RouteCollection();
        var route = CreateTemplateRoute(template);
        routeCollection.Add(route);
        var context = CreateVirtualPathContext(values, options: GetRouteOptions(lowercaseUrls));

        // Act
        var pathData = routeCollection.GetVirtualPath(context);

        // Assert
        Assert.Equal(expectedUrl, pathData.VirtualPath);
        Assert.Same(route, pathData.Router);
        Assert.Empty(pathData.DataTokens);
    }

    public static IEnumerable<object[]> RestoresRouteDataForEachRouterData
    {
        get
        {
            // Here 'area' segment doesn't have a value but the later segments have values. This is an invalid
            // route match and the url generation should look into the next available route in the collection.
            yield return new object[] {
                    new Route[]
                    {
                        CreateTemplateRoute("{area?}/{controller=Home}/{action=Index}/{id?}", "1"),
                        CreateTemplateRoute("{controller=Home}/{action=Index}/{id?}", "2")
                    },
                    new RouteValueDictionary(new { controller = "Test", action = "Index" }),
                    "/Test",
                    "2" };

            // Here the segment 'a' is valid but 'b' is not as it would be empty. This would be an invalid route match, but
            // the route value of 'a' should still be present to be evaluated for the next available route.
            yield return new object[] {
                    new[]
                    {
                        CreateTemplateRoute("{a}/{b?}/{c}", "1"),
                        CreateTemplateRoute("{a=Home}/{b=Index}", "2")
                    },
                    new RouteValueDictionary(new { a = "Test", c = "Foo" }),
                    "/Test?c=Foo",
                    "2" };
        }
    }

    [Theory]
    [MemberData(nameof(RestoresRouteDataForEachRouterData))]
    public void GetVirtualPath_RestoresRouteData_ForEachRouter(
        Route[] routes,
        RouteValueDictionary routeValues,
        string expectedUrl,
        string expectedRouteToMatch)
    {
        // Arrange
        var routeCollection = new RouteCollection();
        foreach (var route in routes)
        {
            routeCollection.Add(route);
        }
        var context = CreateVirtualPathContext(routeValues);

        // Act
        var pathData = routeCollection.GetVirtualPath(context);

        // Assert
        Assert.Equal(expectedUrl, pathData.VirtualPath);
        Assert.Same(expectedRouteToMatch, ((INamedRouter)pathData.Router).Name);
        Assert.Empty(pathData.DataTokens);
    }

    [Fact]
    public void GetVirtualPath_NoBestEffort_NoMatch()
    {
        // Arrange
        var route1 = CreateRoute(accept: false, match: false, matchValue: "bad");
        var route2 = CreateRoute(accept: false, match: false, matchValue: "bad");
        var route3 = CreateRoute(accept: false, match: false, matchValue: "bad");

        var routeCollection = new RouteCollection();
        routeCollection.Add(route1.Object);
        routeCollection.Add(route2.Object);
        routeCollection.Add(route3.Object);

        var virtualPathContext = CreateVirtualPathContext();

        // Act
        var path = routeCollection.GetVirtualPath(virtualPathContext);

        Assert.Null(path);

        // All of these should be called
        route1.Verify(r => r.GetVirtualPath(It.IsAny<VirtualPathContext>()), Times.Once());
        route2.Verify(r => r.GetVirtualPath(It.IsAny<VirtualPathContext>()), Times.Once());
        route3.Verify(r => r.GetVirtualPath(It.IsAny<VirtualPathContext>()), Times.Once());
    }

    // DataTokens test data for RouterCollection.GetVirtualPath
    public static IEnumerable<object[]> DataTokensTestData
    {
        get
        {
            yield return new object[] { null, null };
            yield return new object[] { new RouteValueDictionary(), null };
            yield return new object[] { new RouteValueDictionary() { { "tokenKey", "tokenValue" } }, null };

            yield return new object[] { null, "routerA" };
            yield return new object[] { new RouteValueDictionary(), "routerA" };
            yield return new object[] { new RouteValueDictionary() { { "tokenKey", "tokenValue" } }, "routerA" };
        }
    }

    private static RouteCollection GetRouteCollectionWithNamedRoutes(IEnumerable<string> routeNames)
    {
        var routes = new RouteCollection();
        foreach (var routeName in routeNames)
        {
            var route1 = CreateNamedRoute(routeName, accept: true);
            routes.Add(route1);
        }

        return routes;
    }

    private static RouteCollection GetNestedRouteCollection(string[] routeNames)
    {
        int index = Random.Shared.Next(0, routeNames.Length - 1);
        var first = routeNames.Take(index).ToArray();
        var second = routeNames.Skip(index).ToArray();

        var rc1 = GetRouteCollectionWithNamedRoutes(first);
        var rc2 = GetRouteCollectionWithNamedRoutes(second);
        var rc3 = new RouteCollection();
        var rc4 = new RouteCollection();

        rc1.Add(rc3);
        rc4.Add(rc2);

        // Add a few unnamedRoutes.
        rc1.Add(CreateRoute(accept: false).Object);
        rc2.Add(CreateRoute(accept: false).Object);
        rc3.Add(CreateRoute(accept: false).Object);
        rc3.Add(CreateRoute(accept: false).Object);
        rc4.Add(CreateRoute(accept: false).Object);
        rc4.Add(CreateRoute(accept: false).Object);

        var routeCollection = new RouteCollection();
        routeCollection.Add(rc1);
        routeCollection.Add(rc4);

        return routeCollection;
    }

    private static INamedRouter CreateNamedRoute(string name, bool accept = false, string matchValue = null)
    {
        if (matchValue == null)
        {
            matchValue = name;
        }

        var target = new Mock<INamedRouter>(MockBehavior.Strict);
        target
            .Setup(e => e.GetVirtualPath(It.IsAny<VirtualPathContext>()))
            .Returns<VirtualPathContext>(c =>
                c.RouteName == name ? new VirtualPathData(target.Object, matchValue) : null)
            .Verifiable();

        target
            .SetupGet(e => e.Name)
            .Returns(name);

        target
            .Setup(e => e.RouteAsync(It.IsAny<RouteContext>()))
            .Callback<RouteContext>((c) => c.Handler = accept ? NullHandler : null)
            .Returns(Task.FromResult<object>(null))
            .Verifiable();

        return target.Object;
    }

    private static Route CreateTemplateRoute(
        string template,
        string routerName = null,
        RouteValueDictionary dataTokens = null,
        IInlineConstraintResolver constraintResolver = null)
    {
        var target = new Mock<IRouter>(MockBehavior.Strict);
        target
            .Setup(e => e.GetVirtualPath(It.IsAny<VirtualPathContext>()))
            .Returns<VirtualPathContext>(rc => null);

        if (constraintResolver == null)
        {
            constraintResolver = new Mock<IInlineConstraintResolver>().Object;
        }

        return new Route(
            target.Object,
            routerName,
            template,
            defaults: null,
            constraints: null,
            dataTokens: dataTokens,
            inlineConstraintResolver: constraintResolver);
    }

    private static VirtualPathContext CreateVirtualPathContext(
        string routeName = null,
        ILoggerFactory loggerFactory = null,
        Action<RouteOptions> options = null)
    {
        if (loggerFactory == null)
        {
            loggerFactory = NullLoggerFactory.Instance;
        }

        var request = new Mock<HttpRequest>(MockBehavior.Strict);

        var services = new ServiceCollection();
        services.AddOptions();
        services.AddRouting();
        if (options != null)
        {
            services.Configure(options);
        }

        var context = new Mock<HttpContext>(MockBehavior.Strict);
        context.SetupGet(m => m.RequestServices).Returns(services.BuildServiceProvider());
        context.SetupGet(c => c.Request).Returns(request.Object);

        return new VirtualPathContext(context.Object, null, null, routeName);
    }

    private static VirtualPathContext CreateVirtualPathContext(
        RouteValueDictionary values,
        Action<RouteOptions> options = null,
        string routeName = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddOptions();
        services.AddRouting();
        if (options != null)
        {
            services.Configure<RouteOptions>(options);
        }

        var context = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider(),
        };

        return new VirtualPathContext(
            context,
            ambientValues: null,
            values: values,
            routeName: routeName);
    }

    private static RouteContext CreateRouteContext(
        string requestPath,
        ILoggerFactory loggerFactory = null,
        RouteOptions options = null)
    {
        if (loggerFactory == null)
        {
            loggerFactory = NullLoggerFactory.Instance;
        }

        if (options == null)
        {
            options = new RouteOptions();
        }

        var request = new Mock<HttpRequest>(MockBehavior.Strict);
        request.SetupGet(r => r.Path).Returns(requestPath);

        var optionsAccessor = new Mock<IOptions<RouteOptions>>(MockBehavior.Strict);
        optionsAccessor.SetupGet(o => o.Value).Returns(options);

        var context = new Mock<HttpContext>(MockBehavior.Strict);
        context.Setup(m => m.RequestServices.GetService(typeof(ILoggerFactory)))
            .Returns(loggerFactory);
        context.Setup(m => m.RequestServices.GetService(typeof(IOptions<RouteOptions>)))
            .Returns(optionsAccessor.Object);
        context.SetupGet(c => c.Request).Returns(request.Object);

        return new RouteContext(context.Object);
    }

    private static Mock<IRouter> CreateRoute(
        bool accept = true,
        bool match = false,
        string matchValue = "value")
    {
        var target = new Mock<IRouter>(MockBehavior.Strict);
        target
            .Setup(e => e.GetVirtualPath(It.IsAny<VirtualPathContext>()))
            .Returns(accept || match ? new VirtualPathData(target.Object, matchValue) : null)
            .Verifiable();

        target
            .Setup(e => e.RouteAsync(It.IsAny<RouteContext>()))
            .Callback<RouteContext>((c) => c.Handler = accept ? NullHandler : null)
            .Returns(Task.FromResult<object>(null))
            .Verifiable();

        return target;
    }

    private static Action<RouteOptions> GetRouteOptions(
        bool lowerCaseUrls = false,
        bool appendTrailingSlash = false,
        bool lowercaseQueryStrings = false)
    {
        return (options) =>
        {
            options.LowercaseUrls = lowerCaseUrls;
            options.AppendTrailingSlash = appendTrailingSlash;
            options.LowercaseQueryStrings = lowercaseQueryStrings;
        };
    }
}
