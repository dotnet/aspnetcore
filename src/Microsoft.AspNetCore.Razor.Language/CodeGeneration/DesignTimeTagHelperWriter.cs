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
    public class DesignTimeTagHelperWriter : TagHelperWriter
    {
        public string CreateTagHelperMethodName { get; set; } = "CreateTagHelper";

        public override void WriteDeclareTagHelperFields(CSharpRenderingContext context, DeclareTagHelperFieldsIRNode node)
        {
            foreach (var tagHelperTypeName in node.UsedTagHelperTypeNames)
            {
                var tagHelperVariableName = GetTagHelperVariableName(tagHelperTypeName);
                context.Writer
                    .Write("private global::")
                    .WriteVariableDeclaration(
                        tagHelperTypeName,
                        tagHelperVariableName,
                        value: null);
            }
        }

        public override void WriteAddTagHelperHtmlAttribute(CSharpRenderingContext context, AddTagHelperHtmlAttributeIRNode node)
        {
            context.RenderChildren(node);
        }

        public override void WriteCreateTagHelper(CSharpRenderingContext context, CreateTagHelperIRNode node)
        {
            var tagHelperVariableName = GetTagHelperVariableName(node.TagHelperTypeName);

            context.Writer
                .WriteStartAssignment(tagHelperVariableName)
                .WriteStartMethodInvocation(
                    CreateTagHelperMethodName,
                    "global::" + node.TagHelperTypeName)
                .WriteEndMethodInvocation();
        }

        public override void WriteExecuteTagHelpers(CSharpRenderingContext context, ExecuteTagHelpersIRNode node)
        {
            // Do nothing
        }

        public override void WriteInitializeTagHelperStructure(CSharpRenderingContext context, InitializeTagHelperStructureIRNode node)
        {
            context.RenderChildren(node);
        }

        public override void WriteSetTagHelperProperty(CSharpRenderingContext context, SetTagHelperPropertyIRNode node)
        {
            var tagHelperVariableName = GetTagHelperVariableName(node.TagHelperTypeName);
            var tagHelperRenderingContext = context.TagHelperRenderingContext;
            var propertyValueAccessor = GetTagHelperPropertyAccessor(node.IsIndexerNameMatch, tagHelperVariableName, node.AttributeName, node.Descriptor);

            if (tagHelperRenderingContext.RenderedBoundAttributes.TryGetValue(node.AttributeName, out string previousValueAccessor))
            {
                context.Writer
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

                context.Writer.WriteStartAssignment(propertyValueAccessor);
                if (node.Children.Count == 1 && node.Children.First() is HtmlContentIRNode htmlNode)
                {
                    var content = GetContent(htmlNode);
                    context.Writer.WriteStringLiteral(content);
                }
                else
                {
                    context.Writer.Write("string.Empty");
                }
                context.Writer.WriteLine(";");
            }
            else
            {
                var firstMappedChild = node.Children.FirstOrDefault(child => child.Source != null) as RazorIRNode;
                var valueStart = firstMappedChild?.Source;

                using (context.Writer.BuildLinePragma(node.Source.Value))
                {
                    var assignmentPrefixLength = propertyValueAccessor.Length + " = ".Length;
                    if (node.Descriptor.IsEnum &&
                        node.Children.Count == 1 &&
                        node.Children.First() is RazorIRToken token &&
                        token.IsCSharp)
                    {
                        assignmentPrefixLength += $"global::{node.Descriptor.TypeName}.".Length;

                        if (valueStart != null)
                        {
                            context.Writer.WritePadding(assignmentPrefixLength, node.Source.Value, context);
                        }

                        context.Writer
                            .WriteStartAssignment(propertyValueAccessor)
                            .Write("global::")
                            .Write(node.Descriptor.TypeName)
                            .Write(".");
                    }
                    else
                    {
                        if (valueStart != null)
                        {
                            context.Writer.WritePadding(assignmentPrefixLength, node.Source.Value, context);
                        }

                        context.Writer.WriteStartAssignment(propertyValueAccessor);
                    }

                    RenderTagHelperAttributeInline(context, node, node.Source.Value);

                    context.Writer.WriteLine(";");
                }
            }
        }

        private void RenderTagHelperAttributeInline(
            CSharpRenderingContext context,
            RazorIRNode node,
            SourceSpan documentLocation)
        {
            if (node is SetTagHelperPropertyIRNode || node is CSharpExpressionIRNode || node is HtmlContentIRNode)
            {
                for (var i = 0; i < node.Children.Count; i++)
                {
                    RenderTagHelperAttributeInline(context, node.Children[i], documentLocation);
                }
            }
            else if (node is RazorIRToken token)
            {
                if (node.Source != null)
                {
                    context.AddLineMappingFor(node);
                }

                context.Writer.Write(token.Content);
            }
            else if (node is CSharpStatementIRNode)
            {
                var error = new RazorError(
                    LegacyResources.TagHelpers_CodeBlocks_NotSupported_InAttributes,
                    new SourceLocation(documentLocation.AbsoluteIndex, documentLocation.CharacterIndex, documentLocation.Length),
                    documentLocation.Length);
                context.Diagnostics.Add(RazorDiagnostic.Create(error));
            }
            else if (node is TemplateIRNode)
            {
                var attributeValueNode = (SetTagHelperPropertyIRNode)node.Parent;
                var error = new RazorError(
                    LegacyResources.FormatTagHelpers_InlineMarkupBlocks_NotSupported_InAttributes(attributeValueNode.Descriptor.TypeName),
                    new SourceLocation(documentLocation.AbsoluteIndex, documentLocation.CharacterIndex, documentLocation.Length),
                    documentLocation.Length);
                context.Diagnostics.Add(RazorDiagnostic.Create(error));
            }
        }

        private string GetContent(HtmlContentIRNode node)
        {
            var builder = new StringBuilder();
            for (var i = 0; i < node.Children.Count; i++)
            {
                if (node.Children[i] is RazorIRToken token && token.IsHtml)
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
