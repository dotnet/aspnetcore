// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Microsoft.AspNetCore.Mvc;

public class AcceptedAtActionResultTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("actionName")]
    public void Constructor_InitializesActionName(string actionName)
    {
        // Act
        var result = new AcceptedAtActionResult(actionName: actionName, controllerName: null, routeValues: null, value: null);

        // Assert
        Assert.Equal(actionName, result.ActionName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("controllerName")]
    public void Constructor_InitializesControllerName(string controllerName)
    {
        // Act
        var result = new AcceptedAtActionResult(actionName: null, controllerName: controllerName, routeValues: null, value: null);

        // Assert
        Assert.Equal(controllerName, result.ControllerName);
    }

    public static TheoryData<object, int> RouteValuesData()
    {
        return new TheoryData<object, int>()
            {
                { null, -1 },
                { "value", 1 },
                { new object(), 0 }
            };
    }

    [Theory]
    [MemberData(nameof(RouteValuesData))]
    public void Constructor_InitializesRouteValues(object routeValues, int expectedRouteValuesCount)
    {
        // Act
        var result = new AcceptedAtActionResult(actionName: null, controllerName: null, routeValues: routeValues, value: null);

        // Assert
        if (expectedRouteValuesCount == -1)
        {
            Assert.Null(result.RouteValues);
        }
        else
        {
            Assert.Equal(expectedRouteValuesCount, result.RouteValues.Count);
        }
    }

    public static TheoryData<object> ValuesData
    {
        get
        {
            return new TheoryData<object>
                {
                    null,
                    "Test string",
                    new object(),
                };
        }
    }

    [Theory]
    [MemberData(nameof(ValuesData))]
    public void Constructor_InitializesStatusCodeAndValue(object value)
    {
        // Arrange
        var url = "testAction";

        // Act
        var result = new AcceptedAtActionResult(
            actionName: url,
            controllerName: null,
            routeValues: null,
            value: value);

        // Assert
        Assert.Equal(StatusCodes.Status202Accepted, result.StatusCode);
        Assert.Same(value, result.Value);
    }

    [Fact]
    public void UrlHelper_Get_ReturnsNull()
    {
        // Act
        var result = new AcceptedAtActionResult(actionName: null, controllerName: null, routeValues: null, value: null);

        // Assert
        Assert.Null(result.UrlHelper);
    }

    [Theory]
    [MemberData(nameof(ValuesData))]
    public async Task ExecuteResultAsync_SetsObjectValueOfFormatter(object value)
    {
        // Arrange
        var url = "testAction";
        var formatter = CreateMockFormatter();
        var httpContext = GetHttpContext(formatter);
        object actual = null;
        formatter.Setup(f => f.WriteAsync(It.IsAny<OutputFormatterWriteContext>()))
            .Callback((OutputFormatterWriteContext context) => actual = context.Object)
            .Returns(Task.FromResult(0));

        var actionContext = GetActionContext(httpContext);
        var urlHelper = GetMockUrlHelper(url);

        // Act
        var result = new AcceptedAtActionResult(
            actionName: url,
            controllerName: null,
            routeValues: null,
            value: value);

        result.UrlHelper = urlHelper;
        await result.ExecuteResultAsync(actionContext);

        // Assert
        Assert.Same(value, actual);
    }

    [Fact]
    public async Task ExecuteResultAsync_SetsStatusCodeAndLocationHeader()
    {
        // Arrange
        var expectedUrl = "testAction";
        var formatter = CreateMockFormatter();
        var httpContext = GetHttpContext(formatter);
        var actionContext = GetActionContext(httpContext);
        var urlHelper = GetMockUrlHelper(expectedUrl);

        // Act
        var result = new AcceptedAtActionResult(
            actionName: expectedUrl,
            controllerName: null,
            routeValues: null,
            value: null);

        result.UrlHelper = urlHelper;
        await result.ExecuteResultAsync(actionContext);

        // Assert
        Assert.Equal(StatusCodes.Status202Accepted, httpContext.Response.StatusCode);
        Assert.Equal(expectedUrl, httpContext.Response.Headers["Location"]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task ExecuteResultAsync_ThrowsIfActionUrlIsNullOrEmpty(string returnValue)
    {
        // Arrange
        var formatter = CreateMockFormatter();
        var httpContext = GetHttpContext(formatter);
        var actionContext = GetActionContext(httpContext);
        var urlHelper = GetMockUrlHelper(returnValue);

        // Act
        var result = new AcceptedAtActionResult(
            actionName: null,
            controllerName: null,
            routeValues: null,
            value: null);

        result.UrlHelper = urlHelper;

        // Assert
        await ExceptionAssert.ThrowsAsync<InvalidOperationException>(() =>
            result.ExecuteResultAsync(actionContext),
            "No route matches the supplied values.");
    }

    [Fact]
    public void OnFormatting_NullUrlHelperContextHasRequestServices_ReturnsRequestServicesAction()
    {
        // Arrange
        var context = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
        context.HttpContext.RequestServices = new ForwardingServiceProvider();

        // Act
        var result = new AcceptedAtActionResult(actionName: null, controllerName: null, routeValues: null, value: null);
        result.OnFormatting(context);

        // Assert
        var header = context.HttpContext.Response.Headers.Last();
        Assert.Equal("Location", header.Key);
        Assert.Equal("abc", header.Value);
    }

    [Fact]
    public void OnFormatting_NullUrlHelperContextNoRequestServices_ThrowsArgumentNullException()
    {
        // Arrange
        var context = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());

        // Act
        var result = new AcceptedAtActionResult(actionName: null, controllerName: null, routeValues: null, value: null);

        // Assert
        Assert.Throws<ArgumentNullException>("provider", () => result.OnFormatting(context));
    }

    [Fact]
    public void OnFormatting_NullContext_ThrowsArgumentNullException()
    {
        // Act
        var result = new AcceptedAtActionResult("actionName", "controllerName", "routeValues", "value");

        // Assert
        Assert.Throws<ArgumentNullException>("context", () => result.OnFormatting(null));
    }

    private static ActionContext GetActionContext(HttpContext httpContext)
    {
        var routeData = new RouteData();
        routeData.Routers.Add(Mock.Of<IRouter>());

        return new ActionContext(
            httpContext,
            routeData,
            new ActionDescriptor());
    }

    private static HttpContext GetHttpContext(Mock<IOutputFormatter> formatter)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = CreateServices(formatter);
        return httpContext;
    }

    private static Mock<IOutputFormatter> CreateMockFormatter()
    {
        var formatter = new Mock<IOutputFormatter>
        {
            CallBase = true
        };
        formatter.Setup(f => f.CanWriteResult(It.IsAny<OutputFormatterWriteContext>())).Returns(true);

        return formatter;
    }

    private static IServiceProvider CreateServices(Mock<IOutputFormatter> formatter)
    {
        var options = Options.Create(new MvcOptions());
        options.Value.OutputFormatters.Add(formatter.Object);
        var services = new ServiceCollection();
        services.AddSingleton<IActionResultExecutor<ObjectResult>>(new ObjectResultExecutor(
            new DefaultOutputFormatterSelector(options, NullLoggerFactory.Instance),
            new TestHttpResponseStreamWriterFactory(),
            NullLoggerFactory.Instance,
            options));

        return services.BuildServiceProvider();
    }

    private static IUrlHelper GetMockUrlHelper(string returnValue)
    {
        var urlHelper = new Mock<IUrlHelper>();
        urlHelper.Setup(o => o.Action(It.IsAny<UrlActionContext>())).Returns(returnValue);

        return urlHelper.Object;
    }

    private class ForwardingServiceProvider : IServiceProvider
    {
        public object GetService(Type serviceType) => new ForwardingUrlHelperFactory();
    }

    private class ForwardingUrlHelperFactory : IUrlHelperFactory
    {
        public IUrlHelper GetUrlHelper(ActionContext context) => new ForwardingUrlHelper() { ActionValue = "abc" };
    }

    private class ForwardingUrlHelper : IUrlHelper
    {
        public string ActionValue { get; set; }

        public ActionContext ActionContext => null;

        public string Action(UrlActionContext actionContext) => ActionValue;

        public string Content(string contentPath) => null;

        public bool IsLocalUrl(string url) => false;

        public string Link(string routeName, object values) => null;

        public string RouteUrl(UrlRouteContext routeContext) => null;
    }
}
