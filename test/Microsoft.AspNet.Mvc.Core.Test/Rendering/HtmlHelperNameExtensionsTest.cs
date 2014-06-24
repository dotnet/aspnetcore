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
            var idResult = helper.Id("");
            var idNullResult = helper.Id(name: null);   // null is another alias for current model
            var idForResult = helper.IdFor(m => m);
            var idForModelResult = helper.IdForModel();
            var nameResult = helper.Name("");
            var nameNullResult = helper.Name(name: null);
            var nameForResult = helper.NameFor(m => m);
            var nameForModelResult = helper.NameForModel();

            // Assert
            Assert.Empty(idResult.ToString());
            Assert.Empty(idNullResult.ToString());
            Assert.Empty(idForResult.ToString());
            Assert.Empty(idForModelResult.ToString());
            Assert.Empty(nameResult.ToString());
            Assert.Empty(nameNullResult.ToString());
            Assert.Empty(nameForResult.ToString());
            Assert.Empty(nameForModelResult.ToString());
        }

        [Theory]
        [InlineData("")]
        [InlineData("A")]
        [InlineData("A[23]")]
        [InlineData("A[0].B")]
        [InlineData("A.B.C.D")]
        public void IdAndNameHelpers_ReturnPrefixForModel(string prefix)
        {
            // Arrange
            var helper = DefaultTemplatesUtilities.GetHtmlHelper();
            helper.ViewData.TemplateInfo.HtmlFieldPrefix = prefix;

            // Act
            var idResult = helper.Id("");
            var idForResult = helper.IdFor(m => m);
            var idForModelResult = helper.IdForModel();
            var nameResult = helper.Name("");
            var nameForResult = helper.NameFor(m => m);
            var nameForModelResult = helper.NameForModel();

            // Assert
            Assert.Equal(prefix, idResult.ToString());
            Assert.Equal(prefix, idForResult.ToString());
            Assert.Equal(prefix, idForModelResult.ToString());
            Assert.Equal(prefix, nameResult.ToString());
            Assert.Equal(prefix, nameForResult.ToString());
            Assert.Equal(prefix, nameForModelResult.ToString());
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
            Assert.Equal("Property1", idResult.ToString());
            Assert.Equal("Property1", idForResult.ToString());
            Assert.Equal("Property1", nameResult.ToString());
            Assert.Equal("Property1", nameForResult.ToString());
        }

        [Theory]
        [InlineData(null, "Property1")]
        [InlineData("", "Property1")]
        [InlineData("A", "A.Property1")]
        [InlineData("A[23]", "A[23].Property1")]
        [InlineData("A[0].B", "A[0].B.Property1")]
        [InlineData("A.B.C.D", "A.B.C.D.Property1")]
        public void IdAndNameHelpers_ReturnPrefixAndPropertyName(string prefix, string expectedResult)
        {
            // Arrange
            var helper = DefaultTemplatesUtilities.GetHtmlHelper();

            // Act
            var idResult = helper.Id("Property1");
            var idForResult = helper.IdFor(m => m.Property1);
            var nameResult = helper.Name("Property1");
            var nameForResult = helper.NameFor(m => m.Property1);

            // Assert
            Assert.Equal("Property1", idResult.ToString());
            Assert.Equal("Property1", idForResult.ToString());
            Assert.Equal("Property1", nameResult.ToString());
            Assert.Equal("Property1", nameForResult.ToString());
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
            Assert.Equal("Inner.Id", idResult.ToString());
            Assert.Equal("Inner.Id", idForResult.ToString());
            Assert.Equal("Inner.Id", nameResult.ToString());
            Assert.Equal("Inner.Id", nameForResult.ToString());
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
            var idResult = helper.Id("");
            var idForResult = helper.IdFor(m => m);
            var idForModelResult = helper.IdForModel();
            var nameResult = helper.Name("");
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
        [InlineData("A")]
        [InlineData("A[0].B")]
        [InlineData("A.B.C.D")]
        public void IdAndName_ReturnExpression_EvenIfExpressionNotFound(string expression)
        {
            // Arrange
            var helper = DefaultTemplatesUtilities.GetHtmlHelper();

            // Act
            var idResult = helper.Id(expression);
            var nameResult = helper.Name(expression);

            // Assert
            Assert.Equal(expression, idResult.ToString());
            Assert.Equal(expression, nameResult.ToString());
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
            Assert.Empty(idResult.ToString());
            Assert.Empty(nameResult.ToString());
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
            Assert.Equal("unknownKey", idResult.ToString());
            Assert.Equal("unknownKey", nameResult.ToString());
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