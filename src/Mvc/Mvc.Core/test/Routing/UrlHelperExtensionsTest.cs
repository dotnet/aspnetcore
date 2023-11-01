// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.InternalTesting;
using Moq;

namespace Microsoft.AspNetCore.Mvc.Routing;

public class UrlHelperExtensionsTest
{
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
    [ReplaceCulture("de-CH", "de-CH")]
    public void Page_UsesAmbientRouteValueAndInvariantCulture_WhenPageIsNotNull()
    {
        // Arrange
        UrlRouteContext actual = null;
        var routeData = new RouteData
        {
            Values =
                {
                    { "page", new DateTimeOffset(2018, 10, 31, 7, 37, 38, TimeSpan.FromHours(-7)) },
                }
        };
        var actionContext = new ActionContext
        {
            ActionDescriptor = new ActionDescriptor
            {
                RouteValues = new Dictionary<string, string>
                    {
                        { "page", "10/31/2018 07:37:38 -07:00" },
                    },
            },
            RouteData = routeData,
        };

        var urlHelper = CreateMockUrlHelper(actionContext);
        urlHelper.Setup(h => h.RouteUrl(It.IsAny<UrlRouteContext>()))
            .Callback((UrlRouteContext context) => actual = context);

        // Act
        urlHelper.Object.Page("New Page", new { id = 13 });

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
                Assert.Equal("10/31/New Page", value.Value);
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
    public void Page_UsesValueFromRouteValueIfPageHandlerIsNotExplicitlySpecified()
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
        Assert.Equal(
            $"The relative page path '{expected}' can only be used while executing a Razor Page. " +
            "Specify a root relative path with a leading '/' to generate a URL outside of a Razor Page. " +
            "If you are using LinkGenerator then you must provide the current HttpContext to use relative pages.",
            ex.Message);
    }

    [Fact]
    public void Page_UsesAreaValueFromRouteValueIfSpecified()
    {
        // Arrange
        UrlRouteContext actual = null;
        var routeData = new RouteData
        {
            Values =
                {
                    { "page", "ambient-page" },
                    { "area", "ambient-area" },
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
        urlHelper.Object.Page(page, values: new { area = "specified-area" });

        // Assert
        urlHelper.Verify();
        Assert.NotNull(actual);
        Assert.Null(actual.RouteName);
        Assert.Collection(Assert.IsType<RouteValueDictionary>(actual.Values).OrderBy(v => v.Key),
            value =>
            {
                Assert.Equal("area", value.Key);
                Assert.Equal("specified-area", value.Value);
            },
            value =>
            {
                Assert.Equal("page", value.Key);
                Assert.Equal("ambient-page", value.Value);
            });
    }

    [Fact]
    public void ActionLink_WithActionName_Works()
    {
        // Arrange
        var expectedAction = "TestAction";
        var expectedProtocol = "testprotocol://";
        var expectedHost = "www.example.com";
        UrlActionContext actual = null;

        var httpContext = new DefaultHttpContext
        {
            Request =
                {
                    Scheme = expectedProtocol,
                    Host = new HostString(expectedHost),
                }
        };
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var urlHelper = CreateMockUrlHelper(actionContext);
        urlHelper.Setup(h => h.Action(It.IsAny<UrlActionContext>()))
            .Callback((UrlActionContext context) => actual = context);

        // Act
        urlHelper.Object.ActionLink(expectedAction);

        // Assert
        urlHelper.Verify();
        Assert.NotNull(actual);
        Assert.Equal(expectedAction, actual.Action);
        Assert.Null(actual.Controller);
        Assert.Null(actual.Values);

        Assert.Equal(expectedProtocol, actual.Protocol);
        Assert.Equal(expectedHost, actual.Host);
    }

    [Fact]
    public void ActionLink_UsesSpecifiedProtocol()
    {
        // Arrange
        var expectedProtocol = "testprotocol://";
        var expectedHost = "www.example.com";
        UrlActionContext actual = null;

        var httpContext = new DefaultHttpContext
        {
            Request =
                {
                    Scheme = "http://",
                    Host = new HostString(expectedHost),
                }
        };
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var urlHelper = CreateMockUrlHelper(actionContext);
        urlHelper.Setup(h => h.Action(It.IsAny<UrlActionContext>()))
            .Callback((UrlActionContext context) => actual = context);

        // Act
        urlHelper.Object.ActionLink(protocol: expectedProtocol);

        // Assert
        urlHelper.Verify();
        Assert.NotNull(actual);
        Assert.Null(actual.Action);
        Assert.Null(actual.Controller);
        Assert.Null(actual.Values);

        Assert.Equal(expectedProtocol, actual.Protocol);
        Assert.Equal(expectedHost, actual.Host);
    }

    [Fact]
    public void ActionLink_UsesSpecifiedHost()
    {
        // Arrange
        var expectedProtocol = "testprotocol://";
        var expectedHost = "www.example.com";
        UrlActionContext actual = null;

        var httpContext = new DefaultHttpContext
        {
            Request =
                {
                    Scheme = expectedProtocol,
                    Host = new HostString("www.asp.net"),
                }
        };
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var urlHelper = CreateMockUrlHelper(actionContext);
        urlHelper.Setup(h => h.Action(It.IsAny<UrlActionContext>()))
            .Callback((UrlActionContext context) => actual = context);

        // Act
        urlHelper.Object.ActionLink(host: expectedHost);

        // Assert
        urlHelper.Verify();
        Assert.NotNull(actual);
        Assert.Null(actual.Action);
        Assert.Null(actual.Controller);
        Assert.Null(actual.Values);

        Assert.Equal(expectedProtocol, actual.Protocol);
        Assert.Equal(expectedHost, actual.Host);
    }

    [Fact]
    public void PageLink_WithPageName_Works()
    {
        // Arrange
        var expectedPage = "/TestPage";
        var expectedProtocol = "testprotocol://";
        var expectedHost = "www.example.com";
        UrlRouteContext actual = null;

        var actionContext = new ActionContext
        {
            HttpContext = new DefaultHttpContext
            {
                Request =
                    {
                        Scheme = expectedProtocol,
                        Host = new HostString(expectedHost),
                    }
            },
            RouteData = new RouteData
            {
                Values =
                    {
                        { "page", "ambient-page" },
                    }
            },
        };
        var urlHelper = CreateMockUrlHelper(actionContext);
        urlHelper.Setup(h => h.RouteUrl(It.IsAny<UrlRouteContext>()))
            .Callback((UrlRouteContext context) => actual = context);

        // Act
        urlHelper.Object.PageLink(expectedPage);

        // Assert
        urlHelper.Verify();
        Assert.NotNull(actual);
        Assert.Collection(Assert.IsType<RouteValueDictionary>(actual.Values),
            value =>
            {
                Assert.Equal("page", value.Key);
                Assert.Equal(expectedPage, value.Value);
            });
        Assert.Null(actual.RouteName);

        Assert.Equal(expectedProtocol, actual.Protocol);
        Assert.Equal(expectedHost, actual.Host);
    }

    [Fact]
    public void PageLink_UsesSpecifiedProtocol()
    {
        // Arrange
        var expectedProtocol = "testprotocol://";
        var expectedHost = "www.example.com";
        UrlRouteContext actual = null;

        var actionContext = new ActionContext
        {
            HttpContext = new DefaultHttpContext
            {
                Request =
                    {
                        Scheme = "http://",
                        Host = new HostString(expectedHost),
                    }
            },
            RouteData = new RouteData
            {
                Values =
                    {
                        { "page", "ambient-page" },
                    }
            },
        };
        var urlHelper = CreateMockUrlHelper(actionContext);
        urlHelper.Setup(h => h.RouteUrl(It.IsAny<UrlRouteContext>()))
            .Callback((UrlRouteContext context) => actual = context);

        // Act
        urlHelper.Object.PageLink(protocol: expectedProtocol);

        // Assert
        urlHelper.Verify();
        Assert.NotNull(actual);
        Assert.Collection(Assert.IsType<RouteValueDictionary>(actual.Values),
            value =>
            {
                Assert.Equal("page", value.Key);
                Assert.Equal("ambient-page", value.Value);
            });
        Assert.Null(actual.RouteName);

        Assert.Equal(expectedProtocol, actual.Protocol);
        Assert.Equal(expectedHost, actual.Host);
    }

    [Fact]
    public void PageLink_UsesSpecifiedHost()
    {
        // Arrange
        var expectedProtocol = "testprotocol://";
        var expectedHost = "www.example.com";
        UrlRouteContext actual = null;

        var actionContext = new ActionContext
        {
            HttpContext = new DefaultHttpContext
            {
                Request =
                    {
                        Scheme = expectedProtocol,
                        Host = new HostString("www.asp.net"),
                    }
            },
            RouteData = new RouteData
            {
                Values =
                    {
                        { "page", "ambient-page" },
                    }
            },
        };
        var urlHelper = CreateMockUrlHelper(actionContext);
        urlHelper.Setup(h => h.RouteUrl(It.IsAny<UrlRouteContext>()))
            .Callback((UrlRouteContext context) => actual = context);

        // Act
        urlHelper.Object.PageLink(host: expectedHost);

        // Assert
        urlHelper.Verify();
        Assert.NotNull(actual);
        Assert.Collection(Assert.IsType<RouteValueDictionary>(actual.Values),
            value =>
            {
                Assert.Equal("page", value.Key);
                Assert.Equal("ambient-page", value.Value);
            });
        Assert.Null(actual.RouteName);

        Assert.Equal(expectedProtocol, actual.Protocol);
        Assert.Equal(expectedHost, actual.Host);
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
}
