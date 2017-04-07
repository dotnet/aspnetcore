// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration
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
            };

            var context = new CSharpRenderingContext()
            { 
                BasicWriter = new RuntimeBasicWriter(),
                TagHelperWriter = new RuntimeTagHelperWriter(),
                Writer = new CSharpCodeWriter(),
                Options = RazorParserOptions.CreateDefaultOptions()
            };

            context.RenderChildren = (n) =>
            {
                Assert.Same(node, n);

                var conventions = Assert.IsType<CSharpRedirectRenderingConventions>(context.RenderingConventions);
                Assert.Equal("__razor_template_writer", conventions.RedirectWriter);

                context.Writer.Write(" var s = \"Inside\"");
            };

            // Act
            extension.WriteTemplate(context, node);

            // Assert
            var expected = @"item => new global::TestTemplate(async(__razor_template_writer) => {
     var s = ""Inside""
}
)";

            var output = context.Writer.Builder.ToString();
            Assert.Equal(expected, output);
        }
    }
}
