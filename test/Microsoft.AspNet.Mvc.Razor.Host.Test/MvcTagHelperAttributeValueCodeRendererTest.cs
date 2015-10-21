// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor;
using Microsoft.AspNet.Razor.Chunks.Generators;
using Microsoft.AspNet.Razor.CodeGenerators;
using Microsoft.AspNet.Razor.Compilation.TagHelpers;
using Xunit;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class MvcTagHelperAttributeValueCodeRendererTest
    {
        [Theory]
        [InlineData("SomeType", "SomeType", "SomeMethod(__model => __model.MyValue)")]
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
                    CreateModelExpressionMethodName = "SomeMethod"
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