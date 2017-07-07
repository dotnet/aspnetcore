// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Extensions
{
    public class TemplateTargetExtensionTest
    {
        [Fact]
        public void WriteTemplate_WritesTemplateCode()
        {
            // Arrange
            var node = new TemplateIntermediateNode()
            {
                Children =
                {
                    new CSharpExpressionIntermediateNode()
                }
            };
            var extension = new TemplateTargetExtension()
            {
                TemplateTypeName = "global::TestTemplate"
            };
            
            var nodeWriter = new RuntimeNodeWriter()
            {
                PushWriterMethod = "TestPushWriter",
                PopWriterMethod = "TestPopWriter"
            };

            var context = TestCodeRenderingContext.CreateRuntime(nodeWriter: nodeWriter);

            // Act
            extension.WriteTemplate(context, node);

            // Assert
            var expected = @"item => new global::TestTemplate(async(__razor_template_writer) => {
    TestPushWriter(__razor_template_writer);
    Render Children
    TestPopWriter();
}
)";

            var output = context.CodeWriter.GenerateCode();
            Assert.Equal(expected, output);
        }
    }
}
