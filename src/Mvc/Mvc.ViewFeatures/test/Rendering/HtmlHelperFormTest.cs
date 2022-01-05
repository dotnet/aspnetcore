// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.WebEncoders.Testing;
using Moq;

namespace Microsoft.AspNetCore.Mvc.Rendering;

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
        var expectedStartTag = string.Format(CultureInfo.InvariantCulture, "<form action=\"HtmlEncode[[{0}]]\" method=\"HtmlEncode[[post]]\">", expectedAction);

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
            antiforgery: false,
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
        var expectedStartTag = string.Format(CultureInfo.InvariantCulture, "<form action=\"HtmlEncode[[{0}]]\" method=\"HtmlEncode[[post]]\"{1}>",
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
            antiforgery: false,
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
            CultureInfo.InvariantCulture,
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
        var mvcForm = htmlHelper.BeginForm(
            actionName,
            controllerName,
            routeValues,
            method,
            antiforgery: false,
            htmlAttributes: htmlAttributes);

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
            CultureInfo.InvariantCulture,
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
        var mvcForm = htmlHelper.BeginRouteForm(
            routeName,
            routeValues,
            method,
            antiforgery: false,
            htmlAttributes: htmlAttributes);

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

    // This is an integration for the implicit antiforgery token added by BeginForm.
    [Fact]
    public void BeginForm_EndForm_RendersAntiforgeryToken()
    {
        // Arrange
        var htmlGenerator = new Mock<IHtmlGenerator>(MockBehavior.Strict);
        htmlGenerator
            .Setup(g => g.GenerateForm(
                It.IsAny<ViewContext>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<string>(),
                It.IsAny<object>()))
            .Returns(new TagBuilder("form"));

        htmlGenerator
            .Setup(g => g.GenerateAntiforgery(It.IsAny<ViewContext>()))
            .Returns(new TagBuilder("antiforgery"));

        var htmlHelper = DefaultTemplatesUtilities.GetHtmlHelper(htmlGenerator.Object);
        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(s => s.GetService(typeof(HtmlEncoder))).Returns(new HtmlTestEncoder());
        var viewContext = htmlHelper.ViewContext;
        viewContext.HttpContext.RequestServices = serviceProvider.Object;

        var writer = viewContext.Writer as StringWriter;
        Assert.NotNull(writer);

        // Act & Assert
        using (var form = htmlHelper.BeginForm())
        {
        }

        Assert.Equal(
            "<form><antiforgery></antiforgery></form>",
            writer.GetStringBuilder().ToString());
    }

    [Fact]
    public void BeginForm_EndForm_RendersAntiforgeryTokenWhenMethodIsPost()
    {
        // Arrange
        var htmlGenerator = new Mock<IHtmlGenerator>(MockBehavior.Strict);
        htmlGenerator
            .Setup(g => g.GenerateForm(
                It.IsAny<ViewContext>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<string>(),
                It.IsAny<object>()))
            .Returns(new TagBuilder("form"));

        htmlGenerator
            .Setup(g => g.GenerateAntiforgery(It.IsAny<ViewContext>()))
            .Returns(new TagBuilder("antiforgery"));

        var htmlHelper = DefaultTemplatesUtilities.GetHtmlHelper(htmlGenerator.Object);
        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(s => s.GetService(typeof(HtmlEncoder))).Returns(new HtmlTestEncoder());
        var viewContext = htmlHelper.ViewContext;
        viewContext.HttpContext.RequestServices = serviceProvider.Object;

        var writer = viewContext.Writer as StringWriter;
        Assert.NotNull(writer);

        // Act & Assert
        using (var form = htmlHelper.BeginForm(FormMethod.Post, antiforgery: null, htmlAttributes: null))
        {
        }

        Assert.Equal(
            "<form><antiforgery></antiforgery></form>",
            writer.GetStringBuilder().ToString());
    }

    // This is an integration for suppressing implicit antiforgery token added by BeginForm.
    [Fact]
    public void BeginForm_EndForm_SuppressAntiforgeryToken()
    {
        // Arrange
        var htmlGenerator = new Mock<IHtmlGenerator>(MockBehavior.Strict);
        htmlGenerator
            .Setup(g => g.GenerateForm(
                It.IsAny<ViewContext>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<string>(),
                It.IsAny<object>()))
            .Returns(new TagBuilder("form"));

        htmlGenerator
            .Setup(g => g.GenerateAntiforgery(It.IsAny<ViewContext>()))
            .Returns(new TagBuilder("antiforgery"));

        var htmlHelper = DefaultTemplatesUtilities.GetHtmlHelper(htmlGenerator.Object);
        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(s => s.GetService(typeof(HtmlEncoder))).Returns(new HtmlTestEncoder());
        var viewContext = htmlHelper.ViewContext;
        viewContext.HttpContext.RequestServices = serviceProvider.Object;

        var writer = viewContext.Writer as StringWriter;
        Assert.NotNull(writer);

        // Act & Assert
        using (var form = htmlHelper.BeginForm(FormMethod.Post, antiforgery: false, htmlAttributes: null))
        {
        }

        Assert.Equal(
            "<form></form>",
            writer.GetStringBuilder().ToString());
    }

    [Fact]
    public void BeginForm_EndForm_SuppressAntiforgeryTokenWhenMethodIsGet()
    {
        // Arrange
        var htmlGenerator = new Mock<IHtmlGenerator>(MockBehavior.Strict);
        htmlGenerator
            .Setup(g => g.GenerateForm(
                It.IsAny<ViewContext>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<string>(),
                It.IsAny<object>()))
            .Returns(new TagBuilder("form"));

        htmlGenerator
            .Setup(g => g.GenerateAntiforgery(It.IsAny<ViewContext>()))
            .Returns(new TagBuilder("antiforgery"));

        var htmlHelper = DefaultTemplatesUtilities.GetHtmlHelper(htmlGenerator.Object);
        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(s => s.GetService(typeof(HtmlEncoder))).Returns(new HtmlTestEncoder());
        var viewContext = htmlHelper.ViewContext;
        viewContext.HttpContext.RequestServices = serviceProvider.Object;

        var writer = viewContext.Writer as StringWriter;
        Assert.NotNull(writer);

        // Act & Assert
        using (var form = htmlHelper.BeginForm(FormMethod.Get, antiforgery: null, htmlAttributes: null))
        {
        }

        Assert.Equal(
            "<form></form>",
            writer.GetStringBuilder().ToString());
    }

    [Theory]
    [InlineData(FormMethod.Get)]
    [InlineData(FormMethod.Post)]
    public void BeginForm_EndForm_DoesNotSuppressAntiforgeryTokenWhenAntiforgeryIsTrue(FormMethod method)
    {
        // Arrange
        var htmlGenerator = new Mock<IHtmlGenerator>(MockBehavior.Strict);
        htmlGenerator
            .Setup(g => g.GenerateForm(
                It.IsAny<ViewContext>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<string>(),
                It.IsAny<object>()))
            .Returns(new TagBuilder("form"));

        htmlGenerator
            .Setup(g => g.GenerateAntiforgery(It.IsAny<ViewContext>()))
            .Returns(new TagBuilder("antiforgery"));

        var htmlHelper = DefaultTemplatesUtilities.GetHtmlHelper(htmlGenerator.Object);
        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(s => s.GetService(typeof(HtmlEncoder))).Returns(new HtmlTestEncoder());
        var viewContext = htmlHelper.ViewContext;
        viewContext.HttpContext.RequestServices = serviceProvider.Object;

        var writer = viewContext.Writer as StringWriter;
        Assert.NotNull(writer);

        // Act & Assert
        using (var form = htmlHelper.BeginForm(method, antiforgery: true, htmlAttributes: null))
        {
        }

        Assert.Equal(
            "<form><antiforgery></antiforgery></form>",
            writer.GetStringBuilder().ToString());
    }

    // This is an integration for suppressing implicit antiforgery token added by BeginForm.
    [Fact]
    public void BeginForm_EndForm_SuppressAntiforgeryToken_WithExplicitCallToAntiforgery()
    {
        // Arrange
        var htmlGenerator = new Mock<IHtmlGenerator>(MockBehavior.Strict);
        htmlGenerator
            .Setup(g => g.GenerateForm(
                It.IsAny<ViewContext>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<string>(),
                It.IsAny<object>()))
            .Returns(new TagBuilder("form"));

        htmlGenerator
            .Setup(g => g.GenerateAntiforgery(It.IsAny<ViewContext>()))
            .Returns(new TagBuilder("antiforgery"));

        var htmlHelper = DefaultTemplatesUtilities.GetHtmlHelper(htmlGenerator.Object);
        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(s => s.GetService(typeof(HtmlEncoder))).Returns(new HtmlTestEncoder());
        var viewContext = htmlHelper.ViewContext;
        viewContext.HttpContext.RequestServices = serviceProvider.Object;

        var writer = viewContext.Writer as StringWriter;
        Assert.NotNull(writer);

        // Act & Assert
        using (var form = htmlHelper.BeginForm(FormMethod.Post, antiforgery: false, htmlAttributes: null))
        {
            // This call will output a token.
            Assert.Equal("antiforgery", Assert.IsType<TagBuilder>(htmlHelper.AntiForgeryToken()).TagName);
        }

        Assert.Equal(
            "<form></form>",
            writer.GetStringBuilder().ToString());
    }

    // This is an integration for the implicit antiforgery token added by BeginRouteForm.
    [Fact]
    public void BeginRouteForm_EndForm_RendersAntiforgeryToken()
    {
        // Arrange
        var htmlGenerator = new Mock<IHtmlGenerator>(MockBehavior.Strict);
        htmlGenerator
            .Setup(g => g.GenerateRouteForm(
                It.IsAny<ViewContext>(),
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<string>(),
                It.IsAny<object>()))
            .Returns(new TagBuilder("form"));

        htmlGenerator
            .Setup(g => g.GenerateAntiforgery(It.IsAny<ViewContext>()))
            .Returns(new TagBuilder("antiforgery"));

        var htmlHelper = DefaultTemplatesUtilities.GetHtmlHelper(htmlGenerator.Object);
        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(s => s.GetService(typeof(HtmlEncoder))).Returns(new HtmlTestEncoder());
        var viewContext = htmlHelper.ViewContext;
        viewContext.HttpContext.RequestServices = serviceProvider.Object;

        var writer = viewContext.Writer as StringWriter;
        Assert.NotNull(writer);

        // Act & Assert
        using (var form = htmlHelper.BeginRouteForm(routeValues: null))
        {
        }

        Assert.Equal(
            "<form><antiforgery></antiforgery></form>",
            writer.GetStringBuilder().ToString());
    }

    [Fact]
    public void BeginRouteForm_EndForm_RendersAntiforgeryTokenWhenMethodIsPost()
    {
        // Arrange
        var htmlGenerator = new Mock<IHtmlGenerator>(MockBehavior.Strict);
        htmlGenerator
            .Setup(g => g.GenerateRouteForm(
                It.IsAny<ViewContext>(),
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<string>(),
                It.IsAny<object>()))
            .Returns(new TagBuilder("form"));

        htmlGenerator
            .Setup(g => g.GenerateAntiforgery(It.IsAny<ViewContext>()))
            .Returns(new TagBuilder("antiforgery"));

        var htmlHelper = DefaultTemplatesUtilities.GetHtmlHelper(htmlGenerator.Object);
        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(s => s.GetService(typeof(HtmlEncoder))).Returns(new HtmlTestEncoder());
        var viewContext = htmlHelper.ViewContext;
        viewContext.HttpContext.RequestServices = serviceProvider.Object;

        var writer = viewContext.Writer as StringWriter;
        Assert.NotNull(writer);

        // Act & Assert
        using (var form = htmlHelper.BeginRouteForm(
            routeName: null,
            routeValues: null,
            method: FormMethod.Post,
            antiforgery: null,
            htmlAttributes: null))
        {
        }

        Assert.Equal(
            "<form><antiforgery></antiforgery></form>",
            writer.GetStringBuilder().ToString());
    }

    // This is an integration for suppressing implicit antiforgery token added by BeginRouteForm.
    [Fact]
    public void BeginRouteForm_EndForm_SuppressAntiforgeryToken()
    {
        // Arrange
        var htmlGenerator = new Mock<IHtmlGenerator>(MockBehavior.Strict);
        htmlGenerator
            .Setup(g => g.GenerateRouteForm(
                It.IsAny<ViewContext>(),
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<string>(),
                It.IsAny<object>()))
            .Returns(new TagBuilder("form"));

        htmlGenerator
            .Setup(g => g.GenerateAntiforgery(It.IsAny<ViewContext>()))
            .Returns(new TagBuilder("antiforgery"));

        var htmlHelper = DefaultTemplatesUtilities.GetHtmlHelper(htmlGenerator.Object);
        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(s => s.GetService(typeof(HtmlEncoder))).Returns(new HtmlTestEncoder());
        var viewContext = htmlHelper.ViewContext;
        viewContext.HttpContext.RequestServices = serviceProvider.Object;

        var writer = viewContext.Writer as StringWriter;
        Assert.NotNull(writer);

        // Act & Assert
        using (var form = htmlHelper.BeginRouteForm(
            routeName: null,
            routeValues: null,
            method: FormMethod.Post,
            antiforgery: false,
            htmlAttributes: null))
        {
        }

        Assert.Equal(
            "<form></form>",
            writer.GetStringBuilder().ToString());
    }

    [Fact]
    public void BeginRouteForm_EndForm_SuppressAntiforgeryTokenWhenMethodIsGet()
    {
        // Arrange
        var htmlGenerator = new Mock<IHtmlGenerator>(MockBehavior.Strict);
        htmlGenerator
            .Setup(g => g.GenerateRouteForm(
                It.IsAny<ViewContext>(),
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<string>(),
                It.IsAny<object>()))
            .Returns(new TagBuilder("form"));

        htmlGenerator
            .Setup(g => g.GenerateAntiforgery(It.IsAny<ViewContext>()))
            .Returns(new TagBuilder("antiforgery"));

        var htmlHelper = DefaultTemplatesUtilities.GetHtmlHelper(htmlGenerator.Object);
        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(s => s.GetService(typeof(HtmlEncoder))).Returns(new HtmlTestEncoder());
        var viewContext = htmlHelper.ViewContext;
        viewContext.HttpContext.RequestServices = serviceProvider.Object;

        var writer = viewContext.Writer as StringWriter;
        Assert.NotNull(writer);

        // Act & Assert
        using (var form = htmlHelper.BeginRouteForm(
            routeName: null,
            routeValues: null,
            method: FormMethod.Get,
            antiforgery: null,
            htmlAttributes: null))
        {
        }

        Assert.Equal(
            "<form></form>",
            writer.GetStringBuilder().ToString());
    }

    [Theory]
    [InlineData(FormMethod.Get)]
    [InlineData(FormMethod.Post)]
    public void BeginRouteForm_EndForm_DoesNotSuppressAntiforgeryTokenWhenAntiforgeryIsTrue(FormMethod method)
    {
        // Arrange
        var htmlGenerator = new Mock<IHtmlGenerator>(MockBehavior.Strict);
        htmlGenerator
            .Setup(g => g.GenerateRouteForm(
                It.IsAny<ViewContext>(),
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<string>(),
                It.IsAny<object>()))
            .Returns(new TagBuilder("form"));

        htmlGenerator
            .Setup(g => g.GenerateAntiforgery(It.IsAny<ViewContext>()))
            .Returns(new TagBuilder("antiforgery"));

        var htmlHelper = DefaultTemplatesUtilities.GetHtmlHelper(htmlGenerator.Object);
        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(s => s.GetService(typeof(HtmlEncoder))).Returns(new HtmlTestEncoder());
        var viewContext = htmlHelper.ViewContext;
        viewContext.HttpContext.RequestServices = serviceProvider.Object;

        var writer = viewContext.Writer as StringWriter;
        Assert.NotNull(writer);

        // Act & Assert
        using (var form = htmlHelper.BeginRouteForm(
            routeName: null,
            routeValues: null,
            method: method,
            antiforgery: true,
            htmlAttributes: null))
        {
        }

        Assert.Equal(
            "<form><antiforgery></antiforgery></form>",
            writer.GetStringBuilder().ToString());
    }

    private string GetHtmlAttributesAsString(object htmlAttributes)
    {
        var dictionary = HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes);
        return string.Join(
            string.Empty,
            dictionary.Select(keyValue => string.Format(CultureInfo.InvariantCulture, " {0}=\"HtmlEncode[[{1}]]\"", keyValue.Key, keyValue.Value)));
    }
}
