// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration
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

            var node = new DeclarePreallocatedTagHelperHtmlAttributeIRNode()
            {
                Name = "Foo",
                Value = "Bar",
                ValueStyle = HtmlAttributeValueStyle.DoubleQuotes,
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

            var node = new DeclarePreallocatedTagHelperHtmlAttributeIRNode()
            {
                Name = "Foo",
                Value = "Bar",
                ValueStyle = HtmlAttributeValueStyle.Minimized,
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

            var node = new AddPreallocatedTagHelperHtmlAttributeIRNode()
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

            var node = new DeclarePreallocatedTagHelperAttributeIRNode()
            {
                Name = "Foo",
                Value = "Bar",
                ValueStyle = HtmlAttributeValueStyle.DoubleQuotes,
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

            var descriptor = ITagHelperBoundAttributeDescriptorBuilder
                .Create("FooTagHelper")
                .Name("Foo")
                .TypeName("System.String")
                .PropertyName("FooProp")
                .Build();

            var node = new SetPreallocatedTagHelperPropertyIRNode()
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
                Writer = new CSharpCodeWriter()
            };

            var descriptor = ITagHelperBoundAttributeDescriptorBuilder
                .Create("FooTagHelper")
                .Name("Foo")
                .TypeName("System.Collections.Generic.Dictionary<System.String, System.String>")
                .AsDictionary("pre-", "System.String")
                .PropertyName("FooProp")
                .Build();

            var node = new SetPreallocatedTagHelperPropertyIRNode()
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
@"__FooTagHelper.FooProp[""Foo""] = (string)_tagHelper1.Value;
__tagHelperExecutionContext.AddTagHelperAttribute(_tagHelper1);
",
                csharp,
                ignoreLineEndingDifferences: true);
        }
    }
}
