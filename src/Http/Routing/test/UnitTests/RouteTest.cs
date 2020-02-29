// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.AspNetCore.Routing.TestObjects;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.WebEncoders.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Routing
{
    public class RouteTest
    {
        private static readonly RequestDelegate NullHandler = (c) => Task.CompletedTask;
        private static IInlineConstraintResolver _inlineConstraintResolver = GetInlineConstraintResolver();

        [Fact]
        public void CreateTemplate_InlineConstraint_Regex_Malformed()
        {
            // Arrange
            var template = @"{controller}/{action}/ {p1:regex(abc} ";
            var mockTarget = new Mock<IRouter>(MockBehavior.Strict);

            var exception = Assert.Throws<RouteCreationException>(
                () => new Route(
                    mockTarget.Object,
                    template,
                    defaults: null,
                    constraints: null,
                    dataTokens: null,
                    inlineConstraintResolver: _inlineConstraintResolver));

            var expected = "An error occurred while creating the route with name '' and template" +
                $" '{template}'.";
            Assert.Equal(expected, exception.Message);

            Assert.NotNull(exception.InnerException);
            expected = "The constraint entry 'p1' - 'regex(abc' on the route " +
                "'{controller}/{action}/ {p1:regex(abc} ' could not be resolved by the constraint resolver of type " +
                $"'{nameof(DefaultInlineConstraintResolver)}'.";
            Assert.Equal(expected, exception.InnerException.Message);
        }

        [Fact]
        public async Task RouteAsync_MergesExistingRouteData_IfRouteMatches()
        {
            // Arrange
            var template = "{controller}/{action}/{id:int}";

            var context = CreateRouteContext("/Home/Index/5");

            var originalRouteDataValues = context.RouteData.Values;
            originalRouteDataValues.Add("country", "USA");

            var originalDataTokens = context.RouteData.DataTokens;
            originalDataTokens.Add("company", "Contoso");

            IDictionary<string, object> routeValues = null;
            var mockTarget = new Mock<IRouter>(MockBehavior.Strict);
            mockTarget
                .Setup(s => s.RouteAsync(It.IsAny<RouteContext>()))
                .Callback<RouteContext>(ctx =>
                {
                    routeValues = ctx.RouteData.Values;
                    ctx.Handler = NullHandler;
                })
                .Returns(Task.FromResult(true));

            var route = new Route(
                mockTarget.Object,
                template,
                defaults: null,
                constraints: null,
                dataTokens: new RouteValueDictionary(new { today = "Friday" }),
                inlineConstraintResolver: _inlineConstraintResolver);

            // Act
            await route.RouteAsync(context);

            // Assert
            Assert.NotNull(routeValues);

            Assert.True(routeValues.ContainsKey("country"));
            Assert.Equal("USA", routeValues["country"]);
            Assert.True(routeValues.ContainsKey("id"));
            Assert.Equal("5", routeValues["id"]);

            Assert.True(context.RouteData.Values.ContainsKey("country"));
            Assert.Equal("USA", context.RouteData.Values["country"]);
            Assert.True(context.RouteData.Values.ContainsKey("id"));
            Assert.Equal("5", context.RouteData.Values["id"]);
            Assert.Same(originalRouteDataValues, context.RouteData.Values);

            Assert.Equal("Contoso", context.RouteData.DataTokens["company"]);
            Assert.Equal("Friday", context.RouteData.DataTokens["today"]);
            Assert.Same(originalDataTokens, context.RouteData.DataTokens);
        }

        [Fact]
        public async Task RouteAsync_MergesExistingRouteData_PassedToConstraint()
        {
            // Arrange
            var template = "{controller}/{action}/{id:int}";

            var context = CreateRouteContext("/Home/Index/5");
            var originalRouteDataValues = context.RouteData.Values;
            originalRouteDataValues.Add("country", "USA");

            var originalDataTokens = context.RouteData.DataTokens;
            originalDataTokens.Add("company", "Contoso");

            IDictionary<string, object> routeValues = null;
            var mockTarget = new Mock<IRouter>(MockBehavior.Strict);
            mockTarget
                .Setup(s => s.RouteAsync(It.IsAny<RouteContext>()))
                .Callback<RouteContext>(ctx =>
                {
                    routeValues = ctx.RouteData.Values;
                    ctx.Handler = NullHandler;
                })
                .Returns(Task.FromResult(true));

            var constraint = new CapturingConstraint();

            var route = new Route(
                mockTarget.Object,
                template,
                defaults: null,
                constraints: new RouteValueDictionary(new { action = constraint }),
                dataTokens: new RouteValueDictionary(new { today = "Friday" }),
                inlineConstraintResolver: _inlineConstraintResolver);

            // Act
            await route.RouteAsync(context);

            // Assert
            Assert.NotNull(routeValues);

            Assert.True(routeValues.ContainsKey("country"));
            Assert.Equal("USA", routeValues["country"]);
            Assert.True(routeValues.ContainsKey("id"));
            Assert.Equal("5", routeValues["id"]);

            Assert.True(constraint.Values.ContainsKey("country"));
            Assert.Equal("USA", constraint.Values["country"]);
            Assert.True(constraint.Values.ContainsKey("id"));
            Assert.Equal("5", constraint.Values["id"]);

            Assert.True(context.RouteData.Values.ContainsKey("country"));
            Assert.Equal("USA", context.RouteData.Values["country"]);
            Assert.True(context.RouteData.Values.ContainsKey("id"));
            Assert.Equal("5", context.RouteData.Values["id"]);

            Assert.Equal("Contoso", context.RouteData.DataTokens["company"]);
            Assert.Equal("Friday", context.RouteData.DataTokens["today"]);
        }

        [Fact]
        public async Task RouteAsync_InlineConstraint_OptionalParameter()
        {
            // Arrange
            var template = "{controller}/{action}/{id:int?}";

            var context = CreateRouteContext("/Home/Index/5");

            IDictionary<string, object> routeValues = null;
            var mockTarget = new Mock<IRouter>(MockBehavior.Strict);
            mockTarget
                .Setup(s => s.RouteAsync(It.IsAny<RouteContext>()))
                .Callback<RouteContext>(ctx =>
                {
                    routeValues = ctx.RouteData.Values;
                    ctx.Handler = NullHandler;
                })
                .Returns(Task.FromResult(true));

            var route = new Route(
                mockTarget.Object,
                template,
                defaults: null,
                constraints: null,
                dataTokens: null,
                inlineConstraintResolver: _inlineConstraintResolver);

            Assert.NotEmpty(route.Constraints);
            Assert.IsType<OptionalRouteConstraint>(route.Constraints["id"]);

            // Act
            await route.RouteAsync(context);

            // Assert
            Assert.NotNull(context.Handler);
            Assert.True(routeValues.ContainsKey("id"));
            Assert.Equal("5", routeValues["id"]);

            Assert.True(context.RouteData.Values.ContainsKey("id"));
            Assert.Equal("5", context.RouteData.Values["id"]);
        }

        [Fact]
        public async Task RouteAsync_InlineConstraint_Regex()
        {
            // Arrange
            var template = @"{controller}/{action}/{ssn:regex(^\d{{3}}-\d{{3}}-\d{{4}}$)}";

            var context = CreateRouteContext("/Home/Index/123-456-7890");

            IDictionary<string, object> routeValues = null;
            var mockTarget = new Mock<IRouter>(MockBehavior.Strict);
            mockTarget
                .Setup(s => s.RouteAsync(It.IsAny<RouteContext>()))
                .Callback<RouteContext>(ctx =>
                {
                    routeValues = ctx.RouteData.Values;
                    ctx.Handler = NullHandler;
                })
                .Returns(Task.FromResult(true));

            var route = new Route(
                mockTarget.Object,
                template,
                defaults: null,
                constraints: null,
                dataTokens: null,
                inlineConstraintResolver: _inlineConstraintResolver);

            Assert.NotEmpty(route.Constraints);
            Assert.IsType<RegexInlineRouteConstraint>(route.Constraints["ssn"]);

            // Act
            await route.RouteAsync(context);

            // Assert
            Assert.NotNull(context.Handler);
            Assert.True(routeValues.ContainsKey("ssn"));
            Assert.Equal("123-456-7890", routeValues["ssn"]);

            Assert.True(context.RouteData.Values.ContainsKey("ssn"));
            Assert.Equal("123-456-7890", context.RouteData.Values["ssn"]);
        }

        [Fact]
        public async Task RouteAsync_InlineConstraint_OptionalParameter_NotPresent()
        {
            // Arrange
            var template = "{controller}/{action}/{id:int?}";

            var context = CreateRouteContext("/Home/Index");

            IDictionary<string, object> routeValues = null;
            var mockTarget = new Mock<IRouter>(MockBehavior.Strict);
            mockTarget
                .Setup(s => s.RouteAsync(It.IsAny<RouteContext>()))
                .Callback<RouteContext>(ctx =>
                {
                    routeValues = ctx.RouteData.Values;
                    ctx.Handler = NullHandler;
                })
                .Returns(Task.FromResult(true));

            var route = new Route(
                mockTarget.Object,
                template,
                defaults: null,
                constraints: null,
                dataTokens: null,
                inlineConstraintResolver: _inlineConstraintResolver);

            Assert.NotEmpty(route.Constraints);
            Assert.IsType<OptionalRouteConstraint>(route.Constraints["id"]);

            // Act
            await route.RouteAsync(context);

            // Assert
            Assert.NotNull(context.Handler);
            Assert.NotNull(routeValues);
            Assert.False(routeValues.ContainsKey("id"));
            Assert.False(context.RouteData.Values.ContainsKey("id"));
        }

        [Fact]
        public async Task RouteAsync_InlineConstraint_OptionalParameter_WithInConstructorConstraint()
        {
            // Arrange
            var template = "{controller}/{action}/{id:int?}";

            var context = CreateRouteContext("/Home/Index/5");

            IDictionary<string, object> routeValues = null;
            var mockTarget = new Mock<IRouter>(MockBehavior.Strict);
            mockTarget
                .Setup(s => s.RouteAsync(It.IsAny<RouteContext>()))
                .Callback<RouteContext>(ctx =>
                {
                    routeValues = ctx.RouteData.Values;
                    ctx.Handler = NullHandler;
                })
                .Returns(Task.FromResult(true));

            var constraints = new Dictionary<string, object>();
            constraints.Add("id", new RangeRouteConstraint(1, 20));

            var route = new Route(
                mockTarget.Object,
                template,
                defaults: null,
                constraints: constraints,
                dataTokens: null,
                inlineConstraintResolver: _inlineConstraintResolver);

            Assert.NotEmpty(route.Constraints);
            Assert.IsType<OptionalRouteConstraint>(route.Constraints["id"]);
            var innerConstraint = ((OptionalRouteConstraint)route.Constraints["id"]).InnerConstraint;
            Assert.IsType<CompositeRouteConstraint>(innerConstraint);
            var compositeConstraint = (CompositeRouteConstraint)innerConstraint;
            Assert.Equal(2, compositeConstraint.Constraints.Count<IRouteConstraint>());

            Assert.Single(compositeConstraint.Constraints, c => c is IntRouteConstraint);
            Assert.Single(compositeConstraint.Constraints, c => c is RangeRouteConstraint);

            // Act
            await route.RouteAsync(context);

            // Assert
            Assert.NotNull(context.Handler);
            Assert.True(routeValues.ContainsKey("id"));
            Assert.Equal("5", routeValues["id"]);

            Assert.True(context.RouteData.Values.ContainsKey("id"));
            Assert.Equal("5", context.RouteData.Values["id"]);
        }

        [Fact]
        public async Task RouteAsync_InlineConstraint_OptionalParameter_ConstraintFails()
        {
            // Arrange
            var template = "{controller}/{action}/{id:range(1,20)?}";

            var context = CreateRouteContext("/Home/Index/100");

            IDictionary<string, object> routeValues = null;
            var mockTarget = new Mock<IRouter>(MockBehavior.Strict);
            mockTarget
                .Setup(s => s.RouteAsync(It.IsAny<RouteContext>()))
                .Callback<RouteContext>(ctx =>
                {
                    routeValues = ctx.RouteData.Values;
                    ctx.Handler = NullHandler;
                })
                .Returns(Task.FromResult(true));

            var route = new Route(
                mockTarget.Object,
                template,
                defaults: null,
                constraints: null,
                dataTokens: null,
                inlineConstraintResolver: _inlineConstraintResolver);

            Assert.NotEmpty(route.Constraints);
            Assert.IsType<OptionalRouteConstraint>(route.Constraints["id"]);

            // Act
            await route.RouteAsync(context);

            // Assert
            Assert.Null(context.Handler);
        }

        // PathString in HttpAbstractions guarantees a leading slash - so no value in testing other cases.
        [Fact]
        public async Task Match_Success_LeadingSlash()
        {
            // Arrange
            var route = CreateRoute("{controller}/{action}");
            var context = CreateRouteContext("/Home/Index");

            // Act
            await route.RouteAsync(context);

            // Assert
            Assert.NotNull(context.Handler);
            Assert.Equal(2, context.RouteData.Values.Count);
            Assert.Equal("Home", context.RouteData.Values["controller"]);
            Assert.Equal("Index", context.RouteData.Values["action"]);
        }

        [Fact]
        public async Task Match_Success_RootUrl()
        {
            // Arrange
            var route = CreateRoute("");
            var context = CreateRouteContext("/");

            // Act
            await route.RouteAsync(context);

            // Assert
            Assert.NotNull(context.Handler);
            Assert.Empty(context.RouteData.Values);
        }

        [Fact]
        public async Task Match_Success_Defaults()
        {
            // Arrange
            var route = CreateRoute("{controller}/{action}", new { action = "Index" });
            var context = CreateRouteContext("/Home");

            // Act
            await route.RouteAsync(context);

            // Assert
            Assert.NotNull(context.Handler);
            Assert.Equal(2, context.RouteData.Values.Count);
            Assert.Equal("Home", context.RouteData.Values["controller"]);
            Assert.Equal("Index", context.RouteData.Values["action"]);
        }

        [Fact]
        public async Task Match_Success_CopiesDataTokens()
        {
            // Arrange
            var route = CreateRoute(
                "{controller}/{action}",
                defaults: new { action = "Index" },
                dataTokens: new { culture = "en-CA" });

            var context = CreateRouteContext("/Home");

            // Act
            await route.RouteAsync(context);
            Assert.NotNull(context.Handler);

            // This should not affect the route - RouteData.DataTokens is a copy
            context.RouteData.DataTokens.Add("company", "contoso");

            // Assert
            Assert.Single(route.DataTokens);
            Assert.Single(route.DataTokens, kvp => kvp.Key == "culture" && ((string)kvp.Value) == "en-CA");
        }

        [Fact]
        public async Task Match_Fails()
        {
            // Arrange
            var route = CreateRoute("{controller}/{action}");
            var context = CreateRouteContext("/Home");

            // Act
            await route.RouteAsync(context);

            // Assert
            Assert.Null(context.Handler);
        }

        [Fact]
        public async Task Match_RejectedByHandler()
        {
            // Arrange
            var route = CreateRoute("{controller}", handleRequest: false);
            var context = CreateRouteContext("/Home");

            // Act
            await route.RouteAsync(context);

            // Assert
            Assert.Null(context.Handler);

            var value = Assert.Single(context.RouteData.Values);
            Assert.Equal("controller", value.Key);
            Assert.Equal("Home", Assert.IsType<string>(value.Value));
        }

        [Fact]
        public async Task Match_SetsRouters()
        {
            // Arrange
            var target = CreateTarget(handleRequest: true);
            var route = CreateRoute(target, "{controller}");
            var context = CreateRouteContext("/Home");

            // Act
            await route.RouteAsync(context);

            // Assert
            Assert.NotNull(context.Handler);
            Assert.Equal(1, context.RouteData.Routers.Count);
            Assert.Same(target, context.RouteData.Routers[0]);
        }

        [Fact]
        public async Task Match_RouteValuesDoesntThrowOnKeyNotFound()
        {
            // Arrange
            var route = CreateRoute("{controller}/{action}");
            var context = CreateRouteContext("/Home/Index");

            // Act
            await route.RouteAsync(context);

            // Assert
            Assert.Null(context.RouteData.Values["1controller"]);
        }

        [Fact]
        public async Task Match_Success_OptionalParameter_ValueProvided()
        {
            // Arrange
            var route = CreateRoute("{controller}/{action}.{format?}", new { action = "Index" });
            var context = CreateRouteContext("/Home/Create.xml");

            // Act
            await route.RouteAsync(context);

            // Assert
            Assert.NotNull(context.Handler);
            Assert.Equal(3, context.RouteData.Values.Count);
            Assert.Equal("Home", context.RouteData.Values["controller"]);
            Assert.Equal("Create", context.RouteData.Values["action"]);
            Assert.Equal("xml", context.RouteData.Values["format"]);
        }

        [Fact]
        public async Task Match_Success_OptionalParameter_ValueNotProvided()
        {
            // Arrange
            var route = CreateRoute("{controller}/{action}.{format?}", new { action = "Index" });
            var context = CreateRouteContext("/Home/Create");

            // Act
            await route.RouteAsync(context);

            // Assert
            Assert.NotNull(context.Handler);
            Assert.Equal(2, context.RouteData.Values.Count);
            Assert.Equal("Home", context.RouteData.Values["controller"]);
            Assert.Equal("Create", context.RouteData.Values["action"]);
        }

        [Fact]
        public async Task Match_Success_OptionalParameter_DefaultValue()
        {
            // Arrange
            var route = CreateRoute("{controller}/{action}.{format?}", new { action = "Index", format = "xml" });
            var context = CreateRouteContext("/Home/Create");

            // Act
            await route.RouteAsync(context);

            // Assert
            Assert.NotNull(context.Handler);
            Assert.Equal(3, context.RouteData.Values.Count);
            Assert.Equal("Home", context.RouteData.Values["controller"]);
            Assert.Equal("Create", context.RouteData.Values["action"]);
            Assert.Equal("xml", context.RouteData.Values["format"]);
        }

        [Fact]
        public async Task Match_Success_OptionalParameter_EndsWithDot()
        {
            // Arrange
            var route = CreateRoute("{controller}/{action}.{format?}", new { action = "Index" });
            var context = CreateRouteContext("/Home/Create.");

            // Act
            await route.RouteAsync(context);

            // Assert
            Assert.Null(context.Handler);
        }

        private static RouteContext CreateRouteContext(string requestPath, ILoggerFactory factory = null)
        {
            if (factory == null)
            {
                factory = NullLoggerFactory.Instance;
            }

            var request = new Mock<HttpRequest>(MockBehavior.Strict);
            request.SetupGet(r => r.Path).Returns(requestPath);

            var context = new Mock<HttpContext>(MockBehavior.Strict);
            context.Setup(m => m.RequestServices.GetService(typeof(ILoggerFactory)))
                .Returns(factory);
            context.SetupGet(c => c.Request).Returns(request.Object);

            return new RouteContext(context.Object);
        }

        [Fact]
        public void GetVirtualPath_Success()
        {
            // Arrange
            var route = CreateRoute("{controller}");
            var context = CreateVirtualPathContext(new { controller = "Home" });

            // Act
            var pathData = route.GetVirtualPath(context);

            // Assert
            Assert.Equal("/Home", pathData.VirtualPath);
            Assert.Same(route, pathData.Router);
            Assert.Empty(pathData.DataTokens);
        }

        [Fact]
        public void GetVirtualPath_Fail()
        {
            // Arrange
            var route = CreateRoute("{controller}/{action}");
            var context = CreateVirtualPathContext(new { controller = "Home" });

            // Act
            var path = route.GetVirtualPath(context);

            // Assert
            Assert.Null(path);
        }

        [Fact]
        public void GetVirtualPath_EncodesValues()
        {
            // Arrange
            var route = CreateRoute("{controller}/{action}");
            var context = CreateVirtualPathContext(
                new { name = "name with %special #characters" },
                new { controller = "Home", action = "Index" });

            // Act
            var pathData = route.GetVirtualPath(context);

            // Assert
            Assert.Equal("/Home/Index?name=name%20with%20%25special%20%23characters", pathData.VirtualPath);
            Assert.Same(route, pathData.Router);
            Assert.Empty(pathData.DataTokens);
        }

        [Fact]
        public void GetVirtualPath_AlwaysUsesDefaultUrlEncoder()
        {
            // Arrange
            var nameRouteValue = "name with %special #characters Jörn";
            var expected = "/Home/Index?name=" + UrlEncoder.Default.Encode(nameRouteValue);
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            services.AddOptions();
            services.AddRouting();
            // This test encoder should not be used by Routing and should always use the default one.
            services.AddSingleton<UrlEncoder>(new UrlTestEncoder());
            var httpContext = new DefaultHttpContext
            {
                RequestServices = services.BuildServiceProvider(),
            };

            var context = new VirtualPathContext(
                httpContext,
                values: new RouteValueDictionary(new { name = nameRouteValue }),
                ambientValues: new RouteValueDictionary(new { controller = "Home", action = "Index" }));

            var route = CreateRoute("{controller}/{action}");

            // Act
            var pathData = route.GetVirtualPath(context);

            // Assert
            Assert.Equal(expected, pathData.VirtualPath);
            Assert.Same(route, pathData.Router);
            Assert.Empty(pathData.DataTokens);
        }

        [Fact]
        public void GetVirtualPath_ForListOfStrings()
        {
            // Arrange
            var route = CreateRoute("{controller}/{action}");
            var context = CreateVirtualPathContext(
                new { color = new List<string> { "red", "green", "blue" } },
                new { controller = "Home", action = "Index" });

            // Act
            var pathData = route.GetVirtualPath(context);

            // Assert
            Assert.Equal("/Home/Index?color=red&color=green&color=blue", pathData.VirtualPath);
            Assert.Same(route, pathData.Router);
            Assert.Empty(pathData.DataTokens);
        }

        [Fact]
        public void GetVirtualPath_ForListOfInts()
        {
            // Arrange
            var route = CreateRoute("{controller}/{action}");
            var context = CreateVirtualPathContext(
                new { items = new List<int> { 10, 20, 30 } },
                new { controller = "Home", action = "Index" });

            // Act
            var pathData = route.GetVirtualPath(context);

            // Assert
            Assert.Equal("/Home/Index?items=10&items=20&items=30", pathData.VirtualPath);
            Assert.Same(route, pathData.Router);
            Assert.Empty(pathData.DataTokens);
        }

        [Fact]
        public void GetVirtualPath_ForList_Empty()
        {
            // Arrange
            var route = CreateRoute("{controller}/{action}");
            var context = CreateVirtualPathContext(
                new { color = new List<string> { } },
                new { controller = "Home", action = "Index" });

            // Act
            var pathData = route.GetVirtualPath(context);

            // Assert
            Assert.Equal("/Home/Index", pathData.VirtualPath);
            Assert.Same(route, pathData.Router);
            Assert.Empty(pathData.DataTokens);
        }

        [Fact]
        public void GetVirtualPath_ForList_StringWorkaround()
        {
            // Arrange
            var route = CreateRoute("{controller}/{action}");
            var context = CreateVirtualPathContext(
                new { page = 1, color = new List<string> { "red", "green", "blue" }, message = "textfortest" },
                new { controller = "Home", action = "Index" });

            // Act
            var pathData = route.GetVirtualPath(context);

            // Assert
            Assert.Equal("/Home/Index?page=1&color=red&color=green&color=blue&message=textfortest", pathData.VirtualPath);
            Assert.Same(route, pathData.Router);
            Assert.Empty(pathData.DataTokens);
        }

        [Theory]
        [MemberData(nameof(DataTokensTestData))]
        public void GetVirtualPath_ReturnsDataTokens_WhenTargetReturnsVirtualPathData(
            RouteValueDictionary dataTokens)
        {
            // Arrange
            var path = "/TestPath";

            var target = new Mock<IRouter>(MockBehavior.Strict);
            target
                .Setup(r => r.GetVirtualPath(It.IsAny<VirtualPathContext>()))
                .Returns(() => new VirtualPathData(target.Object, path, dataTokens));

            var routeDataTokens =
                new RouteValueDictionary() { { "ThisShouldBeIgnored", "" } };

            var route = CreateRoute(
                target.Object,
                "{controller}",
                defaults: null,
                dataTokens: routeDataTokens);
            var context = CreateVirtualPathContext(new { controller = path });

            var expectedDataTokens = dataTokens ?? new RouteValueDictionary();

            // Act
            var pathData = route.GetVirtualPath(context);

            // Assert
            Assert.NotNull(pathData);
            Assert.Same(target.Object, pathData.Router);
            Assert.Equal(path, pathData.VirtualPath);
            Assert.NotNull(pathData.DataTokens);

            Assert.DoesNotContain(routeDataTokens.First().Key, pathData.DataTokens.Keys);

            Assert.Equal(expectedDataTokens.Count, pathData.DataTokens.Count);
            foreach (var dataToken in expectedDataTokens)
            {
                Assert.True(pathData.DataTokens.ContainsKey(dataToken.Key));
                Assert.Equal(dataToken.Value, pathData.DataTokens[dataToken.Key]);
            }
        }

        [Theory]
        [MemberData(nameof(DataTokensTestData))]
        public void GetVirtualPath_ReturnsDataTokens_WhenTargetReturnsNullVirtualPathData(
            RouteValueDictionary dataTokens)
        {
            // Arrange
            var path = "/TestPath";

            var target = new Mock<IRouter>(MockBehavior.Strict);
            target
                .Setup(r => r.GetVirtualPath(It.IsAny<VirtualPathContext>()))
                .Returns(() => null);

            var route = CreateRoute(
                target.Object,
                "{controller}",
                defaults: null,
                dataTokens: dataTokens);
            var context = CreateVirtualPathContext(new { controller = path });

            var expectedDataTokens = dataTokens ?? new RouteValueDictionary();

            // Act
            var pathData = route.GetVirtualPath(context);

            // Assert
            Assert.NotNull(pathData);
            Assert.Same(route, pathData.Router);
            Assert.Equal(path, pathData.VirtualPath);
            Assert.NotNull(pathData.DataTokens);

            Assert.Equal(expectedDataTokens.Count, pathData.DataTokens.Count);
            foreach (var dataToken in expectedDataTokens)
            {
                Assert.True(pathData.DataTokens.ContainsKey(dataToken.Key));
                Assert.Equal(dataToken.Value, pathData.DataTokens[dataToken.Key]);
            }
        }

        [Fact]
        public void GetVirtualPath_ValuesRejectedByHandler_StillGeneratesPath()
        {
            // Arrange
            var route = CreateRoute("{controller}", handleRequest: false);
            var context = CreateVirtualPathContext(new { controller = "Home" });

            // Act
            var pathData = route.GetVirtualPath(context);

            // Assert
            Assert.Equal("/Home", pathData.VirtualPath);
            Assert.Same(route, pathData.Router);
            Assert.Empty(pathData.DataTokens);
        }

        [Fact]
        public void GetVirtualPath_Success_AmbientValues()
        {
            // Arrange
            var route = CreateRoute("{controller}/{action}");
            var context = CreateVirtualPathContext(new { action = "Index" }, new { controller = "Home" });

            // Act
            var pathData = route.GetVirtualPath(context);

            // Assert
            Assert.Equal("/Home/Index", pathData.VirtualPath);
            Assert.Same(route, pathData.Router);
            Assert.Empty(pathData.DataTokens);
        }

        [Fact]
        public void RouteGenerationRejectsConstraints()
        {
            // Arrange
            var context = CreateVirtualPathContext(new { p1 = "abcd" });

            var route = CreateRoute(
                "{p1}/{p2}",
                new { p2 = "catchall" },
                true,
                new RouteValueDictionary(new { p2 = "\\d{4}" }));

            // Act
            var virtualPath = route.GetVirtualPath(context);

            // Assert
            Assert.Null(virtualPath);
        }

        [Fact]
        public void RouteGenerationAcceptsConstraints()
        {
            // Arrange
            var context = CreateVirtualPathContext(new { p1 = "hello", p2 = "1234" });

            var route = CreateRoute(
                "{p1}/{p2}",
                new { p2 = "catchall" },
                true,
                new RouteValueDictionary(new { p2 = "\\d{4}" }));

            // Act
            var pathData = route.GetVirtualPath(context);

            // Assert
            Assert.NotNull(pathData);
            Assert.Equal("/hello/1234", pathData.VirtualPath);
            Assert.Same(route, pathData.Router);
            Assert.Empty(pathData.DataTokens);
        }

        [Fact]
        public void RouteWithCatchAllRejectsConstraints()
        {
            // Arrange
            var context = CreateVirtualPathContext(new { p1 = "abcd" });

            var route = CreateRoute(
                "{p1}/{*p2}",
                new { p2 = "catchall" },
                true,
                new RouteValueDictionary(new { p2 = "\\d{4}" }));

            // Act
            var virtualPath = route.GetVirtualPath(context);

            // Assert
            Assert.Null(virtualPath);
        }

        [Fact]
        public void RouteWithCatchAllAcceptsConstraints()
        {
            // Arrange
            var context = CreateVirtualPathContext(new { p1 = "hello", p2 = "1234" });

            var route = CreateRoute(
                "{p1}/{*p2}",
                new { p2 = "catchall" },
                true,
                new RouteValueDictionary(new { p2 = "\\d{4}" }));

            // Act
            var pathData = route.GetVirtualPath(context);

            // Assert
            Assert.NotNull(pathData);
            Assert.Equal("/hello/1234", pathData.VirtualPath);
            Assert.Same(route, pathData.Router);
            Assert.Empty(pathData.DataTokens);
        }

        [Fact]
        public void GetVirtualPathWithNonParameterConstraintReturnsUrlWithoutQueryString()
        {
            // Arrange
            var context = CreateVirtualPathContext(new { p1 = "hello", p2 = "1234" });

            var target = new Mock<IRouteConstraint>();
            target
                .Setup(
                    e => e.Match(
                        It.IsAny<HttpContext>(),
                        It.IsAny<IRouter>(),
                        It.IsAny<string>(),
                        It.IsAny<RouteValueDictionary>(),
                        It.IsAny<RouteDirection>()))
                .Returns(true)
                .Verifiable();

            var route = CreateRoute(
                "{p1}/{p2}",
                new { p2 = "catchall" },
                true,
                new RouteValueDictionary(new { p2 = target.Object }));

            // Act
            var pathData = route.GetVirtualPath(context);

            // Assert
            Assert.NotNull(pathData);
            Assert.Equal("/hello/1234", pathData.VirtualPath);
            Assert.Same(route, pathData.Router);
            Assert.Empty(pathData.DataTokens);

            target.VerifyAll();
        }

        // Any ambient values from the current request should be visible to constraint, even
        // if they have nothing to do with the route generating a link
        [Fact]
        public void GetVirtualPath_ConstraintsSeeAmbientValues()
        {
            // Arrange
            var constraint = new CapturingConstraint();
            var route = CreateRoute(
                template: "slug/{controller}/{action}",
                defaults: null,
                handleRequest: true,
                constraints: new { c = constraint });

            var context = CreateVirtualPathContext(
                values: new { action = "Store" },
                ambientValues: new { Controller = "Home", action = "Blog", extra = "42" });

            var expectedValues = new RouteValueDictionary(
                new { controller = "Home", action = "Store", extra = "42" });

            // Act
            var pathData = route.GetVirtualPath(context);

            // Assert
            Assert.Equal("/slug/Home/Store", pathData.VirtualPath);
            Assert.Same(route, pathData.Router);
            Assert.Empty(pathData.DataTokens);

            Assert.Equal(expectedValues, constraint.Values);
        }

        // Non-parameter default values from the routing generating a link are not in the 'values'
        // collection when constraints are processed.
        [Fact]
        public void GetVirtualPath_ConstraintsDontSeeDefaults_WhenTheyArentParameters()
        {
            // Arrange
            var constraint = new CapturingConstraint();
            var route = CreateRoute(
                template: "slug/{controller}/{action}",
                defaults: new { otherthing = "17" },
                handleRequest: true,
                constraints: new { c = constraint });

            var context = CreateVirtualPathContext(
                values: new { action = "Store" },
                ambientValues: new { Controller = "Home", action = "Blog" });

            var expectedValues = new RouteValueDictionary(
                new { controller = "Home", action = "Store" });

            // Act
            var pathData = route.GetVirtualPath(context);

            // Assert
            Assert.Equal("/slug/Home/Store", pathData.VirtualPath);
            Assert.Same(route, pathData.Router);
            Assert.Empty(pathData.DataTokens);

            Assert.Equal(expectedValues, constraint.Values);
        }

        // Default values are visible to the constraint when they are used to fill a parameter.
        [Fact]
        public void GetVirtualPath_ConstraintsSeesDefault_WhenThereItsAParamter()
        {
            // Arrange
            var constraint = new CapturingConstraint();
            var route = CreateRoute(
                template: "slug/{controller}/{action}",
                defaults: new { action = "Index" },
                handleRequest: true,
                constraints: new { c = constraint });

            var context = CreateVirtualPathContext(
                values: new { controller = "Shopping" },
                ambientValues: new { Controller = "Home", action = "Blog" });

            var expectedValues = new RouteValueDictionary(
                new { controller = "Shopping", action = "Index" });

            // Act
            var pathData = route.GetVirtualPath(context);

            // Assert
            Assert.Equal("/slug/Shopping", pathData.VirtualPath);
            Assert.Same(route, pathData.Router);
            Assert.Empty(pathData.DataTokens);

            Assert.Equal(expectedValues, constraint.Values);
        }

        // Default values from the routing generating a link are in the 'values' collection when
        // constraints are processed - IFF they are specified as values or ambient values.
        [Fact]
        public void GetVirtualPath_ConstraintsSeeDefaults_IfTheyAreSpecifiedOrAmbient()
        {
            // Arrange
            var constraint = new CapturingConstraint();
            var route = CreateRoute(
                template: "slug/{controller}/{action}",
                defaults: new { otherthing = "17", thirdthing = "13" },
                handleRequest: true,
                constraints: new { c = constraint });

            var context = CreateVirtualPathContext(
                values: new { action = "Store", thirdthing = "13" },
                ambientValues: new { Controller = "Home", action = "Blog", otherthing = "17" });

            var expectedValues = new RouteValueDictionary(
                new { controller = "Home", action = "Store", otherthing = "17", thirdthing = "13" });

            // Act
            var pathData = route.GetVirtualPath(context);

            // Assert
            Assert.Equal("/slug/Home/Store", pathData.VirtualPath);
            Assert.Same(route, pathData.Router);
            Assert.Empty(pathData.DataTokens);

            Assert.Equal(expectedValues.OrderBy(kvp => kvp.Key), constraint.Values.OrderBy(kvp => kvp.Key));
        }

        [Fact]
        public void GetVirtualPath_InlineConstraints_Success()
        {
            // Arrange
            var route = CreateRoute("{controller}/{action}/{id:int}");
            var context = CreateVirtualPathContext(
                values: new { action = "Index", controller = "Home", id = 4 });

            // Act
            var pathData = route.GetVirtualPath(context);

            // Assert
            Assert.Equal("/Home/Index/4", pathData.VirtualPath);
            Assert.Same(route, pathData.Router);
            Assert.Empty(pathData.DataTokens);
        }

        [Fact]
        public void GetVirtualPath_InlineConstraints_NonMatchingvalue()
        {
            // Arrange
            var route = CreateRoute("{controller}/{action}/{id:int}");
            var context = CreateVirtualPathContext(
                values: new { action = "Index", controller = "Home", id = "asf" });

            // Act
            var path = route.GetVirtualPath(context);

            // Assert
            Assert.Null(path);
        }

        [Fact]
        public void GetVirtualPath_InlineConstraints_OptionalParameter_ValuePresent()
        {
            // Arrange
            var route = CreateRoute("{controller}/{action}/{id:int?}");
            var context = CreateVirtualPathContext(
                values: new { action = "Index", controller = "Home", id = 98 });

            // Act
            var pathData = route.GetVirtualPath(context);

            // Assert
            Assert.Equal("/Home/Index/98", pathData.VirtualPath);
            Assert.Same(route, pathData.Router);
            Assert.Empty(pathData.DataTokens);
        }

        [Fact]
        public void GetVirtualPath_InlineConstraints_OptionalParameter_ValueNotPresent()
        {
            // Arrange
            var route = CreateRoute("{controller}/{action}/{id:int?}");
            var context = CreateVirtualPathContext(
                values: new { action = "Index", controller = "Home" });

            // Act
            var pathData = route.GetVirtualPath(context);

            // Assert
            Assert.Equal("/Home/Index", pathData.VirtualPath);
            Assert.Same(route, pathData.Router);
            Assert.Empty(pathData.DataTokens);
        }

        [Fact]
        public void GetVirtualPath_InlineConstraints_OptionalParameter_ValuePresent_ConstraintFails()
        {
            // Arrange
            var route = CreateRoute("{controller}/{action}/{id:int?}");
            var context = CreateVirtualPathContext(
                values: new { action = "Index", controller = "Home", id = "sdfd" });

            // Act
            var path = route.GetVirtualPath(context);

            // Assert
            Assert.Null(path);
        }

        [Fact]
        public void GetVirtualPath_InlineConstraints_CompositeInlineConstraint()
        {
            // Arrange
            var route = CreateRoute("{controller}/{action}/{id:int:range(1,20)}");
            var context = CreateVirtualPathContext(
                values: new { action = "Index", controller = "Home", id = 14 });

            // Act
            var pathData = route.GetVirtualPath(context);

            // Assert
            Assert.Equal("/Home/Index/14", pathData.VirtualPath);
            Assert.Same(route, pathData.Router);
            Assert.Empty(pathData.DataTokens);
        }

        [Fact]
        public void GetVirtualPath_InlineConstraints_CompositeConstraint_FromConstructor()
        {
            // Arrange
            var constraint = new MaxLengthRouteConstraint(20);
            var route = CreateRoute(
                template: "{controller}/{action}/{name:alpha}",
                defaults: null,
                handleRequest: true,
                constraints: new { name = constraint });

            var context = CreateVirtualPathContext(
                values: new { action = "Index", controller = "Home", name = "products" });

            // Act
            var pathData = route.GetVirtualPath(context);

            // Assert
            Assert.Equal("/Home/Index/products", pathData.VirtualPath);
            Assert.Same(route, pathData.Router);
            Assert.Empty(pathData.DataTokens);
        }

        [Fact]
        public void GetVirtualPath_OptionalParameter_ParameterPresentInValues()
        {
            // Arrange
            var route = CreateRoute(
                template: "{controller}/{action}/{name}.{format?}",
                defaults: null,
                handleRequest: true,
                constraints: null);

            var context = CreateVirtualPathContext(
                values: new { action = "Index", controller = "Home", name = "products", format = "xml" });

            // Act
            var pathData = route.GetVirtualPath(context);

            // Assert
            Assert.Equal("/Home/Index/products.xml", pathData.VirtualPath);
            Assert.Same(route, pathData.Router);
            Assert.Empty(pathData.DataTokens);
        }

        [Fact]
        public void GetVirtualPath_OptionalParameter_ParameterNotPresentInValues()
        {
            // Arrange
            var route = CreateRoute(
                template: "{controller}/{action}/{name}.{format?}",
                defaults: null,
                handleRequest: true,
                constraints: null);

            var context = CreateVirtualPathContext(
                values: new { action = "Index", controller = "Home", name = "products" });

            // Act
            var pathData = route.GetVirtualPath(context);

            // Assert
            Assert.Equal("/Home/Index/products", pathData.VirtualPath);
            Assert.Same(route, pathData.Router);
            Assert.Empty(pathData.DataTokens);
        }

        [Fact]
        public void GetVirtualPath_OptionalParameter_ParameterPresentInValuesAndDefaults()
        {
            // Arrange
            var route = CreateRoute(
                template: "{controller}/{action}/{name}.{format?}",
                defaults: new { format = "json" },
                handleRequest: true,
                constraints: null);

            var context = CreateVirtualPathContext(
                values: new { action = "Index", controller = "Home", name = "products", format = "xml" });

            // Act
            var pathData = route.GetVirtualPath(context);

            // Assert
            Assert.Equal("/Home/Index/products.xml", pathData.VirtualPath);
            Assert.Same(route, pathData.Router);
            Assert.Empty(pathData.DataTokens);
        }

        [Fact]
        public void GetVirtualPath_OptionalParameter_ParameterNotPresentInValues_PresentInDefaults()
        {
            // Arrange
            var route = CreateRoute(
                template: "{controller}/{action}/{name}.{format?}",
                defaults: new { format = "json" },
                handleRequest: true,
                constraints: null);

            var context = CreateVirtualPathContext(
                values: new { action = "Index", controller = "Home", name = "products" });

            // Act
            var pathData = route.GetVirtualPath(context);

            // Assert
            Assert.Equal("/Home/Index/products", pathData.VirtualPath);
            Assert.Same(route, pathData.Router);
            Assert.Empty(pathData.DataTokens);
        }

        [Fact]
        public void GetVirtualPath_OptionalParameter_ParameterNotPresentInTemplate_PresentInValues()
        {
            // Arrange
            var route = CreateRoute(
                template: "{controller}/{action}/{name}",
                defaults: null,
                handleRequest: true,
                constraints: null);

            var context = CreateVirtualPathContext(
                values: new { action = "Index", controller = "Home", name = "products", format = "json" });

            // Act
            var pathData = route.GetVirtualPath(context);

            // Assert
            Assert.Equal("/Home/Index/products?format=json", pathData.VirtualPath);
            Assert.Same(route, pathData.Router);
            Assert.Empty(pathData.DataTokens);
        }

        [Fact]
        public void GetVirtualPath_OptionalParameter_FollowedByDotAfterSlash_ParameterPresent()
        {
            // Arrange
            var route = CreateRoute(
                template: "{controller}/{action}/.{name?}",
                defaults: null,
                handleRequest: true,
                constraints: null);

            var context = CreateVirtualPathContext(
                values: new { action = "Index", controller = "Home", name = "products" });

            // Act
            var pathData = route.GetVirtualPath(context);

            // Assert
            Assert.Equal("/Home/Index/.products", pathData.VirtualPath);
            Assert.Same(route, pathData.Router);
            Assert.Empty(pathData.DataTokens);
        }

        [Fact]
        public void GetVirtualPath_OptionalParameter_FollowedByDotAfterSlash_ParameterNotPresent()
        {
            // Arrange
            var route = CreateRoute(
                template: "{controller}/{action}/.{name?}",
                defaults: null,
                handleRequest: true,
                constraints: null);

            var context = CreateVirtualPathContext(
                values: new { action = "Index", controller = "Home" });

            // Act
            var pathData = route.GetVirtualPath(context);

            // Assert
            Assert.Equal("/Home/Index/", pathData.VirtualPath);
            Assert.Same(route, pathData.Router);
            Assert.Empty(pathData.DataTokens);
        }

        [Fact]
        public void GetVirtualPath_OptionalParameter_InSimpleSegment()
        {
            // Arrange
            var route = CreateRoute(
                template: "{controller}/{action}/{name?}",
                defaults: null,
                handleRequest: true,
                constraints: null);

            var context = CreateVirtualPathContext(
                values: new { action = "Index", controller = "Home" });

            // Act
            var pathData = route.GetVirtualPath(context);

            // Assert
            Assert.Equal("/Home/Index", pathData.VirtualPath);
            Assert.Same(route, pathData.Router);
            Assert.Empty(pathData.DataTokens);
        }

        [Fact]
        public void GetVirtualPath_TwoOptionalParameters_OneValueFromAmbientValues()
        {
            // Arrange
            var route = CreateRoute(
                template: "a/{b=15}/{c?}/{d?}",
                defaults: null,
                handleRequest: true,
                constraints: null);

            var context = CreateVirtualPathContext(
                values: new { },
                ambientValues: new { c = "17" });

            // Act
            var pathData = route.GetVirtualPath(context);

            // Assert
            Assert.NotNull(pathData);
            Assert.Equal("/a/15/17", pathData.VirtualPath);
            Assert.Same(route, pathData.Router);
            Assert.Empty(pathData.DataTokens);
        }


        [Fact]
        public void GetVirtualPath_OptionalParameterAfterDefault_OneValueFromAmbientValues()
        {
            // Arrange
            var route = CreateRoute(
                template: "a/{b=15}/{c?}",
                defaults: null,
                handleRequest: true,
                constraints: null);

            var context = CreateVirtualPathContext(
                values: new { },
                ambientValues: new { c = "17" });

            // Act
            var pathData = route.GetVirtualPath(context);

            // Assert
            Assert.NotNull(pathData);
            Assert.Equal("/a/15/17", pathData.VirtualPath);
            Assert.Same(route, pathData.Router);
            Assert.Empty(pathData.DataTokens);
        }

        [Fact]
        public void GetVirtualPath_TwoOptionalParametersAfterDefault_OneValueFromAmbientValues()
        {
            // Arrange
            var route = CreateRoute(
                template: "a/{b=15}/{c?}/{d?}",
                defaults: null,
                handleRequest: true,
                constraints: null);

            var context = CreateVirtualPathContext(
                values: new { },
                ambientValues: new { c = "17" });

            // Act
            var pathData = route.GetVirtualPath(context);

            // Assert
            Assert.NotNull(pathData);
            Assert.Equal("/a/15/17", pathData.VirtualPath);
            Assert.Same(route, pathData.Router);
            Assert.Empty(pathData.DataTokens);
        }

        [Fact]
        public void GetVirtualPath_TwoOptionalParametersAfterDefault_LastValueFromAmbientValues()
        {
            // Arrange
            var route = CreateRoute(
                template: "a/{b=15}/{c?}/{d?}",
                defaults: null,
                handleRequest: true,
                constraints: null);

            var context = CreateVirtualPathContext(
                values: new { },
                ambientValues: new { d = "17" });

            // Act
            var pathData = route.GetVirtualPath(context);

            // Assert
            Assert.NotNull(pathData);
            Assert.Equal("/a", pathData.VirtualPath);
            Assert.Same(route, pathData.Router);
            Assert.Empty(pathData.DataTokens);
        }

        private static VirtualPathContext CreateVirtualPathContext(object values)
        {
            return CreateVirtualPathContext(new RouteValueDictionary(values), null);
        }

        private static VirtualPathContext CreateVirtualPathContext(object values, object ambientValues)
        {
            return CreateVirtualPathContext(new RouteValueDictionary(values), new RouteValueDictionary(ambientValues));
        }

        private static VirtualPathContext CreateVirtualPathContext(
            RouteValueDictionary values,
            RouteValueDictionary ambientValues)
        {
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            services.AddOptions();
            services.AddRouting();

            var context = new DefaultHttpContext
            {
                RequestServices = services.BuildServiceProvider(),
            };

            return new VirtualPathContext(context, ambientValues, values);
        }

        private static VirtualPathContext CreateVirtualPathContext(string routeName)
        {
            return new VirtualPathContext(null, null, null, routeName);
        }

        public static IEnumerable<object[]> DataTokens
        {
            get
            {
                yield return new object[] {
                                            new Dictionary<string, object> { { "key1", "data1" }, { "key2", 13 } },
                                            new Dictionary<string, object> { { "key1", "data1" }, { "key2", 13 } },
                                          };
                yield return new object[] {
                                            new RouteValueDictionary { { "key1", "data1" }, { "key2", 13 } },
                                            new Dictionary<string, object> { { "key1", "data1" }, { "key2", 13 } },
                                          };
                yield return new object[] {
                                            new object(),
                                            new Dictionary<string,object>(),
                                          };
                yield return new object[] {
                                            null,
                                            new Dictionary<string, object>()
                                          };
                yield return new object[] {
                                            new { key1 = "data1", key2 = 13 },
                                            new Dictionary<string, object> { { "key1", "data1" }, { "key2", 13 } },
                                          };
            }
        }

        [Theory]
        [MemberData(nameof(DataTokens))]
        public void RegisteringRoute_WithDataTokens_AbleToAddTheRoute(object dataToken,
                                                                      IDictionary<string, object> expectedDictionary)
        {
            // Arrange
            var routeBuilder = CreateRouteBuilder();

            // Act
            routeBuilder.MapRoute("mockName",
                                  "{controller}/{action}",
                                  defaults: null,
                                  constraints: null,
                                  dataTokens: dataToken);

            // Assert
            var templateRoute = (Route)routeBuilder.Routes[0];

            Assert.Equal(expectedDictionary.Count, templateRoute.DataTokens.Count);
            foreach (var expectedKey in expectedDictionary.Keys)
            {
                Assert.True(templateRoute.DataTokens.ContainsKey(expectedKey));
                Assert.Equal(expectedDictionary[expectedKey], templateRoute.DataTokens[expectedKey]);
            }
        }

        [Fact]
        public void RegisteringRoute_WithParameterPolicy_AbleToAddTheRoute()
        {
            // Arrange
            var routeBuilder = CreateRouteBuilder();

            // Act
            routeBuilder.MapRoute("mockName",
                                  "{controller:test-policy}/{action}");

            // Assert
            var templateRoute = (Route)routeBuilder.Routes[0];

            Assert.Empty(templateRoute.Constraints);
        }

        [Fact]
        public void RegisteringRouteWithInvalidConstraints_Throws()
        {
            // Arrange
            var routeBuilder = CreateRouteBuilder();

            // Assert
            var expectedMessage = "An error occurred while creating the route with name 'mockName' and template" +
                " '{controller}/{action}'.";

            var exception = ExceptionAssert.Throws<RouteCreationException>(
                () => routeBuilder.MapRoute("mockName",
                    "{controller}/{action}",
                    defaults: null,
                    constraints: new { controller = "a.*", action = 17 }),
                    expectedMessage);

            expectedMessage = "The constraint entry 'action' - '17' on the route '{controller}/{action}' " +
                "must have a string value or be of a type which implements '" +
                typeof(IRouteConstraint) + "'.";
            Assert.NotNull(exception.InnerException);
            Assert.Equal(expectedMessage, exception.InnerException.Message);
        }

        [Fact]
        public void RegisteringRouteWithTwoConstraints()
        {
            // Arrange
            var routeBuilder = CreateRouteBuilder();

            var mockConstraint = new Mock<IRouteConstraint>().Object;

            routeBuilder.MapRoute("mockName",
                "{controller}/{action}",
                defaults: null,
                constraints: new { controller = "a.*", action = mockConstraint });

            var constraints = ((Route)routeBuilder.Routes[0]).Constraints;

            // Assert
            Assert.Equal(2, constraints.Count);
            Assert.IsType<RegexRouteConstraint>(constraints["controller"]);
            Assert.Equal(mockConstraint, constraints["action"]);
        }

        [Fact]
        public void RegisteringRouteWithOneInlineConstraintAndOneUsingConstraintArgument()
        {
            // Arrange
            var routeBuilder = CreateRouteBuilder();

            // Act
            routeBuilder.MapRoute("mockName",
                "{controller}/{action}/{id:int}",
                defaults: null,
                constraints: new { id = "1*" });

            // Assert
            var constraints = ((Route)routeBuilder.Routes[0]).Constraints;
            Assert.Equal(1, constraints.Count);
            var constraint = (CompositeRouteConstraint)constraints["id"];
            Assert.IsType<CompositeRouteConstraint>(constraint);
            Assert.IsType<RegexRouteConstraint>(constraint.Constraints.ElementAt(0));
            Assert.IsType<IntRouteConstraint>(constraint.Constraints.ElementAt(1));
        }

        [Fact]
        public void RegisteringRoute_WithOneInlineConstraint_AddsItToConstraintCollection()
        {
            // Arrange
            var routeBuilder = CreateRouteBuilder();

            // Act
            routeBuilder.MapRoute("mockName",
                "{controller}/{action}/{id:int}",
                defaults: null,
                constraints: null);

            // Assert
            var constraints = ((Route)routeBuilder.Routes[0]).Constraints;
            Assert.Equal(1, constraints.Count);
            Assert.IsType<IntRouteConstraint>(constraints["id"]);
        }

        [Fact]
        public void RegisteringRouteWithRouteName_WithNullDefaults_AddsTheRoute()
        {
            // Arrange
            var routeBuilder = CreateRouteBuilder();

            routeBuilder.MapRoute(name: "RouteName", template: "{controller}/{action}", defaults: null);

            // Act
            var name = ((Route)routeBuilder.Routes[0]).Name;

            // Assert
            Assert.Equal("RouteName", name);
        }

        [Fact]
        public void RegisteringRouteWithRouteName_WithNullDefaultsAndConstraints_AddsTheRoute()
        {
            // Arrange
            var routeBuilder = CreateRouteBuilder();

            routeBuilder.MapRoute(name: "RouteName",
                                template: "{controller}/{action}",
                                defaults: null,
                                constraints: null);

            // Act
            var name = ((Route)routeBuilder.Routes[0]).Name;

            // Assert
            Assert.Equal("RouteName", name);
        }

        [Theory]
        [InlineData("///")]
        [InlineData("/a//")]
        [InlineData("/a/b//")]
        [InlineData("//b//")]
        [InlineData("///c")]
        [InlineData("///c/")]
        public async Task RouteAsync_MultipleOptionalParameters_WithEmptyIntermediateSegmentsDoesNotMatch(string url)
        {
            // Arrange
            var builder = CreateRouteBuilder();

            builder.MapRoute(name: null,
                    template: "{controller?}/{action?}/{id?}",
                    defaults: null,
                    constraints: null);

            var route = builder.Build();

            var context = CreateRouteContext(url);

            // Act
            await route.RouteAsync(context);

            // Assert
            Assert.Null(context.Handler);
        }

        // DataTokens test data for TemplateRoute.GetVirtualPath
        public static IEnumerable<object[]> DataTokensTestData
        {
            get
            {
                yield return new object[] { null };
                yield return new object[] { new RouteValueDictionary() };
                yield return new object[] { new RouteValueDictionary() { { "tokenKeyA", "tokenValueA" } } };
            }
        }

        private static IRouteBuilder CreateRouteBuilder()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IInlineConstraintResolver>(_inlineConstraintResolver);
            services.AddSingleton<RoutingMarkerService>();
            services.AddSingleton<ParameterPolicyFactory, DefaultParameterPolicyFactory>();
            services.Configure<RouteOptions>(ConfigureRouteOptions);

            var applicationBuilder = Mock.Of<IApplicationBuilder>();
            applicationBuilder.ApplicationServices = services.BuildServiceProvider();

            var routeBuilder = new RouteBuilder(applicationBuilder);
            routeBuilder.DefaultHandler = new RouteHandler(NullHandler);
            return routeBuilder;
        }

        private static Route CreateRoute(string routeName, string template, bool handleRequest = true)
        {
            return new Route(
                CreateTarget(handleRequest),
                routeName,
                template,
                defaults: null,
                constraints: null,
                dataTokens: null,
                inlineConstraintResolver: _inlineConstraintResolver);
        }

        private static Route CreateRoute(string template, bool handleRequest = true)
        {
            return new Route(CreateTarget(handleRequest), template, _inlineConstraintResolver);
        }

        private static Route CreateRoute(
            string template,
            object defaults,
            bool handleRequest = true,
            object constraints = null,
            object dataTokens = null)
        {
            return new Route(
                CreateTarget(handleRequest),
                template,
                new RouteValueDictionary(defaults),
                new RouteValueDictionary(constraints),
                new RouteValueDictionary(dataTokens),
                _inlineConstraintResolver);
        }

        private static Route CreateRoute(IRouter target, string template)
        {
            return new Route(
                target,
                template,
                new RouteValueDictionary(),
                constraints: null,
                dataTokens: null,
                inlineConstraintResolver: _inlineConstraintResolver);
        }

        private static Route CreateRoute(
            IRouter target,
            string template,
            object defaults,
            RouteValueDictionary dataTokens = null)
        {
            return new Route(
                target,
                template,
                new RouteValueDictionary(defaults),
                constraints: null,
                dataTokens: dataTokens,
                inlineConstraintResolver: _inlineConstraintResolver);
        }

        private static IRouter CreateTarget(bool handleRequest = true)
        {
            var target = new Mock<IRouter>(MockBehavior.Strict);
            target
                .Setup(e => e.GetVirtualPath(It.IsAny<VirtualPathContext>()))
                .Returns<VirtualPathContext>(rc => null);

            target
                .Setup(e => e.RouteAsync(It.IsAny<RouteContext>()))
                .Callback<RouteContext>((c) => c.Handler = handleRequest ? NullHandler : null)
                .Returns(Task.FromResult<object>(null));

            return target.Object;
        }

        private static IInlineConstraintResolver GetInlineConstraintResolver()
        {
            var routeOptions = new RouteOptions();
            ConfigureRouteOptions(routeOptions);

            var routeOptionsMock = new Mock<IOptions<RouteOptions>>();
            routeOptionsMock
                .SetupGet(o => o.Value)
                .Returns(routeOptions);

            return new DefaultInlineConstraintResolver(routeOptionsMock.Object, new TestServiceProvider());
        }

        private static void ConfigureRouteOptions(RouteOptions options)
        {
            options.ConstraintMap["test-policy"] = typeof(TestPolicy);
        }

        private class TestPolicy : IParameterPolicy
        {
        }
    }
}
