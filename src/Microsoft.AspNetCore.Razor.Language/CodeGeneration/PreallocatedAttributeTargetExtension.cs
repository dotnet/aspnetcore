// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.AspNetCore.Razor.Language.Legacy;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration
{
    internal class PreallocatedAttributeTargetExtension : IPreallocatedAttributeTargetExtension
    {
        public string TagHelperAttributeTypeName { get; set; } = "Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute";

        public string EncodedHtmlStringTypeName { get; set; } = "Microsoft.AspNetCore.Html.HtmlString";

        public string ExecutionContextVariableName { get; set; } = "__tagHelperExecutionContext";

        public string ExecutionContextAddHtmlAttributeMethodName { get; set; } = "AddHtmlAttribute";

        public string ExecutionContextAddTagHelperAttributeMethodName { get; set; } = "AddTagHelperAttribute";

        public void WriteDeclarePreallocatedTagHelperHtmlAttribute(CSharpRenderingContext context, DeclarePreallocatedTagHelperHtmlAttributeIRNode node)
        {
            context.Writer
                .Write("private static readonly global::")
                .Write(TagHelperAttributeTypeName)
                .Write(" ")
                .Write(node.VariableName)
                .Write(" = ")
                .WriteStartNewObject("global::" + TagHelperAttributeTypeName)
                .WriteStringLiteral(node.Name);

            if (node.ValueStyle == HtmlAttributeValueStyle.Minimized)
            {
                context.Writer.WriteEndMethodInvocation();
            }
            else
            {
                context.Writer
                    .WriteParameterSeparator()
                    .WriteStartNewObject("global::" + EncodedHtmlStringTypeName)
                    .WriteStringLiteral(node.Value)
                    .WriteEndMethodInvocation(endLine: false)
                    .WriteParameterSeparator()
                    .Write($"global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.{node.ValueStyle}")
                    .WriteEndMethodInvocation();
            }
        }

        public void WriteAddPreallocatedTagHelperHtmlAttribute(CSharpRenderingContext context, AddPreallocatedTagHelperHtmlAttributeIRNode node)
        {
            context.Writer
                .WriteStartInstanceMethodInvocation(ExecutionContextVariableName, ExecutionContextAddHtmlAttributeMethodName)
                .Write(node.VariableName)
                .WriteEndMethodInvocation();
        }

        public void WriteDeclarePreallocatedTagHelperAttribute(CSharpRenderingContext context, DeclarePreallocatedTagHelperAttributeIRNode node)
        {
            context.Writer
                .Write("private static readonly global::")
                .Write(TagHelperAttributeTypeName)
                .Write(" ")
                .Write(node.VariableName)
                .Write(" = ")
                .WriteStartNewObject("global::" + TagHelperAttributeTypeName)
                .WriteStringLiteral(node.Name)
                .WriteParameterSeparator()
                .WriteStringLiteral(node.Value)
                .WriteParameterSeparator()
                .Write($"global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.{node.ValueStyle}")
                .WriteEndMethodInvocation();
        }

        public void WriteSetPreallocatedTagHelperProperty(CSharpRenderingContext context, SetPreallocatedTagHelperPropertyIRNode node)
        {
            var tagHelperVariableName = GetTagHelperVariableName(node.TagHelperTypeName);
            var propertyValueAccessor = GetTagHelperPropertyAccessor(node.IsIndexerNameMatch, tagHelperVariableName, node.AttributeName, node.Descriptor);
            var attributeValueAccessor = $"{node.VariableName}.Value" /* ORIGINAL: TagHelperAttributeValuePropertyName */;
            context.Writer
                .WriteStartAssignment(propertyValueAccessor)
                .Write("(string)")
                .Write(attributeValueAccessor)
                .WriteLine(";")
                .WriteStartInstanceMethodInvocation(ExecutionContextVariableName, ExecutionContextAddTagHelperAttributeMethodName)
                .Write(node.VariableName)
                .WriteEndMethodInvocation();
        }

        private static string GetTagHelperVariableName(string tagHelperTypeName) => "__" + tagHelperTypeName.Replace('.', '_');

        private static string GetTagHelperPropertyAccessor(
            bool isIndexerNameMatch,
            string tagHelperVariableName,
            string attributeName,
            BoundAttributeDescriptor descriptor)
        {
            var propertyAccessor = $"{tagHelperVariableName}.{descriptor.Metadata[ITagHelperBoundAttributeDescriptorBuilder.PropertyNameKey]}";

            if (isIndexerNameMatch)
            {
                var dictionaryKey = attributeName.Substring(descriptor.IndexerNamePrefix.Length);
                propertyAccessor += $"[\"{dictionaryKey}\"]";
            }

            return propertyAccessor;
        }
    }
}
