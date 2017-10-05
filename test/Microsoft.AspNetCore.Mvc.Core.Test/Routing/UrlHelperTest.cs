// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Routing
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
            var httpContext = CreateHttpContext(CreateServices(), appRoot);
            var actionContext = CreateActionContext(httpContext);
            var urlHelper = CreateUrlHelper(actionContext);

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
            var httpContext = CreateHttpContext(CreateServices(), appRoot);
            var actionContext = CreateActionContext(httpContext);
            var urlHelper = CreateUrlHelper(actionContext);

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

        [Theory]
        [InlineData("~//www.example.com")]
        [InlineData("~//www.example.com?")]
        [InlineData("~//www.example.com:80")]
        [InlineData("~//www.example.com/foobar.html")]
        [InlineData("~///www.example.com")]
        [InlineData("~//////www.example.com")]
        public void IsLocalUrl_RejectsTokenUrlsWithMissingSchemeName(string url)
        {
            // Arrange
            var helper = CreateUrlHelper("www.mysite.com");

            // Act
            var result = helper.IsLocalUrl(url);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData("~/\\")]
        [InlineData("~/\\foo")]
        public void IsLocalUrl_RejectsInvalidTokenUrls(string url)
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
            var services = CreateServices();
            var urlHelper = CreateUrlHelperWithRouteCollection(services, "/app");

            // Act
            var url = urlHelper.RouteUrl(
                values: new RouteValueDictionary(
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
            var services = CreateServices();
            var urlHelper = CreateUrlHelperWithRouteCollection(services, "/app");

            // Act
            var url = urlHelper.RouteUrl(
                routeName: "namedroute",
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
            var services = CreateServices();
            var urlHelper = CreateUrlHelperWithRouteCollection(services, "/app");

            // Act
            var url = urlHelper.RouteUrl(
                routeName: "namedroute",
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
            var services = CreateServices();
            var urlHelper = CreateUrlHelperWithRouteCollection(services, "/app");

            // Act
            var url = urlHelper.RouteUrl(
                routeName: "namedroute",
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
            var services = CreateServices();
            var urlHelper = CreateUrlHelperWithRouteCollection(services, "/app");

            // Act
            var url = urlHelper.RouteUrl(
                routeName: "namedroute",
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
            var services = CreateServices();
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
            var services = CreateServices();
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
                protocol: "https");

            // Assert
            Assert.Equal("https://localhost/app/named/home2/newaction/someid", url);
        }

        [Fact]
        public void RouteUrl_WithUnicodeHost_DoesNotPunyEncodeTheHost()
        {
            // Arrange
            var services = CreateServices();
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
                protocol: "https",
                host: "pingüino");

            // Assert
            Assert.Equal("https://pingüino/app/named/home2/newaction/someid", url);
        }

        [Fact]
        public void RouteUrlWithRouteNameAndDefaults()
        {
            // Arrange
            var services = CreateServices();
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
            var services = CreateServices();
            var urlHelper = CreateUrlHelperWithRouteCollection(services, "/app");

            // Act
            var url = urlHelper.RouteUrl(
                routeName: "namedroute",
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
            var services = CreateServices();
            var urlHelper = CreateUrlHelperWithRouteCollection(services, "/app");

            // Act
            var url = urlHelper.RouteUrl(
                routeName: "namedroute",
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
            var services = CreateServices();
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
            var services = CreateServices();
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
            var services = CreateServices();
            var urlHelper = CreateUrlHelperWithRouteCollection(services, "/app");

            // We're using a dictionary with a case-sensitive comparer and loading it with data
            // using casings differently from the route. This should still successfully generate a link.
            var dictionary = new Dictionary<string, object>();
            var id = "suppliedid";
            var isprint = "true";
            dictionary["ID"] = id;
            dictionary["isprint"] = isprint;

            // Act
            var url = urlHelper.Action(
                action: "contact",
                controller: "home",
                values: dictionary);

            // Assert
            Assert.Equal(2, dictionary.Count);
            Assert.Same(id, dictionary["ID"]);
            Assert.Same(isprint, dictionary["isprint"]);
            Assert.Equal("/app/home/contact/suppliedid?isprint=true", url);
        }

        [Fact]
        public void UrlAction_WithUnicodeHost_DoesNotPunyEncodeTheHost()
        {
            // Arrange
            var services = CreateServices();
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
            var services = CreateServices();
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
            var services = CreateServices();
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
            var services = CreateServices();
            var urlHelper = CreateUrlHelperWithRouteCollection(services, "/app");

            // Act
            var url = urlHelper.Action(
                controller: "home3",
                action: "contact",
                values: null,
                protocol: "https",
                host: "remotelyhost",
                fragment: "somefragment");

            // Assert
            Assert.Equal("https://remotelyhost/app/home3/contact#somefragment", url);
        }

        [Fact]
        public void LinkWithAllParameters_ReturnsExpectedResult()
        {
            // Arrange
            var services = CreateServices();
            var urlHelper = CreateUrlHelperWithRouteCollection(services, "/app");

            // Act
            var url = urlHelper.Link(
                "namedroute",
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
            var services = CreateServices();
            var urlHelper = CreateUrlHelperWithRouteCollection(services, "/app");

            // Act
            var url = urlHelper.Link(
                null,
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
            var services = CreateServices();
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
            var services = CreateServices();
            var routeCollection = GetRouter(services, "MyRouteName", "any/url");
            var urlHelper = CreateUrlHelper("myhost", "https", routeCollection);

            // Act
            var url = urlHelper.Link(
                "namedroute",
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
            var services = CreateServices();
            var routeBuilder = CreateRouteBuilder(services);

            routeBuilder.MapRoute(
                "default",
                "{first}/{controller}/{action}",
                new { second = "default", controller = "default", action = "default" });

            var actionContext = new ActionContext()
            {
                HttpContext = new DefaultHttpContext()
                {
                    RequestServices = services,
                },
            };

            actionContext.RouteData = new RouteData();
            actionContext.RouteData.Values.Add("first", "a");
            actionContext.RouteData.Values.Add("controller", "Store");
            actionContext.RouteData.Values.Add("action", "Buy");
            actionContext.RouteData.Routers.Add(routeBuilder.Build());

            var urlHelper = CreateUrlHelper(actionContext);

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
            // Arrange
            var services = CreateServices();
            var routeBuilder = CreateRouteBuilder(services);

            routeBuilder.MapRoute(
                "default",
                "{first}/{second}/{controller}/{action}",
                new { second = "default", controller = "default", action = "default" });

            var actionContext = new ActionContext()
            {
                HttpContext = new DefaultHttpContext()
                {
                    RequestServices = services,
                },
            };

            actionContext.RouteData = new RouteData();
            actionContext.RouteData.Values.Add("first", "a");
            actionContext.RouteData.Values.Add("second", "x");
            actionContext.RouteData.Values.Add("controller", "Store");
            actionContext.RouteData.Values.Add("action", "Buy");
            actionContext.RouteData.Routers.Add(routeBuilder.Build());

            var urlHelper = CreateUrlHelper(actionContext);

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
            // Arrange
            var services = CreateServices();
            var routeBuilder = CreateRouteBuilder(services);

            routeBuilder.MapRoute(
                "default",
                "{first}/{controller}/{action}",
                new { second = "default", controller = "default", action = "default" });

            var actionContext = new ActionContext()
            {
                HttpContext = new DefaultHttpContext()
                {
                    RequestServices = services,
                },
            };

            actionContext.RouteData = new RouteData();
            actionContext.RouteData.Values.Add("first", "a");
            actionContext.RouteData.Values.Add("controller", "Store");
            actionContext.RouteData.Values.Add("action", "Buy");
            actionContext.RouteData.Routers.Add(routeBuilder.Build());

            var urlHelper = CreateUrlHelper(actionContext);

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

        public static TheoryData GeneratePathFromRoute_HandlesLeadingAndTrailingSlashesData =>
            new TheoryData<string, string, string>
            {
                {  null, "", "/" },
                {  null, "/", "/"  },
                {  null, "Hello", "/Hello" },
                {  null, "/Hello", "/Hello" },
                { "/", "", "/" },
                { "/", "hello", "/hello" },
                { "/", "/hello", "/hello" },
                { "/hello", "", "/hello" },
                { "/hello/", "", "/hello/" },
                { "/hello", "/", "/hello/" },
                { "/hello/", "world", "/hello/world" },
                { "/hello/", "/world", "/hello/world" },
                { "/hello/", "/world 123", "/hello/world 123" },
                { "/hello/", "/world%20123", "/hello/world%20123" },
            };

        [Theory]
        [MemberData(nameof(GeneratePathFromRoute_HandlesLeadingAndTrailingSlashesData))]
        public void AppendPathAndFragment_HandlesLeadingAndTrailingSlashes(
            string appBase,
            string virtualPath,
            string expected)
        {
            // Arrange
            var router = Mock.Of<IRouter>();
            var pathData = new VirtualPathData(router, virtualPath)
            {
                VirtualPath = virtualPath
            };
            var urlHelper = CreateUrlHelper(appBase, router);
            var builder = new StringBuilder();

            // Act
            urlHelper.AppendPathAndFragment(builder, pathData, string.Empty);

            // Assert
            Assert.Equal(expected, builder.ToString());
        }

        [Theory]
        [MemberData(nameof(GeneratePathFromRoute_HandlesLeadingAndTrailingSlashesData))]
        public void AppendPathAndFragment_AppendsFragments(
            string appBase,
            string virtualPath,
            string expected)
        {
            // Arrange
            var fragmentValue = "fragment-value";
            expected += $"#{fragmentValue}";
            var router = Mock.Of<IRouter>();
            var pathData = new VirtualPathData(router, virtualPath)
            {
                VirtualPath = virtualPath
            };
            var urlHelper = CreateUrlHelper(appBase, router);
            var builder = new StringBuilder();

            // Act
            urlHelper.AppendPathAndFragment(builder, pathData, fragmentValue);

            // Assert
            Assert.Equal(expected, builder.ToString());
        }

        [Theory]
        [InlineData(null, null, null, "/", null, "/")]
        [InlineData(null, null, null, "/Hello", null, "/Hello")]
        [InlineData(null, null, null, "Hello", null, "/Hello")]
        [InlineData("/", null, null, "", null, "/")]
        [InlineData("/hello/", null, null, "/world", null, "/hello/world")]
        [InlineData("/hello/", "https", "myhost", "/world", "fragment-value", "https://myhost/hello/world#fragment-value")]
        public void GenerateUrl_FastAndSlowPathsReturnsExpected(
            string appBase,
            string protocol,
            string host,
            string virtualPath,
            string fragment,
            string expected)
        {
            // Arrange
            var router = Mock.Of<IRouter>();
            var pathData = new VirtualPathData(router, virtualPath)
            {
                VirtualPath = virtualPath
            };
            var urlHelper = CreateUrlHelper(appBase, router);

            // Act
            var url = urlHelper.GenerateUrl(protocol, host, pathData, fragment);

            // Assert
            Assert.Equal(expected, url);
        }

        [Fact]
        public void GetUrlHelper_ReturnsSameInstance_IfAlreadyPresent()
        {
            // Arrange
            var expectedUrlHelper = CreateUrlHelper();
            var httpContext = new Mock<HttpContext>();
            var mockItems = new Dictionary<object, object>
            {
                { typeof(IUrlHelper), expectedUrlHelper }
            };
            httpContext.Setup(h => h.Items).Returns(mockItems);

            var actionContext = CreateActionContext(httpContext.Object, Mock.Of<IRouter>());
            var urlHelperFactory = new UrlHelperFactory();

            // Act
            var urlHelper = urlHelperFactory.GetUrlHelper(actionContext);

            // Assert
            Assert.Same(expectedUrlHelper, urlHelper);
        }

        [Fact]
        public void GetUrlHelper_CreatesNewInstance_IfNotAlreadyPresent()
        {
            // Arrange
            var httpContext = new Mock<HttpContext>();
            httpContext.Setup(h => h.Items).Returns(new Dictionary<object, object>());

            var actionContext = CreateActionContext(httpContext.Object, Mock.Of<IRouter>());
            var urlHelperFactory = new UrlHelperFactory();

            // Act
            var urlHelper = urlHelperFactory.GetUrlHelper(actionContext);

            // Assert
            Assert.NotNull(urlHelper);
            Assert.Same(urlHelper, actionContext.HttpContext.Items[typeof(IUrlHelper)] as IUrlHelper);
        }

        [Fact]
        public void GetUrlHelper_CreatesNewInstance_IfExpectedTypeIsNotPresent()
        {
            // Arrange
            var httpContext = new Mock<HttpContext>();
            var mockItems = new Dictionary<object, object>
            {
                { typeof(IUrlHelper), null }
            };
            httpContext.Setup(h => h.Items).Returns(mockItems);

            var actionContext = CreateActionContext(httpContext.Object, Mock.Of<IRouter>());
            var urlHelperFactory = new UrlHelperFactory();

            // Act
            var urlHelper = urlHelperFactory.GetUrlHelper(actionContext);

            // Assert
            Assert.NotNull(urlHelper);
            Assert.Same(urlHelper, actionContext.HttpContext.Items[typeof(IUrlHelper)] as IUrlHelper);
        }

        [Fact]
        public void Page_WithName_Works()
        {
            // Arrange
            UrlRouteContext actual = null;
            var routeData = new RouteData
            {
                Values =
                {
                    { "page", "ambient-page" },
                }
            };
            var actionContext = new ActionContext
            {
                RouteData = routeData,
            };
            var urlHelper = CreateMockUrlHelper(actionContext);
            urlHelper.Setup(h => h.RouteUrl(It.IsAny<UrlRouteContext>()))
                .Callback((UrlRouteContext context) => actual = context);

            // Act
            urlHelper.Object.Page("/TestPage");

            // Assert
            urlHelper.Verify();
            Assert.NotNull(actual);
            Assert.Null(actual.RouteName);
            Assert.Collection(Assert.IsType<RouteValueDictionary>(actual.Values),
                value =>
                {
                    Assert.Equal("page", value.Key);
                    Assert.Equal("/TestPage", value.Value);
                });
            Assert.Null(actual.Host);
            Assert.Null(actual.Protocol);
            Assert.Null(actual.Fragment);
        }

        public static TheoryData Page_WithNameAndRouteValues_WorksData
        {
            get => new TheoryData<object>
            {
                { new { id = 10 } },
                {
                    new Dictionary<string, object>
                    {
                        ["id"] = 10,
                    }
                },
                {
                    new RouteValueDictionary
                    {
                        ["id"] = 10,
                    }
                },
            };
        }

        [Theory]
        [MemberData(nameof(Page_WithNameAndRouteValues_WorksData))]
        public void Page_WithNameAndRouteValues_Works(object values)
        {
            // Arrange
            UrlRouteContext actual = null;
            var urlHelper = CreateMockUrlHelper();
            urlHelper.Setup(h => h.RouteUrl(It.IsAny<UrlRouteContext>()))
                .Callback((UrlRouteContext context) => actual = context);

            // Act
            urlHelper.Object.Page("/TestPage", values);

            // Assert
            urlHelper.Verify();
            Assert.NotNull(actual);
            Assert.Null(actual.RouteName);
            Assert.Collection(Assert.IsType<RouteValueDictionary>(actual.Values),
                value =>
                {
                    Assert.Equal("id", value.Key);
                    Assert.Equal(10, value.Value);
                },
                value =>
                {
                    Assert.Equal("page", value.Key);
                    Assert.Equal("/TestPage", value.Value);
                });
            Assert.Null(actual.Host);
            Assert.Null(actual.Protocol);
            Assert.Null(actual.Fragment);
        }

        [Fact]
        public void Page_WithNameRouteValuesAndProtocol_Works()
        {
            // Arrange
            UrlRouteContext actual = null;
            var urlHelper = CreateMockUrlHelper();
            urlHelper.Setup(h => h.RouteUrl(It.IsAny<UrlRouteContext>()))
                .Callback((UrlRouteContext context) => actual = context);

            // Act
            urlHelper.Object.Page("/TestPage", pageHandler: null, values: new { id = 13 }, protocol: "https");

            // Assert
            urlHelper.Verify();
            Assert.NotNull(actual);
            Assert.Null(actual.RouteName);
            Assert.Collection(Assert.IsType<RouteValueDictionary>(actual.Values),
                value =>
                {
                    Assert.Equal("id", value.Key);
                    Assert.Equal(13, value.Value);
                },
                value =>
                {
                    Assert.Equal("page", value.Key);
                    Assert.Equal("/TestPage", value.Value);
                });
            Assert.Equal("https", actual.Protocol);
            Assert.Null(actual.Host);
            Assert.Null(actual.Fragment);
        }

        [Fact]
        public void Page_WithNameRouteValuesProtocolAndHost_Works()
        {
            // Arrange
            UrlRouteContext actual = null;
            var urlHelper = CreateMockUrlHelper();
            urlHelper.Setup(h => h.RouteUrl(It.IsAny<UrlRouteContext>()))
                .Callback((UrlRouteContext context) => actual = context);

            // Act
            urlHelper.Object.Page("/TestPage", pageHandler: null, values: new { id = 13 }, protocol: "https", host: "mytesthost");

            // Assert
            urlHelper.Verify();
            Assert.NotNull(actual);
            Assert.Null(actual.RouteName);
            Assert.Collection(Assert.IsType<RouteValueDictionary>(actual.Values),
                value =>
                {
                    Assert.Equal("id", value.Key);
                    Assert.Equal(13, value.Value);
                },
                value =>
                {
                    Assert.Equal("page", value.Key);
                    Assert.Equal("/TestPage", value.Value);
                });
            Assert.Equal("https", actual.Protocol);
            Assert.Equal("mytesthost", actual.Host);
            Assert.Null(actual.Fragment);
        }

        [Fact]
        public void Page_WithNameRouteValuesProtocolHostAndFragment_Works()
        {
            // Arrange
            UrlRouteContext actual = null;
            var urlHelper = CreateMockUrlHelper();
            urlHelper.Setup(h => h.RouteUrl(It.IsAny<UrlRouteContext>()))
                .Callback((UrlRouteContext context) => actual = context);

            // Act
            urlHelper.Object.Page("/TestPage", "test-handler", new { id = 13 }, "https", "mytesthost", "#toc");

            // Assert
            urlHelper.Verify();
            Assert.NotNull(actual);
            Assert.Null(actual.RouteName);
            Assert.Collection(Assert.IsType<RouteValueDictionary>(actual.Values),
                value =>
                {
                    Assert.Equal("id", value.Key);
                    Assert.Equal(13, value.Value);
                },
                value =>
                {
                    Assert.Equal("page", value.Key);
                    Assert.Equal("/TestPage", value.Value);
                },
                value =>
                {
                    Assert.Equal("handler", value.Key);
                    Assert.Equal("test-handler", value.Value);
                });
            Assert.Equal("https", actual.Protocol);
            Assert.Equal("mytesthost", actual.Host);
            Assert.Equal("#toc", actual.Fragment);
        }

        [Fact]
        public void Page_UsesAmbientRouteValue_WhenPageIsNull()
        {
            // Arrange
            UrlRouteContext actual = null;
            var routeData = new RouteData
            {
                Values =
                {
                    { "page", "ambient-page" },
                }
            };
            var actionContext = new ActionContext
            {
                RouteData = routeData,
            };

            var urlHelper = CreateMockUrlHelper(actionContext);
            urlHelper.Setup(h => h.RouteUrl(It.IsAny<UrlRouteContext>()))
                .Callback((UrlRouteContext context) => actual = context);

            // Act
            string page = null;
            urlHelper.Object.Page(page, new { id = 13 });

            // Assert
            urlHelper.Verify();
            Assert.NotNull(actual);
            Assert.Null(actual.RouteName);
            Assert.Collection(Assert.IsType<RouteValueDictionary>(actual.Values),
                value =>
                {
                    Assert.Equal("id", value.Key);
                    Assert.Equal(13, value.Value);
                },
                value =>
                {
                    Assert.Equal("page", value.Key);
                    Assert.Equal("ambient-page", value.Value);
                });
        }

        [Fact]
        public void Page_SetsHandlerToNull_IfValueIsNotSpecifiedInRouteValues()
        {
            // Arrange
            UrlRouteContext actual = null;
            var routeData = new RouteData
            {
                Values =
                {
                    { "page", "ambient-page" },
                    { "handler", "ambient-handler" },
                }
            };
            var actionContext = new ActionContext
            {
                RouteData = routeData,
            };

            var urlHelper = CreateMockUrlHelper(actionContext);
            urlHelper.Setup(h => h.RouteUrl(It.IsAny<UrlRouteContext>()))
                .Callback((UrlRouteContext context) => actual = context);

            // Act
            string page = null;
            urlHelper.Object.Page(page, new { id = 13 });

            // Assert
            urlHelper.Verify();
            Assert.NotNull(actual);
            Assert.Null(actual.RouteName);
            Assert.Collection(Assert.IsType<RouteValueDictionary>(actual.Values),
                value =>
                {
                    Assert.Equal("id", value.Key);
                    Assert.Equal(13, value.Value);
                },
                value =>
                {
                    Assert.Equal("page", value.Key);
                    Assert.Equal("ambient-page", value.Value);
                },
                value =>
                {
                    Assert.Equal("handler", value.Key);
                    Assert.Null(value.Value);
                });
        }

        [Fact]
        public void Page_UsesExplicitlySpecifiedHandlerValue()
        {
            // Arrange
            UrlRouteContext actual = null;
            var routeData = new RouteData
            {
                Values =
                {
                    { "page", "ambient-page" },
                    { "handler", "ambient-handler" },
                }
            };
            var actionContext = new ActionContext
            {
                RouteData = routeData,
            };

            var urlHelper = CreateMockUrlHelper(actionContext);
            urlHelper.Setup(h => h.RouteUrl(It.IsAny<UrlRouteContext>()))
                .Callback((UrlRouteContext context) => actual = context);

            // Act
            string page = null;
            urlHelper.Object.Page(page, "exact-handler", new { handler = "route-value-handler" });

            // Assert
            urlHelper.Verify();
            Assert.NotNull(actual);
            Assert.Null(actual.RouteName);
            Assert.Collection(Assert.IsType<RouteValueDictionary>(actual.Values),
                value =>
                {
                    Assert.Equal("handler", value.Key);
                    Assert.Equal("exact-handler", value.Value);
                },
                value =>
                {
                    Assert.Equal("page", value.Key);
                    Assert.Equal("ambient-page", value.Value);
                });
        }

        [Fact]
        public void Page_UsesValueFromRouteValueIfPageHandlerIsNotExplicitySpecified()
        {
            // Arrange
            UrlRouteContext actual = null;
            var routeData = new RouteData
            {
                Values =
                {
                    { "page", "ambient-page" },
                    { "handler", "ambient-handler" },
                }
            };
            var actionContext = new ActionContext
            {
                RouteData = routeData,
            };

            var urlHelper = CreateMockUrlHelper(actionContext);
            urlHelper.Setup(h => h.RouteUrl(It.IsAny<UrlRouteContext>()))
                .Callback((UrlRouteContext context) => actual = context);

            // Act
            string page = null;
            urlHelper.Object.Page(page, pageHandler: null, values: new { handler = "route-value-handler" });

            // Assert
            urlHelper.Verify();
            Assert.NotNull(actual);
            Assert.Null(actual.RouteName);
            Assert.Collection(Assert.IsType<RouteValueDictionary>(actual.Values),
                value =>
                {
                    Assert.Equal("handler", value.Key);
                    Assert.Equal("route-value-handler", value.Value);
                },
                value =>
                {
                    Assert.Equal("page", value.Key);
                    Assert.Equal("ambient-page", value.Value);
                });
        }

        [Theory]
        [InlineData("Sibling", "/Dir1/Dir2/Sibling")]
        [InlineData("Dir3/Sibling", "/Dir1/Dir2/Dir3/Sibling")]
        [InlineData("Dir4/Dir5/Index", "/Dir1/Dir2/Dir4/Dir5/Index")]
        public void Page_CalculatesPathRelativeToViewEnginePath_WhenNotRooted(string pageName, string expected)
        {
            // Arrange
            UrlRouteContext actual = null;
            var routeData = new RouteData();
            var actionContext = GetActionContextForPage("/Dir1/Dir2/About");

            var urlHelper = CreateMockUrlHelper(actionContext);
            urlHelper.Setup(h => h.RouteUrl(It.IsAny<UrlRouteContext>()))
                .Callback((UrlRouteContext context) => actual = context);

            // Act
            urlHelper.Object.Page(pageName);

            // Assert
            urlHelper.Verify();
            Assert.NotNull(actual);
            Assert.Null(actual.RouteName);
            Assert.Collection(Assert.IsType<RouteValueDictionary>(actual.Values),
                value =>
                {
                    Assert.Equal("page", value.Key);
                    Assert.Equal(expected, value.Value);
                });
        }

        [Fact]
        public void Page_CalculatesPathRelativeToViewEnginePath_ForIndexPagePaths()
        {
            // Arrange
            var expected = "/Dir1/Dir2/Sibling";
            UrlRouteContext actual = null;
            var actionContext = GetActionContextForPage("/Dir1/Dir2/");

            var urlHelper = CreateMockUrlHelper(actionContext);
            urlHelper.Setup(h => h.RouteUrl(It.IsAny<UrlRouteContext>()))
                .Callback((UrlRouteContext context) => actual = context);

            // Act
            urlHelper.Object.Page("Sibling");

            // Assert
            urlHelper.Verify();
            Assert.NotNull(actual);
            Assert.Null(actual.RouteName);
            Assert.Collection(Assert.IsType<RouteValueDictionary>(actual.Values),
                value =>
                {
                    Assert.Equal("page", value.Key);
                    Assert.Equal(expected, value.Value);
                });
        }

        [Fact]
        public void Page_CalculatesPathRelativeToViewEnginePath_WhenNotRooted_ForPageAtRoot()
        {
            // Arrange
            var expected = "/SiblingName";
            UrlRouteContext actual = null;
            var routeData = new RouteData();
            var actionContext = new ActionContext
            {
                ActionDescriptor = new ActionDescriptor
                {
                    RouteValues = new Dictionary<string, string>
                    {
                        { "page", "/Home" },
                    },
                },
                RouteData = new RouteData
                {
                    Values =
                    {
                        [ "page" ] = "/Home"
                    },
                },
            };

            var urlHelper = CreateMockUrlHelper(actionContext);
            urlHelper.Setup(h => h.RouteUrl(It.IsAny<UrlRouteContext>()))
                .Callback((UrlRouteContext context) => actual = context);

            // Act
            urlHelper.Object.Page("SiblingName");

            // Assert
            urlHelper.Verify();
            Assert.NotNull(actual);
            Assert.Null(actual.RouteName);
            Assert.Collection(Assert.IsType<RouteValueDictionary>(actual.Values),
                value =>
                {
                    Assert.Equal("page", value.Key);
                    Assert.Equal(expected, value.Value);
                });
        }

        [Fact]
        public void Page_Throws_IfRouteValueDoesNotIncludePageKey()
        {
            // Arrange
            var expected = "SiblingName";
            UrlRouteContext actual = null;
            var routeData = new RouteData();
            var actionContext = new ActionContext
            {
                RouteData = new RouteData(),
            };

            var urlHelper = CreateMockUrlHelper(actionContext);
            urlHelper.Setup(h => h.RouteUrl(It.IsAny<UrlRouteContext>()))
                .Callback((UrlRouteContext context) => actual = context);

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => urlHelper.Object.Page(expected));
            Assert.Equal($"The relative page path '{expected}' can only be used while executing a Razor Page. " +
                "Specify a root relative path with a leading '/' to generate a URL outside of a Razor Page.", ex.Message);
        }

        private static Mock<IUrlHelper> CreateMockUrlHelper(ActionContext context = null)
        {
            if (context == null)
            {
                context = GetActionContextForPage("/Page");
            }

            var urlHelper = new Mock<IUrlHelper>();
            urlHelper.SetupGet(h => h.ActionContext)
                .Returns(context);
            return urlHelper;
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

        private static ActionContext CreateActionContext(HttpContext context)
        {
            return CreateActionContext(context, (new Mock<IRouter>()).Object);
        }

        private static ActionContext CreateActionContext(HttpContext context, IRouter router)
        {
            var routeData = new RouteData();
            routeData.Routers.Add(router);

            return new ActionContext(context, routeData, new ActionDescriptor());
        }

        private static UrlHelper CreateUrlHelper()
        {
            var services = CreateServices();
            var context = CreateHttpContext(services, string.Empty);
            var actionContext = CreateActionContext(context);

            return new UrlHelper(actionContext);
        }

        private static UrlHelper CreateUrlHelper(ActionContext context)
        {
            return new UrlHelper(context);
        }

        private static UrlHelper CreateUrlHelper(string host)
        {
            var services = CreateServices();
            var context = CreateHttpContext(services, string.Empty);
            context.Request.Host = new HostString(host);

            var actionContext = CreateActionContext(context);

            return new UrlHelper(actionContext);
        }

        private static UrlHelper CreateUrlHelper(string host, string protocol, IRouter router)
        {
            var services = CreateServices();
            var context = CreateHttpContext(services, string.Empty);
            context.Request.Host = new HostString(host);
            context.Request.Scheme = protocol;

            var actionContext = CreateActionContext(context, router);

            return new UrlHelper(actionContext);
        }

        private static TestUrlHelper CreateUrlHelper(string appBase, IRouter router)
        {
            var services = CreateServices();
            var context = CreateHttpContext(services, appBase);
            var actionContext = CreateActionContext(context, router);

            return new TestUrlHelper(actionContext);
        }

        private static UrlHelper CreateUrlHelperWithRouteCollection(
            IServiceProvider services,
            string appPrefix)
        {
            var routeCollection = GetRouter(services);
            return CreateUrlHelper(appPrefix, routeCollection);
        }

        private static IRouter GetRouter(IServiceProvider services)
        {
            return GetRouter(services, "mockRoute", "/mockTemplate");
        }

        private static IServiceProvider CreateServices()
        {
            var services = new ServiceCollection();
            services.AddOptions();
            services.AddLogging();
            services.AddRouting();
            services
                .AddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>()
                .AddSingleton<UrlEncoder>(UrlEncoder.Default);

            return services.BuildServiceProvider();
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

        private static IRouter GetRouter(
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

        private static ActionContext GetActionContextForPage(string page)
        {
            return new ActionContext
            {
                ActionDescriptor = new ActionDescriptor
                {
                    RouteValues = new Dictionary<string, string>
                    {
                        { "page", page },
                    },
                },
                RouteData = new RouteData
                {
                    Values =
                    {
                        [ "page" ] = page
                    },
                },
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

        private class TestUrlHelper : UrlHelper
        {
            public TestUrlHelper(ActionContext actionContext) :
                base(actionContext)
            {

            }
            public new string GenerateUrl(string protocol, string host, VirtualPathData pathData, string fragment)
            {
                return base.GenerateUrl(
                    protocol,
                    host,
                    pathData,
                    fragment);
            }
        }
    }
}