// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.TestCommon;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Core
{
    /// <summary>
    /// Test the <see cref="HtmlHelperLabelExtensions" /> class.
    /// </summary>
    public class HtmlHelperLabelExtensionsTest
    {
        [Fact]
        public void LabelHelpers_ReturnEmptyForModel()
        {
            // Arrange
            var helper = DefaultTemplatesUtilities.GetHtmlHelper();

            // Act
            var labelResult = helper.Label(expression: string.Empty);
            var labelNullResult = helper.Label(expression: null);   // null is another alias for current model
            var labelForResult = helper.LabelFor(m => m);
            var labelForModelResult = helper.LabelForModel();

            // Assert
            Assert.Empty(HtmlContentUtilities.HtmlContentToString(labelResult));
            Assert.Empty(HtmlContentUtilities.HtmlContentToString(labelNullResult));
            Assert.Empty(HtmlContentUtilities.HtmlContentToString(labelForResult));
            Assert.Empty(HtmlContentUtilities.HtmlContentToString(labelForModelResult));
        }

        [Fact]
        public void LabelHelpers_DisplayPropertyName()
        {
            // Arrange
            var helper = DefaultTemplatesUtilities.GetHtmlHelper();

            // Act
            var labelResult = helper.Label("Property1");
            var labelForResult = helper.LabelFor(m => m.Property1);

            // Assert
            Assert.Equal("<label for=\"HtmlEncode[[Property1]]\">HtmlEncode[[Property1]]</label>", HtmlContentUtilities.HtmlContentToString(labelResult));
            Assert.Equal("<label for=\"HtmlEncode[[Property1]]\">HtmlEncode[[Property1]]</label>", HtmlContentUtilities.HtmlContentToString(labelForResult));
        }

        [Fact]
        public void LabelHelpers_DisplayPropertyName_ForNestedProperty()
        {
            // Arrange
            var helper = DefaultTemplatesUtilities.GetHtmlHelper<OuterClass>(model: null);

            // Act
            var labelResult = helper.Label("Inner.Id");
            var labelForResult = helper.LabelFor(m => m.Inner.Id);

            // Assert
            Assert.Equal("<label for=\"HtmlEncode[[Inner_Id]]\">HtmlEncode[[Id]]</label>", HtmlContentUtilities.HtmlContentToString(labelResult));
            Assert.Equal("<label for=\"HtmlEncode[[Inner_Id]]\">HtmlEncode[[Id]]</label>", HtmlContentUtilities.HtmlContentToString(labelForResult));
        }

        [Fact]
        public void LabelHelpers_DisplayMetadataPropertyNameForProperty()
        {
            // Arrange
            var propertyName = "Property1";

            var provider = new EmptyModelMetadataProvider();

            var modelExplorer = provider
                .GetModelExplorerForType(typeof(DefaultTemplatesUtilities.ObjectTemplateModel), model: null)
                .GetExplorerForProperty(propertyName);

            var helper = DefaultTemplatesUtilities.GetHtmlHelper();
            helper.ViewData.ModelExplorer = modelExplorer;

            // Act
            var labelResult = helper.Label(expression: string.Empty);
            var labelForResult = helper.LabelFor(m => m);
            var labelForModelResult = helper.LabelForModel();

            // Assert
            Assert.Equal("<label for=\"\">HtmlEncode[[" + propertyName + "]]</label>", HtmlContentUtilities.HtmlContentToString(labelResult));
            Assert.Equal("<label for=\"\">HtmlEncode[[" + propertyName + "]]</label>", HtmlContentUtilities.HtmlContentToString(labelForResult));
            Assert.Equal("<label for=\"\">HtmlEncode[[" + propertyName + "]]</label>", HtmlContentUtilities.HtmlContentToString(labelForModelResult));
        }

        // If the metadata is for a type (not property), then Label(expression) will evaluate the expression
        [Fact]
        public void LabelHelpers_Label_Evaluates_Expression()
        {
            // Arrange
            var helper = DefaultTemplatesUtilities.GetHtmlHelper();
            helper.ViewData["value"] = "testvalue";

            // Act
            var labelResult = helper.Label(expression: "value");

            // Assert
            Assert.Equal("<label for=\"HtmlEncode[[value]]\">HtmlEncode[[value]]</label>", HtmlContentUtilities.HtmlContentToString(labelResult));
        }

        [Fact]
        public void LabelHelpers_ReturnEmptyForModel_IfDisplayNameEmpty()
        {
            // Arrange
            var provider = new TestModelMetadataProvider();
            provider
                .ForType<DefaultTemplatesUtilities.ObjectTemplateModel>()
                .DisplayDetails(dd => dd.DisplayName = () => string.Empty);

            var helper = DefaultTemplatesUtilities.GetHtmlHelper(provider: provider);

            // Act
            var labelResult = helper.Label(expression: string.Empty);
            var labelNullResult = helper.Label(expression: null);   // null is another alias for current model
            var labelForResult = helper.LabelFor(m => m);
            var labelForModelResult = helper.LabelForModel();

            // Assert
            Assert.Empty(HtmlContentUtilities.HtmlContentToString(labelResult));
            Assert.Empty(HtmlContentUtilities.HtmlContentToString(labelNullResult));
            Assert.Empty(HtmlContentUtilities.HtmlContentToString(labelForResult));
            Assert.Empty(HtmlContentUtilities.HtmlContentToString(labelForModelResult));
        }

        [Theory]
        [InlineData("DisplayName")]
        [InlineData("Custom display name from metadata")]
        public void LabelHelpers_DisplayDisplayName_IfNonNull(string displayName)
        {
            // Arrange
            var provider = new TestModelMetadataProvider();
            provider
                .ForType<DefaultTemplatesUtilities.ObjectTemplateModel>()
                .DisplayDetails(dd => dd.DisplayName = () => displayName);

            var helper = DefaultTemplatesUtilities.GetHtmlHelper(provider: provider);

            // Act
            var labelResult = helper.Label(expression: string.Empty);
            var labelForResult = helper.LabelFor(m => m);
            var labelForModelResult = helper.LabelForModel();

            // Assert
            Assert.Equal("<label for=\"\">HtmlEncode[[" + displayName + "]]</label>", HtmlContentUtilities.HtmlContentToString(labelResult));
            Assert.Equal("<label for=\"\">HtmlEncode[[" + displayName + "]]</label>", HtmlContentUtilities.HtmlContentToString(labelForResult));
            Assert.Equal("<label for=\"\">HtmlEncode[[" + displayName + "]]</label>", HtmlContentUtilities.HtmlContentToString(labelForModelResult));
        }

        [Fact]
        public void LabelHelpers_ReturnEmptyForProperty_IfDisplayNameEmpty()
        {
            // Arrange
            var provider = new TestModelMetadataProvider();
            provider
                .ForProperty<DefaultTemplatesUtilities.ObjectTemplateModel>("Property1")
                .DisplayDetails(dd => dd.DisplayName = () => string.Empty);

            var modelExplorer = provider
                .GetModelExplorerForType(typeof(DefaultTemplatesUtilities.ObjectTemplateModel), model: null)
                .GetExplorerForProperty("Property1");

            var helper = DefaultTemplatesUtilities.GetHtmlHelper();

            // Act
            var labelResult = helper.Label(expression: string.Empty);
            var labelNullResult = helper.Label(expression: null);   // null is another alias for current model
            var labelForResult = helper.LabelFor(m => m);
            var labelForModelResult = helper.LabelForModel();

            // Assert
            Assert.Empty(HtmlContentUtilities.HtmlContentToString(labelResult));
            Assert.Empty(HtmlContentUtilities.HtmlContentToString(labelNullResult));
            Assert.Empty(HtmlContentUtilities.HtmlContentToString(labelForResult));
            Assert.Empty(HtmlContentUtilities.HtmlContentToString(labelForModelResult));
        }

        [Theory]
        [InlineData("DisplayName")]
        [InlineData("Custom display name from metadata")]
        public void LabelHelpers_DisplayDisplayNameForProperty_IfNonNull(string displayName)
        {
            // Arrange
            var provider = new TestModelMetadataProvider();
            provider
                .ForProperty<DefaultTemplatesUtilities.ObjectTemplateModel>("Property1")
                .DisplayDetails(dd => dd.DisplayName = () => displayName);

            var helper = DefaultTemplatesUtilities.GetHtmlHelper(provider: provider);

            // Act
            var labelResult = helper.Label("Property1");
            var labelForResult = helper.LabelFor(m => m.Property1);

            // Assert
            Assert.Equal("<label for=\"HtmlEncode[[Property1]]\">HtmlEncode[[" + displayName + "]]</label>", HtmlContentUtilities.HtmlContentToString(labelResult));
            Assert.Equal("<label for=\"HtmlEncode[[Property1]]\">HtmlEncode[[" + displayName + "]]</label>", HtmlContentUtilities.HtmlContentToString(labelForResult));
        }

        [Theory]
        [InlineData("A", "A", "A")]
        [InlineData("A[23]", "A[23]", "A_23_")]
        [InlineData("A[0].B", "B", "A_0__B")]
        [InlineData("A.B.C.D", "D", "A_B_C_D")]
        public void Label_DisplaysRightmostExpressionSegment_IfPropertiesNotFound(
            string expression,
            string expectedText,
            string expectedId)
        {
            // Arrange
            var helper = DefaultTemplatesUtilities.GetHtmlHelper();

            // Act
            var result = helper.Label(expression);

            // Assert
            // Label() falls back to expression name when DisplayName and PropertyName are null.
            Assert.Equal("<label for=\"HtmlEncode[[" + expectedId + "]]\">HtmlEncode[[" + expectedText + "]]</label>", HtmlContentUtilities.HtmlContentToString(result));
        }

        [Fact]
        public void LabelFor_ThrowsInvalidOperation_IfExpressionUnsupported()
        {
            // Arrange
            var helper = DefaultTemplatesUtilities.GetHtmlHelper();

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => helper.LabelFor(model => new { foo = "Bar" }));
            Assert.Equal(
                "Templates can be used only with field access, property access, single-dimension array index, or single-parameter custom indexer expressions.",
                exception.Message);
        }

        [Fact]
        public void LabelFor_DisplaysVariableName()
        {
            // Arrange
            var unknownKey = "this is a dummy parameter value";
            var helper = DefaultTemplatesUtilities.GetHtmlHelper();

            // Act
            var result = helper.LabelFor(model => unknownKey);

            // Assert
            Assert.Equal("<label for=\"HtmlEncode[[unknownKey]]\">HtmlEncode[[unknownKey]]</label>", HtmlContentUtilities.HtmlContentToString(result));
        }

        [Fact]
        public void Label_UsesSpecifiedLabelText()
        {
            // Arrange
            var helper = DefaultTemplatesUtilities.GetHtmlHelper();

            // Act
            var labelResult = helper.Label("Property1", labelText: "Hello");

            // Assert
            Assert.Equal("<label for=\"HtmlEncode[[Property1]]\">HtmlEncode[[Hello]]</label>", HtmlContentUtilities.HtmlContentToString(labelResult));
        }

        [Fact]
        public void LabelFor_UsesSpecifiedLabelText()
        {
            // Arrange
            var helper = DefaultTemplatesUtilities.GetHtmlHelper();

            // Act
            var labelForResult = helper.LabelFor(m => m.Property1, labelText: "Hello");

            // Assert
            Assert.Equal("<label for=\"HtmlEncode[[Property1]]\">HtmlEncode[[Hello]]</label>", HtmlContentUtilities.HtmlContentToString(labelForResult));
        }

        [Fact]
        public void LabelForModel_UsesSpecifiedLabelText()
        {
            // Arrange
            var helper = DefaultTemplatesUtilities.GetHtmlHelper();

            // Act
            var labelForModelResult = helper.LabelForModel(labelText: "Hello");

            // Assert
            Assert.Equal("<label for=\"\">HtmlEncode[[Hello]]</label>", HtmlContentUtilities.HtmlContentToString(labelForModelResult));
        }

        [Fact]
        public void LabelFor_DisplaysSpecifiedHtmlAttributes()
        {
            // Arrange
            var helper = DefaultTemplatesUtilities.GetHtmlHelper();

            // Act
            var labelForResult = helper.LabelFor(m => m.Property1, htmlAttributes: new { attr="value" });

            // Assert
            Assert.Equal("<label attr=\"HtmlEncode[[value]]\" for=\"HtmlEncode[[Property1]]\">HtmlEncode[[Property1]]</label>", HtmlContentUtilities.HtmlContentToString(labelForResult));
        }

        [Fact]
        public void LabelForModel_DisplaysSpecifiedHtmlAttributes()
        {
            // Arrange
            var helper = DefaultTemplatesUtilities.GetHtmlHelper();

            // Act
            var labelForModelResult = helper.LabelForModel(labelText: "Hello", htmlAttributes: new { attr = "value" });

            // Assert
            Assert.Equal("<label attr=\"HtmlEncode[[value]]\" for=\"\">HtmlEncode[[Hello]]</label>", HtmlContentUtilities.HtmlContentToString(labelForModelResult));
        }

        private sealed class InnerClass
        {
            public int Id { get; set; }
        }

        private sealed class OuterClass
        {
            public InnerClass Inner { get; set; }
        }
    }
}
