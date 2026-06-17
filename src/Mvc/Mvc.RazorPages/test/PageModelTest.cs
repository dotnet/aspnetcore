// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Microsoft.AspNetCore.Mvc.RazorPages;

public class PageModelTest
{
    [Fact]
    public void PageContext_GetsInitialized()
    {
        // Arrange
        var pageModel = new TestPageModel();

        // Act & Assert
        Assert.NotNull(pageModel.PageContext);
    }

    [Fact]
    public void Redirect_WithParameterUrl_SetsRedirectResultSameUrl()
    {
        // Arrange
        var pageModel = new TestPageModel();
        var url = "/test/url";

        // Act
        var result = pageModel.Redirect(url);

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
        var pageModel = new TestPageModel();
        var url = "/test/url";

        // Act
        var result = pageModel.RedirectPermanent(url);

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
        var pageModel = new TestPageModel();
        var url = "/test/url";

        // Act
        var result = pageModel.RedirectPreserveMethod(url);

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
        var pageModel = new TestPageModel();
        var url = "/test/url";

        // Act
        var result = pageModel.RedirectPermanentPreserveMethod(url);

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
        var pageModel = new TestPageModel();

        // Act & Assert
        ExceptionAssert.ThrowsArgument(
            () => pageModel.Redirect(url: url),
            "url",
            expectedMessage);
    }

    [Theory]
    [InlineData(null, "Value cannot be null.")]
    [InlineData("", "The value cannot be an empty string.")]
    public void RedirectPreserveMethod_WithParameter_NullOrEmptyUrl_Throws(string url, string expectedMessage)
    {
        // Arrange
        var pageModel = new TestPageModel();

        // Act & Assert
        ExceptionAssert.ThrowsArgument(
            () => pageModel.RedirectPreserveMethod(url: url),
            "url",
            expectedMessage);
    }

    [Fact]
    public void LocalRedirect_WithParameterUrl_SetsLocalRedirectResultWithSameUrl()
    {
        // Arrange
        var pageModel = new TestPageModel();
        var url = "/test/url";

        // Act
        var result = pageModel.LocalRedirect(url);

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
        var pageModel = new TestPageModel();
        var url = "/test/url";

        // Act
        var result = pageModel.LocalRedirectPermanent(url);

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
        var pageModel = new TestPageModel();
        var url = "/test/url";

        // Act
        var result = pageModel.LocalRedirectPreserveMethod(url);

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
        var pageModel = new TestPageModel();
        var url = "/test/url";

        // Act
        var result = pageModel.LocalRedirectPermanentPreserveMethod(url);

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
        var pageModel = new TestPageModel();

        // Act & Assert
        ExceptionAssert.ThrowsArgument(
            () => pageModel.LocalRedirect(localUrl: url),
            "localUrl",
            expectedMessage);
    }

    [Theory]
    [InlineData(null, "Value cannot be null.")]
    [InlineData("", "The value cannot be an empty string.")]
    public void LocalRedirectPreserveMethod_WithParameter_NullOrEmptyUrl_Throws(string url, string expectedMessage)
    {
        // Arrange
        var pageModel = new TestPageModel();

        // Act & Assert
        ExceptionAssert.ThrowsArgument(
            () => pageModel.LocalRedirectPreserveMethod(localUrl: url),
            "localUrl",
            expectedMessage);
    }

    [Theory]
    [InlineData(null, "Value cannot be null.")]
    [InlineData("", "The value cannot be an empty string.")]
    public void LocalRedirectPermanentPreserveMethod_WithParameter_NullOrEmptyUrl_Throws(string url, string expectedMessage)
    {
        // Arrange
        var pageModel = new TestPageModel();

        // Act & Assert
        ExceptionAssert.ThrowsArgument(
            () => pageModel.LocalRedirectPermanentPreserveMethod(localUrl: url),
            "localUrl",
            expectedMessage);
    }

    [Theory]
    [InlineData(null, "Value cannot be null.")]
    [InlineData("", "The value cannot be an empty string.")]
    public void RedirectPermanent_WithParameter_NullOrEmptyUrl_Throws(string url, string expectedMessage)
    {
        // Arrange
        var pageModel = new TestPageModel();

        // Act & Assert
        ExceptionAssert.ThrowsArgument(
            () => pageModel.RedirectPermanent(url: url),
            "url",
            expectedMessage);
    }

    [Theory]
    [InlineData(null, "Value cannot be null.")]
    [InlineData("", "The value cannot be an empty string.")]
    public void RedirectPermanentPreserveMethod_WithParameter_NullOrEmptyUrl_Throws(string url, string expectedMessage)
    {
        // Arrange
        var pageModel = new TestPageModel();

        // Act & Assert
        ExceptionAssert.ThrowsArgument(
            () => pageModel.RedirectPermanentPreserveMethod(url: url),
            "url",
            expectedMessage);
    }

    [Fact]
    public void RedirectToAction_WithParameterActionName_SetsResultActionName()
    {
        // Arrange
        var pageModel = new TestPageModel();

        // Act
        var resultTemporary = pageModel.RedirectToAction("SampleAction");

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
        var pageModel = new TestPageModel();

        // Act
        var resultTemporary = pageModel.RedirectToActionPreserveMethod(actionName: "SampleAction");

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
        var pageModel = new TestPageModel();

        // Act
        var resultPermanent = pageModel.RedirectToActionPermanent("SampleAction");

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
        var pageModel = new TestPageModel();

        // Act
        var resultPermanent = pageModel.RedirectToActionPermanentPreserveMethod(actionName: "SampleAction");

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
        var pageModel = new TestPageModel();

        // Act
        var resultTemporary = pageModel.RedirectToAction("SampleAction", controllerName);

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
        var pageModel = new TestPageModel();

        // Act
        var resultTemporary = pageModel.RedirectToActionPreserveMethod(actionName: "SampleAction", controllerName: controllerName);

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
        var pageModel = new TestPageModel();

        // Act
        var resultPermanent = pageModel.RedirectToActionPermanent("SampleAction", controllerName);

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
        var pageModel = new TestPageModel();

        // Act
        var resultPermanent = pageModel.RedirectToActionPermanentPreserveMethod(actionName: "SampleAction", controllerName: controllerName);

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
        var pageModel = new TestPageModel();

        // Act
        var resultTemporary = pageModel.RedirectToAction("SampleAction", "SampleController", routeValues);

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
        var pageModel = new TestPageModel();

        // Act
        var resultTemporary = pageModel.RedirectToActionPreserveMethod(
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
        var pageModel = new TestPageModel();

        // Act
        var resultPermanent = pageModel.RedirectToActionPermanent(
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
        var pageModel = new TestPageModel();

        // Act
        var resultPermanent = pageModel.RedirectToActionPermanentPreserveMethod(
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
        var pageModel = new TestPageModel();

        // Act
        var resultTemporary = pageModel.RedirectToAction(actionName: null, routeValues: routeValues);

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
        var pageModel = new TestPageModel();

        // Act
        var resultTemporary = pageModel.RedirectToActionPreserveMethod(actionName: null, routeValues: routeValues);

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
        var pageModel = new TestPageModel();
        var expectedAction = "Action";
        var expectedController = "Home";
        var expectedFragment = "test";

        // Act
        var result = pageModel.RedirectToAction("Action", "Home", routeValues, "test");

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
        var pageModel = new TestPageModel();
        var expectedAction = "Action";
        var expectedController = "Home";
        var expectedFragment = "test";

        // Act
        var result = pageModel.RedirectToActionPreserveMethod("Action", "Home", routeValues, "test");

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
        var pageModel = new TestPageModel();

        // Act
        var resultPermanent = pageModel.RedirectToActionPermanent(null, routeValues);

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
        var pageModel = new TestPageModel();

        // Act
        var resultPermanent = pageModel.RedirectToActionPermanentPreserveMethod(actionName: null, routeValues: routeValues);

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
        var pageModel = new TestPageModel();
        var expectedAction = "Action";
        var expectedController = "Home";
        var expectedFragment = "test";

        // Act
        var result = pageModel.RedirectToActionPermanent("Action", "Home", routeValues, fragment: "test");

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
        var pageModel = new TestPageModel();
        var expectedAction = "Action";
        var expectedController = "Home";
        var expectedFragment = "test";

        // Act
        var result = pageModel.RedirectToActionPermanentPreserveMethod(
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
        var pageModel = new TestPageModel();

        // Act
        var resultTemporary = pageModel.RedirectToRoute(routeValues);

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
        var pageModel = new TestPageModel();

        // Act
        var resultTemporary = pageModel.RedirectToRoutePreserveMethod(routeValues: routeValues);

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
        var pageModel = new TestPageModel();
        var expectedRoute = "TestRoute";
        var expectedFragment = "test";

        // Act
        var result = pageModel.RedirectToRoute("TestRoute", routeValues, "test");

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
        var pageModel = new TestPageModel();
        var expectedRoute = "TestRoute";
        var expectedFragment = "test";

        // Act
        var result = pageModel.RedirectToRoutePreserveMethod(routeName: "TestRoute", routeValues: routeValues, fragment: "test");

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
        var pageModel = new TestPageModel();

        // Act
        var resultPermanent = pageModel.RedirectToRoutePermanent(routeValues);

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
        var pageModel = new TestPageModel();

        // Act
        var resultPermanent = pageModel.RedirectToRoutePermanentPreserveMethod(routeValues: routeValues);

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
        var pageModel = new TestPageModel();
        var expectedRoute = "TestRoute";
        var expectedFragment = "test";

        // Act
        var result = pageModel.RedirectToRoutePermanent("TestRoute", routeValues, "test");

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
        var pageModel = new TestPageModel();
        var expectedRoute = "TestRoute";
        var expectedFragment = "test";

        // Act
        var result = pageModel.RedirectToRoutePermanentPreserveMethod(routeName: "TestRoute", routeValues: routeValues, fragment: "test");

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
        var pageModel = new TestPageModel();
        var routeName = "CustomRouteName";

        // Act
        var resultTemporary = pageModel.RedirectToRoute(routeName);

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
        var pageModel = new TestPageModel();
        var routeName = "CustomRouteName";

        // Act;
        var resultTemporary = pageModel.RedirectToRoutePreserveMethod(routeName: routeName);

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
        var pageModel = new TestPageModel();
        var routeName = "CustomRouteName";

        // Act
        var resultPermanent = pageModel.RedirectToRoutePermanent(routeName);

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
        var pageModel = new TestPageModel();
        var routeName = "CustomRouteName";

        // Act
        var resultPermanent = pageModel.RedirectToRoutePermanentPreserveMethod(routeName: routeName);

        // Assert
        Assert.IsType<RedirectToRouteResult>(resultPermanent);
        Assert.True(resultPermanent.PreserveMethod);
        Assert.True(resultPermanent.Permanent);
        Assert.Same(routeName, resultPermanent.RouteName);
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

    [Fact]
    public void RedirectToPage_WithNoArguments()
    {
        // Arrange
        var pageModel = new TestPageModel();

        // Act
        var result = pageModel.RedirectToPage();

        // Assert
        Assert.IsType<RedirectToPageResult>(result);
        Assert.Null(result.PageName);
    }

    [Fact]
    public void RedirectToPage_WithPageName()
    {
        // Arrange
        var pageModel = new TestPageModel();
        var pageName = "/Page";

        // Act
        var result = pageModel.RedirectToPage(pageName);

        // Assert
        Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(pageName, result.PageName);
    }

    [Fact]
    public void RedirectToPage_WithRouteValues()
    {
        // Arrange
        var pageModel = new TestPageModel();
        var routeValues = new { key = "value" };

        // Act
        var result = pageModel.RedirectToPage(routeValues);

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
        var pageModel = new TestPageModel();
        var pageName = "/Page-Name";
        var pageHandler = "page-handler";

        // Act
        var result = pageModel.RedirectToPage(pageName, pageHandler);

        // Assert
        Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(pageName, result.PageName);
        Assert.Equal(pageHandler, result.PageHandler);
    }

    [Fact]
    public void RedirectToPage_WithPageNameAndRouteValues()
    {
        // Arrange
        var pageModel = new TestPageModel();
        var pageName = "/Page-Name";
        var routeValues = new { key = "value" };

        // Act
        var result = pageModel.RedirectToPage(pageName, routeValues);

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
        var pageModel = new TestPageModel();
        var pageName = "/Page-Name";
        var pageHandler = "page-handler";
        var fragment = "fragment";

        // Act
        var result = pageModel.RedirectToPage(pageName, pageHandler, fragment);

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
        var pageModel = new TestPageModel();
        var pageName = "/Page-Name";
        var pageHandler = "page-handler";
        var fragment = "fragment";
        var routeValues = new { key = "value" };

        // Act
        var result = pageModel.RedirectToPage(pageName, pageHandler, routeValues, fragment);

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
        var pageModel = new TestPageModel();
        var pageName = "/Page-Name";

        // Act
        var result = pageModel.RedirectToPagePermanent(pageName);

        // Assert
        Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(pageName, result.PageName);
        Assert.True(result.Permanent);
    }

    [Fact]
    public void RedirectToPagePermanent_WithPageNameAndPageHandler()
    {
        // Arrange
        var pageModel = new TestPageModel();
        var pageName = "/Page-Name";
        var pageHandler = "page-handler";

        // Act
        var result = pageModel.RedirectToPagePermanent(pageName, pageHandler);

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
        var pageModel = new TestPageModel();
        var pageName = "/Page-Name";
        var routeValues = new { key = "value" };

        // Act
        var result = pageModel.RedirectToPagePermanent(pageName, routeValues);

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
        var pageModel = new TestPageModel();
        var pageName = "/Page-Name";
        var pageHandler = "page-handler";
        var routeValues = new { key = "value" };

        // Act
        var result = pageModel.RedirectToPagePermanent(pageName, pageHandler, routeValues);

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
        var pageModel = new TestPageModel();
        var pageName = "/Page-Name";
        var pageHandler = "page-handler";
        var fragment = "fragment";

        // Act
        var result = pageModel.RedirectToPagePermanent(pageName, pageHandler, fragment);

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
        var pageModel = new TestPageModel();
        var pageName = "/Page-Name";
        var pageHandler = "page-handler";
        var routeValues = new { key = "value" };
        var fragment = "fragment";

        // Act
        var result = pageModel.RedirectToPagePermanent(pageName, pageHandler, routeValues, fragment);

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
        var pageModel = new TestPageModel();
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
        var pageModel = new TestPageModel();
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
        var pageModel = new TestPageModel();
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

    [Theory]
    [MemberData(nameof(RedirectTestData))]
    public void RedirectToRoute_WithParameterRouteNameAndRouteValues_SetsResultSameRouteNameAndRouteValues(
        object routeValues,
        IEnumerable<KeyValuePair<string, object>> expected)
    {
        // Arrange
        var pageModel = new TestPageModel();
        var routeName = "CustomRouteName";

        // Act
        var resultTemporary = pageModel.RedirectToRoute(routeName, routeValues);

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
        var pageModel = new TestPageModel();
        var routeName = "CustomRouteName";

        // Act
        var resultTemporary = pageModel.RedirectToRoutePreserveMethod(routeName: routeName, routeValues: routeValues);

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
        var pageModel = new TestPageModel();
        var routeName = "CustomRouteName";

        // Act
        var resultPermanent = pageModel.RedirectToRoutePermanent(routeName, routeValues);

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
        var pageModel = new TestPageModel();
        var routeName = "CustomRouteName";

        // Act
        var resultPermanent = pageModel.RedirectToRoutePermanentPreserveMethod(routeName: routeName, routeValues: routeValues);

        // Assert
        Assert.IsType<RedirectToRouteResult>(resultPermanent);
        Assert.True(resultPermanent.PreserveMethod);
        Assert.True(resultPermanent.Permanent);
        Assert.Same(routeName, resultPermanent.RouteName);
        Assert.Equal(expected, resultPermanent.RouteValues);
    }

    [Fact]
    public void File_WithContents()
    {
        // Arrange
        var pageModel = new TestPageModel();
        var fileContents = new byte[0];

        // Act
        var result = pageModel.File(fileContents, "application/pdf");

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
        var pageModel = new TestPageModel();
        var fileContents = new byte[0];

        // Act
        var result = pageModel.File(fileContents, "application/pdf", "someDownloadName");

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
        var pageModel = new TestPageModel();
        var path = Path.GetFullPath("somepath");

        // Act
        var result = pageModel.File(path, "application/pdf");

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
        var pageModel = new TestPageModel();
        var path = Path.GetFullPath("somepath");

        // Act
        var result = pageModel.File(path, "application/pdf", "someDownloadName");

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

        var pageModel = new TestPageModel()
        {
            PageContext = new PageContext
            {
                HttpContext = mockHttpContext.Object
            }
        };

        var fileStream = Stream.Null;

        // Act
        var result = pageModel.File(fileStream, "application/pdf");

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

        var pageModel = new TestPageModel()
        {
            PageContext = new PageContext
            {
                HttpContext = mockHttpContext.Object
            }
        };

        var fileStream = Stream.Null;

        // Act
        var result = pageModel.File(fileStream, "application/pdf", "someDownloadName");

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
        var pageModel = new TestPageModel();

        // Act
        var result = pageModel.Unauthorized();

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
        var pageModel = new TestPageModel();

        // Act
        var result = pageModel.NotFound();

        // Assert
        Assert.IsType<NotFoundResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, result.StatusCode);
    }

    [Fact]
    public void NotFound_SetsStatusCodeAndResponseContent()
    {
        // Arrange
        var pageModel = new TestPageModel();

        // Act
        var result = pageModel.NotFound("Test Content");

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, result.StatusCode);
        Assert.Equal("Test Content", result.Value);
    }

    [Fact]
    public void Content_WithParameterContentString_SetsResultContent()
    {
        // Arrange
        var pageModel = new TestPageModel();

        // Act
        var actualContentResult = pageModel.Content("TestContent");

        // Assert
        Assert.IsType<ContentResult>(actualContentResult);
        Assert.Equal("TestContent", actualContentResult.Content);
        Assert.Null(actualContentResult.ContentType);
    }

    [Fact]
    public void Content_WithParameterContentStringAndContentType_SetsResultContentAndContentType()
    {
        // Arrange
        var pageModel = new TestPageModel();

        // Act
        var actualContentResult = pageModel.Content("TestContent", "text/plain");

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
        var pageModel = new TestPageModel();

        // Act
        var actualContentResult = pageModel.Content("TestContent", "text/plain", Encoding.UTF8);

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
        var contentPageModel = new ContentPageModel();

        // Act
        var contentResult = (ContentResult)contentPageModel.Content_WithNoEncoding();

        // Assert
        // The default content type of ContentResult is used when the result is executed.
        Assert.Null(contentResult.ContentType);
    }

    [Fact]
    public void Content_InvalidCharset_DefaultEncodingIsUsed()
    {
        // Arrange
        var contentPageModel = new ContentPageModel();
        var contentType = "text/xml; charset=invalid; p1=p1-value";

        // Act
        var contentResult = (ContentResult)contentPageModel.Content_WithInvalidCharset();

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
        var contentPageModel = new ContentPageModel();
        var contentType = "text/xml; charset=us-ascii; p1=p1-value";

        // Act
        var contentResult = (ContentResult)contentPageModel.Content_WithEncodingInCharset_AndEncodingParameter();

        // Assert
        MediaTypeAssert.Equal(contentType, contentResult.ContentType);
    }

    [Fact]
    public void Content_CharsetInContentType_IsUsedForEncoding()
    {
        // Arrange
        var contentPageModel = new ContentPageModel();
        var contentType = "text/xml; charset=us-ascii; p1=p1-value";

        // Act
        var contentResult = (ContentResult)contentPageModel.Content_WithEncodingInCharset();

        // Assert
        Assert.Equal(contentType, contentResult.ContentType);
    }

    [Fact]
    public void StatusCode_SetObject()
    {
        // Arrange
        var statusCode = 204;
        var value = new { Value = 42 };

        var statusCodeController = new StatusCodePageModel();

        // Act
        var result = (ObjectResult)statusCodeController.StatusCode_Object(statusCode, value);

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

        var statusCodeController = new StatusCodePageModel();

        // Act
        var result = statusCodeController.StatusCode_Object(statusCode, value);

        // Assert
        Assert.Equal(statusCode, result.StatusCode);
        Assert.Equal(value, result.Value);
    }

    [Fact]
    public void StatusCode_SetsStatusCode()
    {
        // Arrange
        var statusCode = 205;
        var statusCodeModel = new StatusCodePageModel();

        // Act
        var result = statusCodeModel.StatusCode_Int(statusCode);

        // Assert
        Assert.Equal(statusCode, result.StatusCode);
    }

    [Fact]
    public void PageModelPropertiesArePopulatedFromContext()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var modelState = new ModelStateDictionary();
        var actionContext = new ActionContext(httpContext, new RouteData(), new PageActionDescriptor(), modelState);
        var modelMetadataProvider = new EmptyModelMetadataProvider();
        var viewData = new ViewDataDictionary(modelMetadataProvider, modelState);
        var pageContext = new PageContext(actionContext)
        {
            ViewData = viewData,
        };

        var page = new TestPage
        {
            PageContext = pageContext,
        };

        var pageModel = new TestPageModel
        {
            PageContext = pageContext,
        };

        // Act & Assert
        Assert.Same(pageContext, pageModel.PageContext);
        Assert.Same(httpContext, pageModel.HttpContext);
        Assert.Same(httpContext.Request, pageModel.Request);
        Assert.Same(httpContext.Response, pageModel.Response);
        Assert.Same(modelState, pageModel.ModelState);
        Assert.Same(viewData, pageModel.ViewData);
    }

    [Fact]
    public async Task TryUpdateModel_ReturnsFalse_IfValueProviderFactoryThrows()
    {
        // Arrange
        var valueProviderFactory = new Mock<IValueProviderFactory>();
        valueProviderFactory.Setup(f => f.CreateValueProviderAsync(It.IsAny<ValueProviderFactoryContext>()))
            .Throws(new ValueProviderException("some error"));

        var pageModel = new TestPageModel
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

    [Fact]
    public void UrlHelperIsSet()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var urlHelper = Mock.Of<IUrlHelper>();
        var urlHelperFactory = new Mock<IUrlHelperFactory>();
        urlHelperFactory.Setup(f => f.GetUrlHelper(It.IsAny<ActionContext>()))
            .Returns(urlHelper);
        httpContext.RequestServices = new ServiceCollection()
            .AddSingleton(urlHelperFactory.Object)
            .BuildServiceProvider();
        var actionContext = new ActionContext
        {
            HttpContext = httpContext,
        };
        var pageContext = new PageContext
        {
            HttpContext = httpContext,
        };

        var pageModel = new TestPageModel
        {
            PageContext = pageContext,
        };

        // Act & Assert
        Assert.Same(urlHelper, pageModel.Url);
    }

    [Fact]
    public void Redirect_ReturnsARedirectResult()
    {
        // Arrange
        var pageModel = new TestPageModel();

        // Act
        var result = pageModel.Redirect("test-url");

        // Assert
        var redirectResult = Assert.IsType<RedirectResult>(result);
        Assert.Equal("test-url", redirectResult.Url);
    }

    [Fact]
    public void View_ReturnsPageViewResult()
    {
        // Arrange
        var page = new TestPage();
        var pageModel = new TestPageModel
        {
            PageContext = new PageContext()
        };

        // Act
        var result = pageModel.Page();

        // Assert
        var pageResult = Assert.IsType<PageResult>(result);
        Assert.Null(pageResult.Page); // This is set by the invoker
    }

    [Fact]
    public async Task AsyncPageHandlerExecutingMethod_InvokeSyncMethods()
    {
        // Arrange
        var pageContext = new PageContext(new ActionContext(
            new DefaultHttpContext(),
            new RouteData(),
            new PageActionDescriptor(),
            new ModelStateDictionary()));
        var pageHandlerExecutingContext = new PageHandlerExecutingContext(
            pageContext,
            Array.Empty<IFilterMetadata>(),
            new HandlerMethodDescriptor(),
            new Dictionary<string, object>(),
            new object());
        var pageHandlerExecutedContext = new PageHandlerExecutedContext(
            pageContext,
            Array.Empty<IFilterMetadata>(),
            new HandlerMethodDescriptor(),
            new object());
        var testPageModel = new Mock<PageModel> { CallBase = true };
        testPageModel.Setup(p => p.OnPageHandlerExecuting(pageHandlerExecutingContext))
            .Verifiable();
        testPageModel.Setup(p => p.OnPageHandlerExecuted(pageHandlerExecutedContext))
            .Verifiable();

        // Act
        await testPageModel.Object.OnPageHandlerExecutionAsync(
            pageHandlerExecutingContext,
            () => Task.FromResult(pageHandlerExecutedContext));

        testPageModel.Verify();
    }

    [Fact]
    public async Task AsyncPageHandlerExecutingMethod__DoesNotInvokeExecutedMethod_IfResultIsSet()
    {
        // Arrange
        var pageContext = new PageContext(new ActionContext(
            new DefaultHttpContext(),
            new RouteData(),
            new PageActionDescriptor(),
            new ModelStateDictionary()));
        var pageHandlerExecutingContext = new PageHandlerExecutingContext(
            pageContext,
            Array.Empty<IFilterMetadata>(),
            new HandlerMethodDescriptor(),
            new Dictionary<string, object>(),
            new object());
        var pageHandlerExecutedContext = new PageHandlerExecutedContext(
            pageContext,
            Array.Empty<IFilterMetadata>(),
            new HandlerMethodDescriptor(),
            new object());
        var testPageModel = new Mock<PageModel>() { CallBase = true };
        testPageModel.Setup(p => p.OnPageHandlerExecuting(pageHandlerExecutingContext))
            .Callback((PageHandlerExecutingContext context) => context.Result = new PageResult())
            .Verifiable();
        testPageModel.Setup(p => p.OnPageHandlerExecuted(pageHandlerExecutedContext))
            .Throws(new Exception("Shouldn't be called"));

        // Act
        await testPageModel.Object.OnPageHandlerExecutionAsync(
            pageHandlerExecutingContext,
            () => Task.FromResult(pageHandlerExecutedContext));

        testPageModel.Verify();
    }

    [Fact]
    public async Task AsyncPageHandlerSelectingMethod_InvokeSyncMethods()
    {
        // Arrange
        var pageContext = new PageContext(new ActionContext(
            new DefaultHttpContext(),
            new RouteData(),
            new PageActionDescriptor(),
            new ModelStateDictionary()));
        var pageHandlerSelectedContext = new PageHandlerSelectedContext(
            pageContext,
            Array.Empty<IFilterMetadata>(),
            new object());

        var testPageModel = new Mock<PageModel> { CallBase = true };
        testPageModel.Setup(p => p.OnPageHandlerSelected(pageHandlerSelectedContext))
            .Verifiable();

        // Act
        await testPageModel.Object.OnPageHandlerSelectionAsync(pageHandlerSelectedContext);

        testPageModel.Verify();
    }

    [Fact]
    public void PartialView_WithName()
    {
        // Arrange
        var modelMetadataProvider = new EmptyModelMetadataProvider();
        var viewData = new ViewDataDictionary(modelMetadataProvider, new ModelStateDictionary());
        var pageModel = new TestPageModel
        {
            PageContext = new PageContext
            {
                ViewData = viewData
            },
            MetadataProvider = modelMetadataProvider,
        };

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
        var pageModel = new TestPageModel
        {
            PageContext = new PageContext
            {
                ViewData = viewData
            },
            MetadataProvider = modelMetadataProvider,
        };
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
        var pageModel = new TestPageModel
        {
            PageContext = new PageContext
            {
                ViewData = viewData,
            },
        };

        // Act
        var result = pageModel.ViewComponent("TagCloud");

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
        var pageModel = new TestPageModel
        {
            PageContext = new PageContext
            {
                ViewData = viewData,
            },
        };

        // Act
        var result = pageModel.ViewComponent(typeof(Guid));

        // Assert
        Assert.NotNull(result);
        Assert.Equal(typeof(Guid), result.ViewComponentType);
        Assert.Same(viewData, result.ViewData);
    }

    [Fact]
    public void ViewComponent_WithArguments()
    {
        // Arrange
        var pageModel = new TestPageModel();
        var arguments = new { Arg1 = "Hi", Arg2 = "There" };

        // Act
        var result = pageModel.ViewComponent(typeof(Guid), arguments);

        // Assert
        Assert.NotNull(result);

        Assert.Equal(typeof(Guid), result.ViewComponentType);
        Assert.Same(arguments, result.Arguments);
    }

    private class ContentPageModel : PageModel
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
    }

    private class StatusCodePageModel : PageModel
    {
        public StatusCodeResult StatusCode_Int(int statusCode)
        {
            return StatusCode(statusCode);
        }

        public ObjectResult StatusCode_Object(int statusCode, object value)
        {
            return StatusCode(statusCode, value);
        }
    }

    private class TestPageModel : PageModel
    {
    }

    private class TestPage : Page
    {
        public override Task ExecuteAsync()
        {
            throw new NotImplementedException();
        }
    }
}
