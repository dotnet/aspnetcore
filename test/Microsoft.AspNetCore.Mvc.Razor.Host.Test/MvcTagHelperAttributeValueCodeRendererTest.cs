// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Chunks.Generators;
using Microsoft.AspNetCore.Razor.CodeGenerators;
using Microsoft.AspNetCore.Razor.Compilation.TagHelpers;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor
{
    public class MvcTagHelperAttributeValueCodeRendererTest
    {
        [Theory]
        [InlineData("SomeType", "SomeType", "Provider.SomeMethod(ViewData, __model => __model.MyValue)")]
        [InlineData("SomeType", "SomeType2", "MyValue")]
        public void RenderAttributeValue_RendersModelExpressionsCorrectly(
            string modelExpressionType,
            string propertyType,
            string expectedValue)
        {
            // Arrange
            var renderer = new MvcTagHelperAttributeValueCodeRenderer(
                new GeneratedTagHelperAttributeContext
                {
                    ModelExpressionTypeName = modelExpressionType,
                    CreateModelExpressionMethodName = "SomeMethod",
                    ModelExpressionProviderPropertyName = "Provider",
                    ViewDataPropertyName = "ViewData"
                });
            var attributeDescriptor = new TagHelperAttributeDescriptor
            {
                Name = "MyAttribute",
                PropertyName = "SomeProperty",
                TypeName = propertyType,
            };
            var writer = new CSharpCodeWriter();
            var generatorContext = new ChunkGeneratorContext(
                host: null,
                className: string.Empty,
                rootNamespace: string.Empty,
                sourceFile: string.Empty,
                shouldGenerateLinePragmas: true);
            var errorSink = new ErrorSink();
            var context = new CodeGeneratorContext(generatorContext, errorSink);

            // Act
            renderer.RenderAttributeValue(attributeDescriptor, writer, context,
            (codeWriter) =>
            {
                codeWriter.Write("MyValue");
            },
            complexValue: false);

            // Assert
            Assert.Equal(expectedValue, writer.GenerateCode());
        }
    }
}