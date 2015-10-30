// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if MOCK_SUPPORT
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.Routing;
using Microsoft.AspNet.Mvc.ViewFeatures;
using Microsoft.Extensions.WebEncoders.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Rendering
{
    /// <summary>
    /// Tests the <see cref="IHtmlHelper"/>'s <see cref="IHtmlHelper.BeginForm"/> and
    /// <see cref="IHtmlHelper.BeginRouteForm"/>methods.
    /// </summary>
    public class HtmlHelperFormTest
    {
        // actionName, controllerName, routeValues, method, htmlAttributes
        public static TheoryData<string, string, object, FormMethod, object> BeginFormDataSet
        {
            get
            {
                return new TheoryData<string, string, object, FormMethod, object>
                {
                    {
                        null, null, null, FormMethod.Get, null
                    },
                    {
                        "Details", "Product", null, FormMethod.Get, null
                    },
                    {
                        "Details", "Product", null, FormMethod.Post, null
                    },
                    {
                        "Details", "Product", new { isprint = "false", showreviews = "false" }, FormMethod.Get, null
                    },
                    {
                        "Details", "Product", new { isprint = "false", showreviews = "true" }, FormMethod.Post, null
                    },
                    {
                        "Details", "Product", new { isprint = "true", showreviews = "false" }, FormMethod.Get,
                        new { p1_name = "p1-value" }
                    },
                    {
                        "Details", "Product", new { isprint = "true", showreviews = "true" }, FormMethod.Post,
                        new { p1_name = "p1-value" }
                    },
                    {
                        "Details", "Product",
                        new Dictionary<string, object> { { "isprint", "false" }, { "showreviews", "false" }, },
                        FormMethod.Get,
                        new Dictionary<string, object> { { "p1-name", "p1-value" }, { "p2-name", "p2-value" } }
                    },
                    {
                        "Details", "Product",
                        new Dictionary<string, object> { { "isprint", "false" }, { "showreviews", "false" }, },
                        FormMethod.Post,
                        new Dictionary<string, object> { { "p1-name", "p1-value" }, { "p2-name", "p2-value" } }
                    },
                };
            }
        }

        // routeName, routeValues, method, htmlAttributes
        public static TheoryData<string, object, FormMethod, object> BeginRouteFormDataSet
        {
            get
            {
                return new TheoryData<string, object, FormMethod, object>
                {
                    {
                        null, null, FormMethod.Get, null
                    },
                    {
                        null, null, FormMethod.Post, null
                    },
                    {
                        "default", null, FormMethod.Get, null
                    },
                    {
                        "default", null, FormMethod.Post, null
                    },
                    {
                        "default", new { isprint = "false", showreviews = "false" }, FormMethod.Get, null
                    },
                    {
                        "default", new { isprint = "false", showreviews = "true" }, FormMethod.Post, null
                    },
                    {
                        "default", new { isprint = "true", showreviews = "false" }, FormMethod.Get,
                        new { p1 = "p1-value" }
                    },
                    {
                        "default", new { isprint = "true", showreviews = "true" }, FormMethod.Post,
                        new { p1 = "p1-value" }
                    },
                    {
                        "default",
                        new Dictionary<string, object> { { "isprint", "false" }, { "showreviews", "false" }, },
                        FormMethod.Get,
                        new Dictionary<string, object> { { "p1-name", "p1-value" }, { "p2-name", "p2-value" } }
                    },
                    {
                        "default",
                        new Dictionary<string, object> { { "isprint", "false" }, { "showreviews", "false" }, },
                        FormMethod.Post,
                        new Dictionary<string, object> { { "p1-name", "p1-value" }, { "p2-name", "p2-value" } }
                    },
                };
            }

        }

        [Fact]
        public void BeginForm_RendersExpectedValues_WithDefaultArguments()
        {
            // Arrange
            var pathBase = "/Base";
            var path = "/Path";
            var queryString = "?query=string";
            var expectedAction = pathBase + path + queryString;
            var expectedStartTag = string.Format("<form action=\"HtmlEncode[[{0}]]\" method=\"HtmlEncode[[post]]\">", expectedAction);

            // IUrlHelper should not be used in this scenario.
            var urlHelper = new Mock<IUrlHelper>(MockBehavior.Strict);
            var htmlHelper = DefaultTemplatesUtilities.GetHtmlHelper(urlHelper.Object);

            // Guards
            Assert.NotNull(htmlHelper.ViewContext);
            var writer = Assert.IsAssignableFrom<StringWriter>(htmlHelper.ViewContext.Writer);
            var builder = writer.GetStringBuilder();
            Assert.NotNull(builder);
            Assert.NotNull(htmlHelper.ViewContext.HttpContext);
            var request = htmlHelper.ViewContext.HttpContext.Request;
            Assert.NotNull(request);

            // Set properties the IHtmlGenerator implementation should use in this scenario.
            request.PathBase = new PathString(pathBase);
            request.Path = new PathString(path);
            request.QueryString = new QueryString(queryString);

            // Act
            var mvcForm = htmlHelper.BeginForm(
                actionName: null,
                controllerName: null,
                routeValues: null,
                method: FormMethod.Post,
                htmlAttributes: null);

            // Assert
            Assert.NotNull(mvcForm);
            Assert.Equal(expectedStartTag, builder.ToString());
            urlHelper.Verify();

            builder.Clear();
            mvcForm.Dispose();
            Assert.Equal("</form>", builder.ToString());
        }

        [Fact]
        public void BeginForm_RendersExpectedValues_WithDefaultArgumentsAndHtmlAttributes()
        {
            // Arrange
            var pathBase = "/Base";
            var path = "/Path";
            var queryString = "?query=string";
            var expectedAction = pathBase + path + queryString;
            var htmlAttributes = new { p1_name = "p1-value" };
            var expectedStartTag = string.Format("<form action=\"HtmlEncode[[{0}]]\" method=\"HtmlEncode[[post]]\"{1}>",
                expectedAction,
                GetHtmlAttributesAsString(htmlAttributes));

            // IUrlHelper should not be used in this scenario.
            var urlHelper = new Mock<IUrlHelper>(MockBehavior.Strict);
            var htmlHelper = DefaultTemplatesUtilities.GetHtmlHelper(urlHelper.Object);

            // Guards
            Assert.NotNull(htmlHelper.ViewContext);
            var writer = Assert.IsAssignableFrom<StringWriter>(htmlHelper.ViewContext.Writer);
            var builder = writer.GetStringBuilder();
            Assert.NotNull(builder);
            Assert.NotNull(htmlHelper.ViewContext.HttpContext);
            var request = htmlHelper.ViewContext.HttpContext.Request;
            Assert.NotNull(request);

            // Set properties the IHtmlGenerator implementation should use in this scenario.
            request.PathBase = new PathString(pathBase);
            request.Path = new PathString(path);
            request.QueryString = new QueryString(queryString);

            // Act
            var mvcForm = htmlHelper.BeginForm(
                actionName: null,
                controllerName: null,
                routeValues: null,
                method: FormMethod.Post,
                htmlAttributes: htmlAttributes);

            // Assert
            Assert.NotNull(mvcForm);
            Assert.Equal(expectedStartTag, builder.ToString());
            urlHelper.Verify();

            builder.Clear();
            mvcForm.Dispose();
            Assert.Equal("</form>", builder.ToString());
        }

        [Theory]
        [MemberData(nameof(BeginFormDataSet))]
        public void BeginForm_RendersExpectedValues(
            string actionName,
            string controllerName,
            object routeValues,
            FormMethod method,
            object htmlAttributes)
        {
            // Arrange
            var expectedAction = "http://localhost/Hello/World";
            var expectedStartTag = string.Format(
                "<form action=\"HtmlEncode[[{0}]]\" method=\"HtmlEncode[[{1}]]\"{2}>",
                expectedAction,
                method.ToString().ToLowerInvariant(),
                GetHtmlAttributesAsString(htmlAttributes));

            var urlHelper = new Mock<IUrlHelper>(MockBehavior.Strict);
            urlHelper
                .Setup(realHelper => realHelper.Action(It.Is<UrlActionContext>((context) =>
                    string.Equals(context.Action, actionName) &&
                    string.Equals(context.Controller, controllerName) &&
                    context.Values == routeValues
                )))
                .Returns(expectedAction)
                .Verifiable();
            var htmlHelper = DefaultTemplatesUtilities.GetHtmlHelper(urlHelper.Object);

            // Guards
            Assert.NotNull(htmlHelper.ViewContext);
            var writer = Assert.IsAssignableFrom<StringWriter>(htmlHelper.ViewContext.Writer);
            var builder = writer.GetStringBuilder();
            Assert.NotNull(builder);

            // Act
            var mvcForm = htmlHelper.BeginForm(actionName, controllerName, routeValues, method, htmlAttributes);

            // Assert
            Assert.NotNull(mvcForm);
            Assert.Equal(expectedStartTag, builder.ToString());
            urlHelper.Verify();
        }

        [Theory]
        [MemberData(nameof(BeginRouteFormDataSet))]
        public void BeginRouteForm_RendersExpectedValues(
            string routeName,
            object routeValues,
            FormMethod method,
            object htmlAttributes)
        {
            // Arrange
            var expectedAction = "http://localhost/Hello/World";
            var expectedStartTag = string.Format(
                "<form action=\"HtmlEncode[[{0}]]\" method=\"HtmlEncode[[{1}]]\"{2}>",
                expectedAction,
                method.ToString().ToLowerInvariant(),
                GetHtmlAttributesAsString(htmlAttributes));

            var urlHelper = new Mock<IUrlHelper>(MockBehavior.Strict);
            urlHelper
                .Setup(realHelper => realHelper.RouteUrl(It.Is<UrlRouteContext>(context =>
                    string.Equals(context.RouteName, routeName) &&
                    context.Values == routeValues &&
                    context.Protocol == null &&
                    context.Host == null &&
                    context.Fragment == null)))
                .Returns(expectedAction)
                .Verifiable();
            var htmlHelper = DefaultTemplatesUtilities.GetHtmlHelper(urlHelper.Object);

            // Guards
            Assert.NotNull(htmlHelper.ViewContext);
            var writer = Assert.IsAssignableFrom<StringWriter>(htmlHelper.ViewContext.Writer);
            var builder = writer.GetStringBuilder();
            Assert.NotNull(builder);

            // Act
            var mvcForm = htmlHelper.BeginRouteForm(routeName, routeValues, method, htmlAttributes);

            // Assert
            Assert.NotNull(mvcForm);
            Assert.Equal(expectedStartTag, builder.ToString());
            urlHelper.Verify();
        }

        [Fact]
        public void EndForm_RendersExpectedValues()
        {
            // Arrange
            // IUrlHelper should not be used in this scenario.
            var urlHelper = new Mock<IUrlHelper>(MockBehavior.Strict);
            var htmlHelper = DefaultTemplatesUtilities.GetHtmlHelper(urlHelper.Object);

            // Guards
            Assert.NotNull(htmlHelper.ViewContext);
            var writer = Assert.IsAssignableFrom<StringWriter>(htmlHelper.ViewContext.Writer);
            var builder = writer.GetStringBuilder();
            Assert.NotNull(builder);

            // Act
            htmlHelper.EndForm();

            // Assert
            Assert.Equal("</form>", builder.ToString());
            urlHelper.Verify();
        }

        [Fact]
        public void EndForm_RendersHiddenTagForCheckBox()
        {
            // Arrange
            var htmlHelper = DefaultTemplatesUtilities.GetHtmlHelper();
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(s => s.GetService(typeof(HtmlEncoder))).Returns(new HtmlTestEncoder());
            var viewContext = htmlHelper.ViewContext;
            viewContext.HttpContext.RequestServices = serviceProvider.Object;

            var writer = viewContext.Writer as StringWriter;
            Assert.NotNull(writer);
            var builder = writer.GetStringBuilder();

            var tagBuilder = new TagBuilder("input");
            tagBuilder.MergeAttribute("name", "SomeName");
            tagBuilder.MergeAttribute("type", "hidden");
            tagBuilder.MergeAttribute("value", "false");
            tagBuilder.TagRenderMode = TagRenderMode.SelfClosing;

            htmlHelper.ViewContext.FormContext.EndOfFormContent.Add(tagBuilder);

            // Act
            htmlHelper.EndForm();

            // Assert
            Assert.Equal(
                "<input name=\"HtmlEncode[[SomeName]]\" type=\"HtmlEncode[[hidden]]\" value=\"HtmlEncode[[false]]\" /></form>",
                builder.ToString());
        }

        private string GetHtmlAttributesAsString(object htmlAttributes)
        {
            var dictionary = HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes);
            return string.Join(
                string.Empty,
                dictionary.Select(keyValue => string.Format(" {0}=\"HtmlEncode[[{1}]]\"", keyValue.Key, keyValue.Value)));
        }
    }
}
#endif