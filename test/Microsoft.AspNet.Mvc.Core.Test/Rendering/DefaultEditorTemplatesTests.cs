// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Core
{
    public class DefaultEditorTemplatesTests
    {
        [Fact]
        public void ObjectTemplateEditsSimplePropertiesOnObjectByDefault()
        {
            var expected =
                "<div class=\"editor-label\"><label for=\"Property1\">Property1</label></div>" + Environment.NewLine
              + "<div class=\"editor-field\">Model = p1, ModelType = System.String, PropertyName = Property1," +
                    " SimpleDisplayText = p1 " +
                    "<span class=\"field-validation-valid\" data-valmsg-for=\"Property1\" data-valmsg-replace=\"true\">" +
                    "</span></div>" + Environment.NewLine
              + "<div class=\"editor-label\"><label for=\"Property2\">Property2</label></div>" + Environment.NewLine
              + "<div class=\"editor-field\">Model = (null), ModelType = System.String, PropertyName = Property2," +
                    " SimpleDisplayText = (null) " +
                    "<span class=\"field-validation-valid\" data-valmsg-for=\"Property2\" data-valmsg-replace=\"true\">" +
                    "</span></div>" + Environment.NewLine;

            // Arrange
            var model = new DefaultTemplatesUtilities.ObjectTemplateModel { Property1 = "p1", Property2 = null };
            var html = DefaultTemplatesUtilities.GetHtmlHelper(model);

            // Act
            var result = DefaultEditorTemplates.ObjectTemplate(html);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ObjectTemplateDisplaysNullDisplayTextWithNullModelAndTemplateDepthGreaterThanOne()
        {
            // Arrange
            var html = DefaultTemplatesUtilities.GetHtmlHelper();
            var metadata =
                new EmptyModelMetadataProvider()
                    .GetMetadataForType(null, typeof(DefaultTemplatesUtilities.ObjectTemplateModel));
            metadata.NullDisplayText = "Null Display Text";
            metadata.SimpleDisplayText = "Simple Display Text";
            html.ViewData.ModelMetadata = metadata;
            html.ViewData.TemplateInfo.AddVisited("foo");
            html.ViewData.TemplateInfo.AddVisited("bar");

            // Act
            var result = DefaultEditorTemplates.ObjectTemplate(html);

            // Assert
            Assert.Equal(metadata.NullDisplayText, result);
        }

        [Fact]
        public void ObjectTemplateDisplaysSimpleDisplayTextWithNonNullModelTemplateDepthGreaterThanOne()
        {
            // Arrange
            var model = new DefaultTemplatesUtilities.ObjectTemplateModel();
            var html = DefaultTemplatesUtilities.GetHtmlHelper(model);
            var metadata =
                new EmptyModelMetadataProvider()
                    .GetMetadataForType(() => model, typeof(DefaultTemplatesUtilities.ObjectTemplateModel));
            html.ViewData.ModelMetadata = metadata;
            metadata.NullDisplayText = "Null Display Text";
            metadata.SimpleDisplayText = "Simple Display Text";
            html.ViewData.TemplateInfo.AddVisited("foo");
            html.ViewData.TemplateInfo.AddVisited("bar");

            // Act
            var result = DefaultEditorTemplates.ObjectTemplate(html);

            // Assert
            Assert.Equal(metadata.SimpleDisplayText, result);
        }

        [Fact]
        public void ObjectTemplate_IgnoresPropertiesWith_ScaffoldColumnFalse()
        {
            // Arrange
            var expected =
@"<div class=""editor-label""><label for=""Property1"">Property1</label></div>" +
Environment.NewLine +
@"<div class=""editor-field""><input class=""text-box single-line"" id=""Property1"" name=""Property1"" type=""text"" value="""" /> " +
@"<span class=""field-validation-valid"" data-valmsg-for=""Property1"" data-valmsg-replace=""true""></span></div>" +
Environment.NewLine +
@"<div class=""editor-label""><label for=""Property3"">Property3</label></div>" +
Environment.NewLine +
@"<div class=""editor-field""><input class=""text-box single-line"" id=""Property3"" name=""Property3"" type=""text"" value="""" /> " +
@"<span class=""field-validation-valid"" data-valmsg-for=""Property3"" data-valmsg-replace=""true""></span></div>" +
Environment.NewLine;

            var model = new DefaultTemplatesUtilities.ObjectWithScaffoldColumn();
            var viewEngine = new Mock<ICompositeViewEngine>();
            viewEngine.Setup(v => v.FindPartialView(It.IsAny<ActionContext>(), It.IsAny<string>()))
                      .Returns(ViewEngineResult.NotFound("", Enumerable.Empty<string>()));
            var htmlHelper = DefaultTemplatesUtilities.GetHtmlHelper(model, viewEngine.Object);

            // Act
            var result = DefaultEditorTemplates.ObjectTemplate(htmlHelper);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ObjectTemplate_HonoursHideSurroundingHtml()
        {
            // Arrange
            var expected =
                "Model = p1, ModelType = System.String, PropertyName = Property1, SimpleDisplayText = p1" +
                "<div class=\"editor-label\"><label for=\"Property2\">Property2</label></div>" +
                Environment.NewLine +
                "<div class=\"editor-field\">" +
                    "Model = (null), ModelType = System.String, PropertyName = Property2, SimpleDisplayText = (null) " +
                    "<span class=\"field-validation-valid\" data-valmsg-for=\"Property2\" data-valmsg-replace=\"true\">" +
                    "</span></div>" +
                Environment.NewLine;

            var model = new DefaultTemplatesUtilities.ObjectTemplateModel { Property1 = "p1", Property2 = null };
            var html = DefaultTemplatesUtilities.GetHtmlHelper(model);
            var metadata =
                html.ViewData.ModelMetadata.Properties.First(m => string.Equals(m.PropertyName, "Property1"));
            metadata.HideSurroundingHtml = true;

            // Act
            var result = DefaultEditorTemplates.ObjectTemplate(html);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void HiddenInputTemplate_ReturnsValueAndHiddenInput()
        {
            // Arrange
            var expected =
                "Formatted string<input id=\"FieldPrefix\" name=\"FieldPrefix\" type=\"hidden\" value=\"Model string\" />";

            var model = "Model string";
            var html = DefaultTemplatesUtilities.GetHtmlHelper(model);
            var templateInfo = html.ViewData.TemplateInfo;
            templateInfo.HtmlFieldPrefix = "FieldPrefix";

            // TemplateBuilder sets FormattedModelValue before calling TemplateRenderer and it's used below.
            templateInfo.FormattedModelValue = "Formatted string";

            // Act
            var result = DefaultEditorTemplates.HiddenInputTemplate(html);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void HiddenInputTemplate_HonoursHideSurroundingHtml()
        {
            // Arrange
            var expected = "<input id=\"FieldPrefix\" name=\"FieldPrefix\" type=\"hidden\" value=\"Model string\" />";

            var model = "Model string";
            var html = DefaultTemplatesUtilities.GetHtmlHelper(model);
            var viewData = html.ViewData;
            viewData.ModelMetadata.HideSurroundingHtml = true;

            var templateInfo = viewData.TemplateInfo;
            templateInfo.HtmlFieldPrefix = "FieldPrefix";
            templateInfo.FormattedModelValue = "Formatted string";

            // Act
            var result = DefaultEditorTemplates.HiddenInputTemplate(html);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Editor_FindsViewDataMember()
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
            var result = helper.Editor("Property1");

            // Assert
            Assert.Equal(
                "<input class=\"text-box single-line\" id=\"Property1\" name=\"Property1\" type=\"text\" value=\"ViewData string\" />",
                result.ToString());
        }

        // DateTime-local is not special-cased unless using Html5DateRenderingMode.Rfc3339.
        [Theory]
        [InlineData("date", "{0:d}", "02/01/2000")]
        [InlineData("datetime", null, "02/01/2000 03:04:05 +00:00")]
        [InlineData("datetime-local", null, "02/01/2000 03:04:05 +00:00")]
        [InlineData("time", "{0:t}", "03:04")]
        [ReplaceCulture]
        public void Editor_FindsCorrectDateOrTimeTemplate(string dataTypeName, string editFormatString, string expected)
        {
            // Arrange
            var expectedInput = "<input class=\"text-box single-line\" id=\"FieldPrefix\" name=\"FieldPrefix\" type=\"" +
                dataTypeName + "\" value=\"" + expected + "\" />";
            var offset = TimeSpan.FromHours(0);
            var model = new DateTimeOffset(
                year: 2000,
                month: 1,
                day: 2,
                hour: 3,
                minute: 4,
                second: 5,
                millisecond: 6,
                offset: offset);
            var viewEngine = new Mock<ICompositeViewEngine>();
            viewEngine
                .Setup(v => v.FindPartialView(It.IsAny<ActionContext>(), It.IsAny<string>()))
                .Returns(ViewEngineResult.NotFound("", Enumerable.Empty<string>()));
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(model, viewEngine.Object);
            helper.ViewData.ModelMetadata.DataTypeName = dataTypeName;
            helper.ViewData.ModelMetadata.EditFormatString = editFormatString; // What [DataType] does for given type.
            helper.ViewData.TemplateInfo.HtmlFieldPrefix = "FieldPrefix";

            // Act
            var result = helper.Editor("");

            // Assert
            Assert.Equal(expectedInput, result.ToString());
        }

        [Theory]
        [InlineData("date", "{0:d}", "2000-01-02")]
        [InlineData("datetime", null, "2000-01-02T03:04:05.060+00:00")]
        [InlineData("datetime-local", null, "2000-01-02T03:04:05.060")]
        [InlineData("time", "{0:t}", "03:04:05.060")]
        [ReplaceCulture]
        public void Editor_AppliesRfc3339(string dataTypeName, string editFormatString, string expected)
        {
            // Arrange
            var expectedInput = "<input class=\"text-box single-line\" id=\"FieldPrefix\" name=\"FieldPrefix\" type=\"" +
                dataTypeName + "\" value=\"" + expected + "\" />";

            // Place DateTime-local value in current timezone.
            var offset = string.Equals("", dataTypeName) ? DateTimeOffset.Now.Offset : TimeSpan.FromHours(0);
            var model = new DateTimeOffset(
                year: 2000,
                month: 1,
                day: 2,
                hour: 3,
                minute: 4,
                second: 5,
                millisecond: 60,
                offset: offset);
            var viewEngine = new Mock<ICompositeViewEngine>();
            viewEngine
                .Setup(v => v.FindPartialView(It.IsAny<ActionContext>(), It.IsAny<string>()))
                .Returns(ViewEngineResult.NotFound("", Enumerable.Empty<string>()));
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(model, viewEngine.Object);
            helper.Html5DateRenderingMode = Html5DateRenderingMode.Rfc3339;
            helper.ViewData.ModelMetadata.DataTypeName = dataTypeName;
            helper.ViewData.ModelMetadata.EditFormatString = editFormatString; // What [DataType] does for given type.
            helper.ViewData.TemplateInfo.HtmlFieldPrefix = "FieldPrefix";

            // Act
            var result = helper.Editor("");

            // Assert
            Assert.Equal(expectedInput, result.ToString());
        }

        [Theory]
        [InlineData("date", Html5DateRenderingMode.CurrentCulture)]
        [InlineData("date", Html5DateRenderingMode.Rfc3339)]
        [InlineData("datetime", Html5DateRenderingMode.CurrentCulture)]
        [InlineData("datetime", Html5DateRenderingMode.Rfc3339)]
        [InlineData("datetime-local", Html5DateRenderingMode.CurrentCulture)]
        [InlineData("datetime-local", Html5DateRenderingMode.Rfc3339)]
        [InlineData("time", Html5DateRenderingMode.CurrentCulture)]
        [InlineData("time", Html5DateRenderingMode.Rfc3339)]
        public void Editor_AppliesNonDefaultEditFormat(string dataTypeName, Html5DateRenderingMode renderingMode)
        {
            // Arrange
            var expectedInput = "<input class=\"text-box single-line\" id=\"FieldPrefix\" name=\"FieldPrefix\" type=\"" +
                dataTypeName + "\" value=\"Formatted as 2000-01-02T03:04:05.0600000+00:00\" />";
            var offset = TimeSpan.FromHours(0);
            var model = new DateTimeOffset(
                year: 2000,
                month: 1,
                day: 2,
                hour: 3,
                minute: 4,
                second: 5,
                millisecond: 60,
                offset: offset);
            var viewEngine = new Mock<ICompositeViewEngine>();
            viewEngine
                .Setup(v => v.FindPartialView(It.IsAny<ActionContext>(), It.IsAny<string>()))
                .Returns(ViewEngineResult.NotFound("", Enumerable.Empty<string>()));
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(model, viewEngine.Object);
            helper.Html5DateRenderingMode = renderingMode; // Ignored due to HasNonDefaultEditFormat.
            helper.ViewData.ModelMetadata.DataTypeName = dataTypeName;
            helper.ViewData.ModelMetadata.EditFormatString = "Formatted as {0:O}";
            helper.ViewData.ModelMetadata.HasNonDefaultEditFormat = true;
            helper.ViewData.TemplateInfo.HtmlFieldPrefix = "FieldPrefix";

            // Act
            var result = helper.Editor("");

            // Assert
            Assert.Equal(expectedInput, result.ToString());
        }

        [Fact]
        public void EditorFor_FindsModel()
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
            var result = helper.EditorFor(m => m.Property1);

            // Assert
            Assert.Equal(
                "<input class=\"text-box single-line\" id=\"Property1\" name=\"Property1\" type=\"text\" value=\"Model string\" />",
                result.ToString());
        }

        [Fact]
        public void Editor_FindsModel_IfNoViewDataMember()
        {
            // Arrange
            var model = new DefaultTemplatesUtilities.ObjectTemplateModel { Property1 = "Model string" };
            var viewEngine = new Mock<ICompositeViewEngine>();
            viewEngine
                .Setup(v => v.FindPartialView(It.IsAny<ActionContext>(), It.IsAny<string>()))
                .Returns(ViewEngineResult.NotFound("", Enumerable.Empty<string>()));
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(model, viewEngine.Object);

            // Act
            var result = helper.Editor("Property1");

            // Assert
            Assert.Equal(
                "<input class=\"text-box single-line\" id=\"Property1\" name=\"Property1\" type=\"text\" value=\"Model string\" />",
                result.ToString());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void EditorFor_FindsModel_EvenIfNullOrEmpty(string propertyValue)
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
            var result = helper.EditorFor(m => m.Property1);

            // Assert
            Assert.Equal(
                "<input class=\"text-box single-line\" id=\"Property1\" name=\"Property1\" type=\"text\" value=\"\" />",
                result.ToString());
        }
    }
}