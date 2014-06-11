// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Rendering;
using Xunit;

namespace Microsoft.AspNet.Mvc.Core.Test
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
            var html = DefaultTemplatesUtilities.GetHtmlHelper(null);
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
    }
}