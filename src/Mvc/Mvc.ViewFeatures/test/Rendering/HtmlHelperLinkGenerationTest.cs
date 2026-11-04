// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Internal;
using Moq;

namespace Microsoft.AspNetCore.Mvc.Rendering;

/// <summary>
/// Tests the <see cref="HtmlHelper"/>'s link generation methods.
/// </summary>
public class HtmlHelperLinkGenerationTest
{
    public static IEnumerable<object[]> ActionLinkGenerationData
    {
        get
        {
            yield return new object[] {
                    "Details", "Product", new { isprint = "true", showreviews = "true" }, "https", "www.contoso.com", "h1",
                    new { p1 = "p1-value" } };
            yield return new object[] {
                    "Details", "Product", new { isprint = "true", showreviews = "true" }, "https", "www.contoso.com", null, null };
            yield return new object[] {
                    "Details", "Product", new { isprint = "true", showreviews = "true" }, "https", null, null, null };
            yield return new object[] {
                    "Details", "Product", new { isprint = "true", showreviews = "true" }, null, null, null, null };
            yield return new object[] {
                    "Details", "Product", null, null, null, null, null };
            yield return new object[] {
                    null, null, null, null, null, null, null };
        }
    }

    [Theory]
    [MemberData(nameof(ActionLinkGenerationData))]
    public void ActionLink_GeneratesLink_WithExpectedValues(
        string action,
        string controller,
        object routeValues,
        string protocol,
        string hostname,
        string fragment,
        object htmlAttributes)
    {
        //Arrange
        var expectedLink = string.Format(
            CultureInfo.InvariantCulture,
            @"<a href=""HtmlEncode[[{0}{1}{2}{3}{4}{5}]]""{6}>HtmlEncode[[Details]]</a>",
            protocol,
            hostname,
            controller,
            action,
            GetRouteValuesAsString(routeValues),
            fragment,
            GetHtmlAttributesAsString(htmlAttributes));
        expectedLink = expectedLink.Replace("HtmlEncode[[]]", string.Empty);

        var urlHelper = new Mock<IUrlHelper>();
        urlHelper.Setup(h => h.Action(It.IsAny<UrlActionContext>()))
            .Returns<UrlActionContext>((actionContext) =>
                string.Format(
                CultureInfo.InvariantCulture,
                "{0}{1}{2}{3}{4}{5}",
                actionContext.Protocol,
                actionContext.Host,
                actionContext.Controller,
                actionContext.Action,
                GetRouteValuesAsString(actionContext.Values),
                actionContext.Fragment));

        var htmlHelper = DefaultTemplatesUtilities.GetHtmlHelper(urlHelper.Object);

        // Act
        var actualLink = htmlHelper.ActionLink(
            linkText: "Details",
            actionName: action,
            controllerName: controller,
            protocol: protocol,
            hostname: hostname,
            fragment: fragment,
            routeValues: routeValues,
            htmlAttributes: htmlAttributes);

        // Assert
        Assert.Equal(expectedLink, HtmlContentUtilities.HtmlContentToString(actualLink));
    }

    public static IEnumerable<object[]> RouteLinkGenerationData
    {
        get
        {
            yield return new object[] {
                    "default", new { isprint = "true", showreviews = "true" }, "https", "www.contoso.com", "h1",
                    new { p1 = "p1-value" } };
            yield return new object[] {
                    "default", new { isprint = "true", showreviews = "true" }, "https", "www.contoso.com", null, null };
            yield return new object[] {
                    "default", new { isprint = "true", showreviews = "true" }, "https", null, null, null };
            yield return new object[] {
                    "default", new { isprint = "true", showreviews = "true" }, null, null, null, null };
            yield return new object[] {
                    "default", null, null, null, null, null };
        }
    }

    [Theory]
    [MemberData(nameof(RouteLinkGenerationData))]
    public void RouteLink_GeneratesLink_WithExpectedValues(
        string routeName,
        object routeValues,
        string protocol,
        string hostname,
        string fragment,
        object htmlAttributes)
    {
        //Arrange
        var expectedLink = string.Format(
            CultureInfo.InvariantCulture,
            @"<a href=""HtmlEncode[[{0}{1}{2}{3}]]""{4}>HtmlEncode[[Details]]</a>",
            protocol,
            hostname,
            GetRouteValuesAsString(routeValues),
            fragment,
            GetHtmlAttributesAsString(htmlAttributes));
        expectedLink = expectedLink.Replace("HtmlEncode[[]]", string.Empty);

        var urlHelper = new Mock<IUrlHelper>();
        urlHelper
            .Setup(
                h => h.RouteUrl(It.IsAny<UrlRouteContext>())).Returns<UrlRouteContext>((context) =>
                    string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}{1}{2}{3}",
                    context.Protocol,
                    context.Host,
                    GetRouteValuesAsString(context.Values),
                    context.Fragment));

        var htmlHelper = DefaultTemplatesUtilities.GetHtmlHelper(urlHelper.Object);

        // Act
        var actualLink = htmlHelper.RouteLink(
            linkText: "Details",
            routeName: routeName,
            protocol: protocol,
            hostName: hostname,
            fragment: fragment,
            routeValues: routeValues,
            htmlAttributes: htmlAttributes);

        // Assert
        Assert.Equal(expectedLink, HtmlContentUtilities.HtmlContentToString(actualLink));
    }

    private string GetRouteValuesAsString(object routeValues)
    {
        var dict = PropertyHelper.ObjectToDictionary(routeValues);
        return string.Join(string.Empty, dict.Select(kvp => string.Format(CultureInfo.InvariantCulture, "{0}={1}", kvp.Key, kvp.Value.ToString())));
    }

    private string GetHtmlAttributesAsString(object routeValues)
    {
        var dict = PropertyHelper.ObjectToDictionary(routeValues);
        return string.Join(string.Empty, dict.Select(kvp => string.Format(CultureInfo.InvariantCulture, " {0}=\"HtmlEncode[[{1}]]\"", kvp.Key, kvp.Value.ToString())));
    }
}
