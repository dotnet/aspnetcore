// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

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
            if (context.Parent as TagHelperIntermediateNode == null)
            {
                var message = Resources.FormatIntermediateNodes_InvalidParentNode(node.GetType(), typeof(TagHelperIntermediateNode));
                throw new InvalidOperationException(message);
            }

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
            var tagHelperNode = context.Parent as TagHelperIntermediateNode;
            if (tagHelperNode == null)
            {
                var message = Resources.FormatIntermediateNodes_InvalidParentNode(node.GetType(), typeof(TagHelperIntermediateNode));
                throw new InvalidOperationException(message);
            }

            // Ensure that the property we're trying to set has initialized its dictionary bound properties.
            if (node.IsIndexerNameMatch &&
                object.ReferenceEquals(FindFirstUseOfIndexer(tagHelperNode, node), node))
            {
                // Throw a reasonable Exception at runtime if the dictionary property is null.
                context.CodeWriter
                    .Write("if (")
                    .Write(node.Field)
                    .Write(".")
                    .Write(node.Property)
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
                        .WriteStringLiteral(node.Property)
                        .WriteEndMethodInvocation(endLine: false)   // End of method call
                        .WriteEndMethodInvocation();   // End of new expression / throw statement
                }
            }

            context.CodeWriter
                .WriteStartAssignment(GetPropertyAccessor(node))
                .Write("(string)")
                .Write($"{node.VariableName}.Value")
                .WriteLine(";")
                .WriteStartInstanceMethodInvocation(ExecutionContextVariableName, ExecutionContextAddTagHelperAttributeMethodName)
                .Write(node.VariableName)
                .WriteEndMethodInvocation();
        }

        private static PreallocatedTagHelperPropertyIntermediateNode FindFirstUseOfIndexer(
            TagHelperIntermediateNode tagHelperNode,
            PreallocatedTagHelperPropertyIntermediateNode propertyNode)
        {
            Debug.Assert(tagHelperNode.Children.Contains(propertyNode));
            Debug.Assert(propertyNode.IsIndexerNameMatch);

            for (var i = 0; i < tagHelperNode.Children.Count; i++)
            {
                if (tagHelperNode.Children[i] is PreallocatedTagHelperPropertyIntermediateNode otherPropertyNode &&
                    otherPropertyNode.TagHelper.Equals(propertyNode.TagHelper) &&
                    otherPropertyNode.BoundAttribute.Equals(propertyNode.BoundAttribute) &&
                    otherPropertyNode.IsIndexerNameMatch)
                {
                    return otherPropertyNode;
                }
            }

            // This is unreachable, we should find 'propertyNode' in the list of children.
            throw new InvalidOperationException();
        }

        private static string GetPropertyAccessor(PreallocatedTagHelperPropertyIntermediateNode node)
        {
            var propertyAccessor = $"{node.Field}.{node.Property}";

            if (node.IsIndexerNameMatch)
            {
                var dictionaryKey = node.AttributeName.Substring(node.BoundAttribute.IndexerNamePrefix.Length);
                propertyAccessor += $"[\"{dictionaryKey}\"]";
            }

            return propertyAccessor;
        }
    }
}
