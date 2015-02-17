// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Rendering;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Core
{
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
                    { "Simple Display Text", true, "Simple Display Text" },
                    { "<blink>text</blink>", false, "<blink>text</blink>" },
                    { "<blink>text</blink>", true, "&lt;blink&gt;text&lt;/blink&gt;" },
                    { "&'\"", false, "&'\"" },
                    { "&'\"", true, "&amp;&#x27;&quot;" },
                    { " ¡ÿĀ", false, " ¡ÿĀ" },                                           // high ASCII
                    { " ¡ÿĀ", true, "&#xA0;&#xA1;&#xFF;&#x100;" },
                    { "Chinese西雅图Chars", false, "Chinese西雅图Chars" },
                    { "Chinese西雅图Chars", true, "Chinese&#x897F;&#x96C5;&#x56FE;Chars" },
                    { "Unicode؃Format؃Char", false, "Unicode؃Format؃Char" },            // class Cf
                    { "Unicode؃Format؃Char", true, "Unicode&#x603;Format&#x603;Char" },
                    { "UnicodeῼTitlecaseῼChar", false, "UnicodeῼTitlecaseῼChar" },       // class Lt
                    { "UnicodeῼTitlecaseῼChar", true, "Unicode&#x1FFC;Titlecase&#x1FFC;Char" },
                    { "UnicodeःCombiningःChar", false, "UnicodeःCombiningःChar" },    // class Mc
                    { "UnicodeःCombiningःChar", true, "Unicode&#x903;Combining&#x903;Char" },
                };
            }
        }

        [Fact]
        public void ObjectTemplateDisplaysSimplePropertiesOnObjectByDefault()
        {
            var expected =
                "<div class=\"display-label\">Property1</div>" + Environment.NewLine
              + "<div class=\"display-field\">Model = p1, ModelType = System.String, PropertyName = Property1," +
                    " SimpleDisplayText = p1</div>" + Environment.NewLine
              + "<div class=\"display-label\">Property2</div>" + Environment.NewLine
              + "<div class=\"display-field\">Model = (null), ModelType = System.String, PropertyName = Property2," +
                    " SimpleDisplayText = (null)</div>" + Environment.NewLine;

            // Arrange
            var model = new DefaultTemplatesUtilities.ObjectTemplateModel { Property1 = "p1", Property2 = null };
            var html = DefaultTemplatesUtilities.GetHtmlHelper(model);

            // Act
            var result = DefaultDisplayTemplates.ObjectTemplate(html);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ObjectTemplateDisplaysNullDisplayTextWhenObjectIsNull()
        {
            // Arrange
            var html = DefaultTemplatesUtilities.GetHtmlHelper();

            html.ViewData.ModelMetadata.NullDisplayText = "(null value)";

            // Act
            var result = DefaultDisplayTemplates.ObjectTemplate(html);

            // Assert
            Assert.Equal("(null value)", result);
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

            var html = DefaultTemplatesUtilities.GetHtmlHelper(model);

            html.ViewData.ModelMetadata.HtmlEncode = htmlEncode;
            html.ViewData.ModelMetadata.SimpleDisplayProperty = "Property1";

            html.ViewData.TemplateInfo.AddVisited("foo");
            html.ViewData.TemplateInfo.AddVisited("bar");

            // Act
            var result = DefaultDisplayTemplates.ObjectTemplate(html);

            // Assert
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public void ObjectTemplate_IgnoresPropertiesWith_ScaffoldColumnFalse()
        {
            // Arrange
            var expected =
@"<div class=""display-label"">Property1</div>
<div class=""display-field""></div>
<div class=""display-label"">Property3</div>
<div class=""display-field""></div>
";
            var model = new DefaultTemplatesUtilities.ObjectWithScaffoldColumn();
            var viewEngine = new Mock<ICompositeViewEngine>();
            viewEngine.Setup(v => v.FindPartialView(It.IsAny<ActionContext>(), It.IsAny<string>()))
                      .Returns(ViewEngineResult.NotFound("", Enumerable.Empty<string>()));
            var htmlHelper = DefaultTemplatesUtilities.GetHtmlHelper(model, viewEngine.Object);

            // Act
            var result = DefaultDisplayTemplates.ObjectTemplate(htmlHelper);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ObjectTemplate_HonoursHideSurroundingHtml()
        {
            // Arrange
            var expected =
                "Model = p1, ModelType = System.String, PropertyName = Property1, SimpleDisplayText = p1" +
                "<div class=\"display-label\">Property2</div>" + Environment.NewLine +
                "<div class=\"display-field\">Model = (null), ModelType = System.String, PropertyName = Property2," +
                    " SimpleDisplayText = (null)</div>" + Environment.NewLine;

            var model = new DefaultTemplatesUtilities.ObjectTemplateModel { Property1 = "p1", Property2 = null };
            var html = DefaultTemplatesUtilities.GetHtmlHelper(model);

            var metadata = html.ViewData.ModelMetadata.Properties["Property1"];
            metadata.HideSurroundingHtml = true;

            // Act
            var result = DefaultDisplayTemplates.ObjectTemplate(html);

            // Assert
            Assert.Equal(expected, result);
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
                    "<div class=\"display-label\">{0}</div>",
                    property);
                stringBuilder.AppendLine(label);

                var value = string.Format(
                    CultureInfo.InvariantCulture,
                    "<div class=\"display-field\">Model = (null), ModelType = System.String, PropertyName = {0}, " +
                    "SimpleDisplayText = (null)</div>",
                    property);
                stringBuilder.AppendLine(value);
            }
            var expected = stringBuilder.ToString();

            // Act
            var result = DefaultDisplayTemplates.ObjectTemplate(html);

            // Assert
            Assert.Equal(expected, result);
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
            Assert.Equal("Formatted string", result);
        }

        [Fact]
        public void HiddenInputTemplate_HonoursHideSurroundingHtml()
        {
            // Arrange
            var model = "Model string";
            var html = DefaultTemplatesUtilities.GetHtmlHelper(model);
            var viewData = html.ViewData;
            viewData.ModelMetadata.HideSurroundingHtml = true;

            var templateInfo = viewData.TemplateInfo;
            templateInfo.HtmlFieldPrefix = "FieldPrefix";
            templateInfo.FormattedModelValue = "Formatted string";

            // Act
            var result = DefaultDisplayTemplates.HiddenInputTemplate(html);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void Display_FindsViewDataMember()
        {
            // Arrange
            var model = new DefaultTemplatesUtilities.ObjectTemplateModel { Property1 = "Model string" };
            var viewEngine = new Mock<ICompositeViewEngine>();
            viewEngine
                .Setup(v => v.FindPartialView(It.IsAny<ActionContext>(), It.IsAny<string>()))
                .Returns(ViewEngineResult.NotFound("", Enumerable.Empty<string>()));
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(model, viewEngine.Object);
            helper.ViewData["Property1"] = "ViewData string";

            // Act
            var result = helper.Display("Property1");

            // Assert
            Assert.Equal("ViewData string", result.ToString());
        }

        [Fact]
        public void DisplayFor_FindsModel()
        {
            // Arrange
            var model = new DefaultTemplatesUtilities.ObjectTemplateModel { Property1 = "Model string" };
            var viewEngine = new Mock<ICompositeViewEngine>();
            viewEngine
                .Setup(v => v.FindPartialView(It.IsAny<ActionContext>(), It.IsAny<string>()))
                .Returns(ViewEngineResult.NotFound("", Enumerable.Empty<string>()));
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(model, viewEngine.Object);
            helper.ViewData["Property1"] = "ViewData string";

            // Act
            var result = helper.DisplayFor(m => m.Property1);

            // Assert
            Assert.Equal("Model string", result.ToString());
        }

        [Fact]
        public void Display_FindsModel_IfNoViewDataMember()
        {
            // Arrange
            var model = new DefaultTemplatesUtilities.ObjectTemplateModel { Property1 = "Model string" };
            var viewEngine = new Mock<ICompositeViewEngine>();
            viewEngine
                .Setup(v => v.FindPartialView(It.IsAny<ActionContext>(), It.IsAny<string>()))
                .Returns(ViewEngineResult.NotFound("", Enumerable.Empty<string>()));
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(model, viewEngine.Object);

            // Act
            var result = helper.Display("Property1");

            // Assert
            Assert.Equal("Model string", result.ToString());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void DisplayFor_FindsModel_EvenIfNullOrEmpty(string propertyValue)
        {
            // Arrange
            var model = new DefaultTemplatesUtilities.ObjectTemplateModel { Property1 = propertyValue, };
            var viewEngine = new Mock<ICompositeViewEngine>();
            viewEngine
                .Setup(v => v.FindPartialView(It.IsAny<ActionContext>(), It.IsAny<string>()))
                .Returns(ViewEngineResult.NotFound("", Enumerable.Empty<string>()));
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(model, viewEngine.Object);
            helper.ViewData["Property1"] = "ViewData string";

            // Act
            var result = helper.DisplayFor(m => m.Property1);

            // Assert
            Assert.Empty(result.ToString());
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
            var viewEngine = new Mock<ICompositeViewEngine>();
            viewEngine
                .Setup(v => v.FindPartialView(It.IsAny<ActionContext>(), It.IsAny<string>()))
                .Returns(ViewEngineResult.Found("test-view", view.Object));
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(model, viewEngine.Object);
            helper.ViewData["Property1"] = "ViewData string";

            // Act and Assert
            var ex = Assert.Throws<ArgumentException>(() => helper.DisplayFor(m => m.Property1));
            Assert.Equal(expectedMessage, ex.Message);
        }

        [Fact]
        public void Display_CallsFindPartialView_WithExpectedPath()
        {
            // Arrange
            var viewEngine = new Mock<ICompositeViewEngine>(MockBehavior.Strict);

            viewEngine
                .Setup(v => v.FindPartialView(It.IsAny<ActionContext>(), 
                                              It.Is<string>(view => view.Equals("DisplayTemplates/String"))))
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
}