// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Extensions
{
    public class TemplateTargetExtensionTest
    {
        [Fact]
        public void WriteTemplate_WritesTemplateCode()
        {
            // Arrange
            var node = new TemplateIntermediateNode();
            var extension = new TemplateTargetExtension()
            {
                TemplateTypeName = "global::TestTemplate"
            };

            var codeWriter = new CodeWriter();
            var nodeWriter = new RuntimeNodeWriter()
            {
                PushWriterMethod = "TestPushWriter",
                PopWriterMethod = "TestPopWriter"
            };
            var options = RazorCodeGenerationOptions.CreateDefault();
            var context = new DefaultCodeRenderingContext(codeWriter, nodeWriter, sourceDocument: null, options: options)
            {
                TagHelperWriter = new RuntimeTagHelperWriter(),
            };

            context.SetRenderChildren((n) =>
            {
                Assert.Same(node, n);
                context.CodeWriter.WriteLine(" var s = \"Inside\"");
            });

            // Act
            extension.WriteTemplate(context, node);

            // Assert
            var expected = @"item => new global::TestTemplate(async(__razor_template_writer) => {
    TestPushWriter(__razor_template_writer);
     var s = ""Inside""
    TestPopWriter();
}
)";

            var output = context.CodeWriter.Builder.ToString();
            Assert.Equal(expected, output);
        }
    }
}
