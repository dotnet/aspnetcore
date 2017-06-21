// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;

namespace Microsoft.AspNetCore.Razor.Language.Extensions
{
    internal class PreallocatedAttributeTargetExtension : IPreallocatedAttributeTargetExtension
    {
        public string TagHelperAttributeTypeName { get; set; } = "Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute";

        public string EncodedHtmlStringTypeName { get; set; } = "Microsoft.AspNetCore.Html.HtmlString";

        public string ExecutionContextVariableName { get; set; } = "__tagHelperExecutionContext";

        public string ExecutionContextAddHtmlAttributeMethodName { get; set; } = "AddHtmlAttribute";

        public string ExecutionContextAddTagHelperAttributeMethodName { get; set; } = "AddTagHelperAttribute";

        public string FormatInvalidIndexerAssignmentMethodName { get; set; } = "InvalidTagHelperIndexerAssignment";

        public void WriteTagHelperHtmlAttributeValue(CodeRenderingContext context, PreallocatedTagHelperHtmlAttributeValueIntermediateNode node)
        {
            context.CodeWriter
                .Write("private static readonly global::")
                .Write(TagHelperAttributeTypeName)
                .Write(" ")
                .Write(node.VariableName)
                .Write(" = ")
                .WriteStartNewObject("global::" + TagHelperAttributeTypeName)
                .WriteStringLiteral(node.AttributeName);

            if (node.AttributeStructure == AttributeStructure.Minimized)
            {
                context.CodeWriter.WriteEndMethodInvocation();
            }
            else
            {
                context.CodeWriter
                    .WriteParameterSeparator()
                    .WriteStartNewObject("global::" + EncodedHtmlStringTypeName)
                    .WriteStringLiteral(node.Value)
                    .WriteEndMethodInvocation(endLine: false)
                    .WriteParameterSeparator()
                    .Write($"global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.{node.AttributeStructure}")
                    .WriteEndMethodInvocation();
            }
        }

        public void WriteTagHelperHtmlAttribute(CodeRenderingContext context, PreallocatedTagHelperHtmlAttributeIntermediateNode node)
        {
            context.CodeWriter
                .WriteStartInstanceMethodInvocation(ExecutionContextVariableName, ExecutionContextAddHtmlAttributeMethodName)
                .Write(node.VariableName)
                .WriteEndMethodInvocation();
        }

        public void WriteTagHelperPropertyValue(CodeRenderingContext context, PreallocatedTagHelperPropertyValueIntermediateNode node)
        {
            context.CodeWriter
                .Write("private static readonly global::")
                .Write(TagHelperAttributeTypeName)
                .Write(" ")
                .Write(node.VariableName)
                .Write(" = ")
                .WriteStartNewObject("global::" + TagHelperAttributeTypeName)
                .WriteStringLiteral(node.AttributeName)
                .WriteParameterSeparator()
                .WriteStringLiteral(node.Value)
                .WriteParameterSeparator()
                .Write($"global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.{node.AttributeStructure}")
                .WriteEndMethodInvocation();
        }

        public void WriteTagHelperProperty(CodeRenderingContext context, PreallocatedTagHelperPropertyIntermediateNode node)
        {
            var propertyName = node.BoundAttribute.GetPropertyName();
            var propertyValueAccessor = GetTagHelperPropertyAccessor(node.IsIndexerNameMatch, node.Field, node.AttributeName, node.BoundAttribute);
            var attributeValueAccessor = $"{node.VariableName}.Value" /* ORIGINAL: TagHelperAttributeValuePropertyName */;

            // Ensure that the property we're trying to set has initialized its dictionary bound properties.
            if (node.IsIndexerNameMatch &&
                context.TagHelperRenderingContext.VerifiedPropertyDictionaries.Add($"{node.TagHelper.GetTypeName()}.{propertyName}"))
            {
                // Throw a reasonable Exception at runtime if the dictionary property is null.
                context.CodeWriter
                    .Write("if (")
                    .Write(node.Field)
                    .Write(".")
                    .Write(propertyName)
                    .WriteLine(" == null)");
                using (context.CodeWriter.BuildScope())
                {
                    // System is in Host.NamespaceImports for all MVC scenarios. No need to generate FullName
                    // of InvalidOperationException type.
                    context.CodeWriter
                        .Write("throw ")
                        .WriteStartNewObject(nameof(InvalidOperationException))
                        .WriteStartMethodInvocation(FormatInvalidIndexerAssignmentMethodName)
                        .WriteStringLiteral(node.AttributeName)
                        .WriteParameterSeparator()
                        .WriteStringLiteral(node.TagHelper.GetTypeName())
                        .WriteParameterSeparator()
                        .WriteStringLiteral(propertyName)
                        .WriteEndMethodInvocation(endLine: false)   // End of method call
                        .WriteEndMethodInvocation();   // End of new expression / throw statement
                }
            }

            context.CodeWriter
                .WriteStartAssignment(propertyValueAccessor)
                .Write("(string)")
                .Write(attributeValueAccessor)
                .WriteLine(";")
                .WriteStartInstanceMethodInvocation(ExecutionContextVariableName, ExecutionContextAddTagHelperAttributeMethodName)
                .Write(node.VariableName)
                .WriteEndMethodInvocation();
        }

        private static string GetTagHelperPropertyAccessor(
            bool isIndexerNameMatch,
            string tagHelperVariableName,
            string attributeName,
            BoundAttributeDescriptor descriptor)
        {
            var propertyAccessor = $"{tagHelperVariableName}.{descriptor.GetPropertyName()}";

            if (isIndexerNameMatch)
            {
                var dictionaryKey = attributeName.Substring(descriptor.IndexerNamePrefix.Length);
                propertyAccessor += $"[\"{dictionaryKey}\"]";
            }

            return propertyAccessor;
        }
    }
}
