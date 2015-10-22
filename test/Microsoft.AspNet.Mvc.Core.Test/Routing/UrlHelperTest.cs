// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Mvc.Abstractions;
using Microsoft.AspNet.Mvc.Infrastructure;
using Microsoft.AspNet.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.OptionsModel;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Routing
{
    public class UrlHelperTest
    {
        [Theory]
        [InlineData(null, null, null)]
        [InlineData("/myapproot", null, null)]
        [InlineData("", "/Home/About", "/Home/About")]
        [InlineData("/myapproot", "/test", "/test")]
        public void Content_ReturnsContentPath_WhenItDoesNotStartWithToken(
            string appRoot,
            string contentPath,
            string expectedPath)
        {
            // Arrange
            var context = CreateHttpContext(GetServices(), appRoot);
            var contextAccessor = CreateActionContext(context);
            var urlHelper = CreateUrlHelper(contextAccessor);

            // Act
            var path = urlHelper.Content(contentPath);

            // Assert
            Assert.Equal(expectedPath, path);
        }

        [Theory]
        [InlineData(null, "~/Home/About", "/Home/About")]
        [InlineData("/", "~/Home/About", "/Home/About")]
        [InlineData("/", "~/", "/")]
        [InlineData("/myapproot", "~/", "/myapproot/")]
        [InlineData("", "~/Home/About", "/Home/About")]
        [InlineData("/", "~", "/")]
        [InlineData("/myapproot", "~/Content/bootstrap.css", "/myapproot/Content/bootstrap.css")]
        public void Content_ReturnsAppRelativePath_WhenItStartsWithToken(
            string appRoot,
            string contentPath,
            string expectedPath)
        {
            // Arrange
            var context = CreateHttpContext(GetServices(), appRoot);
            var contextAccessor = CreateActionContext(context);
            var urlHelper = CreateUrlHelper(contextAccessor);

            // Act
            var path = urlHelper.Content(contentPath);

            // Assert
            Assert.Equal(expectedPath, path);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void IsLocalUrl_ReturnsFalseOnEmpty(string url)
        {
            // Arrange
            var helper = CreateUrlHelper();

            // Act
            var result = helper.IsLocalUrl(url);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData("/foo.html")]
        [InlineData("/www.example.com")]
        [InlineData("/")]
        public void IsLocalUrl_AcceptsRootedUrls(string url)
        {
            // Arrange
            var helper = CreateUrlHelper();

            // Act
            var result = helper.IsLocalUrl(url);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData("~/")]
        [InlineData("~/foo.html")]
        public void IsLocalUrl_AcceptsApplicationRelativeUrls(string url)
        {
            // Arrange
            var helper = CreateUrlHelper();

            // Act
            var result = helper.IsLocalUrl(url);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData("foo.html")]
        [InlineData("../foo.html")]
        [InlineData("fold/foo.html")]
        public void IsLocalUrl_RejectsRelativeUrls(string url)
        {
            // Arrange
            var helper = CreateUrlHelper();

            // Act
            var result = helper.IsLocalUrl(url);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData("http:/foo.html")]
        [InlineData("hTtP:foo.html")]
        [InlineData("http:/www.example.com")]
        [InlineData("HtTpS:/www.example.com")]
        public void IsLocalUrl_RejectValidButUnsafeRelativeUrls(string url)
        {
            // Arrange
            var helper = CreateUrlHelper();

            // Act
            var result = helper.IsLocalUrl(url);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData("http://www.mysite.com/appDir/foo.html")]
        [InlineData("http://WWW.MYSITE.COM")]
        public void IsLocalUrl_RejectsUrlsOnTheSameHost(string url)
        {
            // Arrange
            var helper = CreateUrlHelper("www.mysite.com");

            // Act
            var result = helper.IsLocalUrl(url);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData("http://localhost/foobar.html")]
        [InlineData("http://127.0.0.1/foobar.html")]
        public void IsLocalUrl_RejectsUrlsOnLocalHost(string url)
        {
            // Arrange
            var helper = CreateUrlHelper("www.mysite.com");

            // Act
            var result = helper.IsLocalUrl(url);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData("https://www.mysite.com/")]
        public void IsLocalUrl_RejectsUrlsOnTheSameHostButDifferentScheme(string url)
        {
            // Arrange
            var helper = CreateUrlHelper("www.mysite.com");

            // Act
            var result = helper.IsLocalUrl(url);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData("http://www.example.com")]
        [InlineData("https://www.example.com")]
        [InlineData("hTtP://www.example.com")]
        [InlineData("HtTpS://www.example.com")]
        public void IsLocalUrl_RejectsUrlsOnDifferentHost(string url)
        {
            // Arrange
            var helper = CreateUrlHelper("www.mysite.com");

            // Act
            var result = helper.IsLocalUrl(url);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData("http://///www.example.com/foo.html")]
        [InlineData("https://///www.example.com/foo.html")]
        [InlineData("HtTpS://///www.example.com/foo.html")]
        [InlineData("http:///www.example.com/foo.html")]
        [InlineData("http:////www.example.com/foo.html")]
        public void IsLocalUrl_RejectsUrlsWithTooManySchemeSeparatorCharacters(string url)
        {
            // Arrange
            var helper = CreateUrlHelper("www.mysite.com");

            // Act
            var result = helper.IsLocalUrl(url);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData("//www.example.com")]
        [InlineData("//www.example.com?")]
        [InlineData("//www.example.com:80")]
        [InlineData("//www.example.com/foobar.html")]
        [InlineData("///www.example.com")]
        [InlineData("//////www.example.com")]
        public void IsLocalUrl_RejectsUrlsWithMissingSchemeName(string url)
        {
            // Arrange
            var helper = CreateUrlHelper("www.mysite.com");

            // Act
            var result = helper.IsLocalUrl(url);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData("http:\\\\www.example.com")]
        [InlineData("http:\\\\www.example.com\\")]
        [InlineData("/\\")]
        [InlineData("/\\foo")]
        public void IsLocalUrl_RejectsInvalidUrls(string url)
        {
            // Arrange
            var helper = CreateUrlHelper("www.mysite.com");

            // Act
            var result = helper.IsLocalUrl(url);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void RouteUrlWithDictionary()
        {
            // Arrange
            var services = GetServices();
            var urlHelper = CreateUrlHelperWithRouteCollection(services, "/app");

            // Act
            var url = urlHelper.RouteUrl(values: new RouteValueDictionary(
                                                                    new
                                                                    {
                                                                        Action = "newaction",
                                                                        Controller = "home2",
                                                                        id = "someid"
                                                                    }));

            // Assert
            Assert.Equal("/app/home2/newaction/someid", url);
        }

        [Fact]
        public void RouteUrlWithEmptyHostName()
        {
            // Arrange
            var services = GetServices();
            var urlHelper = CreateUrlHelperWithRouteCollection(services, "/app");

            // Act
            var url = urlHelper.RouteUrl(routeName: "namedroute",
                                         values: new RouteValueDictionary(
                                                                    new
                                                                    {
                                                                        Action = "newaction",
                                                                        Controller = "home2",
                                                                        id = "someid"
                                                                    }),
                                         protocol: "http",
                                         host: string.Empty);

            // Assert
            Assert.Equal("http://localhost/app/named/home2/newaction/someid", url);
        }

        [Fact]
        public void RouteUrlWithEmptyProtocol()
        {
            // Arrange
            var services = GetServices();
            var urlHelper = CreateUrlHelperWithRouteCollection(services, "/app");

            // Act
            var url = urlHelper.RouteUrl(routeName: "namedroute",
                                         values: new RouteValueDictionary(
                                                                    new
                                                                    {
                                                                        Action = "newaction",
                                                                        Controller = "home2",
                                                                        id = "someid"
                                                                    }),
                                         protocol: string.Empty,
                                         host: "foo.bar.com");

            // Assert
            Assert.Equal("http://foo.bar.com/app/named/home2/newaction/someid", url);
        }

        [Fact]
        public void RouteUrlWithNullProtocol()
        {
            // Arrange
            var services = GetServices();
            var urlHelper = CreateUrlHelperWithRouteCollection(services, "/app");

            // Act
            var url = urlHelper.RouteUrl(routeName: "namedroute",
                                         values: new RouteValueDictionary(
                                                                    new
                                                                    {
                                                                        Action = "newaction",
                                                                        Controller = "home2",
                                                                        id = "someid"
                                                                    }),
                                         protocol: null,
                                         host: "foo.bar.com");

            // Assert
            Assert.Equal("http://foo.bar.com/app/named/home2/newaction/someid", url);
        }

        [Fact]
        public void RouteUrlWithNullProtocolAndNullHostName()
        {
            // Arrange
            var services = GetServices();
            var urlHelper = CreateUrlHelperWithRouteCollection(services, "/app");

            // Act
            var url = urlHelper.RouteUrl(routeName: "namedroute",
                                         values: new RouteValueDictionary(
                                                                    new
                                                                    {
                                                                        Action = "newaction",
                                                                        Controller = "home2",
                                                                        id = "someid"
                                                                    }),
                                         protocol: null,
                                         host: null);

            // Assert
            Assert.Equal("/app/named/home2/newaction/someid", url);
        }

        [Fact]
        public void RouteUrlWithObjectProperties()
        {
            // Arrange
            var services = GetServices();
            var urlHelper = CreateUrlHelperWithRouteCollection(services, "/app");

            // Act
            var url = urlHelper.RouteUrl(new { Action = "newaction", Controller = "home2", id = "someid" });

            // Assert
            Assert.Equal("/app/home2/newaction/someid", url);
        }

        [Fact]
        public void RouteUrlWithProtocol()
        {
            // Arrange
            var services = GetServices();
            var urlHelper = CreateUrlHelperWithRouteCollection(services, "/app");

            // Act
            var url = urlHelper.RouteUrl(routeName: "namedroute",
                                         values: new
                                         {
                                             Action = "newaction",
                                             Controller = "home2",
                                             id = "someid"
                                         },
                                         protocol: "https");

            // Assert
            Assert.Equal("https://localhost/app/named/home2/newaction/someid", url);
        }

        [Fact]
        public void RouteUrl_WithUnicodeHost_DoesNotPunyEncodeTheHost()
        {
            // Arrange
            var services = GetServices();
            var urlHelper = CreateUrlHelperWithRouteCollection(services, "/app");

            // Act
            var url = urlHelper.RouteUrl(routeName: "namedroute",
                                         values: new
                                         {
                                             Action = "newaction",
                                             Controller = "home2",
                                             id = "someid"
                                         },
                                         protocol: "https",
                                         host: "pingüino");

            // Assert
            Assert.Equal("https://pingüino/app/named/home2/newaction/someid", url);
        }

        [Fact]
        public void RouteUrlWithRouteNameAndDefaults()
        {
            // Arrange
            var services = GetServices();
            var routeCollection = GetRouter(services, "MyRouteName", "any/url");
            var urlHelper = CreateUrlHelper("/app", routeCollection);

            // Act
            var url = urlHelper.RouteUrl("MyRouteName");

            // Assert
            Assert.Equal("/app/any/url", url);
        }

        [Fact]
        public void RouteUrlWithRouteNameAndDictionary()
        {
            // Arrange
            var services = GetServices();
            var urlHelper = CreateUrlHelperWithRouteCollection(services, "/app");

            // Act
            var url = urlHelper.RouteUrl(routeName: "namedroute",
                                         values: new RouteValueDictionary(
                                                            new
                                                            {
                                                                Action = "newaction",
                                                                Controller = "home2",
                                                                id = "someid"
                                                            }));

            // Assert
            Assert.Equal("/app/named/home2/newaction/someid", url);
        }

        [Fact]
        public void RouteUrlWithRouteNameAndObjectProperties()
        {
            // Arrange
            var services = GetServices();
            var urlHelper = CreateUrlHelperWithRouteCollection(services, "/app");

            // Act
            var url = urlHelper.RouteUrl(routeName: "namedroute",
                                         values: new
                                         {
                                             Action = "newaction",
                                             Controller = "home2",
                                             id = "someid"
                                         });

            // Assert
            Assert.Equal("/app/named/home2/newaction/someid", url);
        }

        [Fact]
        public void RouteUrlWithUrlRouteContext_ReturnsExpectedResult()
        {
            // Arrange
            var services = GetServices();
            var urlHelper = CreateUrlHelperWithRouteCollection(services, "/app");

            var routeContext = new UrlRouteContext()
            {
                RouteName = "namedroute",
                Values = new
                {
                    Action = "newaction",
                    Controller = "home2",
                    id = "someid"
                },
                Fragment = "somefragment",
                Host = "remotetown",
                Protocol = "ftp"
            };

            // Act
            var url = urlHelper.RouteUrl(routeContext);

            // Assert
            Assert.Equal("ftp://remotetown/app/named/home2/newaction/someid#somefragment", url);
        }

        [Fact]
        public void RouteUrlWithAllParameters_ReturnsExpectedResult()
        {
            // Arrange
            var services = GetServices();
            var urlHelper = CreateUrlHelperWithRouteCollection(services, "/app");

            // Act
            var url = urlHelper.RouteUrl(
                routeName: "namedroute",
                values: new
                {
                    Action = "newaction",
                    Controller = "home2",
                    id = "someid"
                },
                fragment: "somefragment",
                host: "remotetown",
                protocol: "https");

            // Assert
            Assert.Equal("https://remotetown/app/named/home2/newaction/someid#somefragment", url);
        }

        [Fact]
        public void UrlAction_RouteValuesAsDictionary_CaseSensitive()
        {
            // Arrange
            var services = GetServices();
            var urlHelper = CreateUrlHelperWithRouteCollection(services, "/app");

            // We're using a dictionary with a case-sensitive comparer and loading it with data
            // using casings differently from the route. This should still successfully generate a link.
            var dict = new Dictionary<string, object>();
            var id = "suppliedid";
            var isprint = "true";
            dict["ID"] = id;
            dict["isprint"] = isprint;

            // Act
            var url = urlHelper.Action(
                                    action: "contact",
                                    controller: "home",
                                    values: dict);

            // Assert
            Assert.Equal(2, dict.Count);
            Assert.Same(id, dict["ID"]);
            Assert.Same(isprint, dict["isprint"]);
            Assert.Equal("/app/home/contact/suppliedid?isprint=true", url);
        }

        [Fact]
        public void UrlAction_WithUnicodeHost_DoesNotPunyEncodeTheHost()
        {
            // Arrange
            var services = GetServices();
            var urlHelper = CreateUrlHelperWithRouteCollection(services, "/app");

            // Act
            var url = urlHelper.Action(
                                    action: "contact",
                                    controller: "home",
                                    values: null,
                                    protocol: "http",
                                    host: "pingüino");

            // Assert
            Assert.Equal("http://pingüino/app/home/contact", url);
        }

        [Fact]
        public void UrlRouteUrl_RouteValuesAsDictionary_CaseSensitive()
        {
            // Arrange
            var services = GetServices();
            var urlHelper = CreateUrlHelperWithRouteCollection(services, "/app");

            // We're using a dictionary with a case-sensitive comparer and loading it with data
            // using casings differently from the route. This should still successfully generate a link.
            var dict = new Dictionary<string, object>();
            var action = "contact";
            var controller = "home";
            var id = "suppliedid";

            dict["ACTION"] = action;
            dict["Controller"] = controller;
            dict["ID"] = id;

            // Act
            var url = urlHelper.RouteUrl(routeName: "namedroute", values: dict);

            // Assert
            Assert.Equal(3, dict.Count);
            Assert.Same(action, dict["ACTION"]);
            Assert.Same(controller, dict["Controller"]);
            Assert.Same(id, dict["ID"]);
            Assert.Equal("/app/named/home/contact/suppliedid", url);
        }

        [Fact]
        public void UrlActionWithUrlActionContext_ReturnsExpectedResult()
        {
            // Arrange
            var services = GetServices();
            var urlHelper = CreateUrlHelperWithRouteCollection(services, "/app");

            var actionContext = new UrlActionContext()
            {
                Action = "contact",
                Controller = "home3",
                Values = new { id = "idone" },
                Protocol = "ftp",
                Host = "remotelyhost",
                Fragment = "somefragment"
            };

            // Act
            var url = urlHelper.Action(actionContext);

            // Assert
            Assert.Equal("ftp://remotelyhost/app/home3/contact/idone#somefragment", url);
        }

        [Fact]
        public void UrlActionWithAllParameters_ReturnsExpectedResult()
        {
            // Arrange
            var services = GetServices();
            var urlHelper = CreateUrlHelperWithRouteCollection(services, "/app");

            // Act
            var url = urlHelper.Action(
                controller: "home3",
                action: "contact",
                values: null,
                protocol: "https",
                host: "remotelyhost",
                fragment: "somefragment"
                );

            // Assert
            Assert.Equal("https://remotelyhost/app/home3/contact#somefragment", url);
        }

        [Fact]
        public void LinkWithAllParameters_ReturnsExpectedResult()
        {
            // Arrange
            var services = GetServices();
            var urlHelper = CreateUrlHelperWithRouteCollection(services, "/app");

            // Act
            var url = urlHelper.Link("namedroute",
                                     new
                                     {
                                         Action = "newaction",
                                         Controller = "home",
                                         id = "someid"
                                     });

            // Assert
            Assert.Equal("http://localhost/app/named/home/newaction/someid", url);
        }

        [Fact]
        public void LinkWithNullRouteName_ReturnsExpectedResult()
        {
            // Arrange
            var services = GetServices();
            var urlHelper = CreateUrlHelperWithRouteCollection(services, "/app");

            // Act
            var url = urlHelper.Link(null,
                                     new
                                     {
                                         Action = "newaction",
                                         Controller = "home",
                                         id = "someid"
                                     });

            // Assert
            Assert.Equal("http://localhost/app/home/newaction/someid", url);
        }

        [Fact]
        public void LinkWithDefaultsAndNullRouteValues_ReturnsExpectedResult()
        {
            // Arrange
            var services = GetServices();
            var routeCollection = GetRouter(services, "MyRouteName", "any/url");
            var urlHelper = CreateUrlHelper("/app", routeCollection);

            // Act
            var url = urlHelper.Link("MyRouteName", null);

            // Assert
            Assert.Equal("http://localhost/app/any/url", url);
        }

        [Fact]
        public void LinkWithCustomHostAndProtocol_ReturnsExpectedResult()
        {
            // Arrange
            var services = GetServices();
            var routeCollection = GetRouter(services, "MyRouteName", "any/url");
            var urlHelper = CreateUrlHelper("myhost", "https", routeCollection);

            // Act
            var url = urlHelper.Link("namedroute",
                                     new
                                     {
                                         Action = "newaction",
                                         Controller = "home",
                                         id = "someid"
                                     });

            // Assert
            Assert.Equal("https://myhost/named/home/newaction/someid", url);
        }

        // Regression test for aspnet/Mvc#2859
        [Fact]
        public void Action_RouteValueInvalidation_DoesNotAffectActionAndController()
        {
            // Arrange
            var services = GetServices();
            var routeBuilder = new RouteBuilder()
            {
                DefaultHandler = new PassThroughRouter(),
                ServiceProvider = services,
            };

            routeBuilder.MapRoute(
                "default",
                "{first}/{controller}/{action}",
                new { second = "default", controller = "default", action = "default" });

            var actionContext = services.GetService<IActionContextAccessor>().ActionContext;
            actionContext.RouteData.Values.Add("first", "a");
            actionContext.RouteData.Values.Add("controller", "Store");
            actionContext.RouteData.Values.Add("action", "Buy");
            actionContext.RouteData.Routers.Add(routeBuilder.Build());

            var urlHelper = CreateUrlHelper(services);

            // Act
            //
            // In this test the 'first' route value has changed, meaning that *normally* the
            // 'controller' value could not be used. However 'controller' and 'action' are treated
            // specially by UrlHelper.
            var url = urlHelper.Action("Checkout", new { first = "b" });

            // Assert
            Assert.NotNull(url);
            Assert.Equal("/b/Store/Checkout", url);
        }

        // Regression test for aspnet/Mvc#2859
        [Fact]
        public void Action_RouteValueInvalidation_AffectsOtherRouteValues()
        {
            // Arrage
            var services = GetServices();
            var routeBuilder = new RouteBuilder()
            {
                DefaultHandler = new PassThroughRouter(),
                ServiceProvider = services,
            };

            routeBuilder.MapRoute(
                "default",
                "{first}/{second}/{controller}/{action}",
                new { second = "default", controller = "default", action = "default" });

            var actionContext = services.GetService<IActionContextAccessor>().ActionContext;
            actionContext.RouteData.Values.Add("first", "a");
            actionContext.RouteData.Values.Add("second", "x");
            actionContext.RouteData.Values.Add("controller", "Store");
            actionContext.RouteData.Values.Add("action", "Buy");
            actionContext.RouteData.Routers.Add(routeBuilder.Build());

            var urlHelper = CreateUrlHelper(services);

            // Act
            //
            // In this test the 'first' route value has changed, meaning that *normally* the
            // 'controller' value could not be used. However 'controller' and 'action' are treated
            // specially by UrlHelper.
            //
            // 'second' gets no special treatment, and picks up its default value instead.
            var url = urlHelper.Action("Checkout", new { first = "b" });

            // Assert
            Assert.NotNull(url);
            Assert.Equal("/b/default/Store/Checkout", url);
        }

        // Regression test for aspnet/Mvc#2859
        [Fact]
        public void Action_RouteValueInvalidation_DoesNotAffectActionAndController_ActionPassedInRouteValues()
        {
            // Arrage
            var services = GetServices();
            var routeBuilder = new RouteBuilder()
            {
                DefaultHandler = new PassThroughRouter(),
                ServiceProvider = services,
            };

            routeBuilder.MapRoute(
                "default",
                "{first}/{controller}/{action}",
                new { second = "default", controller = "default", action = "default" });

            var actionContext = services.GetService<IActionContextAccessor>().ActionContext;
            actionContext.RouteData.Values.Add("first", "a");
            actionContext.RouteData.Values.Add("controller", "Store");
            actionContext.RouteData.Values.Add("action", "Buy");
            actionContext.RouteData.Routers.Add(routeBuilder.Build());

            var urlHelper = CreateUrlHelper(services);

            // Act
            //
            // In this test the 'first' route value has changed, meaning that *normally* the
            // 'controller' value could not be used. However 'controller' and 'action' are treated
            // specially by UrlHelper.
            var url = urlHelper.Action(action: null, values: new { first = "b", action = "Checkout" });

            // Assert
            Assert.NotNull(url);
            Assert.Equal("/b/Store/Checkout", url);
        }

        private static HttpContext CreateHttpContext(
            IServiceProvider services,
            string appRoot)
        {
            var context = new DefaultHttpContext();
            context.RequestServices = services;

            context.Request.PathBase = new PathString(appRoot);
            context.Request.Host = new HostString("localhost");

            return context;
        }

        private static IActionContextAccessor CreateActionContext(HttpContext context)
        {
            return CreateActionContext(context, (new Mock<IRouter>()).Object);
        }

        private static IActionContextAccessor CreateActionContext(HttpContext context, IRouter router)
        {
            var routeData = new RouteData();
            routeData.Routers.Add(router);

            var actionContext = new ActionContext(context, routeData, new ActionDescriptor());
            return new ActionContextAccessor() { ActionContext = actionContext };
        }

        private static UrlHelper CreateUrlHelper()
        {
            var services = GetServices();
            var context = CreateHttpContext(services, string.Empty);
            var actionContext = CreateActionContext(context);

            var actionSelector = new Mock<IActionSelector>(MockBehavior.Strict);
            return new UrlHelper(actionContext, actionSelector.Object);
        }

        private static UrlHelper CreateUrlHelper(IServiceProvider services)
        {
            var actionSelector = new Mock<IActionSelector>(MockBehavior.Strict);
            return new UrlHelper(
                services.GetRequiredService<IActionContextAccessor>(),
                actionSelector.Object);
        }

        private static UrlHelper CreateUrlHelper(string host)
        {
            var services = GetServices();
            var context = CreateHttpContext(services, string.Empty);
            context.Request.Host = new HostString(host);

            var actionContext = CreateActionContext(context);

            var actionSelector = new Mock<IActionSelector>(MockBehavior.Strict);
            return new UrlHelper(actionContext, actionSelector.Object);
        }

        private static UrlHelper CreateUrlHelper(string host, string protocol, IRouter router)
        {
            var services = GetServices();
            var context = CreateHttpContext(services, string.Empty);
            context.Request.Host = new HostString(host);
            context.Request.Scheme = protocol;

            var actionContext = CreateActionContext(context, router);

            var actionSelector = new Mock<IActionSelector>(MockBehavior.Strict);
            return new UrlHelper(actionContext, actionSelector.Object);
        }

        private static UrlHelper CreateUrlHelper(IActionContextAccessor contextAccessor)
        {
            var actionSelector = new Mock<IActionSelector>(MockBehavior.Strict);
            return new UrlHelper(contextAccessor, actionSelector.Object);
        }

        private static UrlHelper CreateUrlHelper(string appBase, IRouter router)
        {
            var services = GetServices();
            var context = CreateHttpContext(services, appBase);
            var actionContext = CreateActionContext(context, router);

            var actionSelector = new Mock<IActionSelector>(MockBehavior.Strict);
            return new UrlHelper(actionContext, actionSelector.Object);
        }

        private static UrlHelper CreateUrlHelperWithRouteCollection(IServiceProvider services, string appPrefix)
        {
            var routeCollection = GetRouter(services);
            return CreateUrlHelper(appPrefix, routeCollection);
        }

        private static IRouter GetRouter(IServiceProvider services)
        {
            return GetRouter(services, "mockRoute", "/mockTemplate");
        }

        private static IServiceProvider GetServices()
        {
            var services = new Mock<IServiceProvider>();

            var optionsAccessor = new Mock<IOptions<RouteOptions>>();
            optionsAccessor
                .SetupGet(o => o.Value)
                .Returns(new RouteOptions());
            services
                .Setup(s => s.GetService(typeof(IOptions<RouteOptions>)))
                .Returns(optionsAccessor.Object);

            services
                .Setup(s => s.GetService(typeof(IInlineConstraintResolver)))
                .Returns(new DefaultInlineConstraintResolver(optionsAccessor.Object));

            services
                .Setup(s => s.GetService(typeof(ILoggerFactory)))
                .Returns(NullLoggerFactory.Instance);

            services
                .Setup(s => s.GetService(typeof(IActionContextAccessor)))
                .Returns(new ActionContextAccessor()
                {
                    ActionContext = new ActionContext()
                    {
                        HttpContext = new DefaultHttpContext()
                        {
                            ApplicationServices = services.Object,
                            RequestServices = services.Object,
                        },
                        RouteData = new RouteData(),
                    },
                });

            return services.Object;
        }

        private static IRouter GetRouter(
            IServiceProvider services,
            string mockRouteName,
            string mockTemplateValue)
        {
            var routeBuilder = new RouteBuilder();
            routeBuilder.ServiceProvider = services;

            var target = new Mock<IRouter>(MockBehavior.Strict);
            target
                .Setup(router => router.GetVirtualPath(It.IsAny<VirtualPathContext>()))
                .Callback<VirtualPathContext>(context => context.IsBound = true)
                .Returns<VirtualPathContext>(context => null);
            routeBuilder.DefaultHandler = target.Object;

            routeBuilder.MapRoute(string.Empty,
                        "{controller}/{action}/{id}",
                        new RouteValueDictionary(new { id = "defaultid" }));

            routeBuilder.MapRoute("namedroute",
                        "named/{controller}/{action}/{id}",
                        new RouteValueDictionary(new { id = "defaultid" }));

            var mockHttpRoute = new Mock<IRouter>();
            mockHttpRoute
                .Setup(mock => mock.GetVirtualPath(It.Is<VirtualPathContext>(c => string.Equals(c.RouteName, mockRouteName))))
                .Callback<VirtualPathContext>(c => c.IsBound = true)
                .Returns(new VirtualPathData(mockHttpRoute.Object, mockTemplateValue));

            routeBuilder.Routes.Add(mockHttpRoute.Object);
            return routeBuilder.Build();
        }

        private class PassThroughRouter : IRouter
        {
            public VirtualPathData GetVirtualPath(VirtualPathContext context)
            {
                context.IsBound = true;
                return null;
            }

            public Task RouteAsync(RouteContext context)
            {
                context.IsHandled = true;
                return Task.FromResult(false);
            }
        }
    }
}