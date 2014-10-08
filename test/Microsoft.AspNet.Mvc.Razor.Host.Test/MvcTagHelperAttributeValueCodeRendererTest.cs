// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Reflection;
using Microsoft.AspNet.Razor.Generator;
using Microsoft.AspNet.Razor.Generator.Compiler.CSharp;
using Microsoft.AspNet.Razor.TagHelpers;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class MvcTagHelperAttributeValueCodeRendererTest
    {
        [Theory]
        [InlineData("SomeType", "SomeType", "SomeMethod(__model => __model.MyValue)")]
        [InlineData("SomeType", "SomeType2", "MyValue")]
        public void RenderAttributeValue_RendersModelExpressionsCorrectly(string modelExpressionType,
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
            var propertyInfo = new Mock<PropertyInfo>();
            propertyInfo.Setup(mock => mock.PropertyType.FullName).Returns(propertyType);
            var attributeDescriptor = new TagHelperAttributeDescriptor("MyAttribute", propertyInfo.Object);
            var writer = new CSharpCodeWriter();
            var generatorContext = new CodeGeneratorContext(host: null,
                                                            className: string.Empty,
                                                            rootNamespace: string.Empty,
                                                            sourceFile: string.Empty,
                                                            shouldGenerateLinePragmas: true);
            var context = new CodeBuilderContext(generatorContext);

            // Act
            renderer.RenderAttributeValue(attributeDescriptor, writer, context,
            (codeWriter) => {
                codeWriter.Write("MyValue");
            });

            // Assert
            Assert.Equal(expectedValue, writer.GenerateCode());
        }
    }
}