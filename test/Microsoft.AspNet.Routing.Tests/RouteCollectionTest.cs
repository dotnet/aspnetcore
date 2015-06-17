// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if DNX451
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Routing.Logging;
using Microsoft.AspNet.Routing.Template;
using Microsoft.Framework.Logging.Testing;
using Microsoft.Framework.Logging;
using Microsoft.Framework.OptionsModel;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Routing
{
    public class RouteCollectionTest
    {
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
                    useBestEffortLinkGeneration: true,
                    appendTrailingSlash: appendTrailingSlash));

            // Act
            var pathData = routeCollection.GetVirtualPath(virtualPathContext);

            // Assert
            Assert.Equal(new PathString(expectedUrl), pathData.VirtualPath);
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
            Assert.Equal(new PathString(lowercaseUrl), pathData.VirtualPath);
            Assert.Same(target.Object, pathData.Router);
            Assert.Empty(pathData.DataTokens);
        }

        [Theory]
        [MemberData("DataTokensTestData")]
        public void GetVirtualPath_ReturnsDataTokens(RouteValueDictionary dataTokens, string routerName)
        {
            // Arrange
            var virtualPath = new PathString("/TestVirtualPath");

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
            Assert.True(context.IsHandled);

            Assert.Equal(1, context.RouteData.Routers.Count);
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
            Assert.True(context.IsHandled);

            Assert.Equal(1, context.RouteData.Routers.Count);
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
            Assert.False(context.IsHandled);

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
            Assert.Equal(new PathString(expectedUrl), pathData.VirtualPath);
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
            Assert.Equal(new PathString("/route1"), pathData.VirtualPath);
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

            var options = new RouteOptions()
            {
                UseBestEffortLinkGeneration = true,
            };

            var virtualPathContext = CreateVirtualPathContext("Ambiguous", options: options);

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => routeCollection.GetVirtualPath(virtualPathContext));
            Assert.Equal("The supplied route name 'Ambiguous' is ambiguous and matched more than one route.", ex.Message);
        }

        [Fact]
        public void GetVirtualPath_NamedRoute_BestEffort_BestInTopCollection()
        {
            // Arrange
            var bestMatch = CreateNamedRoute("Match", accept: true, matchValue: "best");
            var noMatch = CreateNamedRoute("NoMatch", accept: true, matchValue: "bad");

            var routeCollection = new RouteCollection();
            routeCollection.Add(bestMatch);

            var innerRouteCollection = new RouteCollection();
            innerRouteCollection.Add(noMatch);
            routeCollection.Add(innerRouteCollection);

            var options = new RouteOptions()
            {
                UseBestEffortLinkGeneration = true,
            };

            var virtualPathContext = CreateVirtualPathContext("Match", options: options);

            // Act
            var pathData = routeCollection.GetVirtualPath(virtualPathContext);

            Assert.Equal(new PathString("/best"), pathData.VirtualPath);
            var namedRouter = Assert.IsAssignableFrom<INamedRouter>(pathData.Router);
            Assert.Equal("Match", namedRouter.Name);
            Assert.Empty(pathData.DataTokens);
        }

        [Fact]
        public void GetVirtualPath_NamedRoute_BestEffort_BestMatchInNestedCollection()
        {
            // Arrange
            var bestMatch = CreateNamedRoute("NoMatch", accept: true, matchValue: "bad");
            var noMatch = CreateNamedRoute("Match", accept: true, matchValue: "best");

            var routeCollection = new RouteCollection();
            routeCollection.Add(noMatch);

            var innerRouteCollection = new RouteCollection();
            innerRouteCollection.Add(bestMatch);
            routeCollection.Add(innerRouteCollection);

            var options = new RouteOptions()
            {
                UseBestEffortLinkGeneration = true,
            };

            var virtualPathContext = CreateVirtualPathContext("Match", options: options);

            // Act
            var pathData = routeCollection.GetVirtualPath(virtualPathContext);

            Assert.Equal(new PathString("/best"), pathData.VirtualPath);
            var namedRouter = Assert.IsAssignableFrom<INamedRouter>(pathData.Router);
            Assert.Equal("Match", namedRouter.Name);
            Assert.Empty(pathData.DataTokens);
        }

        [Fact]
        public void GetVirtualPath_NamedRoute_BestEffort_FirstRouteWins()
        {
            // Arrange
            var bestMatch = CreateNamedRoute("Match", accept: false, matchValue: "best");
            var noMatch = CreateNamedRoute("NoMatch", accept: false, matchValue: "bad");

            var routeCollection = new RouteCollection();
            routeCollection.Add(noMatch);

            var innerRouteCollection = new RouteCollection();
            innerRouteCollection.Add(bestMatch);
            routeCollection.Add(innerRouteCollection);

            var options = new RouteOptions()
            {
                UseBestEffortLinkGeneration = true,
            };

            var virtualPathContext = CreateVirtualPathContext("Match", options: options);

            // Act
            var pathData = routeCollection.GetVirtualPath(virtualPathContext);

            Assert.Equal(new PathString("/best"), pathData.VirtualPath);
            var namedRouter = Assert.IsAssignableFrom<INamedRouter>(pathData.Router);
            Assert.Equal("Match", namedRouter.Name);
            Assert.Empty(pathData.DataTokens);
        }

        [Fact]
        public void GetVirtualPath_BestEffort_FirstRouteWins()
        {
            // Arrange
            var route1 = CreateRoute(accept: false, match: true, matchValue: "best");
            var route2 = CreateRoute(accept: false, match: true, matchValue: "bad");
            var route3 = CreateRoute(accept: false, match: true, matchValue: "bad");

            var routeCollection = new RouteCollection();
            routeCollection.Add(route1.Object);
            routeCollection.Add(route2.Object);
            routeCollection.Add(route3.Object);

            var options = new RouteOptions()
            {
                UseBestEffortLinkGeneration = true,
            };

            var virtualPathContext = CreateVirtualPathContext(options: options);

            // Act
            var pathData = routeCollection.GetVirtualPath(virtualPathContext);

            Assert.Equal(new PathString("/best"), pathData.VirtualPath);
            Assert.Same(route1.Object, pathData.Router);
            Assert.Empty(pathData.DataTokens);

            // All of these should be called
            route1.Verify(r => r.GetVirtualPath(It.IsAny<VirtualPathContext>()), Times.Once());
            route2.Verify(r => r.GetVirtualPath(It.IsAny<VirtualPathContext>()), Times.Once());
            route3.Verify(r => r.GetVirtualPath(It.IsAny<VirtualPathContext>()), Times.Once());
        }

        [Fact]
        public void GetVirtualPath_NoBestEffort_NoMatch()
        {
            // Arrange
            var route1 = CreateRoute(accept: false, match: true, matchValue: "best");
            var route2 = CreateRoute(accept: false, match: true, matchValue: "bad");
            var route3 = CreateRoute(accept: false, match: true, matchValue: "bad");

            var routeCollection = new RouteCollection();
            routeCollection.Add(route1.Object);
            routeCollection.Add(route2.Object);
            routeCollection.Add(route3.Object);

            var options = new RouteOptions()
            {
                UseBestEffortLinkGeneration = false,
            };

            var virtualPathContext = CreateVirtualPathContext(options: options);

            // Act
            var path = routeCollection.GetVirtualPath(virtualPathContext);

            Assert.Null(path);

            // All of these should be called
            route1.Verify(r => r.GetVirtualPath(It.IsAny<VirtualPathContext>()), Times.Once());
            route2.Verify(r => r.GetVirtualPath(It.IsAny<VirtualPathContext>()), Times.Once());
            route3.Verify(r => r.GetVirtualPath(It.IsAny<VirtualPathContext>()), Times.Once());
        }

        [Fact]
        public void GetVirtualPath_BestEffort_FirstRouteWins_WithNonMatchingRoutes()
        {
            // Arrange
            var route1 = CreateRoute(accept: false, match: false, matchValue: "bad");
            var route2 = CreateRoute(accept: false, match: true, matchValue: "best");
            var route3 = CreateRoute(accept: false, match: true, matchValue: "bad");

            var routeCollection = new RouteCollection();
            routeCollection.Add(route1.Object);
            routeCollection.Add(route2.Object);
            routeCollection.Add(route3.Object);

            var options = new RouteOptions()
            {
                UseBestEffortLinkGeneration = true,
            };

            var virtualPathContext = CreateVirtualPathContext(options: options);

            // Act
            var pathData = routeCollection.GetVirtualPath(virtualPathContext);

            Assert.Equal(new PathString("/best"), pathData.VirtualPath);
            Assert.Same(route2.Object, pathData.Router);
            Assert.Empty(pathData.DataTokens);

            // All of these should be called
            route1.Verify(r => r.GetVirtualPath(It.IsAny<VirtualPathContext>()), Times.Once());
            route2.Verify(r => r.GetVirtualPath(It.IsAny<VirtualPathContext>()), Times.Once());
            route3.Verify(r => r.GetVirtualPath(It.IsAny<VirtualPathContext>()), Times.Once());
        }

        [Fact]
        public void GetVirtualPath_BestEffort_FirstValidatedValuesWins()
        {
            // Arrange
            var route1 = CreateRoute(accept: false, match: true, matchValue: "bad");
            var route2 = CreateRoute(accept: false, match: true, matchValue: "bad");
            var route3 = CreateRoute(accept: true, match: true, matchValue: "best");

            var routeCollection = new RouteCollection();
            routeCollection.Add(route1.Object);
            routeCollection.Add(route2.Object);
            routeCollection.Add(route3.Object);

            var options = new RouteOptions()
            {
                UseBestEffortLinkGeneration = true,
            };

            var virtualPathContext = CreateVirtualPathContext(options: options);

            // Act
            var pathData = routeCollection.GetVirtualPath(virtualPathContext);

            Assert.Equal(new PathString("/best"), pathData.VirtualPath);
            Assert.Same(route3.Object, pathData.Router);
            Assert.Empty(pathData.DataTokens);

            // All of these should be called
            route1.Verify(r => r.GetVirtualPath(It.IsAny<VirtualPathContext>()), Times.Once());
            route2.Verify(r => r.GetVirtualPath(It.IsAny<VirtualPathContext>()), Times.Once());
            route3.Verify(r => r.GetVirtualPath(It.IsAny<VirtualPathContext>()), Times.Once());
        }

        [Fact]
        public void GetVirtualPath_BestEffort_FirstValidatedValuesWins_ShortCircuit()
        {
            // Arrange
            var route1 = CreateRoute(accept: false, match: true, matchValue: "bad");
            var route2 = CreateRoute(accept: true, match: true, matchValue: "best");
            var route3 = CreateRoute(accept: true, match: true, matchValue: "bad");

            var routeCollection = new RouteCollection();
            routeCollection.Add(route1.Object);
            routeCollection.Add(route2.Object);
            routeCollection.Add(route3.Object);

            var options = new RouteOptions()
            {
                UseBestEffortLinkGeneration = true,
            };

            var virtualPathContext = CreateVirtualPathContext(options: options);

            // Act
            var pathData = routeCollection.GetVirtualPath(virtualPathContext);

            Assert.Equal(new PathString("/best"), pathData.VirtualPath);
            Assert.Same(route2.Object, pathData.Router);
            Assert.Empty(pathData.DataTokens);

            route1.Verify(r => r.GetVirtualPath(It.IsAny<VirtualPathContext>()), Times.Once());
            route2.Verify(r => r.GetVirtualPath(It.IsAny<VirtualPathContext>()), Times.Once());
            route3.Verify(r => r.GetVirtualPath(It.IsAny<VirtualPathContext>()), Times.Never());
        }

        [Fact]
        public void GetVirtualPath_BestEffort_FirstValidatedValuesWins_Nested()
        {
            // Arrange
            var route1 = CreateRoute(accept: false, match: true, matchValue: "bad");
            var route2 = CreateRoute(accept: false, match: true, matchValue: "bad");
            var route3 = CreateRoute(accept: true, match: true, matchValue: "best");

            var routeCollection = new RouteCollection();
            routeCollection.Add(route1.Object);

            var innerRouteCollection = new RouteCollection();
            innerRouteCollection.Add(route2.Object);
            innerRouteCollection.Add(route3.Object);
            routeCollection.Add(innerRouteCollection);

            var options = new RouteOptions()
            {
                UseBestEffortLinkGeneration = true,
            };

            var virtualPathContext = CreateVirtualPathContext(options: options);

            // Act
            var pathData = routeCollection.GetVirtualPath(virtualPathContext);

            Assert.Equal(new PathString("/best"), pathData.VirtualPath);
            Assert.Same(route3.Object, pathData.Router);
            Assert.Empty(pathData.DataTokens);

            // All of these should be called
            route1.Verify(r => r.GetVirtualPath(It.IsAny<VirtualPathContext>()), Times.Once());
            route2.Verify(r => r.GetVirtualPath(It.IsAny<VirtualPathContext>()), Times.Once());
            route3.Verify(r => r.GetVirtualPath(It.IsAny<VirtualPathContext>()), Times.Once());
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
        [MemberData("IntegrationTestData")]
        public void GetVirtualPath_Success(
            string template,
            RouteValueDictionary values,
            string expectedUrl,
            bool lowercaseUrls
            )
        {
            // Arrange
            var routeCollection = new RouteCollection();
            var route = CreateTemplateRoute(template);
            routeCollection.Add(route);
            var context = CreateVirtualPathContext(values, options: GetRouteOptions(lowercaseUrls));

            // Act
            var pathData = routeCollection.GetVirtualPath(context);

            // Assert
            Assert.True(context.IsBound);
            Assert.Equal(new PathString(expectedUrl), pathData.VirtualPath);
            Assert.Same(route, pathData.Router);
            Assert.Empty(pathData.DataTokens);
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

        private static async Task<TestSink> SetUp(bool enabled, bool handled)
        {
            // Arrange
            var sink = new TestSink(
                TestSink.EnableWithTypeName<RouteCollection>,
                TestSink.EnableWithTypeName<RouteCollection>);
            var loggerFactory = new TestLoggerFactory(sink, enabled);

            var routes = new RouteCollection();
            var route = CreateRoute(accept: handled);
            routes.Add(route.Object);

            var context = CreateRouteContext("/Cool", loggerFactory);

            // Act
            await routes.RouteAsync(context);

            return sink;
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
            var random = new Random();
            int index = random.Next(0, routeNames.Length - 1);
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
                .Callback<VirtualPathContext>(c => c.IsBound = accept && c.RouteName == name)
                .Returns<VirtualPathContext>(c =>
                    c.RouteName == name ? new VirtualPathData(target.Object, matchValue) : null)
                .Verifiable();

            target
                .SetupGet(e => e.Name)
                .Returns(name);

            target
                .Setup(e => e.RouteAsync(It.IsAny<RouteContext>()))
                .Callback<RouteContext>((c) => c.IsHandled = accept)
                .Returns(Task.FromResult<object>(null))
                .Verifiable();

            return target.Object;
        }

        private static TemplateRoute CreateTemplateRoute(
            string template,
            string routerName = null,
            RouteValueDictionary dataTokens = null)
        {
            var target = new Mock<IRouter>(MockBehavior.Strict);
            target
                .Setup(e => e.GetVirtualPath(It.IsAny<VirtualPathContext>()))
                .Callback<VirtualPathContext>(c => c.IsBound = true)
                .Returns<VirtualPathContext>(rc => null);

            var resolverMock = new Mock<IInlineConstraintResolver>();

            return new TemplateRoute(
                target.Object,
                routerName,
                template,
                defaults: null,
                constraints: null,
                dataTokens: dataTokens,
                inlineConstraintResolver: resolverMock.Object);
        }

        private static VirtualPathContext CreateVirtualPathContext(
            string routeName = null,
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

            var optionsAccessor = new Mock<IOptions<RouteOptions>>(MockBehavior.Strict);
            optionsAccessor.SetupGet(o => o.Options).Returns(options);

            var context = new Mock<HttpContext>(MockBehavior.Strict);
            context.Setup(m => m.RequestServices.GetService(typeof(ILoggerFactory)))
                .Returns(loggerFactory);
            context.Setup(m => m.RequestServices.GetService(typeof(IOptions<RouteOptions>)))
                .Returns(optionsAccessor.Object);
            context.SetupGet(c => c.Request).Returns(request.Object);

            return new VirtualPathContext(context.Object, null, null, routeName);
        }

        private static VirtualPathContext CreateVirtualPathContext(
            RouteValueDictionary values,
            RouteOptions options = null,
            string routeName = null)
        {
            var optionsAccessor = new Mock<IOptions<RouteOptions>>(MockBehavior.Strict);
            optionsAccessor.SetupGet(o => o.Options).Returns(options);

            var context = new Mock<HttpContext>(MockBehavior.Strict);
            context.Setup(m => m.RequestServices.GetService(typeof(IOptions<RouteOptions>)))
                .Returns(optionsAccessor.Object);
            context.Setup(m => m.RequestServices.GetService(typeof(ILoggerFactory)))
                .Returns(NullLoggerFactory.Instance);

            return new VirtualPathContext(
                context.Object,
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
            request.SetupGet(r => r.Path).Returns(new PathString(requestPath));

            var optionsAccessor = new Mock<IOptions<RouteOptions>>(MockBehavior.Strict);
            optionsAccessor.SetupGet(o => o.Options).Returns(options);

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
                .Callback<VirtualPathContext>(c => c.IsBound = accept)
                .Returns(accept || match ? new VirtualPathData(target.Object, matchValue) : null)
                .Verifiable();

            target
                .Setup(e => e.RouteAsync(It.IsAny<RouteContext>()))
                .Callback<RouteContext>((c) => c.IsHandled = accept)
                .Returns(Task.FromResult<object>(null))
                .Verifiable();

            return target;
        }

        private static RouteOptions GetRouteOptions(
            bool lowerCaseUrls = false,
            bool useBestEffortLinkGeneration = true,
            bool appendTrailingSlash = false)
        {
            var routeOptions = new RouteOptions();
            routeOptions.LowercaseUrls = lowerCaseUrls;
            routeOptions.UseBestEffortLinkGeneration = useBestEffortLinkGeneration;
            routeOptions.AppendTrailingSlash = appendTrailingSlash;

            return routeOptions;
        }
    }
}
#endif
