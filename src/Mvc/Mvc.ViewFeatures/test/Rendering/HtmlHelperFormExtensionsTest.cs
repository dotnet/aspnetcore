// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Microsoft.AspNetCore.Mvc.Rendering;

/// <summary>
/// Tests the <see cref="IHtmlHelper"/>'s <see cref="IHtmlHelper.BeginForm"/> and
/// <see cref="IHtmlHelper.BeginRouteForm"/> methods.
/// </summary>
public class HtmlHelperFormExtensionsTest
{
    private static readonly IEnumerable<string> _actionNames = new List<string> { null, "Details" };
    private static readonly IEnumerable<string> _controllerNames = new List<string> { null, "Product" };
    private static readonly IEnumerable<object> _htmlAttributes = new List<object>
        {
            null,
            new { isprint = "false", showreviews = "false" },
            new Dictionary<string, object> { { "isprint", "false" }, { "showreviews", "true" }, },
        };
    private static readonly IEnumerable<FormMethod> _methods = new List<FormMethod>
        {
            FormMethod.Get,
            FormMethod.Post,
        };
    private static readonly IEnumerable<string> _routeNames = new List<string> { null, "default" };
    private static readonly IEnumerable<object> _routeValues = new List<object>
        {
            null,
            new { p1_name = "p1-value" },
            new Dictionary<string, object> { { "p1-name", "p1-value" }, { "p2-name", "p2-value" } },
        };

    public static TheoryData<string, string> ActionNameAndControllerNameDataSet
    {
        get
        {
            var dataSet = new TheoryData<string, string>();
            foreach (var actionName in _actionNames)
            {
                foreach (var controllerName in _controllerNames)
                {
                    dataSet.Add(actionName, controllerName);
                }
            }

            return dataSet;
        }
    }

    public static TheoryData<string, string, FormMethod> ActionNameControllerNameAndMethodDataSet
    {
        get
        {
            var dataSet = new TheoryData<string, string, FormMethod>();
            foreach (var actionName in _actionNames)
            {
                foreach (var controllerName in _controllerNames)
                {
                    foreach (var method in _methods)
                    {
                        dataSet.Add(actionName, controllerName, method);
                    }
                }
            }

            return dataSet;
        }
    }

    public static TheoryData<string, string, FormMethod, object> ActionNameControllerNameMethodAndHtmlAttributesDataSet
    {
        get
        {
            var dataSet = new TheoryData<string, string, FormMethod, object>();
            foreach (var actionName in _actionNames)
            {
                foreach (var controllerName in _controllerNames)
                {
                    foreach (var method in _methods)
                    {
                        foreach (var htmlAttributes in _htmlAttributes)
                        {
                            dataSet.Add(actionName, controllerName, method, htmlAttributes);
                        }
                    }
                }
            }

            return dataSet;
        }
    }

    public static TheoryData<string, string, object> ActionNameControllerNameAndRouteValuesDataSet
    {
        get
        {
            var dataSet = new TheoryData<string, string, object>();
            foreach (var actionName in _actionNames)
            {
                foreach (var controllerName in _controllerNames)
                {
                    foreach (var routeValues in _routeValues)
                    {
                        dataSet.Add(actionName, controllerName, routeValues);
                    }
                }
            }

            return dataSet;
        }
    }

    public static TheoryData<string, string, object, FormMethod> ActionNameControllerNameRouteValuesAndMethodDataSet
    {
        get
        {
            var dataSet = new TheoryData<string, string, object, FormMethod>();
            foreach (var actionName in _actionNames)
            {
                foreach (var controllerName in _controllerNames)
                {
                    foreach (var routeValues in _routeValues)
                    {
                        foreach (var method in _methods)
                        {
                            dataSet.Add(actionName, controllerName, routeValues, method);
                        }
                    }
                }
            }

            return dataSet;
        }
    }

    public static TheoryData<FormMethod> MethodDataSet
    {
        get
        {
            var dataSet = new TheoryData<FormMethod>();
            foreach (var method in _methods)
            {
                dataSet.Add(method);
            }

            return dataSet;
        }
    }

    public static TheoryData<FormMethod, object> MethodAndHtmlAttributesDataSet
    {
        get
        {
            var dataSet = new TheoryData<FormMethod, object>();
            foreach (var method in _methods)
            {
                foreach (var htmlAttributes in _htmlAttributes)
                {
                    dataSet.Add(method, htmlAttributes);
                }
            }

            return dataSet;
        }
    }

    public static TheoryData<string> RouteNameDataSet
    {
        get
        {
            var dataSet = new TheoryData<string>();
            foreach (var routeName in _routeNames)
            {
                dataSet.Add(routeName);
            }

            return dataSet;
        }
    }

    public static TheoryData<string, FormMethod> RouteNameAndMethodDataSet
    {
        get
        {
            var dataSet = new TheoryData<string, FormMethod>();
            foreach (var routeName in _routeNames)
            {
                foreach (var method in _methods)
                {
                    dataSet.Add(routeName, method);
                }
            }

            return dataSet;
        }
    }

    public static TheoryData<string, FormMethod, object> RouteNameMethodAndHtmlAttributesDataSet
    {
        get
        {
            var dataSet = new TheoryData<string, FormMethod, object>();
            foreach (var routeName in _routeNames)
            {
                foreach (var method in _methods)
                {
                    foreach (var htmlAttributes in _htmlAttributes)
                    {
                        dataSet.Add(routeName, method, htmlAttributes);
                    }
                }
            }

            return dataSet;
        }
    }

    public static TheoryData<string, object> RouteNameAndRouteValuesDataSet
    {
        get
        {
            var dataSet = new TheoryData<string, object>();
            foreach (var routeName in _routeNames)
            {
                foreach (var routeValues in _routeValues)
                {
                    dataSet.Add(routeName, routeValues);
                }
            }

            return dataSet;
        }
    }

    public static TheoryData<string, object, FormMethod> RouteNameRouteValuesAndMethodDataSet
    {
        get
        {
            var dataSet = new TheoryData<string, object, FormMethod>();
            foreach (var routeName in _routeNames)
            {
                foreach (var routeValues in _routeValues)
                {
                    foreach (var method in _methods)
                    {
                        dataSet.Add(routeName, routeValues, method);
                    }
                }
            }

            return dataSet;
        }
    }

    public static TheoryData<object> RouteValuesDataSet
    {
        get
        {
            var dataSet = new TheoryData<object>();
            foreach (var routeValues in _routeValues)
            {
                dataSet.Add(routeValues);
            }

            return dataSet;
        }
    }

    [Fact]
    public void BeginFormWithNoParameters_CallsHtmlGeneratorWithExpectedValues()
    {
        // Arrange
        var tagBuilder = new TagBuilder(tagName: "form");
        var htmlGenerator = new Mock<IHtmlGenerator>(MockBehavior.Strict);
        var htmlHelper = DefaultTemplatesUtilities.GetHtmlHelper(htmlGenerator.Object);
        htmlGenerator
            .Setup(g => g.GenerateForm(
                htmlHelper.ViewContext,
                null,   // actionName
                null,   // controllerName
                null,   // routeValues
                "post", // method
                null))  // htmlAttributes
            .Returns(tagBuilder)
            .Verifiable();
        htmlGenerator
            .Setup(g => g.GenerateAntiforgery(htmlHelper.ViewContext))
            .Returns(HtmlString.Empty)
            .Verifiable();

        // Guards
        Assert.NotNull(htmlHelper.ViewContext);
        var writer = Assert.IsAssignableFrom<StringWriter>(htmlHelper.ViewContext.Writer);
        var builder = writer.GetStringBuilder();
        Assert.NotNull(builder);

        // Act
        var mvcForm = htmlHelper.BeginForm();

        // Assert
        Assert.NotNull(mvcForm);
        Assert.Equal("<form>", builder.ToString());
        htmlGenerator.Verify();
    }

    [Fact]
    public void BeginForm_WithAntiforgery_CallsHtmlGeneratorWithExpectedValues()
    {
        // Arrange
        var tagBuilder = new TagBuilder(tagName: "form");
        var htmlGenerator = new Mock<IHtmlGenerator>(MockBehavior.Strict);
        var htmlHelper = DefaultTemplatesUtilities.GetHtmlHelper(htmlGenerator.Object);
        htmlGenerator
            .Setup(g => g.GenerateForm(
                htmlHelper.ViewContext,
                null,   // actionName
                null,   // controllerName
                null,   // routeValues
                "post", // method
                null))  // htmlAttributes
            .Returns(tagBuilder)
            .Verifiable();
        htmlGenerator
            .Setup(g => g.GenerateAntiforgery(htmlHelper.ViewContext))
            .Returns(HtmlString.Empty)
            .Verifiable();

        // Guards
        Assert.NotNull(htmlHelper.ViewContext);
        var writer = Assert.IsAssignableFrom<StringWriter>(htmlHelper.ViewContext.Writer);
        var builder = writer.GetStringBuilder();
        Assert.NotNull(builder);

        // Act
        var mvcForm = htmlHelper.BeginForm(antiforgery: true);

        // Assert
        Assert.NotNull(mvcForm);
        Assert.Equal("<form>", builder.ToString());
        htmlGenerator.Verify();
    }

    [Fact]
    public void BeginForm_SuppressAntiforgery_CallsHtmlGeneratorWithExpectedValues()
    {
        // Arrange
        var tagBuilder = new TagBuilder(tagName: "form");
        var htmlGenerator = new Mock<IHtmlGenerator>(MockBehavior.Strict);
        var htmlHelper = DefaultTemplatesUtilities.GetHtmlHelper(htmlGenerator.Object);
        htmlGenerator
            .Setup(g => g.GenerateForm(
                htmlHelper.ViewContext,
                null,   // actionName
                null,   // controllerName
                null,   // routeValues
                "post", // method
                null))  // htmlAttributes
            .Returns(tagBuilder)
            .Verifiable();

        // Guards
        Assert.NotNull(htmlHelper.ViewContext);
        var writer = Assert.IsAssignableFrom<StringWriter>(htmlHelper.ViewContext.Writer);
        var builder = writer.GetStringBuilder();
        Assert.NotNull(builder);

        // Act
        var mvcForm = htmlHelper.BeginForm(antiforgery: false);

        // Assert
        Assert.NotNull(mvcForm);
        Assert.Equal("<form>", builder.ToString());
        htmlGenerator.Verify();
    }

    [Theory]
    [MemberData(nameof(MethodDataSet))]
    public void BeginFormWithMethodParameter_CallsHtmlGeneratorWithExpectedValues(FormMethod method)
    {
        // Arrange
        var tagBuilder = new TagBuilder(tagName: "form");
        var htmlGenerator = new Mock<IHtmlGenerator>(MockBehavior.Strict);
        var htmlHelper = DefaultTemplatesUtilities.GetHtmlHelper(htmlGenerator.Object);
        htmlGenerator
            .Setup(g => g.GenerateForm(
                htmlHelper.ViewContext,
                null,   // actionName
                null,   // controllerName
                null,   // routeValues
                method.ToString().ToLowerInvariant(),
                null))  // htmlAttributes
            .Returns(tagBuilder)
            .Verifiable();

        if (method != FormMethod.Get)
        {
            htmlGenerator
                .Setup(g => g.GenerateAntiforgery(htmlHelper.ViewContext))
                .Returns(HtmlString.Empty)
                .Verifiable();
        }

        // Guards
        Assert.NotNull(htmlHelper.ViewContext);
        var writer = Assert.IsAssignableFrom<StringWriter>(htmlHelper.ViewContext.Writer);
        var builder = writer.GetStringBuilder();
        Assert.NotNull(builder);

        // Act
        var mvcForm = htmlHelper.BeginForm(method);

        // Assert
        Assert.NotNull(mvcForm);
        Assert.Equal("<form>", builder.ToString());
        htmlGenerator.Verify();
    }

    [Theory]
    [MemberData(nameof(MethodAndHtmlAttributesDataSet))]
    public void BeginFormWithMethodAndHtmlAttributesParameters_CallsHtmlGeneratorWithExpectedValues(
        FormMethod method,
        object htmlAttributes)
    {
        // Arrange
        var tagBuilder = new TagBuilder(tagName: "form");
        var htmlGenerator = new Mock<IHtmlGenerator>(MockBehavior.Strict);
        var htmlHelper = DefaultTemplatesUtilities.GetHtmlHelper(htmlGenerator.Object);
        htmlGenerator
            .Setup(g => g.GenerateForm(
                htmlHelper.ViewContext,
                null,   // actionName
                null,   // controllerName
                null,   // routeValues
                method.ToString().ToLowerInvariant(),
                htmlAttributes))
            .Returns(tagBuilder)
            .Verifiable();

        if (method != FormMethod.Get)
        {
            htmlGenerator
                .Setup(g => g.GenerateAntiforgery(htmlHelper.ViewContext))
                .Returns(HtmlString.Empty)
                .Verifiable();
        }

        // Guards
        Assert.NotNull(htmlHelper.ViewContext);
        var writer = Assert.IsAssignableFrom<StringWriter>(htmlHelper.ViewContext.Writer);
        var builder = writer.GetStringBuilder();
        Assert.NotNull(builder);

        // Act
        var mvcForm = htmlHelper.BeginForm(method, htmlAttributes);

        // Assert
        Assert.NotNull(mvcForm);
        Assert.Equal("<form>", builder.ToString());
        htmlGenerator.Verify();
    }

    [Theory]
    [MemberData(nameof(MethodAndHtmlAttributesDataSet))]
    public void BeginFormWithMethodAndHtmlAttributesParameters_WithAntiforgery_CallsHtmlGeneratorWithExpectedValues(
        FormMethod method,
        object htmlAttributes)
    {
        // Arrange
        var tagBuilder = new TagBuilder(tagName: "form");
        var htmlGenerator = new Mock<IHtmlGenerator>(MockBehavior.Strict);
        var htmlHelper = DefaultTemplatesUtilities.GetHtmlHelper(htmlGenerator.Object);
        htmlGenerator
            .Setup(g => g.GenerateForm(
                htmlHelper.ViewContext,
                null,   // actionName
                null,   // controllerName
                null,   // routeValues
                method.ToString().ToLowerInvariant(),
                htmlAttributes))
            .Returns(tagBuilder)
            .Verifiable();
        htmlGenerator
            .Setup(g => g.GenerateAntiforgery(htmlHelper.ViewContext))
            .Returns(HtmlString.Empty)
            .Verifiable();

        // Guards
        Assert.NotNull(htmlHelper.ViewContext);
        var writer = Assert.IsAssignableFrom<StringWriter>(htmlHelper.ViewContext.Writer);
        var builder = writer.GetStringBuilder();
        Assert.NotNull(builder);

        // Act
        var mvcForm = htmlHelper.BeginForm(method, antiforgery: true, htmlAttributes: htmlAttributes);

        // Assert
        Assert.NotNull(mvcForm);
        Assert.Equal("<form>", builder.ToString());
        htmlGenerator.Verify();
    }

    [Theory]
    [MemberData(nameof(MethodAndHtmlAttributesDataSet))]
    public void BeginFormWithMethodAndHtmlAttributesParameters_SuppressAntiforgery_CallsHtmlGeneratorWithExpectedValues(
        FormMethod method,
        object htmlAttributes)
    {
        // Arrange
        var tagBuilder = new TagBuilder(tagName: "form");
        var htmlGenerator = new Mock<IHtmlGenerator>(MockBehavior.Strict);
        var htmlHelper = DefaultTemplatesUtilities.GetHtmlHelper(htmlGenerator.Object);
        htmlGenerator
            .Setup(g => g.GenerateForm(
                htmlHelper.ViewContext,
                null,   // actionName
                null,   // controllerName
                null,   // routeValues
                method.ToString().ToLowerInvariant(),
                htmlAttributes))
            .Returns(tagBuilder)
            .Verifiable();

        // Guards
        Assert.NotNull(htmlHelper.ViewContext);
        var writer = Assert.IsAssignableFrom<StringWriter>(htmlHelper.ViewContext.Writer);
        var builder = writer.GetStringBuilder();
        Assert.NotNull(builder);

        // Act
        var mvcForm = htmlHelper.BeginForm(method, antiforgery: false, htmlAttributes: htmlAttributes);

        // Assert
        Assert.NotNull(mvcForm);
        Assert.Equal("<form>", builder.ToString());
        htmlGenerator.Verify();
    }

    [Theory]
    [MemberData(nameof(RouteValuesDataSet))]
    public void BeginFormWithRouteValuesParameter_CallsHtmlGeneratorWithExpectedValues(object routeValues)
    {
        // Arrange
        var tagBuilder = new TagBuilder(tagName: "form");
        var htmlGenerator = new Mock<IHtmlGenerator>(MockBehavior.Strict);
        var htmlHelper = DefaultTemplatesUtilities.GetHtmlHelper(htmlGenerator.Object);
        htmlGenerator
            .Setup(g => g.GenerateForm(
                htmlHelper.ViewContext,
                null,   // actionName
                null,   // controllerName
                routeValues,
                "post", // method
                null))  // htmlAttributes
            .Returns(tagBuilder)
            .Verifiable();
        htmlGenerator
            .Setup(g => g.GenerateAntiforgery(htmlHelper.ViewContext))
            .Returns(HtmlString.Empty)
            .Verifiable();

        // Guards
        Assert.NotNull(htmlHelper.ViewContext);
        var writer = Assert.IsAssignableFrom<StringWriter>(htmlHelper.ViewContext.Writer);
        var builder = writer.GetStringBuilder();
        Assert.NotNull(builder);

        // Act
        var mvcForm = htmlHelper.BeginForm(routeValues);

        // Assert
        Assert.NotNull(mvcForm);
        Assert.Equal("<form>", builder.ToString());
        htmlGenerator.Verify();
    }

    [Theory]
    [MemberData(nameof(ActionNameAndControllerNameDataSet))]
    public void BeginFormWithActionNameAndControllerNameParameters_CallsHtmlGeneratorWithExpectedValues(
        string actionName,
        string controllerName)
    {
        // Arrange
        var tagBuilder = new TagBuilder(tagName: "form");
        var htmlGenerator = new Mock<IHtmlGenerator>(MockBehavior.Strict);
        var htmlHelper = DefaultTemplatesUtilities.GetHtmlHelper(htmlGenerator.Object);
        htmlGenerator
            .Setup(g => g.GenerateForm(
                htmlHelper.ViewContext,
                actionName,
                controllerName,
                null,   // routeValues
                "post", // method
                null))  // htmlAttributes
            .Returns(tagBuilder)
            .Verifiable();
        htmlGenerator
            .Setup(g => g.GenerateAntiforgery(htmlHelper.ViewContext))
            .Returns(HtmlString.Empty)
            .Verifiable();

        // Guards
        Assert.NotNull(htmlHelper.ViewContext);
        var writer = Assert.IsAssignableFrom<StringWriter>(htmlHelper.ViewContext.Writer);
        var builder = writer.GetStringBuilder();
        Assert.NotNull(builder);

        // Act
        var mvcForm = htmlHelper.BeginForm(actionName, controllerName);

        // Assert
        Assert.NotNull(mvcForm);
        Assert.Equal("<form>", builder.ToString());
        htmlGenerator.Verify();
    }

    [Theory]
    [MemberData(nameof(ActionNameControllerNameAndRouteValuesDataSet))]
    public void BeginFormWithActionNameControllerNameAndRouteValuesParameters_CallsHtmlGeneratorWithExpectedValues(
        string actionName,
        string controllerName,
        object routeValues)
    {
        // Arrange
        var tagBuilder = new TagBuilder(tagName: "form");
        var htmlGenerator = new Mock<IHtmlGenerator>(MockBehavior.Strict);
        var htmlHelper = DefaultTemplatesUtilities.GetHtmlHelper(htmlGenerator.Object);
        htmlGenerator
            .Setup(g => g.GenerateForm(
                htmlHelper.ViewContext,
                actionName,
                controllerName,
                routeValues,
                "post", // method
                null))  // htmlAttributes
            .Returns(tagBuilder)
            .Verifiable();
        htmlGenerator
            .Setup(g => g.GenerateAntiforgery(htmlHelper.ViewContext))
            .Returns(HtmlString.Empty)
            .Verifiable();

        // Guards
        Assert.NotNull(htmlHelper.ViewContext);
        var writer = Assert.IsAssignableFrom<StringWriter>(htmlHelper.ViewContext.Writer);
        var builder = writer.GetStringBuilder();
        Assert.NotNull(builder);

        // Act
        var mvcForm = htmlHelper.BeginForm(actionName, controllerName, routeValues);

        // Assert
        Assert.NotNull(mvcForm);
        Assert.Equal("<form>", builder.ToString());
        htmlGenerator.Verify();
    }

    [Theory]
    [MemberData(nameof(ActionNameControllerNameAndMethodDataSet))]
    public void BeginFormWithActionNameControllerNameAndMethodParameters_CallsHtmlGeneratorWithExpectedValues(
        string actionName,
        string controllerName,
        FormMethod method)
    {
        // Arrange
        var tagBuilder = new TagBuilder(tagName: "form");
        var htmlGenerator = new Mock<IHtmlGenerator>(MockBehavior.Strict);
        var htmlHelper = DefaultTemplatesUtilities.GetHtmlHelper(htmlGenerator.Object);
        htmlGenerator
            .Setup(g => g.GenerateForm(
                htmlHelper.ViewContext,
                actionName,
                controllerName,
                null,   // routeValues
                method.ToString().ToLowerInvariant(),
                null))  // htmlAttributes
            .Returns(tagBuilder)
            .Verifiable();

        if (method != FormMethod.Get)
        {
            htmlGenerator
                .Setup(g => g.GenerateAntiforgery(htmlHelper.ViewContext))
                .Returns(HtmlString.Empty)
                .Verifiable();
        }

        // Guards
        Assert.NotNull(htmlHelper.ViewContext);
        var writer = Assert.IsAssignableFrom<StringWriter>(htmlHelper.ViewContext.Writer);
        var builder = writer.GetStringBuilder();
        Assert.NotNull(builder);

        // Act
        var mvcForm = htmlHelper.BeginForm(actionName, controllerName, method);

        // Assert
        Assert.NotNull(mvcForm);
        Assert.Equal("<form>", builder.ToString());
        htmlGenerator.Verify();
    }

    [Theory]
    [MemberData(nameof(ActionNameControllerNameRouteValuesAndMethodDataSet))]
    public void BeginFormWithActionNameControllerNameRouteValuesAndMethodParameters_CallsHtmlGeneratorWithExpectedValues(
        string actionName,
        string controllerName,
        object routeValues,
        FormMethod method)
    {
        // Arrange
        var tagBuilder = new TagBuilder(tagName: "form");
        var htmlGenerator = new Mock<IHtmlGenerator>(MockBehavior.Strict);
        var htmlHelper = DefaultTemplatesUtilities.GetHtmlHelper(htmlGenerator.Object);
        htmlGenerator
            .Setup(g => g.GenerateForm(
                htmlHelper.ViewContext,
                actionName,
                controllerName,
                routeValues,
                method.ToString().ToLowerInvariant(),
                null))  // htmlAttributes
            .Returns(tagBuilder)
            .Verifiable();

        if (method != FormMethod.Get)
        {
            htmlGenerator
            .Setup(g => g.GenerateAntiforgery(htmlHelper.ViewContext))
            .Returns(HtmlString.Empty)
            .Verifiable();
        }

        // Guards
        Assert.NotNull(htmlHelper.ViewContext);
        var writer = Assert.IsAssignableFrom<StringWriter>(htmlHelper.ViewContext.Writer);
        var builder = writer.GetStringBuilder();
        Assert.NotNull(builder);

        // Act
        var mvcForm = htmlHelper.BeginForm(actionName, controllerName, routeValues, method);

        // Assert
        Assert.NotNull(mvcForm);
        Assert.Equal("<form>", builder.ToString());
        htmlGenerator.Verify();
    }

    [Theory]
    [MemberData(nameof(ActionNameControllerNameMethodAndHtmlAttributesDataSet))]
    public void BeginFormWithActionNameControllerNameMethodAndHtmlAttributesParameters_CallsHtmlGeneratorWithExpectedValues(
        string actionName,
        string controllerName,
        FormMethod method,
        object htmlAttributes)
    {
        // Arrange
        var tagBuilder = new TagBuilder(tagName: "form");
        var htmlGenerator = new Mock<IHtmlGenerator>(MockBehavior.Strict);
        var htmlHelper = DefaultTemplatesUtilities.GetHtmlHelper(htmlGenerator.Object);
        htmlGenerator
            .Setup(g => g.GenerateForm(
                htmlHelper.ViewContext,
                actionName,
                controllerName,
                null,   // routeValues
                method.ToString().ToLowerInvariant(),
                htmlAttributes))
            .Returns(tagBuilder)
            .Verifiable();

        if (method != FormMethod.Get)
        {
            htmlGenerator
                .Setup(g => g.GenerateAntiforgery(htmlHelper.ViewContext))
                .Returns(HtmlString.Empty)
                .Verifiable();
        }

        // Guards
        Assert.NotNull(htmlHelper.ViewContext);
        var writer = Assert.IsAssignableFrom<StringWriter>(htmlHelper.ViewContext.Writer);
        var builder = writer.GetStringBuilder();
        Assert.NotNull(builder);

        // Act
        var mvcForm = htmlHelper.BeginForm(actionName, controllerName, method, htmlAttributes);

        // Assert
        Assert.NotNull(mvcForm);
        Assert.Equal("<form>", builder.ToString());
        htmlGenerator.Verify();
    }

    [Theory]
    [MemberData(nameof(ActionNameControllerNameMethodAndHtmlAttributesDataSet))]
    public void BeginFormWithActionNameControllerNameMethodAndHtmlAttributesParameters_WithAntiforgery_CallsHtmlGeneratorWithExpectedValues(
        string actionName,
        string controllerName,
        FormMethod method,
        object htmlAttributes)
    {
        // Arrange
        var tagBuilder = new TagBuilder(tagName: "form");
        var htmlGenerator = new Mock<IHtmlGenerator>(MockBehavior.Strict);
        var htmlHelper = DefaultTemplatesUtilities.GetHtmlHelper(htmlGenerator.Object);
        htmlGenerator
            .Setup(g => g.GenerateForm(
                htmlHelper.ViewContext,
                actionName,
                controllerName,
                null,   // routeValues
                method.ToString().ToLowerInvariant(),
                htmlAttributes))
            .Returns(tagBuilder)
            .Verifiable();
        htmlGenerator
            .Setup(g => g.GenerateAntiforgery(htmlHelper.ViewContext))
            .Returns(HtmlString.Empty)
            .Verifiable();

        // Guards
        Assert.NotNull(htmlHelper.ViewContext);
        var writer = Assert.IsAssignableFrom<StringWriter>(htmlHelper.ViewContext.Writer);
        var builder = writer.GetStringBuilder();
        Assert.NotNull(builder);

        // Act
        var mvcForm = htmlHelper.BeginForm(
            actionName,
            controllerName,
            routeValues: null,
            method: method,
            antiforgery: true,
            htmlAttributes: htmlAttributes);

        // Assert
        Assert.NotNull(mvcForm);
        Assert.Equal("<form>", builder.ToString());
        htmlGenerator.Verify();
    }

    [Theory]
    [MemberData(nameof(ActionNameControllerNameMethodAndHtmlAttributesDataSet))]
    public void BeginFormWithActionNameControllerNameMethodAndHtmlAttributesParameters_SuppressAntiforgery_CallsHtmlGeneratorWithExpectedValues(
        string actionName,
        string controllerName,
        FormMethod method,
        object htmlAttributes)
    {
        // Arrange
        var tagBuilder = new TagBuilder(tagName: "form");
        var htmlGenerator = new Mock<IHtmlGenerator>(MockBehavior.Strict);
        var htmlHelper = DefaultTemplatesUtilities.GetHtmlHelper(htmlGenerator.Object);
        htmlGenerator
            .Setup(g => g.GenerateForm(
                htmlHelper.ViewContext,
                actionName,
                controllerName,
                null,   // routeValues
                method.ToString().ToLowerInvariant(),
                htmlAttributes))
            .Returns(tagBuilder)
            .Verifiable();

        // Guards
        Assert.NotNull(htmlHelper.ViewContext);
        var writer = Assert.IsAssignableFrom<StringWriter>(htmlHelper.ViewContext.Writer);
        var builder = writer.GetStringBuilder();
        Assert.NotNull(builder);

        // Act
        var mvcForm = htmlHelper.BeginForm(
            actionName,
            controllerName,
            routeValues: null,
            method: method,
            antiforgery: false,
            htmlAttributes: htmlAttributes);

        // Assert
        Assert.NotNull(mvcForm);
        Assert.Equal("<form>", builder.ToString());
        htmlGenerator.Verify();
    }

    [Theory]
    [MemberData(nameof(RouteValuesDataSet))]
    public void BeginRouteFormWithRouteValuesParameter_CallsHtmlGeneratorWithExpectedValues(object routeValues)
    {
        // Arrange
        var tagBuilder = new TagBuilder(tagName: "form");
        var htmlGenerator = new Mock<IHtmlGenerator>(MockBehavior.Strict);
        var htmlHelper = DefaultTemplatesUtilities.GetHtmlHelper(htmlGenerator.Object);
        htmlGenerator
            .Setup(g => g.GenerateRouteForm(
                htmlHelper.ViewContext,
                null,   // routeName
                routeValues,
                "post", // method
                null))  // htmlAttributes
            .Returns(tagBuilder)
            .Verifiable();
        htmlGenerator
            .Setup(g => g.GenerateAntiforgery(htmlHelper.ViewContext))
            .Returns(HtmlString.Empty)
            .Verifiable();

        // Guards
        Assert.NotNull(htmlHelper.ViewContext);
        var writer = Assert.IsAssignableFrom<StringWriter>(htmlHelper.ViewContext.Writer);
        var builder = writer.GetStringBuilder();
        Assert.NotNull(builder);

        // Act
        var mvcForm = htmlHelper.BeginRouteForm(routeValues);

        // Assert
        Assert.NotNull(mvcForm);
        Assert.Equal("<form>", builder.ToString());
        htmlGenerator.Verify();
    }

    [Theory]
    [MemberData(nameof(RouteValuesDataSet))]
    public void BeginRouteFormWithRouteValuesParameter_WithAntiforgery_CallsHtmlGeneratorWithExpectedValues(
        object routeValues)
    {
        // Arrange
        var tagBuilder = new TagBuilder(tagName: "form");
        var htmlGenerator = new Mock<IHtmlGenerator>(MockBehavior.Strict);
        var htmlHelper = DefaultTemplatesUtilities.GetHtmlHelper(htmlGenerator.Object);
        htmlGenerator
            .Setup(g => g.GenerateRouteForm(
                htmlHelper.ViewContext,
                null,   // routeName
                routeValues,
                "post", // method
                null))  // htmlAttributes
            .Returns(tagBuilder)
            .Verifiable();
        htmlGenerator
            .Setup(g => g.GenerateAntiforgery(htmlHelper.ViewContext))
            .Returns(HtmlString.Empty)
            .Verifiable();

        // Guards
        Assert.NotNull(htmlHelper.ViewContext);
        var writer = Assert.IsAssignableFrom<StringWriter>(htmlHelper.ViewContext.Writer);
        var builder = writer.GetStringBuilder();
        Assert.NotNull(builder);

        // Act
        var mvcForm = htmlHelper.BeginRouteForm(routeValues, antiforgery: true);

        // Assert
        Assert.NotNull(mvcForm);
        Assert.Equal("<form>", builder.ToString());
        htmlGenerator.Verify();
    }

    [Theory]
    [MemberData(nameof(RouteValuesDataSet))]
    public void BeginRouteFormWithRouteValuesParameter_SuppressAntiforgery_CallsHtmlGeneratorWithExpectedValues(
        object routeValues)
    {
        // Arrange
        var tagBuilder = new TagBuilder(tagName: "form");
        var htmlGenerator = new Mock<IHtmlGenerator>(MockBehavior.Strict);
        var htmlHelper = DefaultTemplatesUtilities.GetHtmlHelper(htmlGenerator.Object);
        htmlGenerator
            .Setup(g => g.GenerateRouteForm(
                htmlHelper.ViewContext,
                null,   // routeName
                routeValues,
                "post", // method
                null))  // htmlAttributes
            .Returns(tagBuilder)
            .Verifiable();

        // Guards
        Assert.NotNull(htmlHelper.ViewContext);
        var writer = Assert.IsAssignableFrom<StringWriter>(htmlHelper.ViewContext.Writer);
        var builder = writer.GetStringBuilder();
        Assert.NotNull(builder);

        // Act
        var mvcForm = htmlHelper.BeginRouteForm(routeValues, antiforgery: false);

        // Assert
        Assert.NotNull(mvcForm);
        Assert.Equal("<form>", builder.ToString());
        htmlGenerator.Verify();
    }

    [Theory]
    [MemberData(nameof(RouteNameDataSet))]
    public void BeginRouteFormWithRouteNameParameter_CallsHtmlGeneratorWithExpectedValues(string routeName)
    {
        // Arrange
        var tagBuilder = new TagBuilder(tagName: "form");
        var htmlGenerator = new Mock<IHtmlGenerator>(MockBehavior.Strict);
        var htmlHelper = DefaultTemplatesUtilities.GetHtmlHelper(htmlGenerator.Object);
        htmlGenerator
            .Setup(g => g.GenerateRouteForm(
                htmlHelper.ViewContext,
                routeName,
                null,   // routeValues
                "post", // method
                null))  // htmlAttributes
            .Returns(tagBuilder)
            .Verifiable();
        htmlGenerator
            .Setup(g => g.GenerateAntiforgery(htmlHelper.ViewContext))
            .Returns(HtmlString.Empty)
            .Verifiable();

        // Guards
        Assert.NotNull(htmlHelper.ViewContext);
        var writer = Assert.IsAssignableFrom<StringWriter>(htmlHelper.ViewContext.Writer);
        var builder = writer.GetStringBuilder();
        Assert.NotNull(builder);

        // Act
        var mvcForm = htmlHelper.BeginRouteForm(routeName);

        // Assert
        Assert.NotNull(mvcForm);
        Assert.Equal("<form>", builder.ToString());
        htmlGenerator.Verify();
    }

    [Theory]
    [MemberData(nameof(RouteNameDataSet))]
    public void BeginRouteFormWithRouteNameParameter_WithAntiforgery_CallsHtmlGeneratorWithExpectedValues(
        string routeName)
    {
        // Arrange
        var tagBuilder = new TagBuilder(tagName: "form");
        var htmlGenerator = new Mock<IHtmlGenerator>(MockBehavior.Strict);
        var htmlHelper = DefaultTemplatesUtilities.GetHtmlHelper(htmlGenerator.Object);
        htmlGenerator
            .Setup(g => g.GenerateRouteForm(
                htmlHelper.ViewContext,
                routeName,
                null,   // routeValues
                "post", // method
                null))  // htmlAttributes
            .Returns(tagBuilder)
            .Verifiable();
        htmlGenerator
            .Setup(g => g.GenerateAntiforgery(htmlHelper.ViewContext))
            .Returns(HtmlString.Empty)
            .Verifiable();

        // Guards
        Assert.NotNull(htmlHelper.ViewContext);
        var writer = Assert.IsAssignableFrom<StringWriter>(htmlHelper.ViewContext.Writer);
        var builder = writer.GetStringBuilder();
        Assert.NotNull(builder);

        // Act
        var mvcForm = htmlHelper.BeginRouteForm(routeName, antiforgery: true);

        // Assert
        Assert.NotNull(mvcForm);
        Assert.Equal("<form>", builder.ToString());
        htmlGenerator.Verify();
    }

    [Theory]
    [MemberData(nameof(RouteNameDataSet))]
    public void BeginRouteFormWithRouteNameParameter_SuppressAntiforgery_CallsHtmlGeneratorWithExpectedValues(
        string routeName)
    {
        // Arrange
        var tagBuilder = new TagBuilder(tagName: "form");
        var htmlGenerator = new Mock<IHtmlGenerator>(MockBehavior.Strict);
        var htmlHelper = DefaultTemplatesUtilities.GetHtmlHelper(htmlGenerator.Object);
        htmlGenerator
            .Setup(g => g.GenerateRouteForm(
                htmlHelper.ViewContext,
                routeName,
                null,   // routeValues
                "post", // method
                null))  // htmlAttributes
            .Returns(tagBuilder)
            .Verifiable();

        // Guards
        Assert.NotNull(htmlHelper.ViewContext);
        var writer = Assert.IsAssignableFrom<StringWriter>(htmlHelper.ViewContext.Writer);
        var builder = writer.GetStringBuilder();
        Assert.NotNull(builder);

        // Act
        var mvcForm = htmlHelper.BeginRouteForm(routeName, antiforgery: false);

        // Assert
        Assert.NotNull(mvcForm);
        Assert.Equal("<form>", builder.ToString());
        htmlGenerator.Verify();
    }

    [Theory]
    [MemberData(nameof(RouteNameAndRouteValuesDataSet))]
    public void BeginRouteFormWithRouteNameAndRouteValuesParameters_CallsHtmlGeneratorWithExpectedValues(
        string routeName,
        object routeValues)
    {
        // Arrange
        var tagBuilder = new TagBuilder(tagName: "form");
        var htmlGenerator = new Mock<IHtmlGenerator>(MockBehavior.Strict);
        var htmlHelper = DefaultTemplatesUtilities.GetHtmlHelper(htmlGenerator.Object);
        htmlGenerator
            .Setup(g => g.GenerateRouteForm(
                htmlHelper.ViewContext,
                routeName,
                routeValues,
                "post", // method
                null))  // htmlAttributes
            .Returns(tagBuilder)
            .Verifiable();
        htmlGenerator
            .Setup(g => g.GenerateAntiforgery(htmlHelper.ViewContext))
            .Returns(HtmlString.Empty)
            .Verifiable();

        // Guards
        Assert.NotNull(htmlHelper.ViewContext);
        var writer = Assert.IsAssignableFrom<StringWriter>(htmlHelper.ViewContext.Writer);
        var builder = writer.GetStringBuilder();
        Assert.NotNull(builder);

        // Act
        var mvcForm = htmlHelper.BeginRouteForm(routeName, routeValues);

        // Assert
        Assert.NotNull(mvcForm);
        Assert.Equal("<form>", builder.ToString());
        htmlGenerator.Verify();
    }

    [Theory]
    [MemberData(nameof(RouteNameAndMethodDataSet))]
    public void BeginRouteFormWithRouteNameAndMethodParameters_CallsHtmlGeneratorWithExpectedValues(
        string routeName,
        FormMethod method)
    {
        // Arrange
        var tagBuilder = new TagBuilder(tagName: "form");
        var htmlGenerator = new Mock<IHtmlGenerator>(MockBehavior.Strict);
        var htmlHelper = DefaultTemplatesUtilities.GetHtmlHelper(htmlGenerator.Object);
        htmlGenerator
            .Setup(g => g.GenerateRouteForm(
                htmlHelper.ViewContext,
                routeName,
                null,   // routeValues
                method.ToString().ToLowerInvariant(),
                null))  // htmlAttributes
            .Returns(tagBuilder)
            .Verifiable();

        if (method != FormMethod.Get)
        {
            htmlGenerator
                .Setup(g => g.GenerateAntiforgery(htmlHelper.ViewContext))
                .Returns(HtmlString.Empty)
                .Verifiable();
        }

        // Guards
        Assert.NotNull(htmlHelper.ViewContext);
        var writer = Assert.IsAssignableFrom<StringWriter>(htmlHelper.ViewContext.Writer);
        var builder = writer.GetStringBuilder();
        Assert.NotNull(builder);

        // Act
        var mvcForm = htmlHelper.BeginRouteForm(routeName, method);

        // Assert
        Assert.NotNull(mvcForm);
        Assert.Equal("<form>", builder.ToString());
        htmlGenerator.Verify();
    }

    [Theory]
    [MemberData(nameof(RouteNameRouteValuesAndMethodDataSet))]
    public void BeginRouteFormWithRouteNameRouteValuesAndMethodParameters_CallsHtmlGeneratorWithExpectedValues(
        string routeName,
        object routeValues,
        FormMethod method)
    {
        // Arrange
        var tagBuilder = new TagBuilder(tagName: "form");
        var htmlGenerator = new Mock<IHtmlGenerator>(MockBehavior.Strict);
        var htmlHelper = DefaultTemplatesUtilities.GetHtmlHelper(htmlGenerator.Object);
        htmlGenerator
            .Setup(g => g.GenerateRouteForm(
                htmlHelper.ViewContext,
                routeName,
                routeValues,
                method.ToString().ToLowerInvariant(),
                null))  // htmlAttributes
            .Returns(tagBuilder)
            .Verifiable();

        if (method != FormMethod.Get)
        {
            htmlGenerator
                .Setup(g => g.GenerateAntiforgery(htmlHelper.ViewContext))
                .Returns(HtmlString.Empty)
                .Verifiable();
        }

        // Guards
        Assert.NotNull(htmlHelper.ViewContext);
        var writer = Assert.IsAssignableFrom<StringWriter>(htmlHelper.ViewContext.Writer);
        var builder = writer.GetStringBuilder();
        Assert.NotNull(builder);

        // Act
        var mvcForm = htmlHelper.BeginRouteForm(routeName, routeValues, method);

        // Assert
        Assert.NotNull(mvcForm);
        Assert.Equal("<form>", builder.ToString());
        htmlGenerator.Verify();
    }

    [Theory]
    [MemberData(nameof(RouteNameMethodAndHtmlAttributesDataSet))]
    public void BeginRouteFormWithRouteNameMethodAndHtmlAttributesParameters_CallsHtmlGeneratorWithExpectedValues(
        string routeName,
        FormMethod method,
        object htmlAttributes)
    {
        // Arrange
        var tagBuilder = new TagBuilder(tagName: "form");
        var htmlGenerator = new Mock<IHtmlGenerator>(MockBehavior.Strict);
        var htmlHelper = DefaultTemplatesUtilities.GetHtmlHelper(htmlGenerator.Object);
        htmlGenerator
            .Setup(g => g.GenerateRouteForm(
                htmlHelper.ViewContext,
                routeName,
                null,   // routeValues
                method.ToString().ToLowerInvariant(),
                htmlAttributes))
            .Returns(tagBuilder)
            .Verifiable();

        if (method != FormMethod.Get)
        {
            htmlGenerator
                .Setup(g => g.GenerateAntiforgery(htmlHelper.ViewContext))
                .Returns(HtmlString.Empty)
                .Verifiable();
        }

        // Guards
        Assert.NotNull(htmlHelper.ViewContext);
        var writer = Assert.IsAssignableFrom<StringWriter>(htmlHelper.ViewContext.Writer);
        var builder = writer.GetStringBuilder();
        Assert.NotNull(builder);

        // Act
        var mvcForm = htmlHelper.BeginRouteForm(routeName, method, htmlAttributes);

        // Assert
        Assert.NotNull(mvcForm);
        Assert.Equal("<form>", builder.ToString());
        htmlGenerator.Verify();
    }

    [Theory]
    [MemberData(nameof(RouteNameMethodAndHtmlAttributesDataSet))]
    public void BeginRouteFormWithRouteNameMethodAndHtmlAttributesParameters_WithAntiforgery_CallsHtmlGeneratorWithExpectedValues(
        string routeName,
        FormMethod method,
        object htmlAttributes)
    {
        // Arrange
        var tagBuilder = new TagBuilder(tagName: "form");
        var htmlGenerator = new Mock<IHtmlGenerator>(MockBehavior.Strict);
        var htmlHelper = DefaultTemplatesUtilities.GetHtmlHelper(htmlGenerator.Object);
        htmlGenerator
            .Setup(g => g.GenerateRouteForm(
                htmlHelper.ViewContext,
                routeName,
                null,   // routeValues
                method.ToString().ToLowerInvariant(),
                htmlAttributes))
            .Returns(tagBuilder)
            .Verifiable();
        htmlGenerator
            .Setup(g => g.GenerateAntiforgery(htmlHelper.ViewContext))
            .Returns(HtmlString.Empty)
            .Verifiable();

        // Guards
        Assert.NotNull(htmlHelper.ViewContext);
        var writer = Assert.IsAssignableFrom<StringWriter>(htmlHelper.ViewContext.Writer);
        var builder = writer.GetStringBuilder();
        Assert.NotNull(builder);

        // Act
        var mvcForm = htmlHelper.BeginRouteForm(
            routeName,
            routeValues: null,
            method: method,
            antiforgery: true,
            htmlAttributes: htmlAttributes);

        // Assert
        Assert.NotNull(mvcForm);
        Assert.Equal("<form>", builder.ToString());
        htmlGenerator.Verify();
    }

    [Theory]
    [MemberData(nameof(RouteNameMethodAndHtmlAttributesDataSet))]
    public void BeginRouteFormWithRouteNameMethodAndHtmlAttributesParameters_SuppressAntiforgery_CallsHtmlGeneratorWithExpectedValues(
        string routeName,
        FormMethod method,
        object htmlAttributes)
    {
        // Arrange
        var tagBuilder = new TagBuilder(tagName: "form");
        var htmlGenerator = new Mock<IHtmlGenerator>(MockBehavior.Strict);
        var htmlHelper = DefaultTemplatesUtilities.GetHtmlHelper(htmlGenerator.Object);
        htmlGenerator
            .Setup(g => g.GenerateRouteForm(
                htmlHelper.ViewContext,
                routeName,
                null,   // routeValues
                method.ToString().ToLowerInvariant(),
                htmlAttributes))
            .Returns(tagBuilder)
            .Verifiable();

        // Guards
        Assert.NotNull(htmlHelper.ViewContext);
        var writer = Assert.IsAssignableFrom<StringWriter>(htmlHelper.ViewContext.Writer);
        var builder = writer.GetStringBuilder();
        Assert.NotNull(builder);

        // Act
        var mvcForm = htmlHelper.BeginRouteForm(
            routeName,
            routeValues: null,
            method: method,
            antiforgery: false,
            htmlAttributes: htmlAttributes);

        // Assert
        Assert.NotNull(mvcForm);
        Assert.Equal("<form>", builder.ToString());
        htmlGenerator.Verify();
    }
}
