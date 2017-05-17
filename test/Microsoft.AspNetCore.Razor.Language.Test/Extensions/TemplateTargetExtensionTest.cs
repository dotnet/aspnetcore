// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Extensions
{
    public class TemplateTargetExtensionTest
    {
        [Fact]
        public void WriteTemplate_WritesTemplateCode()
        {
            // Arrange
            var node = new TemplateIRNode();

            var extension = new TemplateTargetExtension()
            {
                TemplateTypeName = "global::TestTemplate",
                PushWriterMethod = "TestPushWriter",
                PopWriterMethod = "TestPopWriter"
            };

            var context = new CSharpRenderingContext()
            { 
                BasicWriter = new RuntimeBasicWriter(),
                TagHelperWriter = new RuntimeTagHelperWriter(),
                Writer = new CSharpCodeWriter(),
                Options = RazorCodeGenerationOptions.CreateDefault(),
            };

            context.RenderChildren = (n) =>
            {
                Assert.Same(node, n);
                context.Writer.WriteLine(" var s = \"Inside\"");
            };

            // Act
            extension.WriteTemplate(context, node);

            // Assert
            var expected = @"item => new global::TestTemplate(async(__razor_template_writer) => {
    TestPushWriter(__razor_template_writer);
     var s = ""Inside""
    TestPopWriter();
}
)";

            var output = context.Writer.Builder.ToString();
            Assert.Equal(expected, output);
        }
    }
}
