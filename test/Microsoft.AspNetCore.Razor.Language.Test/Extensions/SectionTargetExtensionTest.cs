// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
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
                Children =
                {
                    new CSharpExpressionIntermediateNode(),
                },
                Name = "MySection"
            };

            var extension = new SectionTargetExtension()
            {
                SectionMethodName = "CreateSection"
            };

            var context = TestCodeRenderingContext.CreateRuntime();

            // Act
            extension.WriteSection(context, node);

            // Assert
            var expected = @"CreateSection(""MySection"", async() => {
    Render Children
}
);
";

            var output = context.CodeWriter.GenerateCode();
            Assert.Equal(expected, output);
        }

        [Fact]
        public void WriteSection_WritesSectionCode_DesignTime()
        {
            // Arrange
            var node = new SectionIntermediateNode()
            {
                Children =
                {
                    new CSharpExpressionIntermediateNode(),
                },
                Name = "MySection"
            };

            var extension = new SectionTargetExtension()
            {
                SectionMethodName = "CreateSection"
            };

            var context = TestCodeRenderingContext.CreateDesignTime();

            // Act
            extension.WriteSection(context, node);

            // Assert
            var expected = @"CreateSection(""MySection"", async(__razor_section_writer) => {
    Render Children
}
);
";

            var output = context.CodeWriter.GenerateCode();
            Assert.Equal(expected, output);
        }
    }
}
