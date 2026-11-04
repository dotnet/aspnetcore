// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Moq;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures;

public class DefaultDisplayTemplatesTest
{
    // Input value; HTML encode; expected value.
    public static TheoryData<string, bool, string> HtmlEncodeData
    {
        get
        {
            return new TheoryData<string, bool, string>
                {
                    { "Simple Display Text", false, "Simple Display Text" },
                    { "Simple Display Text", true, "HtmlEncode[[Simple Display Text]]" },
                    { "<blink>text</blink>", false, "<blink>text</blink>" },
                    { "<blink>text</blink>", true, "HtmlEncode[[<blink>text</blink>]]" },
                    { "&'\"", false, "&'\"" },
                    { "&'\"", true, "HtmlEncode[[&'\"]]" },
                    { " ¡ÿĀ", false, " ¡ÿĀ" },                                           // high ASCII
                    { " ¡ÿĀ", true, "HtmlEncode[[ ¡ÿĀ]]" },
                    { "Chinese西雅图Chars", false, "Chinese西雅图Chars" },
                    { "Chinese西雅图Chars", true, "HtmlEncode[[Chinese西雅图Chars]]" },
                    { "Unicode؃Format؃Char", false, "Unicode؃Format؃Char" },            // class Cf
                    { "Unicode؃Format؃Char", true, "HtmlEncode[[Unicode؃Format؃Char]]" },
                    { "UnicodeῼTitlecaseῼChar", false, "UnicodeῼTitlecaseῼChar" },       // class Lt
                    { "UnicodeῼTitlecaseῼChar", true, "HtmlEncode[[UnicodeῼTitlecaseῼChar]]" },
                    { "UnicodeःCombiningःChar", false, "UnicodeःCombiningःChar" },    // class Mc
                    { "UnicodeःCombiningःChar", true, "HtmlEncode[[UnicodeःCombiningःChar]]" },
                };
        }
    }

    [Fact]
    public void ObjectTemplateDisplaysSimplePropertiesOnObjectByDefault()
    {
        var expected =
            "<div class=\"HtmlEncode[[display-label]]\">HtmlEncode[[Property1]]</div>" + Environment.NewLine
          + "<div class=\"HtmlEncode[[display-field]]\">Model = p1, ModelType = System.String, PropertyName = Property1," +
                " SimpleDisplayText = p1</div>" + Environment.NewLine
          + "<div class=\"HtmlEncode[[display-label]]\">HtmlEncode[[Prop2]]</div>" + Environment.NewLine
          + "<div class=\"HtmlEncode[[display-field]]\">Model = (null), ModelType = System.String, PropertyName = Property2," +
                " SimpleDisplayText = (null)</div>" + Environment.NewLine;

        // Arrange
        var model = new DefaultTemplatesUtilities.ObjectTemplateModel { Property1 = "p1", Property2 = null };
        var html = DefaultTemplatesUtilities.GetHtmlHelper(model);

        // Act
        var result = DefaultDisplayTemplates.ObjectTemplate(html);

        // Assert
        Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(result));
    }

    [Fact]
    public void ObjectTemplateDisplaysNullDisplayTextWhenObjectIsNull()
    {
        // Arrange
        var provider = new TestModelMetadataProvider();
        provider.ForType<DefaultTemplatesUtilities.ObjectTemplateModel>().DisplayDetails(dd =>
        {
            dd.NullDisplayText = "(null value)";
        });

        var html = DefaultTemplatesUtilities.GetHtmlHelper(provider: provider);

        // Act
        var result = DefaultDisplayTemplates.ObjectTemplate(html);

        // Assert
        Assert.Equal("(null value)", HtmlContentUtilities.HtmlContentToString(result));
    }

    [Theory]
    [MemberData(nameof(HtmlEncodeData))]
    public void ObjectTemplateDisplaysSimpleDisplayTextWhenTemplateDepthGreaterThanOne(
        string simpleDisplayText,
        bool htmlEncode,
        string expectedResult)
    {
        // Arrange
        var model = new DefaultTemplatesUtilities.ObjectTemplateModel();
        model.Property1 = simpleDisplayText;

        var provider = new TestModelMetadataProvider();
        provider.ForType<DefaultTemplatesUtilities.ObjectTemplateModel>().DisplayDetails(dd =>
        {
            dd.HtmlEncode = htmlEncode;
            dd.SimpleDisplayProperty = "Property1";
        });

        var html = DefaultTemplatesUtilities.GetHtmlHelper(model, provider);

        html.ViewData.TemplateInfo.AddVisited("foo");
        html.ViewData.TemplateInfo.AddVisited("bar");

        // Act
        var result = DefaultDisplayTemplates.ObjectTemplate(html);

        // Assert
        Assert.Equal(expectedResult, HtmlContentUtilities.HtmlContentToString(result));
    }

    [Fact]
    public void ObjectTemplate_IgnoresPropertiesWith_ScaffoldColumnFalse()
    {
        // Arrange
        var expected = "<div class=\"HtmlEncode[[display-label]]\">HtmlEncode[[Property1]]</div>" + Environment.NewLine +
            "<div class=\"HtmlEncode[[display-field]]\"></div>" + Environment.NewLine +
            "<div class=\"HtmlEncode[[display-label]]\">HtmlEncode[[Property3]]</div>" + Environment.NewLine +
            "<div class=\"HtmlEncode[[display-field]]\"></div>" + Environment.NewLine;

        var model = new DefaultTemplatesUtilities.ObjectWithScaffoldColumn();
        var viewEngine = new Mock<ICompositeViewEngine>(MockBehavior.Strict);
        viewEngine
            .Setup(v => v.GetView(/*executingFilePath*/ null, It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound(string.Empty, Enumerable.Empty<string>()));
        viewEngine
            .Setup(v => v.FindView(It.IsAny<ActionContext>(), It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound(string.Empty, Enumerable.Empty<string>()));
        var htmlHelper = DefaultTemplatesUtilities.GetHtmlHelper(model, viewEngine.Object);

        // Act
        var result = DefaultDisplayTemplates.ObjectTemplate(htmlHelper);

        // Assert
        Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(result));
    }

    [Fact]
    public void ObjectTemplate_HonorsHideSurroundingHtml()
    {
        // Arrange
        var expected =
            "Model = p1, ModelType = System.String, PropertyName = Property1, SimpleDisplayText = p1" +
            "<div class=\"HtmlEncode[[display-label]]\">HtmlEncode[[Prop2]]</div>" + Environment.NewLine +
            "<div class=\"HtmlEncode[[display-field]]\">Model = (null), ModelType = System.String, PropertyName = Property2," +
                " SimpleDisplayText = (null)</div>" + Environment.NewLine;

        var model = new DefaultTemplatesUtilities.ObjectTemplateModel { Property1 = "p1", Property2 = null };

        var provider = new TestModelMetadataProvider();
        provider.ForProperty<DefaultTemplatesUtilities.ObjectTemplateModel>("Property1").DisplayDetails(dd =>
        {
            dd.HideSurroundingHtml = true;
        });

        var html = DefaultTemplatesUtilities.GetHtmlHelper(model, provider);

        // Act
        var result = DefaultDisplayTemplates.ObjectTemplate(html);

        // Assert
        Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(result));
    }

    [Fact]
    public void ObjectTemplate_OrdersProperties_AsExpected()
    {
        // Arrange
        var model = new OrderedModel();
        var html = DefaultTemplatesUtilities.GetHtmlHelper(model);
        var expectedProperties = new List<string>
            {
                "OrderedProperty3",
                "OrderedProperty2",
                "OrderedProperty1",
                "Property3",
                "Property1",
                "Property2",
                "LastProperty",
            };

        var stringBuilder = new StringBuilder();
        foreach (var property in expectedProperties)
        {
            var label = string.Format(
                CultureInfo.InvariantCulture,
                "<div class=\"HtmlEncode[[display-label]]\">HtmlEncode[[{0}]]</div>",
                property);
            stringBuilder.AppendLine(label);

            var value = string.Format(
                CultureInfo.InvariantCulture,
                "<div class=\"HtmlEncode[[display-field]]\">Model = (null), ModelType = System.String, PropertyName = {0}, " +
                "SimpleDisplayText = (null)</div>",
                property);
            stringBuilder.AppendLine(value);
        }
        var expected = stringBuilder.ToString();

        // Act
        var result = DefaultDisplayTemplates.ObjectTemplate(html);

        // Assert
        Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(result));
    }

    [Fact]
    public void HiddenInputTemplate_ReturnsValue()
    {
        // Arrange
        var model = "Model string";
        var html = DefaultTemplatesUtilities.GetHtmlHelper(model);
        var templateInfo = html.ViewData.TemplateInfo;
        templateInfo.HtmlFieldPrefix = "FieldPrefix";

        // TemplateBuilder sets FormattedModelValue before calling TemplateRenderer and it's used below.
        templateInfo.FormattedModelValue = "Formatted string";

        // Act
        var result = DefaultDisplayTemplates.HiddenInputTemplate(html);

        // Assert
        Assert.Equal("HtmlEncode[[Formatted string]]", HtmlContentUtilities.HtmlContentToString(result));
    }

    [Fact]
    public void HiddenInputTemplate_HonorsHideSurroundingHtml()
    {
        // Arrange
        var model = "Model string";

        var provider = new TestModelMetadataProvider();
        provider.ForType<string>().DisplayDetails(dd =>
        {
            dd.HideSurroundingHtml = true;
        });

        var html = DefaultTemplatesUtilities.GetHtmlHelper(model, provider: provider);
        var viewData = html.ViewData;

        var templateInfo = viewData.TemplateInfo;
        templateInfo.HtmlFieldPrefix = "FieldPrefix";
        templateInfo.FormattedModelValue = "Formatted string";

        // Act
        var result = DefaultDisplayTemplates.HiddenInputTemplate(html);

        // Assert
        Assert.Empty(HtmlContentUtilities.HtmlContentToString(result));
    }

    [Fact]
    public void Display_FindsViewDataMember()
    {
        // Arrange
        var model = new DefaultTemplatesUtilities.ObjectTemplateModel { Property1 = "Model string" };
        var viewEngine = new Mock<ICompositeViewEngine>(MockBehavior.Strict);
        viewEngine
            .Setup(v => v.GetView(/*executingFilePath*/ null, It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound(string.Empty, Enumerable.Empty<string>()));
        viewEngine
            .Setup(v => v.FindView(It.IsAny<ActionContext>(), It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound(string.Empty, Enumerable.Empty<string>()));
        var helper = DefaultTemplatesUtilities.GetHtmlHelper(model, viewEngine.Object);
        helper.ViewData["Property1"] = "ViewData string";

        // Act
        var result = helper.Display("Property1");

        // Assert
        Assert.Equal("HtmlEncode[[ViewData string]]", HtmlContentUtilities.HtmlContentToString(result));
    }

    [Fact]
    public void DisplayFor_FindsModel()
    {
        // Arrange
        var model = new DefaultTemplatesUtilities.ObjectTemplateModel { Property1 = "Model string" };
        var viewEngine = new Mock<ICompositeViewEngine>(MockBehavior.Strict);
        viewEngine
            .Setup(v => v.GetView(/*executingFilePath*/ null, It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound(string.Empty, Enumerable.Empty<string>()));
        viewEngine
            .Setup(v => v.FindView(It.IsAny<ActionContext>(), It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound(string.Empty, Enumerable.Empty<string>()));
        var helper = DefaultTemplatesUtilities.GetHtmlHelper(model, viewEngine.Object);
        helper.ViewData["Property1"] = "ViewData string";

        // Act
        var result = helper.DisplayFor(m => m.Property1);

        // Assert
        Assert.Equal("HtmlEncode[[Model string]]", HtmlContentUtilities.HtmlContentToString(result));
    }

    [Fact]
    public void Display_FindsModel_IfNoViewDataMember()
    {
        // Arrange
        var model = new DefaultTemplatesUtilities.ObjectTemplateModel { Property1 = "Model string" };
        var viewEngine = new Mock<ICompositeViewEngine>(MockBehavior.Strict);
        viewEngine
            .Setup(v => v.GetView(/*executingFilePath*/ null, It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound(string.Empty, Enumerable.Empty<string>()));
        viewEngine
            .Setup(v => v.FindView(It.IsAny<ActionContext>(), It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound(string.Empty, Enumerable.Empty<string>()));
        var helper = DefaultTemplatesUtilities.GetHtmlHelper(model, viewEngine.Object);

        // Act
        var result = helper.Display("Property1");

        // Assert
        Assert.Equal("HtmlEncode[[Model string]]", HtmlContentUtilities.HtmlContentToString(result));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void DisplayFor_FindsModel_EvenIfNullOrEmpty(string propertyValue)
    {
        // Arrange
        var model = new DefaultTemplatesUtilities.ObjectTemplateModel { Property1 = propertyValue, };
        var viewEngine = new Mock<ICompositeViewEngine>(MockBehavior.Strict);
        viewEngine
            .Setup(v => v.GetView(/*executingFilePath*/ null, It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound(string.Empty, Enumerable.Empty<string>()));
        viewEngine
            .Setup(v => v.FindView(It.IsAny<ActionContext>(), It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound(string.Empty, Enumerable.Empty<string>()));
        var helper = DefaultTemplatesUtilities.GetHtmlHelper(model, viewEngine.Object);
        helper.ViewData["Property1"] = "ViewData string";

        // Act
        var result = helper.DisplayFor(m => m.Property1);

        // Assert
        Assert.Equal(string.Empty, HtmlContentUtilities.HtmlContentToString(result));
    }

    [Fact]
    public void DisplayFor_DoesNotWrapExceptionThrowsDuringViewRendering()
    {
        // Arrange
        var expectedMessage = "my exception message";
        var model = new DefaultTemplatesUtilities.ObjectTemplateModel { Property1 = "Test string", };
        var view = new Mock<IView>();
        view.Setup(v => v.RenderAsync(It.IsAny<ViewContext>()))
            .Returns(Task.Run(() =>
            {
                throw new ArgumentException(expectedMessage);
            }));
        var viewEngine = new Mock<ICompositeViewEngine>(MockBehavior.Strict);
        viewEngine
            .Setup(v => v.GetView(/*executingFilePath*/ null, It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound(string.Empty, Enumerable.Empty<string>()));
        viewEngine
            .Setup(v => v.FindView(It.IsAny<ActionContext>(), It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.Found("test-view", view.Object));
        var helper = DefaultTemplatesUtilities.GetHtmlHelper(model, viewEngine.Object);
        helper.ViewData["Property1"] = "ViewData string";

        // Act and Assert
        var ex = Assert.Throws<ArgumentException>(() => helper.DisplayFor(m => m.Property1));
        Assert.Equal(expectedMessage, ex.Message);
    }

    [Fact]
    public void Display_CallsFindView_WithExpectedPath()
    {
        // Arrange
        var viewEngine = new Mock<ICompositeViewEngine>(MockBehavior.Strict);
        viewEngine
            .Setup(v => v.GetView(/*executingFilePath*/ null, It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound(string.Empty, Enumerable.Empty<string>()));
        viewEngine
            .Setup(v => v.FindView(It.IsAny<ActionContext>(), "DisplayTemplates/String", /*isMainPage*/ false))
            .Returns(ViewEngineResult.Found(string.Empty, new Mock<IView>().Object))
            .Verifiable();
        var html = DefaultTemplatesUtilities.GetHtmlHelper(new object(), viewEngine: viewEngine.Object);

        // Act & Assert
        html.Display(expression: string.Empty, templateName: null, htmlFieldName: null, additionalViewData: null);
        viewEngine.Verify();
    }

    private class OrderedModel
    {
        [Display(Order = 10001)]
        public string LastProperty { get; set; }

        public string Property3 { get; set; }
        public string Property1 { get; set; }
        public string Property2 { get; set; }

        [Display(Order = 23)]
        public string OrderedProperty3 { get; set; }
        [Display(Order = 23)]
        public string OrderedProperty2 { get; set; }
        [Display(Order = 23)]
        public string OrderedProperty1 { get; set; }
    }
}
