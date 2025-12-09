// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.InternalTesting;
using Moq;

namespace Microsoft.AspNetCore.Mvc.RazorPages;

public class PageTest
{
    [Fact]
    public void PagePropertiesArePopulatedFromContext()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var modelState = new ModelStateDictionary();
        var actionContext = new ActionContext(httpContext, new RouteData(), new PageActionDescriptor(), modelState);
        var modelMetadataProvider = new EmptyModelMetadataProvider();
        var viewData = new ViewDataDictionary(modelMetadataProvider, modelState);
        var tempData = Mock.Of<ITempDataDictionary>();

        var pageContext = new PageContext(actionContext)
        {
            ViewData = viewData,
        };
        var viewContext = new ViewContext(pageContext, NullView.Instance, viewData, tempData, TextWriter.Null, new HtmlHelperOptions());

        var page = new TestPage
        {
            PageContext = pageContext,
            ViewContext = viewContext,
        };

        // Act & Assert
        Assert.Same(pageContext, page.PageContext);
        Assert.Same(viewContext, page.ViewContext);
        Assert.Same(httpContext, page.HttpContext);
        Assert.Same(httpContext.Request, page.Request);
        Assert.Same(httpContext.Response, page.Response);
        Assert.Same(modelState, page.ModelState);
        Assert.Same(tempData, page.TempData);
    }

    [Fact]
    public void Redirect_WithParameterUrl_SetsRedirectResultSameUrl()
    {
        // Arrange
        var page = new TestPage();
        var url = "/test/url";

        // Act
        var result = page.Redirect(url);

        // Assert
        Assert.IsType<RedirectResult>(result);
        Assert.False(result.PreserveMethod);
        Assert.False(result.Permanent);
        Assert.Same(url, result.Url);
    }

    [Fact]
    public void RedirectPermanent_WithParameterUrl_SetsRedirectResultPermanentAndSameUrl()
    {
        // Arrange
        var page = new TestPage();
        var url = "/test/url";

        // Act
        var result = page.RedirectPermanent(url);

        // Assert
        Assert.IsType<RedirectResult>(result);
        Assert.False(result.PreserveMethod);
        Assert.True(result.Permanent);
        Assert.Same(url, result.Url);
    }

    [Fact]
    public void RedirectPermanent_WithParameterUrl_SetsRedirectResultPreserveMethodAndSameUrl()
    {
        // Arrange
        var page = new TestPage();
        var url = "/test/url";

        // Act
        var result = page.RedirectPreserveMethod(url);

        // Assert
        Assert.IsType<RedirectResult>(result);
        Assert.True(result.PreserveMethod);
        Assert.False(result.Permanent);
        Assert.Same(url, result.Url);
    }

    [Fact]
    public void RedirectPermanent_WithParameterUrl_SetsRedirectResultPermanentPreserveMethodAndSameUrl()
    {
        // Arrange
        var page = new TestPage();
        var url = "/test/url";

        // Act
        var result = page.RedirectPermanentPreserveMethod(url);

        // Assert
        Assert.IsType<RedirectResult>(result);
        Assert.True(result.PreserveMethod);
        Assert.True(result.Permanent);
        Assert.Same(url, result.Url);
    }

    [Theory]
    [InlineData(null, "Value cannot be null.")]
    [InlineData("", "The value cannot be an empty string.")]
    public void Redirect_WithParameter_NullOrEmptyUrl_Throws(string url, string expectedMessage)
    {
        // Arrange
        var page = new TestPage();

        // Act & Assert
        ExceptionAssert.ThrowsArgument(
            () => page.Redirect(url: url),
            "url",
            expectedMessage);
    }

    [Theory]
    [InlineData(null, "Value cannot be null.")]
    [InlineData("", "The value cannot be an empty string.")]
    public void RedirectPreserveMethod_WithParameter_NullOrEmptyUrl_Throws(string url, string expectedMessage)
    {
        // Arrange
        var page = new TestPage();

        // Act & Assert
        ExceptionAssert.ThrowsArgument(
            () => page.RedirectPreserveMethod(url: url),
            "url",
            expectedMessage);
    }

    [Fact]
    public void LocalRedirect_WithParameterUrl_SetsLocalRedirectResultWithSameUrl()
    {
        // Arrange
        var page = new TestPage();
        var url = "/test/url";

        // Act
        var result = page.LocalRedirect(url);

        // Assert
        Assert.IsType<LocalRedirectResult>(result);
        Assert.False(result.PreserveMethod);
        Assert.False(result.Permanent);
        Assert.Same(url, result.Url);
    }

    [Fact]
    public void LocalRedirectPermanent_WithParameterUrl_SetsLocalRedirectResultPermanentWithSameUrl()
    {
        // Arrange
        var page = new TestPage();
        var url = "/test/url";

        // Act
        var result = page.LocalRedirectPermanent(url);

        // Assert
        Assert.IsType<LocalRedirectResult>(result);
        Assert.False(result.PreserveMethod);
        Assert.True(result.Permanent);
        Assert.Same(url, result.Url);
    }

    [Fact]
    public void LocalRedirectPermanent_WithParameterUrl_SetsLocalRedirectResultPreserveMethodWithSameUrl()
    {
        // Arrange
        var page = new TestPage();
        var url = "/test/url";

        // Act
        var result = page.LocalRedirectPreserveMethod(url);

        // Assert
        Assert.IsType<LocalRedirectResult>(result);
        Assert.True(result.PreserveMethod);
        Assert.False(result.Permanent);
        Assert.Same(url, result.Url);
    }

    [Fact]
    public void LocalRedirectPermanent_WithParameterUrl_SetsLocalRedirectResultPermanentPreservesMethodWithSameUrl()
    {
        // Arrange
        var page = new TestPage();
        var url = "/test/url";

        // Act
        var result = page.LocalRedirectPermanentPreserveMethod(url);

        // Assert
        Assert.IsType<LocalRedirectResult>(result);
        Assert.True(result.PreserveMethod);
        Assert.True(result.Permanent);
        Assert.Same(url, result.Url);
    }

    [Theory]
    [InlineData(null, "Value cannot be null.")]
    [InlineData("", "The value cannot be an empty string.")]
    public void LocalRedirect_WithParameter_NullOrEmptyUrl_Throws(string url, string expectedMessage)
    {
        // Arrange
        var page = new TestPage();

        // Act & Assert
        ExceptionAssert.ThrowsArgument(
            () => page.LocalRedirect(localUrl: url),
            "localUrl",
            expectedMessage);
    }

    [Theory]
    [InlineData(null, "Value cannot be null.")]
    [InlineData("", "The value cannot be an empty string.")]
    public void LocalRedirectPreserveMethod_WithParameter_NullOrEmptyUrl_Throws(string url, string expectedMessage)
    {
        // Arrange
        var page = new TestPage();

        // Act & Assert
        ExceptionAssert.ThrowsArgument(
            () => page.LocalRedirectPreserveMethod(localUrl: url),
            "localUrl",
            expectedMessage);
    }

    [Theory]
    [InlineData(null, "Value cannot be null.")]
    [InlineData("", "The value cannot be an empty string.")]
    public void LocalRedirectPermanentPreserveMethod_WithParameter_NullOrEmptyUrl_Throws(string url, string expectedMessage)
    {
        // Arrange
        var page = new TestPage();

        // Act & Assert
        ExceptionAssert.ThrowsArgument(
            () => page.LocalRedirectPermanentPreserveMethod(localUrl: url),
            "localUrl",
            expectedMessage);
    }

    [Theory]
    [InlineData(null, "Value cannot be null.")]
    [InlineData("", "The value cannot be an empty string.")]
    public void RedirectPermanent_WithParameter_NullOrEmptyUrl_Throws(string url, string expectedMessage)
    {
        // Arrange
        var page = new TestPage();

        // Act & Assert
        ExceptionAssert.ThrowsArgument(
            () => page.RedirectPermanent(url: url),
            "url",
            expectedMessage);
    }

    [Theory]
    [InlineData(null, "Value cannot be null.")]
    [InlineData("", "The value cannot be an empty string.")]
    public void RedirectPermanentPreserveMethod_WithParameter_NullOrEmptyUrl_Throws(string url, string expectedMessage)
    {
        // Arrange
        var page = new TestPage();

        // Act & Assert
        ExceptionAssert.ThrowsArgument(
            () => page.RedirectPermanentPreserveMethod(url: url),
            "url",
            expectedMessage);
    }

    [Fact]
    public void RedirectToAction_WithParameterActionName_SetsResultActionName()
    {
        // Arrange
        var page = new TestPage();

        // Act
        var resultTemporary = page.RedirectToAction("SampleAction");

        // Assert
        Assert.IsType<RedirectToActionResult>(resultTemporary);
        Assert.False(resultTemporary.PreserveMethod);
        Assert.False(resultTemporary.Permanent);
        Assert.Equal("SampleAction", resultTemporary.ActionName);
    }

    [Fact]
    public void RedirectToActionPreserveMethod_WithParameterActionName_SetsResultActionName()
    {
        // Arrange
        var page = new TestPage();

        // Act
        var resultTemporary = page.RedirectToActionPreserveMethod(actionName: "SampleAction");

        // Assert
        Assert.IsType<RedirectToActionResult>(resultTemporary);
        Assert.True(resultTemporary.PreserveMethod);
        Assert.False(resultTemporary.Permanent);
        Assert.Equal("SampleAction", resultTemporary.ActionName);
    }

    [Fact]
    public void RedirectToActionPermanent_WithParameterActionName_SetsResultActionNameAndPermanent()
    {
        // Arrange
        var page = new TestPage();

        // Act
        var resultPermanent = page.RedirectToActionPermanent("SampleAction");

        // Assert
        Assert.IsType<RedirectToActionResult>(resultPermanent);
        Assert.False(resultPermanent.PreserveMethod);
        Assert.True(resultPermanent.Permanent);
        Assert.Equal("SampleAction", resultPermanent.ActionName);
    }

    [Fact]
    public void RedirectToActionPermanentPreserveMethod_WithParameterActionName_SetsResultActionNameAndPermanent()
    {
        // Arrange
        var page = new TestPage();

        // Act
        var resultPermanent = page.RedirectToActionPermanentPreserveMethod(actionName: "SampleAction");

        // Assert
        Assert.IsType<RedirectToActionResult>(resultPermanent);
        Assert.True(resultPermanent.PreserveMethod);
        Assert.True(resultPermanent.Permanent);
        Assert.Equal("SampleAction", resultPermanent.ActionName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("SampleController")]
    public void RedirectToAction_WithParameterActionAndControllerName_SetsEqualNames(string controllerName)
    {
        // Arrange
        var page = new TestPage();

        // Act
        var resultTemporary = page.RedirectToAction("SampleAction", controllerName);

        // Assert
        Assert.IsType<RedirectToActionResult>(resultTemporary);
        Assert.False(resultTemporary.PreserveMethod);
        Assert.False(resultTemporary.Permanent);
        Assert.Equal("SampleAction", resultTemporary.ActionName);
        Assert.Equal(controllerName, resultTemporary.ControllerName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("SampleController")]
    public void RedirectToActionPreserveMethod_WithParameterActionAndControllerName_SetsEqualNames(string controllerName)
    {
        // Arrange
        var page = new TestPage();

        // Act
        var resultTemporary = page.RedirectToActionPreserveMethod(actionName: "SampleAction", controllerName: controllerName);

        // Assert
        Assert.IsType<RedirectToActionResult>(resultTemporary);
        Assert.True(resultTemporary.PreserveMethod);
        Assert.False(resultTemporary.Permanent);
        Assert.Equal("SampleAction", resultTemporary.ActionName);
        Assert.Equal(controllerName, resultTemporary.ControllerName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("SampleController")]
    public void RedirectToActionPermanent_WithParameterActionAndControllerName_SetsEqualNames(string controllerName)
    {
        // Arrange
        var page = new TestPage();

        // Act
        var resultPermanent = page.RedirectToActionPermanent("SampleAction", controllerName);

        // Assert
        Assert.IsType<RedirectToActionResult>(resultPermanent);
        Assert.False(resultPermanent.PreserveMethod);
        Assert.True(resultPermanent.Permanent);
        Assert.Equal("SampleAction", resultPermanent.ActionName);
        Assert.Equal(controllerName, resultPermanent.ControllerName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("SampleController")]
    public void RedirectToActionPermanentPreserveMethod_WithParameterActionAndControllerName_SetsEqualNames(string controllerName)
    {
        // Arrange
        var page = new TestPage();

        // Act
        var resultPermanent = page.RedirectToActionPermanentPreserveMethod(actionName: "SampleAction", controllerName: controllerName);

        // Assert
        Assert.IsType<RedirectToActionResult>(resultPermanent);
        Assert.True(resultPermanent.PreserveMethod);
        Assert.True(resultPermanent.Permanent);
        Assert.Equal("SampleAction", resultPermanent.ActionName);
        Assert.Equal(controllerName, resultPermanent.ControllerName);
    }

    [Theory]
    [MemberData(nameof(RedirectTestData))]
    public void RedirectToAction_WithParameterActionControllerRouteValues_SetsResultProperties(
        object routeValues,
        IEnumerable<KeyValuePair<string, object>> expected)
    {
        // Arrange
        var page = new TestPage();

        // Act
        var resultTemporary = page.RedirectToAction("SampleAction", "SampleController", routeValues);

        // Assert
        Assert.IsType<RedirectToActionResult>(resultTemporary);
        Assert.False(resultTemporary.PreserveMethod);
        Assert.False(resultTemporary.Permanent);
        Assert.Equal("SampleAction", resultTemporary.ActionName);
        Assert.Equal("SampleController", resultTemporary.ControllerName);
        Assert.Equal(expected, resultTemporary.RouteValues);
    }

    [Theory]
    [MemberData(nameof(RedirectTestData))]
    public void RedirectToActionPreserveMethod_WithParameterActionControllerRouteValues_SetsResultProperties(
        object routeValues,
        IEnumerable<KeyValuePair<string, object>> expected)
    {
        // Arrange
        var page = new TestPage();

        // Act
        var resultTemporary = page.RedirectToActionPreserveMethod(
            actionName: "SampleAction",
            controllerName: "SampleController",
            routeValues: routeValues);

        // Assert
        Assert.IsType<RedirectToActionResult>(resultTemporary);
        Assert.True(resultTemporary.PreserveMethod);
        Assert.False(resultTemporary.Permanent);
        Assert.Equal("SampleAction", resultTemporary.ActionName);
        Assert.Equal("SampleController", resultTemporary.ControllerName);
        Assert.Equal(expected, resultTemporary.RouteValues);
    }

    [Theory]
    [MemberData(nameof(RedirectTestData))]
    public void RedirectToActionPermanent_WithParameterActionControllerRouteValues_SetsResultProperties(
        object routeValues,
        IEnumerable<KeyValuePair<string, object>> expected)
    {
        // Arrange
        var page = new TestPage();

        // Act
        var resultPermanent = page.RedirectToActionPermanent(
            "SampleAction",
            "SampleController",
            routeValues);

        // Assert
        Assert.IsType<RedirectToActionResult>(resultPermanent);
        Assert.False(resultPermanent.PreserveMethod);
        Assert.True(resultPermanent.Permanent);
        Assert.Equal("SampleAction", resultPermanent.ActionName);
        Assert.Equal("SampleController", resultPermanent.ControllerName);
        Assert.Equal(expected, resultPermanent.RouteValues);
    }

    [Theory]
    [MemberData(nameof(RedirectTestData))]
    public void RedirectToActionPermanentPreserveMethod_WithParameterActionControllerRouteValues_SetsResultProperties(
        object routeValues,
        IEnumerable<KeyValuePair<string, object>> expected)
    {
        // Arrange
        var page = new TestPage();

        // Act
        var resultPermanent = page.RedirectToActionPermanentPreserveMethod(
            actionName: "SampleAction",
            controllerName: "SampleController",
            routeValues: routeValues);

        // Assert
        Assert.IsType<RedirectToActionResult>(resultPermanent);
        Assert.True(resultPermanent.PreserveMethod);
        Assert.True(resultPermanent.Permanent);
        Assert.Equal("SampleAction", resultPermanent.ActionName);
        Assert.Equal("SampleController", resultPermanent.ControllerName);
        Assert.Equal(expected, resultPermanent.RouteValues);
    }

    [Theory]
    [MemberData(nameof(RedirectTestData))]
    public void RedirectToAction_WithParameterActionAndRouteValues_SetsResultProperties(
        object routeValues,
        IEnumerable<KeyValuePair<string, object>> expected)
    {
        // Arrange
        var page = new TestPage();

        // Act
        var resultTemporary = page.RedirectToAction(actionName: null, routeValues: routeValues);

        // Assert
        Assert.IsType<RedirectToActionResult>(resultTemporary);
        Assert.False(resultTemporary.PreserveMethod);
        Assert.False(resultTemporary.Permanent);
        Assert.Null(resultTemporary.ActionName);
        Assert.Equal(expected, resultTemporary.RouteValues);
    }

    [Theory]
    [MemberData(nameof(RedirectTestData))]
    public void RedirectToActionPreserveMethod_WithParameterActionAndRouteValues_SetsResultProperties(
        object routeValues,
        IEnumerable<KeyValuePair<string, object>> expected)
    {
        // Arrange
        var page = new TestPage();

        // Act
        var resultTemporary = page.RedirectToActionPreserveMethod(actionName: null, routeValues: routeValues);

        // Assert
        Assert.IsType<RedirectToActionResult>(resultTemporary);
        Assert.True(resultTemporary.PreserveMethod);
        Assert.False(resultTemporary.Permanent);
        Assert.Null(resultTemporary.ActionName);
        Assert.Equal(expected, resultTemporary.RouteValues);
    }

    [Theory]
    [MemberData(nameof(RedirectTestData))]
    public void RedirectToAction_WithParameterActionAndControllerAndRouteValuesAndFragment_SetsResultProperties(
        object routeValues,
        IEnumerable<KeyValuePair<string, object>> expectedRouteValues)
    {
        // Arrange
        var page = new TestPage();
        var expectedAction = "Action";
        var expectedController = "Home";
        var expectedFragment = "test";

        // Act
        var result = page.RedirectToAction("Action", "Home", routeValues, "test");

        // Assert
        Assert.IsType<RedirectToActionResult>(result);
        Assert.False(result.PreserveMethod);
        Assert.False(result.Permanent);
        Assert.Equal(expectedAction, result.ActionName);
        Assert.Equal(expectedRouteValues, result.RouteValues);
        Assert.Equal(expectedController, result.ControllerName);
        Assert.Equal(expectedFragment, result.Fragment);
    }

    [Theory]
    [MemberData(nameof(RedirectTestData))]
    public void RedirectToActionPreserveMethod_WithParameterActionAndControllerAndRouteValuesAndFragment_SetsResultProperties(
        object routeValues,
        IEnumerable<KeyValuePair<string, object>> expectedRouteValues)
    {
        // Arrange
        var page = new TestPage();
        var expectedAction = "Action";
        var expectedController = "Home";
        var expectedFragment = "test";

        // Act
        var result = page.RedirectToActionPreserveMethod("Action", "Home", routeValues, "test");

        // Assert
        Assert.IsType<RedirectToActionResult>(result);
        Assert.True(result.PreserveMethod);
        Assert.False(result.Permanent);
        Assert.Equal(expectedAction, result.ActionName);
        Assert.Equal(expectedRouteValues, result.RouteValues);
        Assert.Equal(expectedController, result.ControllerName);
        Assert.Equal(expectedFragment, result.Fragment);
    }

    [Theory]
    [MemberData(nameof(RedirectTestData))]
    public void RedirectToActionPermanent_WithParameterActionAndRouteValues_SetsResultProperties(
        object routeValues,
        IEnumerable<KeyValuePair<string, object>> expected)
    {
        // Arrange
        var page = new TestPage();

        // Act
        var resultPermanent = page.RedirectToActionPermanent(null, routeValues);

        // Assert
        Assert.IsType<RedirectToActionResult>(resultPermanent);
        Assert.False(resultPermanent.PreserveMethod);
        Assert.True(resultPermanent.Permanent);
        Assert.Null(resultPermanent.ActionName);
        Assert.Equal(expected, resultPermanent.RouteValues);
    }

    [Theory]
    [MemberData(nameof(RedirectTestData))]
    public void RedirectToActionPermanentPreserveMethod_WithParameterActionAndRouteValues_SetsResultProperties(
        object routeValues,
        IEnumerable<KeyValuePair<string, object>> expected)
    {
        // Arrange
        var page = new TestPage();

        // Act
        var resultPermanent = page.RedirectToActionPermanentPreserveMethod(actionName: null, routeValues: routeValues);

        // Assert
        Assert.IsType<RedirectToActionResult>(resultPermanent);
        Assert.True(resultPermanent.PreserveMethod);
        Assert.True(resultPermanent.Permanent);
        Assert.Null(resultPermanent.ActionName);
        Assert.Equal(expected, resultPermanent.RouteValues);
    }

    [Theory]
    [MemberData(nameof(RedirectTestData))]
    public void RedirectToActionPermanent_WithParameterActionAndControllerAndRouteValuesAndFragment_SetsResultProperties(
        object routeValues,
        IEnumerable<KeyValuePair<string, object>> expectedRouteValues)
    {
        // Arrange
        var page = new TestPage();
        var expectedAction = "Action";
        var expectedController = "Home";
        var expectedFragment = "test";

        // Act
        var result = page.RedirectToActionPermanent("Action", "Home", routeValues, fragment: "test");

        // Assert
        Assert.IsType<RedirectToActionResult>(result);
        Assert.False(result.PreserveMethod);
        Assert.True(result.Permanent);
        Assert.Equal(expectedAction, result.ActionName);
        Assert.Equal(expectedRouteValues, result.RouteValues);
        Assert.Equal(expectedController, result.ControllerName);
        Assert.Equal(expectedFragment, result.Fragment);
    }

    [Theory]
    [MemberData(nameof(RedirectTestData))]
    public void RedirectToActionPermanentPreserveMethod_WithParameterActionAndControllerAndRouteValuesAndFragment_SetsResultProperties(
        object routeValues,
        IEnumerable<KeyValuePair<string, object>> expectedRouteValues)
    {
        // Arrange
        var page = new TestPage();
        var expectedAction = "Action";
        var expectedController = "Home";
        var expectedFragment = "test";

        // Act
        var result = page.RedirectToActionPermanentPreserveMethod(
            actionName: "Action",
            controllerName: "Home",
            routeValues: routeValues,
            fragment: "test");

        // Assert
        Assert.IsType<RedirectToActionResult>(result);
        Assert.True(result.PreserveMethod);
        Assert.True(result.Permanent);
        Assert.Equal(expectedAction, result.ActionName);
        Assert.Equal(expectedRouteValues, result.RouteValues);
        Assert.Equal(expectedController, result.ControllerName);
        Assert.Equal(expectedFragment, result.Fragment);
    }

    [Theory]
    [MemberData(nameof(RedirectTestData))]
    public void RedirectToRoute_WithParameterRouteValues_SetsResultEqualRouteValues(
        object routeValues,
        IEnumerable<KeyValuePair<string, object>> expected)
    {
        // Arrange
        var page = new TestPage();

        // Act
        var resultTemporary = page.RedirectToRoute(routeValues);

        // Assert
        Assert.IsType<RedirectToRouteResult>(resultTemporary);
        Assert.False(resultTemporary.PreserveMethod);
        Assert.False(resultTemporary.Permanent);
        Assert.Equal(expected, resultTemporary.RouteValues);
    }

    [Theory]
    [MemberData(nameof(RedirectTestData))]
    public void RedirectToRoutePreserveMethod_WithParameterRouteValues_SetsResultEqualRouteValues(
        object routeValues,
        IEnumerable<KeyValuePair<string, object>> expected)
    {
        // Arrange
        var page = new TestPage();

        // Act
        var resultTemporary = page.RedirectToRoutePreserveMethod(routeValues: routeValues);

        // Assert
        Assert.IsType<RedirectToRouteResult>(resultTemporary);
        Assert.True(resultTemporary.PreserveMethod);
        Assert.False(resultTemporary.Permanent);
        Assert.Equal(expected, resultTemporary.RouteValues);
    }

    [Theory]
    [MemberData(nameof(RedirectTestData))]
    public void RedirectToRoute_WithParameterRouteNameAndRouteValuesAndFragment_SetsResultProperties(
        object routeValues,
        IEnumerable<KeyValuePair<string, object>> expectedRouteValues)
    {
        // Arrange
        var page = new TestPage();
        var expectedRoute = "TestRoute";
        var expectedFragment = "test";

        // Act
        var result = page.RedirectToRoute("TestRoute", routeValues, "test");

        // Assert
        Assert.IsType<RedirectToRouteResult>(result);
        Assert.False(result.PreserveMethod);
        Assert.False(result.Permanent);
        Assert.Equal(expectedRoute, result.RouteName);
        Assert.Equal(expectedRouteValues, result.RouteValues);
        Assert.Equal(expectedFragment, result.Fragment);
    }

    [Theory]
    [MemberData(nameof(RedirectTestData))]
    public void RedirectToRoutePreserveMethod_WithParameterRouteNameAndRouteValuesAndFragment_SetsResultProperties(
        object routeValues,
        IEnumerable<KeyValuePair<string, object>> expectedRouteValues)
    {
        // Arrange
        var page = new TestPage();
        var expectedRoute = "TestRoute";
        var expectedFragment = "test";

        // Act
        var result = page.RedirectToRoutePreserveMethod(routeName: "TestRoute", routeValues: routeValues, fragment: "test");

        // Assert
        Assert.IsType<RedirectToRouteResult>(result);
        Assert.True(result.PreserveMethod);
        Assert.False(result.Permanent);
        Assert.Equal(expectedRoute, result.RouteName);
        Assert.Equal(expectedRouteValues, result.RouteValues);
        Assert.Equal(expectedFragment, result.Fragment);
    }

    [Theory]
    [MemberData(nameof(RedirectTestData))]
    public void RedirectToRoutePermanent_WithParameterRouteValues_SetsResultEqualRouteValuesAndPermanent(
        object routeValues,
        IEnumerable<KeyValuePair<string, object>> expected)
    {
        // Arrange
        var page = new TestPage();

        // Act
        var resultPermanent = page.RedirectToRoutePermanent(routeValues);

        // Assert
        Assert.IsType<RedirectToRouteResult>(resultPermanent);
        Assert.False(resultPermanent.PreserveMethod);
        Assert.True(resultPermanent.Permanent);
        Assert.Equal(expected, resultPermanent.RouteValues);
    }

    [Theory]
    [MemberData(nameof(RedirectTestData))]
    public void RedirectToRoutePermanentPreserveMethod_WithParameterRouteValues_SetsResultEqualRouteValuesAndPermanent(
        object routeValues,
        IEnumerable<KeyValuePair<string, object>> expected)
    {
        // Arrange
        var page = new TestPage();

        // Act
        var resultPermanent = page.RedirectToRoutePermanentPreserveMethod(routeValues: routeValues);

        // Assert
        Assert.IsType<RedirectToRouteResult>(resultPermanent);
        Assert.True(resultPermanent.PreserveMethod);
        Assert.True(resultPermanent.Permanent);
        Assert.Equal(expected, resultPermanent.RouteValues);
    }

    [Theory]
    [MemberData(nameof(RedirectTestData))]
    public void RedirectToRoutePermanent_WithParameterRouteNameAndRouteValuesAndFragment_SetsResultProperties(
        object routeValues,
        IEnumerable<KeyValuePair<string, object>> expectedRouteValues)
    {
        // Arrange
        var page = new TestPage();
        var expectedRoute = "TestRoute";
        var expectedFragment = "test";

        // Act
        var result = page.RedirectToRoutePermanent("TestRoute", routeValues, "test");

        // Assert
        Assert.IsType<RedirectToRouteResult>(result);
        Assert.False(result.PreserveMethod);
        Assert.True(result.Permanent);
        Assert.Equal(expectedRoute, result.RouteName);
        Assert.Equal(expectedRouteValues, result.RouteValues);
        Assert.Equal(expectedFragment, result.Fragment);
    }

    [Theory]
    [MemberData(nameof(RedirectTestData))]
    public void RedirectToRoutePermanentPreserveMethod_WithParameterRouteNameAndRouteValuesAndFragment_SetsResultProperties(
        object routeValues,
        IEnumerable<KeyValuePair<string, object>> expectedRouteValues)
    {
        // Arrange
        var page = new TestPage();
        var expectedRoute = "TestRoute";
        var expectedFragment = "test";

        // Act
        var result = page.RedirectToRoutePermanentPreserveMethod(routeName: "TestRoute", routeValues: routeValues, fragment: "test");

        // Assert
        Assert.IsType<RedirectToRouteResult>(result);
        Assert.True(result.PreserveMethod);
        Assert.True(result.Permanent);
        Assert.Equal(expectedRoute, result.RouteName);
        Assert.Equal(expectedRouteValues, result.RouteValues);
        Assert.Equal(expectedFragment, result.Fragment);
    }

    [Fact]
    public void RedirectToRoute_WithParameterRouteName_SetsResultSameRouteName()
    {
        // Arrange
        var page = new TestPage();
        var routeName = "CustomRouteName";

        // Act
        var resultTemporary = page.RedirectToRoute(routeName);

        // Assert
        Assert.IsType<RedirectToRouteResult>(resultTemporary);
        Assert.False(resultTemporary.PreserveMethod);
        Assert.False(resultTemporary.Permanent);
        Assert.Same(routeName, resultTemporary.RouteName);
    }

    [Fact]
    public void RedirectToRoutePreserveMethod_WithParameterRouteName_SetsResultSameRouteName()
    {
        // Arrange
        var page = new TestPage();
        var routeName = "CustomRouteName";

        // Act;
        var resultTemporary = page.RedirectToRoutePreserveMethod(routeName: routeName);

        // Assert
        Assert.IsType<RedirectToRouteResult>(resultTemporary);
        Assert.True(resultTemporary.PreserveMethod);
        Assert.False(resultTemporary.Permanent);
        Assert.Same(routeName, resultTemporary.RouteName);
    }

    [Fact]
    public void RedirectToRoutePermanent_WithParameterRouteName_SetsResultSameRouteNameAndPermanent()
    {
        // Arrange
        var page = new TestPage();
        var routeName = "CustomRouteName";

        // Act
        var resultPermanent = page.RedirectToRoutePermanent(routeName);

        // Assert
        Assert.IsType<RedirectToRouteResult>(resultPermanent);
        Assert.False(resultPermanent.PreserveMethod);
        Assert.True(resultPermanent.Permanent);
        Assert.Same(routeName, resultPermanent.RouteName);
    }

    [Fact]
    public void RedirectToRoutePermanentPreserveMethod_WithParameterRouteName_SetsResultSameRouteNameAndPermanent()
    {
        // Arrange
        var page = new TestPage();
        var routeName = "CustomRouteName";

        // Act
        var resultPermanent = page.RedirectToRoutePermanentPreserveMethod(routeName: routeName);

        // Assert
        Assert.IsType<RedirectToRouteResult>(resultPermanent);
        Assert.True(resultPermanent.PreserveMethod);
        Assert.True(resultPermanent.Permanent);
        Assert.Same(routeName, resultPermanent.RouteName);
    }

    [Theory]
    [MemberData(nameof(RedirectTestData))]
    public void RedirectToRoute_WithParameterRouteNameAndRouteValues_SetsResultSameRouteNameAndRouteValues(
        object routeValues,
        IEnumerable<KeyValuePair<string, object>> expected)
    {
        // Arrange
        var page = new TestPage();
        var routeName = "CustomRouteName";

        // Act
        var resultTemporary = page.RedirectToRoute(routeName, routeValues);

        // Assert
        Assert.IsType<RedirectToRouteResult>(resultTemporary);
        Assert.False(resultTemporary.PreserveMethod);
        Assert.False(resultTemporary.Permanent);
        Assert.Same(routeName, resultTemporary.RouteName);
        Assert.Equal(expected, resultTemporary.RouteValues);
    }

    [Theory]
    [MemberData(nameof(RedirectTestData))]
    public void RedirectToRoutePreserveMethod_WithParameterRouteNameAndRouteValues_SetsResultSameRouteNameAndRouteValues(
        object routeValues,
        IEnumerable<KeyValuePair<string, object>> expected)
    {
        // Arrange
        var page = new TestPage();
        var routeName = "CustomRouteName";

        // Act
        var resultTemporary = page.RedirectToRoutePreserveMethod(routeName: routeName, routeValues: routeValues);

        // Assert
        Assert.IsType<RedirectToRouteResult>(resultTemporary);
        Assert.True(resultTemporary.PreserveMethod);
        Assert.False(resultTemporary.Permanent);
        Assert.Same(routeName, resultTemporary.RouteName);
        Assert.Equal(expected, resultTemporary.RouteValues);
    }

    [Theory]
    [MemberData(nameof(RedirectTestData))]
    public void RedirectToRoutePermanent_WithParameterRouteNameAndRouteValues_SetsResultProperties(
        object routeValues,
        IEnumerable<KeyValuePair<string, object>> expected)
    {
        // Arrange
        var page = new TestPage();
        var routeName = "CustomRouteName";

        // Act
        var resultPermanent = page.RedirectToRoutePermanent(routeName, routeValues);

        // Assert
        Assert.IsType<RedirectToRouteResult>(resultPermanent);
        Assert.False(resultPermanent.PreserveMethod);
        Assert.True(resultPermanent.Permanent);
        Assert.Same(routeName, resultPermanent.RouteName);
        Assert.Equal(expected, resultPermanent.RouteValues);
    }

    [Theory]
    [MemberData(nameof(RedirectTestData))]
    public void RedirectToRoutePermanentPreserveMethod_WithParameterRouteNameAndRouteValues_SetsResultProperties(
        object routeValues,
        IEnumerable<KeyValuePair<string, object>> expected)
    {
        // Arrange
        var page = new TestPage();
        var routeName = "CustomRouteName";

        // Act
        var resultPermanent = page.RedirectToRoutePermanentPreserveMethod(routeName: routeName, routeValues: routeValues);

        // Assert
        Assert.IsType<RedirectToRouteResult>(resultPermanent);
        Assert.True(resultPermanent.PreserveMethod);
        Assert.True(resultPermanent.Permanent);
        Assert.Same(routeName, resultPermanent.RouteName);
        Assert.Equal(expected, resultPermanent.RouteValues);
    }

    [Fact]
    public void RedirectToPage_WithNoArguments()
    {
        // Arrange
        var page = new TestPage();

        // Act
        var result = page.RedirectToPage();

        // Assert
        Assert.IsType<RedirectToPageResult>(result);
        Assert.Null(result.PageName);
    }

    [Fact]
    public void RedirectToPage_WithPageName()
    {
        // Arrange
        var page = new TestPage();
        var pageName = "/Page";

        // Act
        var result = page.RedirectToPage(pageName);

        // Assert
        Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(pageName, result.PageName);
    }

    [Fact]
    public void RedirectToPage_WithRouteValues()
    {
        // Arrange
        var page = new TestPage();
        var routeValues = new { key = "value" };

        // Act
        var result = page.RedirectToPage(routeValues);

        // Assert
        Assert.IsType<RedirectToPageResult>(result);
        Assert.Null(result.PageName);
        Assert.Collection(
            result.RouteValues,
            item =>
            {
                Assert.Equal("key", item.Key);
                Assert.Equal("value", item.Value);
            });
    }

    [Fact]
    public void RedirectToPage_WithPageNameAndHandler()
    {
        // Arrange
        var page = new TestPage();
        var pageName = "/Page-Name";
        var pageHandler = "page-handler";

        // Act
        var result = page.RedirectToPage(pageName, pageHandler);

        // Assert
        Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(pageName, result.PageName);
        Assert.Equal(pageHandler, result.PageHandler);
    }

    [Fact]
    public void RedirectToPage_WithPageNameAndRouteValues()
    {
        // Arrange
        var page = new TestPage();
        var pageName = "/Page-Name";
        var routeValues = new { key = "value" };

        // Act
        var result = page.RedirectToPage(pageName, routeValues);

        // Assert
        Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(pageName, result.PageName);
        Assert.Collection(
            result.RouteValues,
            item =>
            {
                Assert.Equal("key", item.Key);
                Assert.Equal("value", item.Value);
            });
    }

    [Fact]
    public void RedirectToPage_WithPageNameHandlerAndFragment()
    {
        // Arrange
        var page = new TestPage();
        var pageName = "/Page-Name";
        var pageHandler = "page-handler";
        var fragment = "fragment";

        // Act
        var result = page.RedirectToPage(pageName, pageHandler, fragment);

        // Assert
        Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(pageName, result.PageName);
        Assert.Equal(pageHandler, result.PageHandler);
        Assert.Equal(fragment, result.Fragment);
    }

    [Fact]
    public void RedirectToPage_WithPageNameRouteValuesHandlerAndFragment()
    {
        // Arrange
        var page = new TestPage();
        var pageName = "/Page-Name";
        var pageHandler = "page-handler";
        var fragment = "fragment";
        var routeValues = new { key = "value" };

        // Act
        var result = page.RedirectToPage(pageName, pageHandler, routeValues, fragment);

        // Assert
        Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(pageName, result.PageName);
        Assert.Equal(pageHandler, result.PageHandler);
        Assert.Collection(
            result.RouteValues,
            item =>
            {
                Assert.Equal("key", item.Key);
                Assert.Equal("value", item.Value);
            });
        Assert.Equal(fragment, result.Fragment);
    }

    [Fact]
    public void RedirectToPagePermanent_WithPageName()
    {
        // Arrange
        var page = new TestPage();
        var pageName = "/Page-Name";

        // Act
        var result = page.RedirectToPagePermanent(pageName);

        // Assert
        Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(pageName, result.PageName);
        Assert.True(result.Permanent);
    }

    [Fact]
    public void RedirectToPagePermanent_WithPageNameAndPageHandler()
    {
        // Arrange
        var page = new TestPage();
        var pageName = "/Page-Name";
        var pageHandler = "page-handler";

        // Act
        var result = page.RedirectToPagePermanent(pageName, pageHandler);

        // Assert
        Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(pageName, result.PageName);
        Assert.Equal(pageHandler, result.PageHandler);
        Assert.True(result.Permanent);
    }

    [Fact]
    public void RedirectToPagePermanent_WithPageNameAndRouteValues()
    {
        // Arrange
        var page = new TestPage();
        var pageName = "/Page-Name";
        var routeValues = new { key = "value" };

        // Act
        var result = page.RedirectToPagePermanent(pageName, routeValues);

        // Assert
        Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(pageName, result.PageName);
        Assert.Collection(
            result.RouteValues,
            item =>
            {
                Assert.Equal("key", item.Key);
                Assert.Equal("value", item.Value);
            });
        Assert.True(result.Permanent);
    }

    [Fact]
    public void RedirectToPagePermanent_WithPageNamePageHandlerAndRouteValues()
    {
        // Arrange
        var page = new TestPage();
        var pageName = "/Page-Name";
        var pageHandler = "page-handler";
        var routeValues = new { key = "value" };

        // Act
        var result = page.RedirectToPagePermanent(pageName, pageHandler, routeValues);

        // Assert
        Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(pageName, result.PageName);
        Assert.Equal(pageHandler, result.PageHandler);
        Assert.Collection(
            result.RouteValues,
            item =>
            {
                Assert.Equal("key", item.Key);
                Assert.Equal("value", item.Value);
            });
        Assert.True(result.Permanent);
    }

    [Fact]
    public void RedirectToPagePermanent_WithPageNamePageHandlerAndFragment()
    {
        // Arrange
        var page = new TestPage();
        var pageName = "/Page-Name";
        var pageHandler = "page-handler";
        var fragment = "fragment";

        // Act
        var result = page.RedirectToPagePermanent(pageName, pageHandler, fragment);

        // Assert
        Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(pageName, result.PageName);
        Assert.Equal(pageHandler, result.PageHandler);
        Assert.Equal(fragment, result.Fragment);
        Assert.True(result.Permanent);
    }

    [Fact]
    public void RedirectToPagePermanent_WithPageNamePageHandlerRouteValuesAndFragment()
    {
        // Arrange
        var page = new TestPage();
        var pageName = "/Page-Name";
        var pageHandler = "page-handler";
        var routeValues = new { key = "value" };
        var fragment = "fragment";

        // Act
        var result = page.RedirectToPagePermanent(pageName, pageHandler, routeValues, fragment);

        // Assert
        Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(pageName, result.PageName);
        Assert.Equal(pageHandler, result.PageHandler);
        Assert.Collection(
            result.RouteValues,
            item =>
            {
                Assert.Equal("key", item.Key);
                Assert.Equal("value", item.Value);
            });
        Assert.Equal(fragment, result.Fragment);
        Assert.True(result.Permanent);
    }

    [Fact]
    public void RedirectToPagePreserveMethod_WithParameterUrl_SetsRedirectResultPreserveMethod()
    {
        // Arrange
        var pageModel = new TestPage();
        var url = "/test/url";

        // Act
        var result = pageModel.RedirectToPagePreserveMethod(url);

        // Assert
        Assert.IsType<RedirectToPageResult>(result);
        Assert.True(result.PreserveMethod);
        Assert.False(result.Permanent);
        Assert.Same(url, result.PageName);
    }

    [Theory]
    [MemberData(nameof(RedirectTestData))]
    public void RedirectToPagePreserveMethod_SetsResultProperties(
        object routeValues,
        IEnumerable<KeyValuePair<string, object>> expected)
    {
        // Arrange
        var pageModel = new TestPage();
        var pageName = "CustomRouteName";

        // Act
        var resultPermanent = pageModel.RedirectToPagePreserveMethod(pageName, routeValues: routeValues);

        // Assert
        Assert.IsType<RedirectToPageResult>(resultPermanent);
        Assert.True(resultPermanent.PreserveMethod);
        Assert.False(resultPermanent.Permanent);
        Assert.Same(pageName, resultPermanent.PageName);
        Assert.Equal(expected, resultPermanent.RouteValues);
    }

    [Theory]
    [MemberData(nameof(RedirectTestData))]
    public void RedirectToPagePermanentPreserveMethod_SetsResultProperties(
        object routeValues,
        IEnumerable<KeyValuePair<string, object>> expected)
    {
        // Arrange
        var pageModel = new TestPage();
        var routeName = "CustomRouteName";

        // Act
        var resultPermanent = pageModel.RedirectToPagePermanentPreserveMethod(routeName, routeValues: routeValues);

        // Assert
        Assert.IsType<RedirectToPageResult>(resultPermanent);
        Assert.True(resultPermanent.PreserveMethod);
        Assert.True(resultPermanent.Permanent);
        Assert.Same(routeName, resultPermanent.PageName);
        Assert.Equal(expected, resultPermanent.RouteValues);
    }

    [Fact]
    public void File_WithContents()
    {
        // Arrange
        var page = new TestPage();
        var fileContents = new byte[0];

        // Act
        var result = page.File(fileContents, "application/pdf");

        // Assert
        Assert.NotNull(result);
        Assert.Same(fileContents, result.FileContents);
        Assert.Equal("application/pdf", result.ContentType.ToString());
        Assert.Equal(string.Empty, result.FileDownloadName);
    }

    [Fact]
    public void File_WithContentsAndFileDownloadName()
    {
        // Arrange
        var page = new TestPage();
        var fileContents = new byte[0];

        // Act
        var result = page.File(fileContents, "application/pdf", "someDownloadName");

        // Assert
        Assert.NotNull(result);
        Assert.Same(fileContents, result.FileContents);
        Assert.Equal("application/pdf", result.ContentType.ToString());
        Assert.Equal("someDownloadName", result.FileDownloadName);
    }

    [Fact]
    public void File_WithPath()
    {
        // Arrange
        var page = new TestPage();
        var path = Path.GetFullPath("somepath");

        // Act
        var result = page.File(path, "application/pdf");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(path, result.FileName);
        Assert.Equal("application/pdf", result.ContentType.ToString());
        Assert.Equal(string.Empty, result.FileDownloadName);
    }

    [Fact]
    public void File_WithPathAndFileDownloadName()
    {
        // Arrange
        var page = new TestPage();
        var path = Path.GetFullPath("somepath");

        // Act
        var result = page.File(path, "application/pdf", "someDownloadName");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(path, result.FileName);
        Assert.Equal("application/pdf", result.ContentType.ToString());
        Assert.Equal("someDownloadName", result.FileDownloadName);
    }

    [Fact]
    public void File_WithStream()
    {
        // Arrange
        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(x => x.Response.RegisterForDispose(It.IsAny<IDisposable>()));

        var page = new TestPage()
        {
            PageContext = new PageContext
            {
                HttpContext = mockHttpContext.Object
            }
        };

        var fileStream = Stream.Null;

        // Act
        var result = page.File(fileStream, "application/pdf");

        // Assert
        Assert.NotNull(result);
        Assert.Same(fileStream, result.FileStream);
        Assert.Equal("application/pdf", result.ContentType.ToString());
        Assert.Equal(string.Empty, result.FileDownloadName);
    }

    [Fact]
    public void File_WithStreamAndFileDownloadName()
    {
        // Arrange
        var mockHttpContext = new Mock<HttpContext>();

        var page = new TestPage()
        {
            PageContext = new PageContext
            {
                HttpContext = mockHttpContext.Object
            }
        };

        var fileStream = Stream.Null;

        // Act
        var result = page.File(fileStream, "application/pdf", "someDownloadName");

        // Assert
        Assert.NotNull(result);
        Assert.Same(fileStream, result.FileStream);
        Assert.Equal("application/pdf", result.ContentType.ToString());
        Assert.Equal("someDownloadName", result.FileDownloadName);
    }

    [Fact]
    public void Unauthorized_SetsStatusCode()
    {
        // Arrange
        var page = new TestPage();

        // Act
        var result = page.Unauthorized();

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, result.StatusCode);
    }

    [Fact]
    public void BadRequest_SetsStatusCode()
    {
        // Arrange
        var page = new TestPage();

        // Act
        var result = page.BadRequest();

        // Assert
        Assert.IsType<BadRequestResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
    }

    [Fact]
    public void BadRequest_SetsStatusCodeAndResponseContent()
    {
        // Arrange
        var page = new TestPage();

        // Act
        var result = page.BadRequest("Test Content");

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
        Assert.Equal("Test Content", result.Value);
    }

    [Fact]
    public void NotFound_SetsStatusCode()
    {
        // Arrange
        var page = new TestPage();

        // Act
        var result = page.NotFound();

        // Assert
        Assert.IsType<NotFoundResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, result.StatusCode);
    }

    [Fact]
    public void NotFound_SetsStatusCodeAndResponseContent()
    {
        // Arrange
        var page = new TestPage();

        // Act
        var result = page.NotFound("Test Content");

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, result.StatusCode);
        Assert.Equal("Test Content", result.Value);
    }

    [Fact]
    public void Content_WithParameterContentString_SetsResultContent()
    {
        // Arrange
        var page = new TestPage();

        // Act
        var actualContentResult = page.Content("TestContent");

        // Assert
        Assert.IsType<ContentResult>(actualContentResult);
        Assert.Equal("TestContent", actualContentResult.Content);
        Assert.Null(actualContentResult.ContentType);
    }

    [Fact]
    public void Content_WithParameterContentStringAndContentType_SetsResultContentAndContentType()
    {
        // Arrange
        var page = new TestPage();

        // Act
        var actualContentResult = page.Content("TestContent", "text/plain");

        // Assert
        Assert.IsType<ContentResult>(actualContentResult);
        Assert.Equal("TestContent", actualContentResult.Content);
        Assert.Null(MediaType.GetEncoding(actualContentResult.ContentType));
        Assert.Equal("text/plain", actualContentResult.ContentType.ToString());
    }

    [Fact]
    public void Content_WithParameterContentAndTypeAndEncoding_SetsResultContentAndTypeAndEncoding()
    {
        // Arrange
        var page = new TestPage();

        // Act
        var actualContentResult = page.Content("TestContent", "text/plain", Encoding.UTF8);

        // Assert
        Assert.IsType<ContentResult>(actualContentResult);
        Assert.Equal("TestContent", actualContentResult.Content);
        Assert.Same(Encoding.UTF8, MediaType.GetEncoding(actualContentResult.ContentType));
        Assert.Equal("text/plain; charset=utf-8", actualContentResult.ContentType.ToString());
    }

    [Fact]
    public void Content_NoContentType_DefaultEncodingIsUsed()
    {
        // Arrange
        var contentPage = new ContentPage();

        // Act
        var contentResult = (ContentResult)contentPage.Content_WithNoEncoding();

        // Assert
        // The default content type of ContentResult is used when the result is executed.
        Assert.Null(contentResult.ContentType);
    }

    [Fact]
    public void Content_InvalidCharset_DefaultEncodingIsUsed()
    {
        // Arrange
        var contentPage = new ContentPage();
        var contentType = "text/xml; charset=invalid; p1=p1-value";

        // Act
        var contentResult = (ContentResult)contentPage.Content_WithInvalidCharset();

        // Assert
        Assert.NotNull(contentResult.ContentType);
        Assert.Equal(contentType, contentResult.ContentType.ToString());
        // The default encoding of ContentResult is used when this result is executed.
        Assert.Null(MediaType.GetEncoding(contentResult.ContentType));
    }

    [Fact]
    public void Content_CharsetAndEncodingProvided_EncodingIsUsed()
    {
        // Arrange
        var contentPage = new ContentPage();
        var contentType = "text/xml; charset=us-ascii; p1=p1-value";

        // Act
        var contentResult = (ContentResult)contentPage.Content_WithEncodingInCharset_AndEncodingParameter();

        // Assert
        MediaTypeAssert.Equal(contentType, contentResult.ContentType);
    }

    [Fact]
    public void Content_CharsetInContentType_IsUsedForEncoding()
    {
        // Arrange
        var contentPage = new ContentPage();
        var contentType = "text/xml; charset=us-ascii; p1=p1-value";

        // Act
        var contentResult = (ContentResult)contentPage.Content_WithEncodingInCharset();

        // Assert
        Assert.Equal(contentType, contentResult.ContentType);
    }

    [Fact]
    public void StatusCode_SetObject()
    {
        // Arrange
        var statusCode = 204;
        var value = new { Value = 42 };

        var statusCodePage = new StatusCodePage();

        // Act
        var result = (ObjectResult)statusCodePage.StatusCode_Object(statusCode, value);

        // Assert
        Assert.Equal(statusCode, result.StatusCode);
        Assert.Equal(value, result.Value);
    }

    [Fact]
    public void StatusCode_SetObjectNull()
    {
        // Arrange
        var statusCode = 204;
        object value = null;

        var statusCodePage = new StatusCodePage();

        // Act
        var result = statusCodePage.StatusCode_Object(statusCode, value);

        // Assert
        Assert.Equal(statusCode, result.StatusCode);
        Assert.Equal(value, result.Value);
    }

    [Fact]
    public void StatusCode_SetsStatusCode()
    {
        // Arrange
        var statusCode = 205;
        var statusCodePage = new StatusCodePage();

        // Act
        var result = statusCodePage.StatusCode_Int(statusCode);

        // Assert
        Assert.Equal(statusCode, result.StatusCode);
    }

    [Fact]
    public void PartialView_WithName()
    {
        // Arrange
        var modelMetadataProvider = new EmptyModelMetadataProvider();
        var viewData = new ViewDataDictionary(modelMetadataProvider, new ModelStateDictionary());
        var pageModel = new TestPage
        {
            ViewContext = new ViewContext
            {
                ViewData = viewData
            },
            MetadataProvider = modelMetadataProvider,
        };
        viewData.Model = pageModel;

        // Act
        var result = pageModel.Partial("LoginStatus");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("LoginStatus", result.ViewName);
        Assert.Null(result.Model);
    }

    [Fact]
    public void PartialView_WithNameAndModel()
    {
        // Arrange
        var modelMetadataProvider = new EmptyModelMetadataProvider();
        var viewData = new ViewDataDictionary(modelMetadataProvider, new ModelStateDictionary());
        var pageModel = new TestPage
        {
            ViewContext = new ViewContext
            {
                ViewData = viewData
            },
            MetadataProvider = modelMetadataProvider,
        };
        viewData.Model = pageModel;
        var model = new { Username = "Admin" };

        // Act
        var result = pageModel.Partial("LoginStatus", model);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("LoginStatus", result.ViewName);
        Assert.Equal(model, result.Model);
    }

    [Fact]
    public void ViewComponent_WithName()
    {
        // Arrange
        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary());
        var page = new TestPage
        {
            ViewContext = new ViewContext
            {
                ViewData = viewData,
            },
        };

        // Act
        var result = page.ViewComponent("TagCloud");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("TagCloud", result.ViewComponentName);
        Assert.Same(viewData, result.ViewData);
    }

    [Fact]
    public void ViewComponent_WithType()
    {
        // Arrange
        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary());
        var page = new TestPage
        {
            ViewContext = new ViewContext
            {
                ViewData = viewData,
            },
        };

        // Act
        var result = page.ViewComponent(typeof(Guid));

        // Assert
        Assert.NotNull(result);
        Assert.Equal(typeof(Guid), result.ViewComponentType);
        Assert.Same(viewData, result.ViewData);
    }

    [Fact]
    public void ViewComponent_WithArguments()
    {
        // Arrange
        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary());
        var page = new TestPage
        {
            ViewContext = new ViewContext
            {
                ViewData = viewData,
            },
        };

        var arguments = new { Arg1 = "Hi", Arg2 = "There" };

        // Act
        var result = page.ViewComponent(typeof(Guid), arguments);

        // Assert
        Assert.NotNull(result);

        Assert.Equal(typeof(Guid), result.ViewComponentType);
        Assert.Same(arguments, result.Arguments);
        Assert.Same(viewData, result.ViewData);
    }

    [Fact]
    public async Task TryUpdateModel_ReturnsFalse_IfValueProviderFactoryThrows()
    {
        // Arrange
        var valueProviderFactory = new Mock<IValueProviderFactory>();
        valueProviderFactory.Setup(f => f.CreateValueProviderAsync(It.IsAny<ValueProviderFactoryContext>()))
            .Throws(new ValueProviderException("some error"));

        var pageModel = new TestPage
        {
            PageContext = new PageContext
            {
                ValueProviderFactories = new[] { valueProviderFactory.Object },
            }
        };

        var model = new object();

        // Act
        var result = await pageModel.TryUpdateModelAsync(model);

        // Assert
        Assert.False(result);
        var modelState = Assert.Single(pageModel.ModelState);
        Assert.Empty(modelState.Key);
        var error = Assert.Single(modelState.Value.Errors);
        Assert.Equal("some error", error.ErrorMessage);
    }

    public static IEnumerable<object[]> RedirectTestData
    {
        get
        {
            yield return new object[]
            {
                    null,
                    null,
            };

            yield return new object[]
            {
                    new Dictionary<string, object> { { "hello", "world" } },
                    new RouteValueDictionary() { { "hello", "world" } },
            };

            var expected2 = new Dictionary<string, object>
                {
                    { "test", "case" },
                    { "sample", "route" },
                };

            yield return new object[]
            {
                    new RouteValueDictionary(expected2),
                    new RouteValueDictionary(expected2),
            };
        }
    }

    private class ContentPage : Page
    {
        public IActionResult Content_WithNoEncoding()
        {
            return Content("Hello!!");
        }

        public IActionResult Content_WithEncodingInCharset()
        {
            return Content("Hello!!", "text/xml; charset=us-ascii; p1=p1-value");
        }

        public IActionResult Content_WithInvalidCharset()
        {
            return Content("Hello!!", "text/xml; charset=invalid; p1=p1-value");
        }

        public IActionResult Content_WithEncodingInCharset_AndEncodingParameter()
        {
            return Content("Hello!!", "text/xml; charset=invalid; p1=p1-value", Encoding.ASCII);
        }

        public override Task ExecuteAsync()
        {
            throw new NotImplementedException();
        }
    }

    private class StatusCodePage : Page
    {
        public override Task ExecuteAsync()
        {
            throw new NotImplementedException();
        }

        public StatusCodeResult StatusCode_Int(int statusCode)
        {
            return StatusCode(statusCode);
        }

        public ObjectResult StatusCode_Object(int statusCode, object value)
        {
            return StatusCode(statusCode, value);
        }
    }

    private class TestPage : Page
    {
        public override Task ExecuteAsync()
        {
            throw new NotImplementedException();
        }
    }
}
