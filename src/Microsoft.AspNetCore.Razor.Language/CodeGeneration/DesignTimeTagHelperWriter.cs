// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.AspNetCore.Razor.Language.Legacy;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration
{
    internal class DesignTimeTagHelperWriter : TagHelperWriter
    {
        public string CreateTagHelperMethodName { get; set; } = "CreateTagHelper";

        public override void WriteDeclareTagHelperFields(CodeRenderingContext context, DeclareTagHelperFieldsIntermediateNode node)
        {
            foreach (var tagHelperTypeName in node.UsedTagHelperTypeNames)
            {
                var tagHelperVariableName = GetTagHelperVariableName(tagHelperTypeName);
                context.CodeWriter
                    .Write("private global::")
                    .WriteVariableDeclaration(
                        tagHelperTypeName,
                        tagHelperVariableName,
                        value: null);
            }
        }

        public override void WriteTagHelper(CodeRenderingContext context, TagHelperIntermediateNode node)
        {
            context.RenderChildren(node);
        }

        public override void WriteTagHelperBody(CodeRenderingContext context, TagHelperBodyIntermediateNode node)
        {
            context.RenderChildren(node);
        }

        public override void WriteCreateTagHelper(CodeRenderingContext context, CreateTagHelperIntermediateNode node)
        {
            var tagHelperVariableName = GetTagHelperVariableName(node.TagHelperTypeName);

            context.CodeWriter
                .WriteStartAssignment(tagHelperVariableName)
                .Write(CreateTagHelperMethodName)
                .WriteLine($"<global::{node.TagHelperTypeName}>();");
        }

        public override void WriteAddTagHelperHtmlAttribute(CodeRenderingContext context, AddTagHelperHtmlAttributeIntermediateNode node)
        {
            context.RenderChildren(node);
        }

        public override void WriteSetTagHelperProperty(CodeRenderingContext context, SetTagHelperPropertyIntermediateNode node)
        {
            var tagHelperVariableName = GetTagHelperVariableName(node.TagHelperTypeName);
            var tagHelperRenderingContext = context.TagHelperRenderingContext;
            var propertyValueAccessor = GetTagHelperPropertyAccessor(node.IsIndexerNameMatch, tagHelperVariableName, node.AttributeName, node.Descriptor);

            if (tagHelperRenderingContext.RenderedBoundAttributes.TryGetValue(node.AttributeName, out string previousValueAccessor))
            {
                context.CodeWriter
                    .WriteStartAssignment(propertyValueAccessor)
                    .Write(previousValueAccessor)
                    .WriteLine(";");

                return;
            }
            else
            {
                tagHelperRenderingContext.RenderedBoundAttributes[node.AttributeName] = propertyValueAccessor;
            }

            if (node.Descriptor.IsStringProperty || (node.IsIndexerNameMatch && node.Descriptor.IsIndexerStringProperty))
            {
                context.RenderChildren(node);

                context.CodeWriter.WriteStartAssignment(propertyValueAccessor);
                if (node.Children.Count == 1 && node.Children.First() is HtmlContentIntermediateNode htmlNode)
                {
                    var content = GetContent(htmlNode);
                    context.CodeWriter.WriteStringLiteral(content);
                }
                else
                {
                    context.CodeWriter.Write("string.Empty");
                }
                context.CodeWriter.WriteLine(";");
            }
            else
            {
                var firstMappedChild = node.Children.FirstOrDefault(child => child.Source != null) as IntermediateNode;
                var valueStart = firstMappedChild?.Source;

                using (context.CodeWriter.BuildLinePragma(node.Source.Value))
                {
                    var assignmentPrefixLength = propertyValueAccessor.Length + " = ".Length;
                    if (node.Descriptor.IsEnum &&
                        node.Children.Count == 1 &&
                        node.Children.First() is IntermediateToken token &&
                        token.IsCSharp)
                    {
                        assignmentPrefixLength += $"global::{node.Descriptor.TypeName}.".Length;

                        if (valueStart != null)
                        {
                            context.CodeWriter.WritePadding(assignmentPrefixLength, node.Source.Value, context);
                        }

                        context.CodeWriter
                            .WriteStartAssignment(propertyValueAccessor)
                            .Write("global::")
                            .Write(node.Descriptor.TypeName)
                            .Write(".");
                    }
                    else
                    {
                        if (valueStart != null)
                        {
                            context.CodeWriter.WritePadding(assignmentPrefixLength, node.Source.Value, context);
                        }

                        context.CodeWriter.WriteStartAssignment(propertyValueAccessor);
                    }

                    RenderTagHelperAttributeInline(context, node, node.Source.Value);

                    context.CodeWriter.WriteLine(";");
                }
            }
        }

        private void RenderTagHelperAttributeInline(
            CodeRenderingContext context,
            SetTagHelperPropertyIntermediateNode property,
            SourceSpan documentLocation)
        {
            for (var i = 0; i < property.Children.Count; i++)
            {
                RenderTagHelperAttributeInline(context, property, property.Children[i], documentLocation);
            }
        }

        private void RenderTagHelperAttributeInline(
            CodeRenderingContext context,
            SetTagHelperPropertyIntermediateNode property,
            IntermediateNode node,
            SourceSpan documentLocation)
        {
            if (node is CSharpExpressionIntermediateNode || node is HtmlContentIntermediateNode)
            {
                for (var i = 0; i < node.Children.Count; i++)
                {
                    RenderTagHelperAttributeInline(context, property, node.Children[i], documentLocation);
                }
            }
            else if (node is IntermediateToken token)
            {
                if (node.Source != null)
                {
                    context.AddLineMappingFor(node);
                }

                context.CodeWriter.Write(token.Content);
            }
            else if (node is CSharpCodeIntermediateNode)
            {
                var error = new RazorError(
                    LegacyResources.TagHelpers_CodeBlocks_NotSupported_InAttributes,
                    new SourceLocation(documentLocation.AbsoluteIndex, documentLocation.CharacterIndex, documentLocation.Length),
                    documentLocation.Length);
                context.Diagnostics.Add(RazorDiagnostic.Create(error));
            }
            else if (node is TemplateIntermediateNode)
            {
                var expectedTypeName = property.IsIndexerNameMatch ? property.Descriptor.IndexerTypeName : property.Descriptor.TypeName;
                var error = new RazorError(
                    LegacyResources.FormatTagHelpers_InlineMarkupBlocks_NotSupported_InAttributes(expectedTypeName),
                    new SourceLocation(documentLocation.AbsoluteIndex, documentLocation.CharacterIndex, documentLocation.Length),
                    documentLocation.Length);
                context.Diagnostics.Add(RazorDiagnostic.Create(error));
            }
        }

        private string GetContent(HtmlContentIntermediateNode node)
        {
            var builder = new StringBuilder();
            for (var i = 0; i < node.Children.Count; i++)
            {
                if (node.Children[i] is IntermediateToken token && token.IsHtml)
                {
                    builder.Append(token.Content);
                }
            }

            return builder.ToString();
        }

        private static string GetTagHelperVariableName(string tagHelperTypeName) => "__" + tagHelperTypeName.Replace('.', '_');

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
