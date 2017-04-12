// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.RazorPages
{
    public class PageUrlHelperExtensionsTest
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
            var urlHelper = CreateUrlHelper(actionContext);
            urlHelper.Setup(h => h.RouteUrl(It.IsAny<UrlRouteContext>()))
                .Callback((UrlRouteContext context) => actual = context);

            // Act
            urlHelper.Object.Page("TestPage");

            // Assert
            urlHelper.Verify();
            Assert.NotNull(actual);
            Assert.Null(actual.RouteName);
            Assert.Collection(Assert.IsType<RouteValueDictionary>(actual.Values),
                value =>
                {
                    Assert.Equal("page", value.Key);
                    Assert.Equal("TestPage", value.Value);
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
            var urlHelper = CreateUrlHelper();
            urlHelper.Setup(h => h.RouteUrl(It.IsAny<UrlRouteContext>()))
                .Callback((UrlRouteContext context) => actual = context);

            // Act
            urlHelper.Object.Page("TestPage", values);

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
                    Assert.Equal("TestPage", value.Value);
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
            var urlHelper = CreateUrlHelper();
            urlHelper.Setup(h => h.RouteUrl(It.IsAny<UrlRouteContext>()))
                .Callback((UrlRouteContext context) => actual = context);

            // Act
            urlHelper.Object.Page("TestPage", new { id = 13 }, "https");

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
                    Assert.Equal("TestPage", value.Value);
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
            var urlHelper = CreateUrlHelper();
            urlHelper.Setup(h => h.RouteUrl(It.IsAny<UrlRouteContext>()))
                .Callback((UrlRouteContext context) => actual = context);

            // Act
            urlHelper.Object.Page("TestPage", new { id = 13 }, "https", "mytesthost");

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
                    Assert.Equal("TestPage", value.Value);
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
            var urlHelper = CreateUrlHelper();
            urlHelper.Setup(h => h.RouteUrl(It.IsAny<UrlRouteContext>()))
                .Callback((UrlRouteContext context) => actual = context);

            // Act
            urlHelper.Object.Page("TestPage", new { id = 13 }, "https", "mytesthost", "#toc");

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
                    Assert.Equal("TestPage", value.Value);
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

            var urlHelper = CreateUrlHelper(actionContext);
            urlHelper.Setup(h => h.RouteUrl(It.IsAny<UrlRouteContext>()))
                .Callback((UrlRouteContext context) => actual = context);

            // Act
            string page = null;
            urlHelper.Object.Page(page, new { id = 13 }, "https", "mytesthost", "#toc");

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
            Assert.Equal("https", actual.Protocol);
            Assert.Equal("mytesthost", actual.Host);
            Assert.Equal("#toc", actual.Fragment);
        }

        [Fact]
        public void Page_SetsFormActionToNull_IfValueIsNotSpecifiedInRouteValues()
        {
            // Arrange
            UrlRouteContext actual = null;
            var routeData = new RouteData
            {
                Values =
                {
                    { "page", "ambient-page" },
                    { "formaction", "ambient-formaction" },
                }
            };
            var actionContext = new ActionContext
            {
                RouteData = routeData,
            };

            var urlHelper = CreateUrlHelper(actionContext);
            urlHelper.Setup(h => h.RouteUrl(It.IsAny<UrlRouteContext>()))
                .Callback((UrlRouteContext context) => actual = context);

            // Act
            string page = null;
            urlHelper.Object.Page(page, new { id = 13 }, "https", "mytesthost", "#toc");

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
                    Assert.Equal("formaction", value.Key);
                    Assert.Null(value.Value);
                });
        }

        [Fact]
        public void Page_UsesExplicitlySpecifiedFormActionValue()
        {
            // Arrange
            UrlRouteContext actual = null;
            var routeData = new RouteData
            {
                Values =
                {
                    { "page", "ambient-page" },
                    { "formaction", "ambient-formaction" },
                }
            };
            var actionContext = new ActionContext
            {
                RouteData = routeData,
            };

            var urlHelper = CreateUrlHelper(actionContext);
            urlHelper.Setup(h => h.RouteUrl(It.IsAny<UrlRouteContext>()))
                .Callback((UrlRouteContext context) => actual = context);

            // Act
            string page = null;
            urlHelper.Object.Page(page, new { formaction = "exact-formaction" }, "https", "mytesthost", "#toc");

            // Assert
            urlHelper.Verify();
            Assert.NotNull(actual);
            Assert.Null(actual.RouteName);
            Assert.Collection(Assert.IsType<RouteValueDictionary>(actual.Values),
                value =>
                {
                    Assert.Equal("formaction", value.Key);
                    Assert.Equal("exact-formaction", value.Value);
                },
                value =>
                {
                    Assert.Equal("page", value.Key);
                    Assert.Equal("ambient-page", value.Value);
                });
        }

        private static Mock<IUrlHelper> CreateUrlHelper(ActionContext context = null)
        {
            if (context == null)
            {
                context = new ActionContext
                {
                    RouteData = new RouteData(),
                };
            }

            var urlHelper = new Mock<IUrlHelper>();
            urlHelper.SetupGet(h => h.ActionContext)
                .Returns(context);
            return urlHelper;
        }
    }
}
