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
    /// Test the RadioButton extensions in <see cref="HtmlHelperInputExtensions" /> class.
    /// </summary>
    public class HtmlHelperRadioButtonExtensionsTest
    {
        [Fact]
        public void RadioButton_UsesSpecifiedExpression()
        {
            // Arrange
            var helper = DefaultTemplatesUtilities.GetHtmlHelper();

            // Act
            var radioButtonResult = helper.RadioButton("Property1", value: "myvalue");

            // Assert
            Assert.Equal(
                "<input id=\"HtmlEncode[[Property1]]\" name=\"HtmlEncode[[Property1]]\" type=\"HtmlEncode[[radio]]\" value=\"HtmlEncode[[myvalue]]\" />",
                HtmlContentUtilities.HtmlContentToString(radioButtonResult));
        }

        [Fact]
        public void RadioButtonFor_UsesSpecifiedExpression()
        {
            // Arrange
            var helper = DefaultTemplatesUtilities.GetHtmlHelper();

            // Act
            var radioButtonForResult = helper.RadioButtonFor(m => m.Property1, value: "myvalue");

            // Assert
            Assert.Equal(
                "<input id=\"HtmlEncode[[Property1]]\" name=\"HtmlEncode[[Property1]]\" type=\"HtmlEncode[[radio]]\" value=\"HtmlEncode[[myvalue]]\" />",
                HtmlContentUtilities.HtmlContentToString(radioButtonForResult));
        }

        [Theory]
        [InlineData("MyValue")]
        [InlineData("myvalue")]
        public void RadioButton_CheckedWhenValueMatchesSpecifiedExpression(string value)
        {
            // Arrange
            var metadataProvider = new EmptyModelMetadataProvider();
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(new ViewDataDictionary<TestModel>(metadataProvider));
            helper.ViewContext.ClientValidationEnabled = false;
            helper.ViewData.Model = new TestModel { Property1 = value };

            // Act
            var radioButtonResult = helper.RadioButton("Property1", value: "myvalue");

            // Assert
            Assert.Equal(
                "<input checked=\"HtmlEncode[[checked]]\" id=\"HtmlEncode[[Property1]]\" name=\"HtmlEncode[[Property1]]\" type=\"HtmlEncode[[radio]]\" value=\"HtmlEncode[[myvalue]]\" />",
                HtmlContentUtilities.HtmlContentToString(radioButtonResult));
        }

        [Theory]
        [InlineData("MyValue")]
        [InlineData("myvalue")]
        public void RadioButtonFor_CheckedWhenValueMatchesSpecifiedExpression(string value)
        {
            // Arrange
            var metadataProvider = new EmptyModelMetadataProvider();
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(new ViewDataDictionary<TestModel>(metadataProvider));
            helper.ViewContext.ClientValidationEnabled = false;
            helper.ViewData.Model = new TestModel { Property1 = value };

            // Act
            var radioButtonForResult = helper.RadioButtonFor(m => m.Property1, value: "myvalue");

            // Assert
            Assert.Equal(
                "<input checked=\"HtmlEncode[[checked]]\" id=\"HtmlEncode[[Property1]]\" name=\"HtmlEncode[[Property1]]\" type=\"HtmlEncode[[radio]]\" value=\"HtmlEncode[[myvalue]]\" />",
                HtmlContentUtilities.HtmlContentToString(radioButtonForResult));
        }

        [Fact]
        public void RadioButtonHelpers_UsesSpecifiedIsChecked()
        {
            // Arrange
            var helper = DefaultTemplatesUtilities.GetHtmlHelper();

            // Act
            var radioButtonResult = helper.RadioButton("Property1", value: "myvalue", isChecked: true);

            // Assert
            Assert.Equal(
                "<input checked=\"HtmlEncode[[checked]]\" id=\"HtmlEncode[[Property1]]\" name=\"HtmlEncode[[Property1]]\" type=\"HtmlEncode[[radio]]\" value=\"HtmlEncode[[myvalue]]\" />",
                HtmlContentUtilities.HtmlContentToString(radioButtonResult));
        }

        [Fact]
        public void RadioButtonHelpers_UsesSpecifiedIsCheckedRegardlessOfValue()
        {
            // Arrange
            var metadataProvider = new EmptyModelMetadataProvider();
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(new ViewDataDictionary<TestModel>(metadataProvider));
            helper.ViewContext.ClientValidationEnabled = false;
            helper.ViewData.Model = new TestModel { Property2 = true };

            // Act
            var radioButtonResult = helper.RadioButton("Property2", value: "myvalue", isChecked: false);

            // Assert
            Assert.Equal(
                "<input id=\"HtmlEncode[[Property2]]\" name=\"HtmlEncode[[Property2]]\" type=\"HtmlEncode[[radio]]\" value=\"HtmlEncode[[myvalue]]\" />",
                HtmlContentUtilities.HtmlContentToString(radioButtonResult));
        }

        [Fact]
        public void RadioButton_UsesSpecifiedHtmlAttributes()
        {
            // Arrange
            var helper = DefaultTemplatesUtilities.GetHtmlHelper();

            // Act
            var radioButtonResult = helper.RadioButton("Property1", value: "myvalue", htmlAttributes: new { attr = "value" });

            // Assert
            Assert.Equal(
                "<input attr=\"HtmlEncode[[value]]\" id=\"HtmlEncode[[Property1]]\" name=\"HtmlEncode[[Property1]]\" type=\"HtmlEncode[[radio]]\" value=\"HtmlEncode[[myvalue]]\" />",
                HtmlContentUtilities.HtmlContentToString(radioButtonResult));
        }

        [Fact]
        public void RadioButtonFor_UsesSpecifiedHtmlAttributes()
        {
            // Arrange
            var helper = DefaultTemplatesUtilities.GetHtmlHelper();

            // Act
            var radioButtonForResult = helper.RadioButtonFor(m => m.Property1, value: "myvalue", htmlAttributes: new { attr = "value" });

            // Assert
            Assert.Equal(
                "<input attr=\"HtmlEncode[[value]]\" id=\"HtmlEncode[[Property1]]\" name=\"HtmlEncode[[Property1]]\" type=\"HtmlEncode[[radio]]\" value=\"HtmlEncode[[myvalue]]\" />",
                HtmlContentUtilities.HtmlContentToString(radioButtonForResult));
        }

        private class TestModel
        {
            public string Property1 { get; set; }

            public bool Property2 { get; set; }
        }
    }
}
