// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Extensions
{
    public class PreallocatedAttributeTargetExtensionTest
    {
        [Fact]
        public void WriteDeclarePreallocatedTagHelperHtmlAttribute_RendersCorrectly()
        {
            // Arrange
            var extension = new PreallocatedAttributeTargetExtension();
            var context = new CSharpRenderingContext()
            {
                Writer = new CSharpCodeWriter()
            };

            var node = new DeclarePreallocatedTagHelperHtmlAttributeIntermediateNode()
            {
                Name = "Foo",
                Value = "Bar",
                AttributeStructure = AttributeStructure.DoubleQuotes,
                VariableName = "MyProp"
            };

            // Act
            extension.WriteDeclarePreallocatedTagHelperHtmlAttribute(context, node);

            // Assert
            var csharp = context.Writer.Builder.ToString();
            Assert.Equal(
@"private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute MyProp = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute(""Foo"", new global::Microsoft.AspNetCore.Html.HtmlString(""Bar""), global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void WriteDeclarePreallocatedTagHelperHtmlAttribute_Minimized_RendersCorrectly()
        {
            // Arrange
            var extension = new PreallocatedAttributeTargetExtension();
            var context = new CSharpRenderingContext()
            {
                Writer = new CSharpCodeWriter()
            };

            var node = new DeclarePreallocatedTagHelperHtmlAttributeIntermediateNode()
            {
                Name = "Foo",
                Value = "Bar",
                AttributeStructure = AttributeStructure.Minimized,
                VariableName = "_tagHelper1"
            };

            // Act
            extension.WriteDeclarePreallocatedTagHelperHtmlAttribute(context, node);

            // Assert
            var csharp = context.Writer.Builder.ToString();
            Assert.Equal(
@"private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute _tagHelper1 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute(""Foo"");
",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void WriteAddPreallocatedTagHelperHtmlAttribute_RendersCorrectly()
        {
            // Arrange
            var extension = new PreallocatedAttributeTargetExtension();
            var context = new CSharpRenderingContext()
            {
                Writer = new CSharpCodeWriter()
            };

            var node = new AddPreallocatedTagHelperHtmlAttributeIntermediateNode()
            {
                VariableName = "_tagHelper1"
            };

            // Act
            extension.WriteAddPreallocatedTagHelperHtmlAttribute(context, node);

            // Assert
            var csharp = context.Writer.Builder.ToString();
            Assert.Equal(
@"__tagHelperExecutionContext.AddHtmlAttribute(_tagHelper1);
",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void WriteDeclarePreallocatedTagHelperAttribute_RendersCorrectly()
        {
            // Arrange
            var extension = new PreallocatedAttributeTargetExtension();
            var context = new CSharpRenderingContext()
            {
                Writer = new CSharpCodeWriter()
            };

            var node = new DeclarePreallocatedTagHelperAttributeIntermediateNode()
            {
                Name = "Foo",
                Value = "Bar",
                AttributeStructure = AttributeStructure.DoubleQuotes,
                VariableName = "_tagHelper1",
            };

            // Act
            extension.WriteDeclarePreallocatedTagHelperAttribute(context, node);

            // Assert
            var csharp = context.Writer.Builder.ToString();
            Assert.Equal(
@"private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute _tagHelper1 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute(""Foo"", ""Bar"", global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void WriteSetPreallocatedTagHelperProperty_RendersCorrectly()
        {
            // Arrange
            var extension = new PreallocatedAttributeTargetExtension();
            var context = new CSharpRenderingContext()
            {
                Writer = new CSharpCodeWriter()
            };

            var tagHelperBuilder = new DefaultTagHelperDescriptorBuilder(TagHelperConventions.DefaultKind, "FooTagHelper", "Test");
            tagHelperBuilder.TypeName("FooTagHelper");

            var builder = new DefaultBoundAttributeDescriptorBuilder(tagHelperBuilder, TagHelperConventions.DefaultKind);

            builder
                .Name("Foo")
                .TypeName("System.String")
                .PropertyName("FooProp");
            
            var descriptor = builder.Build();

            var node = new SetPreallocatedTagHelperPropertyIntermediateNode()
            {
                AttributeName = descriptor.Name,
                TagHelperTypeName = "FooTagHelper",
                VariableName = "_tagHelper1",
                Descriptor = descriptor,
            };

            // Act
            extension.WriteSetPreallocatedTagHelperProperty(context, node);

            // Assert
            var csharp = context.Writer.Builder.ToString();
            Assert.Equal(
@"__FooTagHelper.FooProp = (string)_tagHelper1.Value;
__tagHelperExecutionContext.AddTagHelperAttribute(_tagHelper1);
",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void WriteSetPreallocatedTagHelperProperty_IndexerAttribute_RendersCorrectly()
        {
            // Arrange
            var extension = new PreallocatedAttributeTargetExtension();
            var context = new CSharpRenderingContext()
            {
                Writer = new CSharpCodeWriter(),
                TagHelperRenderingContext = new TagHelperRenderingContext()
            };

            var tagHelperBuilder = new DefaultTagHelperDescriptorBuilder(TagHelperConventions.DefaultKind, "FooTagHelper", "Test");
            tagHelperBuilder.TypeName("FooTagHelper");

            var builder = new DefaultBoundAttributeDescriptorBuilder(tagHelperBuilder, TagHelperConventions.DefaultKind);

            builder
                .Name("Foo")
                .TypeName("System.Collections.Generic.Dictionary<System.String, System.String>")
                .AsDictionary("pre-", "System.String")
                .PropertyName("FooProp");

            var descriptor = builder.Build();

            var node = new SetPreallocatedTagHelperPropertyIntermediateNode()
            {
                AttributeName = "pre-Foo",
                TagHelperTypeName = "FooTagHelper",
                VariableName = "_tagHelper1",
                Descriptor = descriptor,
                IsIndexerNameMatch = true
            };

            // Act
            extension.WriteSetPreallocatedTagHelperProperty(context, node);

            // Assert
            var csharp = context.Writer.Builder.ToString();
            Assert.Equal(
@"if (__FooTagHelper.FooProp == null)
{
    throw new InvalidOperationException(InvalidTagHelperIndexerAssignment(""pre-Foo"", ""FooTagHelper"", ""FooProp""));
}
__FooTagHelper.FooProp[""Foo""] = (string)_tagHelper1.Value;
__tagHelperExecutionContext.AddTagHelperAttribute(_tagHelper1);
",
                csharp,
                ignoreLineEndingDifferences: true);
        }
    }
}
