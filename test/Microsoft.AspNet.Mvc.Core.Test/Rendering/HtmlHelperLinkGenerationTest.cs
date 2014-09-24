using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNet.Mvc.Core;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Rendering
{
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
            string expectedLink = string.Format(@"<a href=""{0}{1}{2}{3}{4}{5}""{6}>Details</a>",
                                                                protocol, 
                                                                hostname, 
                                                                controller, 
                                                                action, 
                                                                GetRouteValuesAsString(routeValues), 
                                                                fragment,
                                                                GetHtmlAttributesAsString(htmlAttributes));

            var urlHelper = new Mock<IUrlHelper>();
            urlHelper.Setup(
                            h => h.Action(
                                    It.IsAny<string>(),
                                    It.IsAny<string>(),
                                    It.IsAny<object>(),
                                    It.IsAny<string>(),
                                    It.IsAny<string>(),
                                    It.IsAny<string>()))
                     .Returns<string, string, object, string, string, string>(
                            (actn, cntrlr, rvalues, prtcl, hname, frgmt) =>
                                    string.Format("{0}{1}{2}{3}{4}{5}",
                                    prtcl,
                                    hname,
                                    cntrlr,
                                    actn,
                                    GetRouteValuesAsString(rvalues),
                                    frgmt));

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
                                            htmlAttributes: htmlAttributes).ToString();

            // Assert
            Assert.Equal(expectedLink, actualLink);
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
            string expectedLink = string.Format(@"<a href=""{0}{1}{2}{3}""{4}>Details</a>",
                                                                    protocol,
                                                                    hostname,
                                                                    GetRouteValuesAsString(routeValues),
                                                                    fragment,
                                                                    GetHtmlAttributesAsString(htmlAttributes));

            var urlHelper = new Mock<IUrlHelper>();
            urlHelper
                .Setup(
                    h => h.RouteUrl(
                            It.IsAny<string>(),
                            It.IsAny<object>(),
                            It.IsAny<string>(),
                            It.IsAny<string>(),
                            It.IsAny<string>()))
                .Returns<string, object, string, string, string>(
                    (rname, rvalues, prtcl, hname, frgmt) =>
                        string.Format("{0}{1}{2}{3}",
                            prtcl,
                            hname,
                            GetRouteValuesAsString(rvalues),
                            frgmt));

            var htmlHelper = DefaultTemplatesUtilities.GetHtmlHelper(urlHelper.Object);

            // Act
            var actualLink = htmlHelper.RouteLink(
                                            linkText: "Details",
                                            routeName: routeName,
                                            protocol: protocol,
                                            hostName: hostname,
                                            fragment: fragment,
                                            routeValues: routeValues,
                                            htmlAttributes: htmlAttributes).ToString();

            // Assert
            Assert.Equal(expectedLink, actualLink);
        }

        private string GetRouteValuesAsString(object routeValues)
        {
            var dict = TypeHelper.ObjectToDictionary(routeValues);
            return string.Join(string.Empty, dict.Select(kvp => string.Format("{0}={1}", kvp.Key, kvp.Value.ToString())));
        }

        private string GetHtmlAttributesAsString(object routeValues)
        {
            var dict = TypeHelper.ObjectToDictionary(routeValues);
            return string.Join(string.Empty, dict.Select(kvp => string.Format(" {0}=\"{1}\"", kvp.Key, kvp.Value.ToString())));
        }
    }
}