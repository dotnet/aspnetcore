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
            var context = TestCodeRenderingContext.CreateRuntime();

            var node = new PreallocatedTagHelperHtmlAttributeValueIntermediateNode()
            {
                AttributeName = "Foo",
                Value = "Bar",
                AttributeStructure = AttributeStructure.DoubleQuotes,
                VariableName = "MyProp"
            };

            // Act
            extension.WriteTagHelperHtmlAttributeValue(context, node);

            // Assert
            var csharp = context.CodeWriter.Builder.ToString();
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
            var context = TestCodeRenderingContext.CreateRuntime();

            var node = new PreallocatedTagHelperHtmlAttributeValueIntermediateNode()
            {
                AttributeName = "Foo",
                Value = "Bar",
                AttributeStructure = AttributeStructure.Minimized,
                VariableName = "_tagHelper1"
            };

            // Act
            extension.WriteTagHelperHtmlAttributeValue(context, node);

            // Assert
            var csharp = context.CodeWriter.Builder.ToString();
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
            var context = TestCodeRenderingContext.CreateRuntime();

            var node = new PreallocatedTagHelperHtmlAttributeIntermediateNode()
            {
                VariableName = "_tagHelper1"
            };

            // Act
            extension.WriteTagHelperHtmlAttribute(context, node);

            // Assert
            var csharp = context.CodeWriter.Builder.ToString();
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
            var context = TestCodeRenderingContext.CreateRuntime();

            var node = new PreallocatedTagHelperPropertyValueIntermediateNode()
            {
                AttributeName = "Foo",
                Value = "Bar",
                AttributeStructure = AttributeStructure.DoubleQuotes,
                VariableName = "_tagHelper1",
            };

            // Act
            extension.WriteTagHelperPropertyValue(context, node);

            // Assert
            var csharp = context.CodeWriter.Builder.ToString();
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
            var context = TestCodeRenderingContext.CreateRuntime();

            var tagHelperBuilder = new DefaultTagHelperDescriptorBuilder(TagHelperConventions.DefaultKind, "FooTagHelper", "Test");
            tagHelperBuilder.TypeName("FooTagHelper");

            var builder = new DefaultBoundAttributeDescriptorBuilder(tagHelperBuilder, TagHelperConventions.DefaultKind);

            builder
                .Name("Foo")
                .TypeName("System.String")
                .PropertyName("FooProp");

            var descriptor = builder.Build();

            var node = new PreallocatedTagHelperPropertyIntermediateNode()
            {
                AttributeName = descriptor.Name,
                Field = "__FooTagHelper",
                VariableName = "_tagHelper1",
                BoundAttribute = descriptor,
            };

            // Act
            extension.WriteTagHelperProperty(context, node);

            // Assert
            var csharp = context.CodeWriter.Builder.ToString();
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
            var context = TestCodeRenderingContext.CreateRuntime();

            var tagHelperBuilder = new DefaultTagHelperDescriptorBuilder(TagHelperConventions.DefaultKind, "FooTagHelper", "Test");
            tagHelperBuilder.TypeName("FooTagHelper");

            var builder = new DefaultBoundAttributeDescriptorBuilder(tagHelperBuilder, TagHelperConventions.DefaultKind);

            builder
                .Name("Foo")
                .TypeName("System.Collections.Generic.Dictionary<System.String, System.String>")
                .AsDictionaryAttribute("pre-", "System.String")
                .PropertyName("FooProp");

            var descriptor = builder.Build();

            var node = new PreallocatedTagHelperPropertyIntermediateNode()
            {
                AttributeName = "pre-Foo",
                Field = "__FooTagHelper",
                VariableName = "_tagHelper1",
                BoundAttribute = descriptor,
                IsIndexerNameMatch = true,
                TagHelper = tagHelperBuilder.Build(),
            };

            // Act
            extension.WriteTagHelperProperty(context, node);

            // Assert
            var csharp = context.CodeWriter.Builder.ToString();
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
