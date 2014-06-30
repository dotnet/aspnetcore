// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Rendering;
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
                    " SimpleDisplayText = p1 </div>" + Environment.NewLine
              + "<div class=\"editor-label\"><label for=\"Property2\">Property2</label></div>" + Environment.NewLine
              + "<div class=\"editor-field\">Model = (null), ModelType = System.String, PropertyName = Property2," +
                    " SimpleDisplayText = (null) </div>" + Environment.NewLine;

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
@"<div class=""editor-field""><input class=""text-box single-line"" id=""Property1"" name=""Property1"" type=""text"" value="""" /> </div>" +
Environment.NewLine +
@"<div class=""editor-label""><label for=""Property3"">Property3</label></div>" +
Environment.NewLine +
@"<div class=""editor-field""><input class=""text-box single-line"" id=""Property3"" name=""Property3"" type=""text"" value="""" /> </div>" +
Environment.NewLine;

            var model = new DefaultTemplatesUtilities.ObjectWithScaffoldColumn();
            var viewEngine = new Mock<ICompositeViewEngine>();
            viewEngine.Setup(v => v.FindPartialView(It.IsAny<IDictionary<string, object>>(), It.IsAny<string>()))
                      .Returns(ViewEngineResult.NotFound("", Enumerable.Empty<string>()));
            var htmlHelper = DefaultTemplatesUtilities.GetHtmlHelper(model, viewEngine.Object);

            // Act
            var result = DefaultEditorTemplates.ObjectTemplate(htmlHelper);

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
                .Setup(v => v.FindPartialView(It.IsAny<IDictionary<string, object>>(), It.IsAny<string>()))
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

        [Fact]
        public void EditorFor_FindsModel()
        {
            // Arrange
            var model = new DefaultTemplatesUtilities.ObjectTemplateModel { Property1 = "Model string" };
            var viewEngine = new Mock<ICompositeViewEngine>();
            viewEngine
                .Setup(v => v.FindPartialView(It.IsAny<IDictionary<string, object>>(), It.IsAny<string>()))
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
                .Setup(v => v.FindPartialView(It.IsAny<IDictionary<string, object>>(), It.IsAny<string>()))
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
                .Setup(v => v.FindPartialView(It.IsAny<IDictionary<string, object>>(), It.IsAny<string>()))
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