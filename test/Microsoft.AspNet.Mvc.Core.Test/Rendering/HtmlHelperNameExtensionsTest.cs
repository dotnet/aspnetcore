// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Rendering;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Core
{
    /// <summary>
    /// Test the <see cref="HtmlHelperNameExtensions" /> class.
    /// </summary>
    /// <remarks>
    /// TODO #704: When that bug is fixed and Id() behaves differently than Name(), will need to break some
    /// test methods below in two.
    /// </remarks>
    public class HtmlHelperNameExtensionsTest
    {
        [Fact]
        public void IdAndNameHelpers_ReturnEmptyForModel()
        {
            // Arrange
            var helper = DefaultTemplatesUtilities.GetHtmlHelper();

            // Act
            var idResult = helper.Id(name: string.Empty);
            var idNullResult = helper.Id(name: null);   // null is another alias for current model
            var idForResult = helper.IdFor(m => m);
            var idForModelResult = helper.IdForModel();
            var nameResult = helper.Name(name: string.Empty);
            var nameNullResult = helper.Name(name: null);
            var nameForResult = helper.NameFor(m => m);
            var nameForModelResult = helper.NameForModel();

            // Assert
            Assert.Empty(idResult);
            Assert.Empty(idNullResult);
            Assert.Empty(idForResult);
            Assert.Empty(idForModelResult);
            Assert.Empty(nameResult);
            Assert.Empty(nameNullResult);
            Assert.Empty(nameForResult);
            Assert.Empty(nameForModelResult);
        }

        [Theory]
        [InlineData("", "")]
        [InlineData("A", "A")]
        [InlineData("A[23]", "A_23_")]
        [InlineData("A[0].B", "A_0__B")]
        [InlineData("A.B.C.D", "A_B_C_D")]
        public void IdAndNameHelpers_ReturnPrefixForModel(string prefix, string expectedId)
        {
            // Arrange
            var helper = DefaultTemplatesUtilities.GetHtmlHelper();
            helper.ViewData.TemplateInfo.HtmlFieldPrefix = prefix;

            // Act
            var idResult = helper.Id(name: string.Empty);
            var idForResult = helper.IdFor(m => m);
            var idForModelResult = helper.IdForModel();
            var nameResult = helper.Name(name: string.Empty);
            var nameForResult = helper.NameFor(m => m);
            var nameForModelResult = helper.NameForModel();

            // Assert
            Assert.Equal(expectedId, idResult);
            Assert.Equal(expectedId, idForResult);
            Assert.Equal(expectedId, idForModelResult);
            Assert.Equal(prefix, nameResult);
            Assert.Equal(prefix, nameForResult);
            Assert.Equal(prefix, nameForModelResult);
        }

        [Fact]
        public void IdAndNameHelpers_ReturnPropertyName()
        {
            // Arrange
            var helper = DefaultTemplatesUtilities.GetHtmlHelper();

            // Act
            var idResult = helper.Id("Property1");
            var idForResult = helper.IdFor(m => m.Property1);
            var nameResult = helper.Name("Property1");
            var nameForResult = helper.NameFor(m => m.Property1);

            // Assert
            Assert.Equal("Property1", idResult);
            Assert.Equal("Property1", idForResult);
            Assert.Equal("Property1", nameResult);
            Assert.Equal("Property1", nameForResult);
        }

        [Theory]
        [InlineData(null, "Property1", "Property1")]
        [InlineData("", "Property1", "Property1")]
        [InlineData("A", "A.Property1", "A_Property1")]
        [InlineData("A[23]", "A[23].Property1", "A_23__Property1")]
        [InlineData("A[0].B", "A[0].B.Property1", "A_0__B_Property1")]
        [InlineData("A.B.C.D", "A.B.C.D.Property1", "A_B_C_D_Property1")]
        public void IdAndNameHelpers_ReturnPrefixAndPropertyName(string prefix, string expectedName, string expectedId)
        {
            // Arrange
            var helper = DefaultTemplatesUtilities.GetHtmlHelper();
            helper.ViewData.TemplateInfo.HtmlFieldPrefix = prefix;

            // Act
            var idResult = helper.Id("Property1");
            var idForResult = helper.IdFor(m => m.Property1);
            var nameResult = helper.Name("Property1");
            var nameForResult = helper.NameFor(m => m.Property1);

            // Assert
            Assert.Equal(expectedId, idResult);
            Assert.Equal(expectedId, idForResult);
            Assert.Equal(expectedName, nameResult);
            Assert.Equal(expectedName, nameForResult);
        }

        [Fact]
        public void IdAndNameHelpers_ReturnPropertyPath_ForNestedProperty()
        {
            // Arrange
            var helper = DefaultTemplatesUtilities.GetHtmlHelper<OuterClass>(model: null);

            // Act
            var idResult = helper.Id("Inner.Id");
            var idForResult = helper.IdFor(m => m.Inner.Id);
            var nameResult = helper.Name("Inner.Id");
            var nameForResult = helper.NameFor(m => m.Inner.Id);

            // Assert
            Assert.Equal("Inner_Id", idResult);
            Assert.Equal("Inner_Id", idForResult);
            Assert.Equal("Inner.Id", nameResult);
            Assert.Equal("Inner.Id", nameForResult);
        }

        [Fact]
        public void IdAndNameHelpers_DoNotConsultMetadataOrMetadataProvider()
        {
            // Arrange
            var provider = new Mock<IModelMetadataProvider>(MockBehavior.Strict);
            var metadata =
                new Mock<ModelMetadata>(MockBehavior.Strict, provider.Object, null, null, typeof(object), null);
            provider
                .Setup(m => m.GetMetadataForType(
                    It.IsAny<Func<object>>(),
                    typeof(DefaultTemplatesUtilities.ObjectTemplateModel)))
                .Returns(metadata.Object);
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(provider.Object);

            // Act (do not throw)
            var idResult = helper.Id(name: string.Empty);
            var idForResult = helper.IdFor(m => m);
            var idForModelResult = helper.IdForModel();
            var nameResult = helper.Name(name: string.Empty);
            var nameForResult = helper.NameFor(m => m);
            var nameForModelResult = helper.NameForModel();

            // Assert
            // Only the ViewDataDictionary should do anything with metadata.
            provider.Verify(
                m => m.GetMetadataForType(It.IsAny<Func<object>>(), typeof(DefaultTemplatesUtilities.ObjectTemplateModel)),
                Times.Once);
        }

        [Fact]
        public void IdAndNameHelpers_DoNotConsultMetadataOrMetadataProvider_ForProperty()
        {
            // Arrange
            var provider = new Mock<IModelMetadataProvider>(MockBehavior.Strict);
            var metadata =
                new Mock<ModelMetadata>(MockBehavior.Strict, provider.Object, null, null, typeof(object), null);
            provider
                .Setup(m => m.GetMetadataForType(
                    It.IsAny<Func<object>>(),
                    typeof(DefaultTemplatesUtilities.ObjectTemplateModel)))
                .Returns(metadata.Object);
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(provider.Object);

            // Act (do not throw)
            var idResult = helper.Id("Property1");
            var idForResult = helper.IdFor(m => m.Property1);
            var nameResult = helper.Name("Property1");
            var nameForResult = helper.NameFor(m => m.Property1);

            // Assert
            // Only the ViewDataDictionary should do anything with metadata.
            provider.Verify(
                m => m.GetMetadataForType(It.IsAny<Func<object>>(), typeof(DefaultTemplatesUtilities.ObjectTemplateModel)),
                Times.Once);
        }

        [Theory]
        [InlineData("A", "A")]
        [InlineData("A[0].B", "A_0__B")]
        [InlineData("A.B.C.D", "A_B_C_D")]
        public void IdAndName_ReturnExpression_EvenIfExpressionNotFound(string expression, string expectedId)
        {
            // Arrange
            var helper = DefaultTemplatesUtilities.GetHtmlHelper();

            // Act
            var idResult = helper.Id(expression);
            var nameResult = helper.Name(expression);

            // Assert
            Assert.Equal(expectedId, idResult);
            Assert.Equal(expression, nameResult);
        }

        [Fact]
        public void IdForAndNameFor_ReturnEmpty_IfExpressionUnsupported()
        {
            // Arrange
            var helper = DefaultTemplatesUtilities.GetHtmlHelper();

            // Act
            var idResult = helper.IdFor(model => new { foo = "Bar" });
            var nameResult = helper.NameFor(model => new { foo = "Bar" });

            // Assert
            Assert.Empty(idResult);
            Assert.Empty(nameResult);
        }

        [Fact]
        public void IdForAndNameFor_ReturnVariableName()
        {
            // Arrange
            var unknownKey = "this is a dummy parameter value";
            var helper = DefaultTemplatesUtilities.GetHtmlHelper();

            // Act
            var idResult = helper.IdFor(model => unknownKey);
            var nameResult = helper.NameFor(model => unknownKey);

            // Assert
            Assert.Equal("unknownKey", idResult);
            Assert.Equal("unknownKey", nameResult);
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