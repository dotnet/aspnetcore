// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Xunit;
using static Microsoft.AspNetCore.Razor.Language.Intermediate.IntermediateNodeAssert;

namespace Microsoft.AspNetCore.Razor.Language.Extensions
{
    public class SectionTargetExtensionTest
    {
        [Fact]
        public void WriteSection_WritesSectionCode()
        {
            // Arrange
            var node = new SectionIntermediateNode()
            {
                Name = "MySection"
            };

            var extension = new SectionTargetExtension()
            {
                SectionMethodName = "CreateSection"
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
            extension.WriteSection(context, node);

            // Assert
            var expected = @"CreateSection(""MySection"", async() => {
     var s = ""Inside""
}
);
";

            var output = context.Writer.Builder.ToString();
            Assert.Equal(expected, output);
        }

        [Fact]
        public void WriteSection_WritesSectionCode_DesignTime()
        {
            // Arrange
            var node = new SectionIntermediateNode()
            {
                Name = "MySection"
            };

            var extension = new SectionTargetExtension()
            {
                SectionMethodName = "CreateSection"
            };

            var context = new CSharpRenderingContext()
            {
                BasicWriter = new RuntimeBasicWriter(),
                TagHelperWriter = new RuntimeTagHelperWriter(),
                Writer = new CSharpCodeWriter(),
                Options = RazorCodeGenerationOptions.Create(false, 4, true, false),
            };

            context.RenderChildren = (n) =>
            {
                Assert.Same(node, n);
                context.Writer.WriteLine(" var s = \"Inside\"");
            };

            // Act
            extension.WriteSection(context, node);

            // Assert
            var expected = @"CreateSection(""MySection"", async(__razor_section_writer) => {
     var s = ""Inside""
}
);
";

            var output = context.Writer.Builder.ToString();
            Assert.Equal(expected, output);
        }
    }
}
