// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Routing;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.OptionsModel;
using Moq;
using Xunit;
using Microsoft.Framework.Logging;

namespace Microsoft.AspNet.Mvc.Core.Test
{
    public class UrlHelperTest
    {
        [Theory]
        [InlineData("", "/Home/About", "/Home/About")]
        [InlineData("/myapproot", "/test", "/test")]
        public void Content_ReturnsContentPath_WhenItDoesNotStartWithToken(string appRoot,
                                                                           string contentPath,
                                                                           string expectedPath)
        {
            // Arrange
            var context = CreateHttpContext(appRoot);
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
        [InlineData("/myapproot", "~/Content/bootstrap.css", "/myapproot/Content/bootstrap.css")]
        public void Content_ReturnsAppRelativePath_WhenItStartsWithToken(string appRoot,
                                                                         string contentPath,
                                                                         string expectedPath)
        {
            // Arrange
            var context = CreateHttpContext(appRoot);
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
        [InlineData("http://///www.example.com/foo.html")]
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
            var urlHelper = CreateUrlHelperWithRouteCollection("/app");

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
            var urlHelper = CreateUrlHelperWithRouteCollection("/app");

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
            var urlHelper = CreateUrlHelperWithRouteCollection("/app");

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
            var urlHelper = CreateUrlHelperWithRouteCollection("/app");

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
            var urlHelper = CreateUrlHelperWithRouteCollection("/app");

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
            var urlHelper = CreateUrlHelperWithRouteCollection("/app");

            // Act
            var url = urlHelper.RouteUrl(new { Action = "newaction", Controller = "home2", id = "someid" });

            // Assert
            Assert.Equal("/app/home2/newaction/someid", url);
        }

        [Fact]
        public void RouteUrlWithProtocol()
        {
            // Arrange
            var urlHelper = CreateUrlHelperWithRouteCollection("/app");

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
            var urlHelper = CreateUrlHelperWithRouteCollection("/app");

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
            var routeCollection = GetRouter("MyRouteName", "any/url");
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
            var urlHelper = CreateUrlHelperWithRouteCollection("/app");

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
            var urlHelper = CreateUrlHelperWithRouteCollection("/app");

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
        public void UrlAction_RouteValuesAsDictionary_CaseSensitive()
        {
            // Arrange
            var urlHelper = CreateUrlHelperWithRouteCollection("/app");

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
            var urlHelper = CreateUrlHelperWithRouteCollection("/app");

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
            var urlHelper = CreateUrlHelperWithRouteCollection("/app");

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

        private static HttpContext CreateHttpContext(string appRoot, ILoggerFactory factory = null)
        {
            if (factory == null)
            {
                factory = NullLoggerFactory.Instance;
            }

            var appRootPath = new PathString(appRoot);
            var request = new Mock<HttpRequest>();
            request.SetupGet(r => r.PathBase)
                   .Returns(appRootPath);
            request.SetupGet(r => r.Host)
                   .Returns(new HostString("localhost"));
            var context = new Mock<HttpContext>();
            context.Setup(m => m.RequestServices.GetService(typeof(ILoggerFactory)))
                   .Returns(factory);
            context.SetupGet(c => c.Request)
                   .Returns(request.Object);

            return context.Object;
        }

        private static IContextAccessor<ActionContext> CreateActionContext(HttpContext context)
        {
            return CreateActionContext(context, (new Mock<IRouter>()).Object);
        }

        private static IContextAccessor<ActionContext> CreateActionContext(HttpContext context, IRouter router)
        {
            var routeData = new RouteData();
            routeData.Values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            routeData.Routers.Add(router);

            var actionContext = new ActionContext(context,
                                                  routeData,
                                                  new ActionDescriptor());
            var contextAccessor = new Mock<IContextAccessor<ActionContext>>();
            contextAccessor.SetupGet(c => c.Value)
                           .Returns(actionContext);
            return contextAccessor.Object;
        }

        private static UrlHelper CreateUrlHelper()
        {
            var context = CreateHttpContext(string.Empty);
            var actionContext = CreateActionContext(context);

            var actionSelector = new Mock<IActionSelector>(MockBehavior.Strict);
            return new UrlHelper(actionContext, actionSelector.Object);
        }

        private static UrlHelper CreateUrlHelper(string host)
        {
            var context = CreateHttpContext(string.Empty);
            context.Request.Host = new HostString(host);

            var actionContext = CreateActionContext(context);

            var actionSelector = new Mock<IActionSelector>(MockBehavior.Strict);
            return new UrlHelper(actionContext, actionSelector.Object);
        }

        private static UrlHelper CreateUrlHelper(IContextAccessor<ActionContext> contextAccessor)
        {
            var actionSelector = new Mock<IActionSelector>(MockBehavior.Strict);
            return new UrlHelper(contextAccessor, actionSelector.Object);
        }

        private static UrlHelper CreateUrlHelper(string appBase, IRouter router)
        {
            var context = CreateHttpContext(appBase);
            var actionContext = CreateActionContext(context, router);

            var actionSelector = new Mock<IActionSelector>(MockBehavior.Strict);
            return new UrlHelper(actionContext, actionSelector.Object);
        }

        private static UrlHelper CreateUrlHelperWithRouteCollection(string appPrefix)
        {
            var routeCollection = GetRouter();
            return CreateUrlHelper("/app", routeCollection);
        }

        private static IRouter GetRouter()
        {
            return GetRouter("mockRoute", "/mockTemplate");
        }

        private static IRouter GetRouter(string mockRouteName, string mockTemplateValue)
        {
            var rt = new RouteBuilder();
            var target = new Mock<IRouter>(MockBehavior.Strict);
            target
                .Setup(e => e.GetVirtualPath(It.IsAny<VirtualPathContext>()))
                .Callback<VirtualPathContext>(c =>
                {
                    rt.ToString();
                    c.IsBound = true;
                })
                .Returns<VirtualPathContext>(rc => null);
            rt.DefaultHandler = target.Object;
            var serviceProviderMock = new Mock<IServiceProvider>();
            var accessorMock = new Mock<IOptions<RouteOptions>>();
            accessorMock.SetupGet(o => o.Options).Returns(new RouteOptions());
            serviceProviderMock.Setup(o => o.GetService(typeof(IInlineConstraintResolver)))
                               .Returns(new DefaultInlineConstraintResolver(serviceProviderMock.Object,
                                                                            accessorMock.Object));

            rt.ServiceProvider = serviceProviderMock.Object;
            rt.MapRoute(string.Empty,
                        "{controller}/{action}/{id}",
                        new RouteValueDictionary(new { id = "defaultid" }));
            rt.MapRoute("namedroute",
                        "named/{controller}/{action}/{id}",
                        new RouteValueDictionary(new { id = "defaultid" }));

            var mockHttpRoute = new Mock<IRouter>();
            mockHttpRoute.Setup(mock =>
                                    mock.GetVirtualPath(It.Is<VirtualPathContext>(c => string.Equals(c.RouteName,
                                                                                                  mockRouteName)
                                                                                  )))
                         .Returns(mockTemplateValue);
            rt.Routes.Add(mockHttpRoute.Object);
            return rt.Build();
        }
    }
}