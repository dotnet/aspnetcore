// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.TestCommon;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Core
{
    /// <summary>
    /// Test the TextBox extensions in <see cref="HtmlHelperInputExtensions" /> class.
    /// </summary>
    public class HtmlHelperTextBoxExtensionsTest
    {
        [Fact]
        public void TextBox_UsesSpecifiedExpression()
        {
            // Arrange
            var metadataProvider = new EmptyModelMetadataProvider();
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(new ViewDataDictionary<TestModel>(metadataProvider));
            helper.ViewContext.ClientValidationEnabled = false;
            helper.ViewData.Model = new TestModel { Property1 = "propValue" };

            // Act
            var textBoxResult = helper.TextBox("Property1");

            // Assert
            Assert.Equal(
                "<input id=\"HtmlEncode[[Property1]]\" name=\"HtmlEncode[[Property1]]\" type=\"HtmlEncode[[text]]\" value=\"HtmlEncode[[propValue]]\" />",
                HtmlContentUtilities.HtmlContentToString(textBoxResult));
        }

        [Fact]
        public void TextBoxFor_UsesSpecifiedExpression()
        {
            // Arrange
            var metadataProvider = new EmptyModelMetadataProvider();
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(new ViewDataDictionary<TestModel>(metadataProvider));
            helper.ViewContext.ClientValidationEnabled = false;
            helper.ViewData.Model = new TestModel { Property1 = "propValue" };

            // Act
            var textBoxForResult = helper.TextBoxFor(m => m.Property1);

            // Assert
            Assert.Equal(
                "<input id=\"HtmlEncode[[Property1]]\" name=\"HtmlEncode[[Property1]]\" type=\"HtmlEncode[[text]]\" value=\"HtmlEncode[[propValue]]\" />",
                HtmlContentUtilities.HtmlContentToString(textBoxForResult));
        }

        [Fact]
        public void TextBox_UsesSpecifiedValue()
        {
            // Arrange
            var metadataProvider = new EmptyModelMetadataProvider();
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(new ViewDataDictionary<TestModel>(metadataProvider));
            helper.ViewContext.ClientValidationEnabled = false;
            helper.ViewData.Model = new TestModel { Property1 = "propValue" };

            // Act
            var textBoxResult = helper.TextBox("Property1", value: "myvalue");

            // Assert
            Assert.Equal(
                "<input id=\"HtmlEncode[[Property1]]\" name=\"HtmlEncode[[Property1]]\" type=\"HtmlEncode[[text]]\" value=\"HtmlEncode[[myvalue]]\" />",
                HtmlContentUtilities.HtmlContentToString(textBoxResult));
        }

        [Fact]
        public void TextBox_UsesSpecifiedFormat()
        {
            // Arrange
            var metadataProvider = new EmptyModelMetadataProvider();
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(new ViewDataDictionary<TestModel>(metadataProvider));
            helper.ViewContext.ClientValidationEnabled = false;
            helper.ViewData.Model = new TestModel { Property1 = "propValue" };

            // Act
            var textBoxResult = helper.TextBox("Property1", value: null, format: "prefix: {0}");

            // Assert
            Assert.Equal(
                "<input id=\"HtmlEncode[[Property1]]\" name=\"HtmlEncode[[Property1]]\" type=\"HtmlEncode[[text]]\" value=\"HtmlEncode[[prefix: propValue]]\" />",
                HtmlContentUtilities.HtmlContentToString(textBoxResult));
        }

        [Fact]
        public void TextBox_UsesSpecifiedFormatOverridesPropertyValue()
        {
            // Arrange
            var metadataProvider = new EmptyModelMetadataProvider();
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(new ViewDataDictionary<TestModel>(metadataProvider));
            helper.ViewContext.ClientValidationEnabled = false;
            helper.ViewData.Model = new TestModel { Property1 = "propValue" };

            // Act
            var textBoxResult = helper.TextBox("Property1", value: "myvalue", format: "prefix: {0}");

            // Assert
            Assert.Equal(
                "<input id=\"HtmlEncode[[Property1]]\" name=\"HtmlEncode[[Property1]]\" type=\"HtmlEncode[[text]]\" value=\"HtmlEncode[[prefix: myvalue]]\" />",
                HtmlContentUtilities.HtmlContentToString(textBoxResult));
        }

        [Fact]
        public void TextBox_UsesSpecifiedHtmlAttributes()
        {
            // Arrange
            var metadataProvider = new EmptyModelMetadataProvider();
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(new ViewDataDictionary<TestModel>(metadataProvider));
            helper.ViewContext.ClientValidationEnabled = false;
            helper.ViewData.Model = new TestModel { Property1 = "propValue" };

            // Act
            var textBoxResult = helper.TextBox("Property1", value: "myvalue", htmlAttributes: new { attr = "value" });

            // Assert
            Assert.Equal(
                "<input attr=\"HtmlEncode[[value]]\" id=\"HtmlEncode[[Property1]]\" name=\"HtmlEncode[[Property1]]\" type=\"HtmlEncode[[text]]\" value=\"HtmlEncode[[myvalue]]\" />",
                HtmlContentUtilities.HtmlContentToString(textBoxResult));
        }

        [Fact]
        public void TextBoxFor_UsesSpecifiedHtmlAttributes()
        {
            // Arrange
            var metadataProvider = new EmptyModelMetadataProvider();
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(new ViewDataDictionary<TestModel>(metadataProvider));
            helper.ViewContext.ClientValidationEnabled = false;
            helper.ViewData.Model = new TestModel { Property1 = "propValue" };

            // Act
            var textBoxForResult = helper.TextBoxFor(m => m.Property1, htmlAttributes: new { attr = "value" });

            // Assert
            Assert.Equal(
                "<input attr=\"HtmlEncode[[value]]\" id=\"HtmlEncode[[Property1]]\" name=\"HtmlEncode[[Property1]]\" type=\"HtmlEncode[[text]]\" value=\"HtmlEncode[[propValue]]\" />",
                HtmlContentUtilities.HtmlContentToString(textBoxForResult));
        }

        private class TestModel
        {
            public string Property1 { get; set; }
        }
    }
}
