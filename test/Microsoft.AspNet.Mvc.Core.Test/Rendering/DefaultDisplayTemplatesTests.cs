// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Rendering;
using Xunit;

namespace Microsoft.AspNet.Mvc.Core.Test
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
            var html = DefaultTemplatesUtilities.GetHtmlHelper(null);
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
    }
}