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
        public void LabelHelpers_ReturnExpectedElementForModel_WithLabelText()
        {
            // Arrange
            var expectedLabel = "<label for=\"\">HtmlEncode[[a label]]</label>";
            var helper = DefaultTemplatesUtilities.GetHtmlHelper();

            // Act
            var labelResult = helper.Label(expression: string.Empty, labelText: "a label");
            var labelNullResult = helper.Label(expression: null, labelText: "a label");
            var labelForResult = helper.LabelFor(m => m, labelText: "a label");
            var labelForModelResult = helper.LabelForModel(labelText: "a label");

            // Assert
            Assert.Equal(expectedLabel, HtmlContentUtilities.HtmlContentToString(labelResult));
            Assert.Equal(expectedLabel, HtmlContentUtilities.HtmlContentToString(labelNullResult));
            Assert.Equal(expectedLabel, HtmlContentUtilities.HtmlContentToString(labelForResult));
            Assert.Equal(expectedLabel, HtmlContentUtilities.HtmlContentToString(labelForModelResult));
        }

        // Prior to aspnet/Mvc#6638 fix, helpers generated nothing with this setup.
        [Fact]
        public void LabelHelpers_ReturnExpectedElementForModel_WithEmptyLabelText()
        {
            // Arrange
            var expectedLabel = "<label for=\"\"></label>";
            var helper = DefaultTemplatesUtilities.GetHtmlHelper();

            // Act
            var labelResult = helper.Label(expression: string.Empty, labelText: string.Empty);
            var labelNullResult = helper.Label(expression: null, labelText: string.Empty);
            var labelForResult = helper.LabelFor(m => m, labelText: string.Empty);
            var labelForModelResult = helper.LabelForModel(labelText: string.Empty);

            // Assert
            Assert.Equal(expectedLabel, HtmlContentUtilities.HtmlContentToString(labelResult));
            Assert.Equal(expectedLabel, HtmlContentUtilities.HtmlContentToString(labelNullResult));
            Assert.Equal(expectedLabel, HtmlContentUtilities.HtmlContentToString(labelForResult));
            Assert.Equal(expectedLabel, HtmlContentUtilities.HtmlContentToString(labelForModelResult));
        }

        [Fact]
        public void LabelHelpers_DisplayPropertyName()
        {
            // Arrange
            var expectedLabel = "<label for=\"HtmlEncode[[Property1]]\">HtmlEncode[[Property1]]</label>";
            var helper = DefaultTemplatesUtilities.GetHtmlHelper();

            // Act
            var labelResult = helper.Label("Property1");
            var labelForResult = helper.LabelFor(m => m.Property1);

            // Assert
            Assert.Equal(expectedLabel, HtmlContentUtilities.HtmlContentToString(labelResult));
            Assert.Equal(expectedLabel, HtmlContentUtilities.HtmlContentToString(labelForResult));
        }

        [Fact]
        public void LabelHelpers_DisplayPropertyName_ForNestedProperty()
        {
            // Arrange
            var expectedLabel = "<label for=\"HtmlEncode[[Inner_Id]]\">HtmlEncode[[Id]]</label>";
            var helper = DefaultTemplatesUtilities.GetHtmlHelper<OuterClass>(model: null);

            // Act
            var labelResult = helper.Label("Inner.Id");
            var labelForResult = helper.LabelFor(m => m.Inner.Id);

            // Assert
            Assert.Equal(expectedLabel, HtmlContentUtilities.HtmlContentToString(labelResult));
            Assert.Equal(expectedLabel, HtmlContentUtilities.HtmlContentToString(labelForResult));
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

        // Following test is identical to LabelHelpers_ReturnEmptyForModel() from the HTML helpers' perspective. But,
        // test confirms the added metadata does not change the behavior.
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

        // Prior to aspnet/Mvc#6638 fix, helpers generated nothing with this setup.
        // Following test mimics use of an identity expression in an editor template if invoked for an element in a
        // collection. See also LabelHelpers_ReturnExpectedElementForProperty_IfDisplayNameEmptyAndNotTopLevel().
        [Fact]
        public void LabelHelpers_ReturnExpectedElementForModel_IfDisplayNameEmptyAndNotTopLevel()
        {
            // Arrange
            var expectedLabel = "<label for=\"HtmlEncode[[prefix]]\"></label>";
            var provider = new TestModelMetadataProvider();
            provider
                .ForType<DefaultTemplatesUtilities.ObjectTemplateModel>()
                .DisplayDetails(dd => dd.DisplayName = () => string.Empty);

            var helper = DefaultTemplatesUtilities.GetHtmlHelper(provider: provider);
            helper.ViewData.TemplateInfo.HtmlFieldPrefix = "prefix";

            // Act
            var labelResult = helper.Label(expression: string.Empty);
            var labelNullResult = helper.Label(expression: null);   // null is another alias for current model
            var labelForResult = helper.LabelFor(m => m);
            var labelForModelResult = helper.LabelForModel();

            // Assert
            Assert.Equal(expectedLabel, HtmlContentUtilities.HtmlContentToString(labelResult));
            Assert.Equal(expectedLabel, HtmlContentUtilities.HtmlContentToString(labelNullResult));
            Assert.Equal(expectedLabel, HtmlContentUtilities.HtmlContentToString(labelForResult));
            Assert.Equal(expectedLabel, HtmlContentUtilities.HtmlContentToString(labelForModelResult));
        }

        [Fact]
        public void LabelHelpers_ReturnExpectedElementForModel_IfDisplayNameEmpty_WithLabelText()
        {
            // Arrange
            var expectedLabel = "<label for=\"\">HtmlEncode[[a label]]</label>";
            var provider = new TestModelMetadataProvider();
            provider
                .ForType<DefaultTemplatesUtilities.ObjectTemplateModel>()
                .DisplayDetails(dd => dd.DisplayName = () => string.Empty);

            var helper = DefaultTemplatesUtilities.GetHtmlHelper(provider: provider);

            // Act
            var labelResult = helper.Label(expression: string.Empty, labelText: "a label");
            var labelNullResult = helper.Label(expression: null, labelText: "a label");
            var labelForResult = helper.LabelFor(m => m, labelText: "a label");
            var labelForModelResult = helper.LabelForModel(labelText: "a label");

            // Assert
            Assert.Equal(expectedLabel, HtmlContentUtilities.HtmlContentToString(labelResult));
            Assert.Equal(expectedLabel, HtmlContentUtilities.HtmlContentToString(labelNullResult));
            Assert.Equal(expectedLabel, HtmlContentUtilities.HtmlContentToString(labelForResult));
            Assert.Equal(expectedLabel, HtmlContentUtilities.HtmlContentToString(labelForModelResult));
        }

        // Prior to aspnet/Mvc#6638 fix, helpers generated nothing with this setup.
        [Fact]
        public void LabelHelpers_ReturnExpectedElementForModel_IfDisplayNameEmpty_WithEmptyLabelText()
        {
            // Arrange
            var expectedLabel = "<label for=\"\"></label>";
            var provider = new TestModelMetadataProvider();
            provider
                .ForType<DefaultTemplatesUtilities.ObjectTemplateModel>()
                .DisplayDetails(dd => dd.DisplayName = () => string.Empty);

            var helper = DefaultTemplatesUtilities.GetHtmlHelper(provider: provider);

            // Act
            var labelResult = helper.Label(expression: string.Empty, labelText: string.Empty);
            var labelNullResult = helper.Label(expression: null, labelText: string.Empty);
            var labelForResult = helper.LabelFor(m => m, labelText: string.Empty);
            var labelForModelResult = helper.LabelForModel(labelText: string.Empty);

            // Assert
            Assert.Equal(expectedLabel, HtmlContentUtilities.HtmlContentToString(labelResult));
            Assert.Equal(expectedLabel, HtmlContentUtilities.HtmlContentToString(labelNullResult));
            Assert.Equal(expectedLabel, HtmlContentUtilities.HtmlContentToString(labelForResult));
            Assert.Equal(expectedLabel, HtmlContentUtilities.HtmlContentToString(labelForModelResult));
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

        // Prior to aspnet/Mvc#6638 fix, helpers generated nothing with this setup.
        // Following test mimics use of an identity expression in an editor template if invoked for a property. See
        // also LabelHelpers_ReturnExpectedElementForModel_IfDisplayNameEmptyAndNotTopLevel().
        [Fact]
        public void LabelHelpers_ReturnExpectedElementForProperty_IfDisplayNameEmptyAndNotTopLevel()
        {
            // Arrange
            var expectedLabel = "<label for=\"HtmlEncode[[Property1]]\"></label>";
            var provider = new TestModelMetadataProvider();
            provider
                .ForProperty<DefaultTemplatesUtilities.ObjectTemplateModel>("Property1")
                .DisplayDetails(dd => dd.DisplayName = () => string.Empty);

            var helper = DefaultTemplatesUtilities.GetHtmlHelper<string>(provider: provider);
            helper.ViewData.ModelExplorer = provider
                .GetModelExplorerForType(typeof(DefaultTemplatesUtilities.ObjectTemplateModel), model: null)
                .GetExplorerForProperty(nameof(DefaultTemplatesUtilities.ObjectTemplateModel.Property1));
            helper.ViewData.TemplateInfo.HtmlFieldPrefix = nameof(DefaultTemplatesUtilities.ObjectTemplateModel.Property1);

            // Act
            var labelResult = helper.Label(expression: string.Empty);
            var labelNullResult = helper.Label(expression: null);   // null is another alias for current model
            var labelForResult = helper.LabelFor(m => m);
            var labelForModelResult = helper.LabelForModel();

            // Assert
            Assert.Equal(expectedLabel, HtmlContentUtilities.HtmlContentToString(labelResult));
            Assert.Equal(expectedLabel, HtmlContentUtilities.HtmlContentToString(labelNullResult));
            Assert.Equal(expectedLabel, HtmlContentUtilities.HtmlContentToString(labelForResult));
            Assert.Equal(expectedLabel, HtmlContentUtilities.HtmlContentToString(labelForModelResult));
        }

        // Prior to aspnet/Mvc#6638 fix, helpers generated nothing with this setup.
        [Fact]
        public void LabelHelpers_ReturnExpectedElementForProperty_IfDisplayNameEmpty()
        {
            // Arrange
            var expectedLabel = "<label for=\"HtmlEncode[[Property1]]\"></label>";
            var provider = new TestModelMetadataProvider();
            provider
                .ForProperty<DefaultTemplatesUtilities.ObjectTemplateModel>("Property1")
                .DisplayDetails(dd => dd.DisplayName = () => string.Empty);

            var helper = DefaultTemplatesUtilities.GetHtmlHelper(provider: provider);

            // Act
            var labelResult = helper.Label(expression: nameof(DefaultTemplatesUtilities.ObjectTemplateModel.Property1));
            var labelForResult = helper.LabelFor(m => m.Property1);

            // Assert
            Assert.Equal(expectedLabel, HtmlContentUtilities.HtmlContentToString(labelResult));
            Assert.Equal(expectedLabel, HtmlContentUtilities.HtmlContentToString(labelForResult));
        }

        [Fact]
        public void LabelHelpers_ReturnExpectedElementForProperty_IfDisplayNameEmpty_WithLabelText()
        {
            // Arrange
            var expectedLabel = "<label for=\"HtmlEncode[[Property1]]\">HtmlEncode[[a label]]</label>";
            var provider = new TestModelMetadataProvider();
            provider
                .ForProperty<DefaultTemplatesUtilities.ObjectTemplateModel>("Property1")
                .DisplayDetails(dd => dd.DisplayName = () => string.Empty);

            var helper = DefaultTemplatesUtilities.GetHtmlHelper(provider: provider);

            // Act
            var labelResult = helper.Label(
                expression: nameof(DefaultTemplatesUtilities.ObjectTemplateModel.Property1),
                labelText: "a label");
            var labelForResult = helper.LabelFor(m => m.Property1, labelText: "a label");

            // Assert
            Assert.Equal(expectedLabel, HtmlContentUtilities.HtmlContentToString(labelResult));
            Assert.Equal(expectedLabel, HtmlContentUtilities.HtmlContentToString(labelForResult));
        }

        // Prior to aspnet/Mvc#6638 fix, helpers generated nothing with this setup.
        [Fact]
        public void LabelHelpers_ReturnExpectedElementForProperty_IfDisplayNameEmpty_WithEmptyLabelText()
        {
            // Arrange
            var expectedLabel = "<label for=\"HtmlEncode[[Property1]]\"></label>";
            var provider = new TestModelMetadataProvider();
            provider
                .ForProperty<DefaultTemplatesUtilities.ObjectTemplateModel>("Property1")
                .DisplayDetails(dd => dd.DisplayName = () => string.Empty);

            var helper = DefaultTemplatesUtilities.GetHtmlHelper(provider: provider);

            // Act
            var labelResult = helper.Label(
                expression: nameof(DefaultTemplatesUtilities.ObjectTemplateModel.Property1),
                labelText: string.Empty);
            var labelForResult = helper.LabelFor(m => m.Property1, labelText: string.Empty);

            // Assert
            Assert.Equal(expectedLabel, HtmlContentUtilities.HtmlContentToString(labelResult));
            Assert.Equal(expectedLabel, HtmlContentUtilities.HtmlContentToString(labelForResult));
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
