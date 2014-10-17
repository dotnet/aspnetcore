// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Rendering;
using Moq;
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
            Assert.Equal("<label for=\"Property1\">Property1</label>", labelResult.ToString());
            Assert.Equal("<label for=\"Property1\">Property1</label>", labelForResult.ToString());
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
            Assert.Equal("<label for=\"Inner_Id\">Id</label>", labelResult.ToString());
            Assert.Equal("<label for=\"Inner_Id\">Id</label>", labelForResult.ToString());
        }

        [Fact]
        public void LabelHelpers_ReturnEmptyForModel_IfMetadataPropertyNameEmpty()
        {
            // Arrange
            var metadata = new ModelMetadata(
                new DataAnnotationsModelMetadataProvider(),
                containerType: null,
                modelAccessor: null,
                modelType: typeof(object),
                propertyName: string.Empty);

            var helper = DefaultTemplatesUtilities.GetHtmlHelper();
            helper.ViewData.ModelMetadata = metadata;

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
        [InlineData("MyProperty")]
        [InlineData("Custom property name from metadata")]
        public void LabelHelpers_DisplayMetadataPropertyName_IfOverridden(string propertyName)
        {
            // Arrange
            var metadata = new ModelMetadata(
                new DataAnnotationsModelMetadataProvider(),
                containerType: null,
                modelAccessor: null,
                modelType: typeof(object),
                propertyName: propertyName);

            var helper = DefaultTemplatesUtilities.GetHtmlHelper();
            helper.ViewData.ModelMetadata = metadata;

            // Act
            var labelResult = helper.Label(expression: string.Empty);
            var labelForResult = helper.LabelFor(m => m);
            var labelForModelResult = helper.LabelForModel();

            // Assert
            Assert.Equal("<label for=\"\">" + propertyName + "</label>", labelResult.ToString());
            Assert.Equal("<label for=\"\">" + propertyName + "</label>", labelForResult.ToString());
            Assert.Equal("<label for=\"\">" + propertyName + "</label>", labelForModelResult.ToString());
        }

        [Theory]
        [InlineData("MyProperty")]
        [InlineData("Custom property name from metadata")]
        public void LabelHelpers_DisplayMetadataPropertyNameForProperty_IfOverridden(string propertyName)
        {
            // Arrange
            var metadataHelper = new MetadataHelper();
            var metadata = new ModelMetadata(
                metadataHelper.MetadataProvider.Object,
                containerType: null,
                modelAccessor: null,
                modelType: typeof(object),
                propertyName: propertyName);
            metadataHelper.MetadataProvider
                .Setup(provider => provider.GetMetadataForProperty(It.IsAny<Func<object>>(), It.IsAny<Type>(), "Property1"))
                .Returns(metadata);

            var helper = DefaultTemplatesUtilities.GetHtmlHelper(metadataHelper.MetadataProvider.Object);

            // Act
            var labelForResult = helper.LabelFor(m => m.Property1);

            // Assert
            Assert.Equal("<label for=\"Property1\">" + propertyName + "</label>", labelForResult.ToString());
        }

        [Fact]
        public void LabelHelpers_ReturnEmptyForModel_IfDisplayNameEmpty()
        {
            // Arrange
            var helper = DefaultTemplatesUtilities.GetHtmlHelper();
            helper.ViewData.ModelMetadata.DisplayName = string.Empty;

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
            var helper = DefaultTemplatesUtilities.GetHtmlHelper();
            helper.ViewData.ModelMetadata.DisplayName = displayName;

            // Act
            var labelResult = helper.Label(expression: string.Empty);
            var labelForResult = helper.LabelFor(m => m);
            var labelForModelResult = helper.LabelForModel();

            // Assert
            Assert.Equal("<label for=\"\">" + displayName + "</label>", labelResult.ToString());
            Assert.Equal("<label for=\"\">" + displayName + "</label>", labelForResult.ToString());
            Assert.Equal("<label for=\"\">" + displayName + "</label>", labelForModelResult.ToString());
        }

        [Fact]
        public void LabelHelpers_ReturnEmptyForProperty_IfDisplayNameEmpty()
        {
            // Arrange
            var metadataHelper = new MetadataHelper();  // All properties will use the same metadata.
            metadataHelper.Metadata
                .Setup(metadata => metadata.DisplayName)
                .Returns(string.Empty);
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(metadataHelper.MetadataProvider.Object);

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
            var metadataHelper = new MetadataHelper();  // All properties will use the same metadata.
            metadataHelper.Metadata
                .Setup(metadata => metadata.DisplayName)
                .Returns(displayName);
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(metadataHelper.MetadataProvider.Object);

            // Act
            var labelResult = helper.Label("Property1");
            var labelForResult = helper.LabelFor(m => m.Property1);

            // Assert
            Assert.Equal("<label for=\"Property1\">" + displayName + "</label>", labelResult.ToString());
            Assert.Equal("<label for=\"Property1\">" + displayName + "</label>", labelForResult.ToString());
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
            var metadataHelper = new MetadataHelper();
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(metadataHelper.MetadataProvider.Object);

            // Act
            var result = helper.Label(expression);

            // Assert
            // Label() falls back to expression name when DisplayName and PropertyName are null.
            Assert.Equal("<label for=\"" + expectedId + "\">" + expectedText + "</label>", result.ToString());
        }

        [Fact]
        public void LabelFor_ConsultsMetadataProviderForMetadataAboutProperty()
        {
            // Arrange
            var modelType = typeof(DefaultTemplatesUtilities.ObjectTemplateModel);
            var metadataHelper = new MetadataHelper();
            metadataHelper.MetadataProvider
                .Setup(p => p.GetMetadataForProperty(It.IsAny<Func<object>>(), modelType, "Property1"))
                .Returns(metadataHelper.Metadata.Object)
                .Verifiable();

            var helper = DefaultTemplatesUtilities.GetHtmlHelper(metadataHelper.MetadataProvider.Object);

            // Act
            var result = helper.LabelFor(m => m.Property1);

            // Assert
            metadataHelper.MetadataProvider.Verify();

            // LabelFor() falls back to expression name when DisplayName and PropertyName are null.
            Assert.Equal("<label for=\"Property1\">Property1</label>", result.ToString());
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
            Assert.Equal("<label for=\"unknownKey\">unknownKey</label>", result.ToString());
        }

        private sealed class InnerClass
        {
            public int Id { get; set; }
        }

        private sealed class OuterClass
        {
            public InnerClass Inner { get; set; }
        }

        private sealed class MetadataHelper
        {
            public Mock<ModelMetadata> Metadata { get; set; }
            public Mock<IModelMetadataProvider> MetadataProvider { get; set; }

            public MetadataHelper()
            {
                MetadataProvider = new Mock<IModelMetadataProvider>();
                Metadata = new Mock<ModelMetadata>(MetadataProvider.Object, null, null, typeof(object), null);

                MetadataProvider.Setup(p => p.GetMetadataForProperties(It.IsAny<object>(), It.IsAny<Type>()))
                    .Returns(new ModelMetadata[0]);
                MetadataProvider.Setup(p => p.GetMetadataForProperty(It.IsAny<Func<object>>(), It.IsAny<Type>(), It.IsAny<string>()))
                    .Returns(Metadata.Object);
                MetadataProvider.Setup(p => p.GetMetadataForType(It.IsAny<Func<object>>(), It.IsAny<Type>()))
                    .Returns(Metadata.Object);
            }
        }
    }
}