// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration
{
    public class DesignTimeTagHelperWriterTest
    {
        [Fact]
        public void WriteDeclareTagHelperFields_DeclaresUsedTagHelperTypes()
        {
            // Arrange
            var writer = new DesignTimeTagHelperWriter();
            var context = new CSharpRenderingContext()
            {
                Writer = new Legacy.CSharpCodeWriter(),
            };
            var node = new DeclareTagHelperFieldsIRNode();
            node.UsedTagHelperTypeNames.Add("PTagHelper");
            node.UsedTagHelperTypeNames.Add("MyTagHelper");

            // Act
            writer.WriteDeclareTagHelperFields(context, node);

            // Assert
            var csharp = context.Writer.Builder.ToString();
            Assert.Equal(
@"private global::PTagHelper __PTagHelper = null;
private global::MyTagHelper __MyTagHelper = null;
",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void WriteCreateTagHelper_RendersCorrectly_UsesSpecifiedTagHelperType()
        {
            // Arrange
            var writer = new DesignTimeTagHelperWriter();
            var context = new CSharpRenderingContext()
            {
                Writer = new Legacy.CSharpCodeWriter(),
            };
            var node = new CreateTagHelperIRNode()
            {
                TagHelperTypeName = "TestNamespace.MyTagHelper"
            };

            // Act
            writer.WriteCreateTagHelper(context, node);

            // Assert
            var csharp = context.Writer.Builder.ToString();
            Assert.Equal(
@"__TestNamespace_MyTagHelper = CreateTagHelper<global::TestNamespace.MyTagHelper>();
",
                csharp,
                ignoreLineEndingDifferences: true);
        }
    }
}
