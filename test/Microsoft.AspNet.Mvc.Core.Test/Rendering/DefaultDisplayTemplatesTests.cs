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
    public class DefaultDisplayTemplateTests
    {
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
            var metadata =
                new EmptyModelMetadataProvider()
                    .GetMetadataForType(null, typeof(DefaultTemplatesUtilities.ObjectTemplateModel));
            metadata.NullDisplayText = "(null value)";
            html.ViewData.ModelMetadata = metadata;

            // Act
            var result = DefaultDisplayTemplates.ObjectTemplate(html);

            // Assert
            Assert.Equal(metadata.NullDisplayText, result);
        }

        [Fact]
        public void ObjectTemplateDisplaysSimpleDisplayTextWhenTemplateDepthGreaterThanOne()
        {
            // Arrange
            var model = new DefaultTemplatesUtilities.ObjectTemplateModel();
            var html = DefaultTemplatesUtilities.GetHtmlHelper(model);
            var metadata =
                new EmptyModelMetadataProvider()
                    .GetMetadataForType(() => model, typeof(DefaultTemplatesUtilities.ObjectTemplateModel));
            metadata.SimpleDisplayText = "Simple Display Text";
            html.ViewData.ModelMetadata = metadata;
            html.ViewData.TemplateInfo.AddVisited("foo");
            html.ViewData.TemplateInfo.AddVisited("bar");

            // Act
            var result = DefaultDisplayTemplates.ObjectTemplate(html);

            // Assert
            Assert.Equal(metadata.SimpleDisplayText, result);
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
            viewEngine.Setup(v => v.FindPartialView(It.IsAny<IDictionary<string, object>>(), It.IsAny<string>()))
                      .Returns(ViewEngineResult.NotFound("", Enumerable.Empty<string>()));
            var htmlHelper = DefaultTemplatesUtilities.GetHtmlHelper(model, viewEngine.Object);

            // Act
            var result = DefaultDisplayTemplates.ObjectTemplate(htmlHelper);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Display_FindsViewDataMember()
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
                .Setup(v => v.FindPartialView(It.IsAny<IDictionary<string, object>>(), It.IsAny<string>()))
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
                .Setup(v => v.FindPartialView(It.IsAny<IDictionary<string, object>>(), It.IsAny<string>()))
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
                .Setup(v => v.FindPartialView(It.IsAny<IDictionary<string, object>>(), It.IsAny<string>()))
                .Returns(ViewEngineResult.NotFound("", Enumerable.Empty<string>()));
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(model, viewEngine.Object);
            helper.ViewData["Property1"] = "ViewData string";

            // Act
            var result = helper.DisplayFor(m => m.Property1);

            // Assert
            Assert.Empty(result.ToString());
        }
    }
}