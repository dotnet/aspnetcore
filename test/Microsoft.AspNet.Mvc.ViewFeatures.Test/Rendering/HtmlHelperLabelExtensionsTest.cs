// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Rendering;
using Xunit;

namespace Microsoft.AspNet.Mvc.Core
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
            Assert.Empty(labelResult.ToString());
            Assert.Empty(labelNullResult.ToString());
            Assert.Empty(labelForResult.ToString());
            Assert.Empty(labelForModelResult.ToString());
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
            Assert.Equal("<label for=\"HtmlEncode[[Property1]]\">HtmlEncode[[Property1]]</label>", labelResult.ToString());
            Assert.Equal("<label for=\"HtmlEncode[[Property1]]\">HtmlEncode[[Property1]]</label>", labelForResult.ToString());
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
            Assert.Equal("<label for=\"HtmlEncode[[Inner_Id]]\">HtmlEncode[[Id]]</label>", labelResult.ToString());
            Assert.Equal("<label for=\"HtmlEncode[[Inner_Id]]\">HtmlEncode[[Id]]</label>", labelForResult.ToString());
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
            Assert.Equal("<label for=\"\">HtmlEncode[[" + propertyName + "]]</label>", labelResult.ToString());
            Assert.Equal("<label for=\"\">HtmlEncode[[" + propertyName + "]]</label>", labelForResult.ToString());
            Assert.Equal("<label for=\"\">HtmlEncode[[" + propertyName + "]]</label>", labelForModelResult.ToString());
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
            Assert.Equal("<label for=\"HtmlEncode[[value]]\">HtmlEncode[[value]]</label>", labelResult.ToString());
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
            Assert.Empty(labelResult.ToString());
            Assert.Empty(labelNullResult.ToString());
            Assert.Empty(labelForResult.ToString());
            Assert.Empty(labelForModelResult.ToString());
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
            Assert.Equal("<label for=\"\">HtmlEncode[[" + displayName + "]]</label>", labelResult.ToString());
            Assert.Equal("<label for=\"\">HtmlEncode[[" + displayName + "]]</label>", labelForResult.ToString());
            Assert.Equal("<label for=\"\">HtmlEncode[[" + displayName + "]]</label>", labelForModelResult.ToString());
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
            Assert.Empty(labelResult.ToString());
            Assert.Empty(labelNullResult.ToString());
            Assert.Empty(labelForResult.ToString());
            Assert.Empty(labelForModelResult.ToString());
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
            Assert.Equal("<label for=\"HtmlEncode[[Property1]]\">HtmlEncode[[" + displayName + "]]</label>", labelResult.ToString());
            Assert.Equal("<label for=\"HtmlEncode[[Property1]]\">HtmlEncode[[" + displayName + "]]</label>", labelForResult.ToString());
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
            Assert.Equal("<label for=\"HtmlEncode[[" + expectedId + "]]\">HtmlEncode[[" + expectedText + "]]</label>", result.ToString());
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
            Assert.Equal("<label for=\"HtmlEncode[[unknownKey]]\">HtmlEncode[[unknownKey]]</label>", result.ToString());
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