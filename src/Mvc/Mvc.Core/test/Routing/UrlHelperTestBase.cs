// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Microsoft.AspNetCore.Mvc.Routing;

public abstract class UrlHelperTestBase
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
        var urlHelper = CreateUrlHelper(appRoot);

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
        var urlHelper = CreateUrlHelper(appRoot);

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
        var helper = CreateUrlHelper(appRoot: string.Empty, host: "www.mysite.com", protocol: null);

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
        var helper = CreateUrlHelper(appRoot: string.Empty, host: "www.mysite.com", protocol: null);

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
        var helper = CreateUrlHelper(appRoot: string.Empty, host: "www.mysite.com", protocol: null);

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
        var helper = CreateUrlHelper(appRoot: string.Empty, host: "www.mysite.com", protocol: null);

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
        var helper = CreateUrlHelper(appRoot: string.Empty, host: "www.mysite.com", protocol: null);

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
        var helper = CreateUrlHelper(appRoot: string.Empty, host: "www.mysite.com", protocol: null);

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
        var helper = CreateUrlHelper(appRoot: string.Empty, host: "www.mysite.com", protocol: null);

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
        var helper = CreateUrlHelper(appRoot: string.Empty, host: "www.mysite.com", protocol: null);

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
        var helper = CreateUrlHelper(appRoot: string.Empty, host: "www.mysite.com", protocol: null);

        // Act
        var result = helper.IsLocalUrl(url);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("\n")]
    [InlineData("\\n")]
    [InlineData("/\n")]
    [InlineData("~\n")]
    [InlineData("~/\n")]
    public void IsLocalUrl_RejectsUrlWithNewLineAtStart(string url)
    {
        // Arrange
        var helper = CreateUrlHelper(appRoot: string.Empty, host: "www.mysite.com", protocol: null);

        // Act
        var result = helper.IsLocalUrl(url);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("/\r\nsomepath")]
    [InlineData("~/\r\nsomepath")]
    [InlineData("/some\npath")]
    [InlineData("~/some\npath")]
    [InlineData("\\path\b")]
    [InlineData("~\\path\b")]
    public void IsLocalUrl_RejectsUrlWithControlCharacters(string url)
    {
        // Arrange
        var helper = CreateUrlHelper(appRoot: string.Empty, host: "www.mysite.com", protocol: null);

        // Act
        var result = helper.IsLocalUrl(url);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void RouteUrlWithDictionary()
    {
        // Arrange
        var urlHelper = CreateUrlHelperWithDefaultRoutes();

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
        var urlHelper = CreateUrlHelperWithDefaultRoutes();

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
        var urlHelper = CreateUrlHelperWithDefaultRoutes();

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
        var urlHelper = CreateUrlHelperWithDefaultRoutes();

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
        var urlHelper = CreateUrlHelperWithDefaultRoutes();

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
        var urlHelper = CreateUrlHelperWithDefaultRoutes();

        // Act
        var url = urlHelper.RouteUrl(new { Action = "newaction", Controller = "home2", id = "someid" });

        // Assert
        Assert.Equal("/app/home2/newaction/someid", url);
    }

    [Fact]
    public void RouteUrlWithProtocol()
    {
        // Arrange
        var urlHelper = CreateUrlHelperWithDefaultRoutes();

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
        var urlHelper = CreateUrlHelperWithDefaultRoutes();

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
    public void RouteUrl_GeneratesUrl_WithRouteName_UsingDefaultValues_WhenExplicitOrAmbientValues_NotPresent()
    {
        // Arrange
        var urlHelper = CreateUrlHelperWithDefaultRoutes();

        // Act
        var url = urlHelper.RouteUrl(
            routeName: "OrdersApi",
            values: new { id = "500" });

        // Assert
        Assert.Equal("/app/api/orders/500", url);
    }

    [Fact]
    public void RouteUrl_WithRouteName_DoesNotGenerateUrl_WhenRequiredValueForParameter_NotPresent()
    {
        // Arrange
        var urlHelper = CreateUrlHelperWithDefaultRoutes();

        // Act
        var url = urlHelper.RouteUrl(
            routeName: "OrdersApi",
            values: new { });

        // Assert
        Assert.Null(url);
    }

    [Fact]
    public void RouteUrlWithRouteNameAndDictionary()
    {
        // Arrange
        var urlHelper = CreateUrlHelperWithDefaultRoutes();

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
        var urlHelper = CreateUrlHelperWithDefaultRoutes();

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
        var urlHelper = CreateUrlHelperWithDefaultRoutes();

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
        var urlHelper = CreateUrlHelperWithDefaultRoutes();

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
        var urlHelper = CreateUrlHelperWithDefaultRoutes();

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
        var urlHelper = CreateUrlHelperWithDefaultRoutes();

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
        var urlHelper = CreateUrlHelperWithDefaultRoutes();

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
        var urlHelper = CreateUrlHelperWithDefaultRoutes();

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
        var urlHelper = CreateUrlHelperWithDefaultRoutes();

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
        var urlHelper = CreateUrlHelperWithDefaultRoutes();

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
        var urlHelper = CreateUrlHelperWithDefaultRoutes();

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
    public void LinkWithNullRouteNameGivenExtraEndpointWithNoRouteNameAndNoRequiredValues_ReturnsExpectedResult()
    {
        // Arrange
        var urlHelper = CreateUrlHelperWithDefaultRoutes(
            "/app",
            host: null,
            protocol: null,
            routeName: null,
            template: "any/url");

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

    // Regression test for https://github.com/dotnet/aspnetcore/issues/35592
    [Fact]
    public void LinkWithNullRouteNameGivenExtraEndpointWithRouteNameAndNoRequiredValues_ReturnsExpectedResult()
    {
        // Arrange
        var urlHelper = CreateUrlHelperWithDefaultRoutes(
            "/app",
            host: null,
            protocol: null,
            routeName: "MyRouteName",
            template: "any/url");

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
    public void RouteUrlWithRouteNameAndDefaults()
    {
        // Arrange
        var urlHelper = CreateUrlHelperWithDefaultRoutes(
            "/app",
            host: null,
            protocol: null,
            routeName: "MyRouteName",
            template: "any/url");

        // Act
        var url = urlHelper.RouteUrl("MyRouteName");

        // Assert
        Assert.Equal("/app/any/url", url);
    }

    [Fact]
    public void LinkWithDefaultsAndNullRouteValues_ReturnsExpectedResult()
    {
        // Arrange
        var urlHelper = CreateUrlHelperWithDefaultRoutes(
            "/app",
            host: null,
            protocol: null,
            routeName: "MyRouteName",
            template: "any/url");

        // Act
        var url = urlHelper.Link("MyRouteName", null);

        // Assert
        Assert.Equal("http://localhost/app/any/url", url);
    }

    [Fact]
    public void LinkWithCustomHostAndProtocol_ReturnsExpectedResult()
    {
        // Arrange
        var urlHelper = CreateUrlHelperWithDefaultRoutes(
            string.Empty,
            "myhost",
            "https",
            routeName: "MyRouteName",
            template: "any/url");

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

    [Fact]
    public void GetUrlHelper_ReturnsSameInstance_IfAlreadyPresent()
    {
        // Arrange
        var expectedUrlHelper = CreateUrlHelper();
        var httpContext = new Mock<HttpContext>();
        httpContext.SetupGet(h => h.Features).Returns(new FeatureCollection());
        var mockItems = new Dictionary<object, object>
            {
                { typeof(IUrlHelper), expectedUrlHelper }
            };
        httpContext.Setup(h => h.Items).Returns(mockItems);

        var actionContext = CreateActionContext(httpContext.Object);
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
        httpContext.SetupGet(h => h.Features).Returns(new FeatureCollection());
        httpContext.Setup(h => h.Items).Returns(new Dictionary<object, object>());

        var actionContext = CreateActionContext(httpContext.Object);
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
        httpContext.SetupGet(h => h.Features).Returns(new FeatureCollection());
        var mockItems = new Dictionary<object, object>
            {
                { typeof(IUrlHelper), null }
            };
        httpContext.Setup(h => h.Items).Returns(mockItems);

        var actionContext = CreateActionContext(httpContext.Object);
        var urlHelperFactory = new UrlHelperFactory();

        // Act
        var urlHelper = urlHelperFactory.GetUrlHelper(actionContext);

        // Assert
        Assert.NotNull(urlHelper);
        Assert.Same(urlHelper, actionContext.HttpContext.Items[typeof(IUrlHelper)] as IUrlHelper);
    }

    // Regression test for https://github.com/aspnet/Mvc/issues/2859
    [Fact]
    public void Action_RouteValueInvalidation_DoesNotAffectActionAndController()
    {
        // Arrange
        var urlHelper = CreateUrlHelper(
            appRoot: "",
            host: null,
            protocol: null,
            routeName: "default",
            template: "{first}/{controller}/{action}",
            defaults: new { second = "default", controller = "default", action = "default" },
            // Emulate ActionEndpointFactory.AddConventionalLinkGenerationRoute().
            // The "controller" and "action" keys are defined automatically by ControllerActionDescriptorBuilder.AddRouteValues().
            requiredValues: new { controller = RoutePattern.RequiredValueAny, action = RoutePattern.RequiredValueAny });

        var routeData = urlHelper.ActionContext.RouteData;
        routeData.Values.Add("first", "a");
        routeData.Values.Add("controller", "Store");
        routeData.Values.Add("action", "Buy");

        urlHelper.ActionContext.HttpContext.Features.Set<IRouteValuesFeature>(new RouteValuesFeature
        {
            RouteValues = routeData.Values
        });

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

    // Regression test for https://github.com/aspnet/Mvc/issues/2859
    [Fact]
    public void Action_RouteValueInvalidation_AffectsOtherRouteValues()
    {
        // Arrange
        var urlHelper = CreateUrlHelper(
            appRoot: "",
            host: null,
            protocol: null,
            routeName: "default",
            template: "{first}/{second}/{controller}/{action}",
            defaults: new { second = "default", controller = "default", action = "default" },
            // Emulate ActionEndpointFactory.AddConventionalLinkGenerationRoute().
            // The "controller" and "action" keys are defined automatically by ControllerActionDescriptorBuilder.AddRouteValues().
            requiredValues: new { controller = RoutePattern.RequiredValueAny, action = RoutePattern.RequiredValueAny });

        var routeData = urlHelper.ActionContext.RouteData;
        routeData.Values.Add("first", "a");
        routeData.Values.Add("second", "x");
        routeData.Values.Add("controller", "Store");
        routeData.Values.Add("action", "Buy");

        urlHelper.ActionContext.HttpContext.Features.Set<IRouteValuesFeature>(new RouteValuesFeature
        {
            RouteValues = routeData.Values
        });

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

    // Regression test for https://github.com/aspnet/Mvc/issues/2859
    [Fact]
    public void Action_RouteValueInvalidation_DoesNotAffectActionAndController_ActionPassedInRouteValues()
    {
        // Arrange
        var urlHelper = CreateUrlHelper(
            appRoot: "",
            host: null,
            protocol: null,
            routeName: "default",
            template: "{first}/{controller}/{action}",
            defaults: new { second = "default", controller = "default", action = "default" },
            // Emulate ActionEndpointFactory.AddConventionalLinkGenerationRoute().
            // The "controller" and "action" keys are defined automatically by ControllerActionDescriptorBuilder.AddRouteValues().
            requiredValues: new { controller = RoutePattern.RequiredValueAny, action = RoutePattern.RequiredValueAny });

        var routeData = urlHelper.ActionContext.RouteData;
        routeData.Values.Add("first", "a");
        routeData.Values.Add("controller", "Store");
        routeData.Values.Add("action", "Buy");

        urlHelper.ActionContext.HttpContext.Features.Set<IRouteValuesFeature>(new RouteValuesFeature
        {
            RouteValues = routeData.Values
        });

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

    [Fact]
    public void ActionLink_ReturnsAbsoluteUrlToAction()
    {
        // Arrange
        var urlHelper = CreateUrlHelperWithDefaultRoutes();

        // Act
        var url = urlHelper.ActionLink("contact", "home");

        // Assert
        Assert.Equal("http://localhost/app/home/contact", url);
    }

    [Fact]
    public void NoRouter_ErrorsWithFriendlyErrorMessage()
    {
        // Arrange
        var urlHelper = new UrlHelper(new ActionContext
        {
            RouteData = new RouteData(new RouteValueDictionary()),
            HttpContext = new DefaultHttpContext()
        });

        // Act
        var ex = Assert.Throws<InvalidOperationException>(() => urlHelper.ActionLink("contact", "home"));

        // Assert
        var expectedMessage = "Could not find an IRouter associated with the ActionContext. "
            + "If your application is using endpoint routing then you can get a IUrlHelperFactory with "
            + "dependency injection and use it to create a UrlHelper, or use Microsoft.AspNetCore.Routing.LinkGenerator.";

        Assert.Equal(expectedMessage, ex.Message);
    }

    protected abstract IServiceProvider CreateServices();

    protected abstract IUrlHelper CreateUrlHelper(ActionContext actionContext);

    protected abstract IUrlHelper CreateUrlHelperWithDefaultRoutes(
        string appRoot,
        string host,
        string protocol);

    protected abstract IUrlHelper CreateUrlHelperWithDefaultRoutes(
        string appRoot,
        string host,
        string protocol,
        string routeName,
        string template);

    protected abstract IUrlHelper CreateUrlHelper(
        string appRoot,
        string host,
        string protocol,
        string routeName,
        string template,
        object defaults,
        object requiredValues);

    protected virtual IUrlHelper CreateUrlHelper(string appRoot, string host, string protocol)
    {
        appRoot = string.IsNullOrEmpty(appRoot) ? string.Empty : appRoot;
        host = string.IsNullOrEmpty(host) ? "localhost" : host;

        var services = CreateServices();
        var httpContext = CreateHttpContext(services, appRoot, host, protocol);

        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        return CreateUrlHelper(actionContext);
    }

    protected virtual ActionContext CreateActionContext(HttpContext httpContext, RouteData routeData = null)
    {
        routeData = routeData ?? new RouteData();
        return new ActionContext(httpContext, routeData, new ActionDescriptor());
    }

    protected virtual HttpContext CreateHttpContext(
        IServiceProvider services,
        string appRoot,
        string host,
        string protocol)
    {
        appRoot = string.IsNullOrEmpty(appRoot) ? string.Empty : appRoot;
        host = string.IsNullOrEmpty(host) ? "localhost" : host;

        var context = new DefaultHttpContext();
        context.RequestServices = services;
        context.Request.PathBase = new PathString(appRoot);
        context.Request.Host = new HostString(host);
        context.Request.Scheme = protocol;
        return context;
    }

    protected IServiceCollection GetCommonServices()
    {
        var services = new ServiceCollection();
        services.AddOptions();
        services.AddLogging();
        services.AddRouting();
        services
            .AddSingleton<UrlEncoder>(UrlEncoder.Default);
        return services;
    }

    private IUrlHelper CreateUrlHelper(string appRoot = "")
    {
        return CreateUrlHelper(appRoot, host: null, protocol: null);
    }

    private IUrlHelper CreateUrlHelperWithDefaultRoutes()
    {
        return CreateUrlHelperWithDefaultRoutes(appRoot: "/app", host: null, protocol: null);
    }
}
