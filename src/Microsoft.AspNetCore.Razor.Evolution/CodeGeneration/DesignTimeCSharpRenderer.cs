// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Razor.Evolution.Intermediate;
using Microsoft.AspNetCore.Razor.Evolution.Legacy;

namespace Microsoft.AspNetCore.Razor.Evolution.CodeGeneration
{
    internal class DesignTimeCSharpRenderer : PageStructureCSharpRenderer
    {
        public DesignTimeCSharpRenderer(RuntimeTarget target, CSharpRenderingContext context)
            : base(target, context)
        {
        }

        public override void VisitCSharpExpression(CSharpExpressionIRNode node)
        {
            // We can't remove this yet, because it's still used recursively in a few places.
            if (node.Children.Count == 0)
            {
                return;
            }

            if (node.Source != null)
            {
                using (Context.Writer.BuildLinePragma(node.Source.Value))
                {
                    var offset = RazorDesignTimeIRPass.DesignTimeVariable.Length + " = ".Length;
                    var padding = BuildOffsetPadding(offset, node.Source.Value, Context);

                    Context.Writer
                        .Write(padding)
                        .WriteStartAssignment(RazorDesignTimeIRPass.DesignTimeVariable);

                    for (var i = 0; i < node.Children.Count; i++)
                    {
                        var token = node.Children[i] as RazorIRToken;
                        if (token != null && token.IsCSharp)
                        {
                            Context.AddLineMappingFor(token);
                            Context.Writer.Write(token.Content);
                        }
                        else
                        {
                            // There may be something else inside the expression like a Template or another extension node.
                            Visit(node.Children[i]);
                        }
                    }

                    Context.Writer.WriteLine(";");
                }
            }
            else
            {
                Context.Writer.WriteStartAssignment(RazorDesignTimeIRPass.DesignTimeVariable);
                VisitDefault(node);
                Context.Writer.WriteLine(";");
            }
        }

        public override void VisitUsingStatement(UsingStatementIRNode node)
        {
            if (node.Source.HasValue)
            {
                using (Context.Writer.BuildLinePragma(node.Source.Value))
                {
                    Context.AddLineMappingFor(node);
                    Context.Writer.WriteUsing(node.Content);
                }
            }
            else
            {
                Context.Writer.WriteUsing(node.Content);
            }
        }

        public override void VisitCSharpStatement(CSharpStatementIRNode node)
        {
            // We can't remove this yet, because it's still used recursively in a few places.
            var isWhitespaceStatement = true;
            for (var i = 0; i < node.Children.Count; i++)
            {
                var token = node.Children[i] as RazorIRToken;
                if (token == null || !string.IsNullOrWhiteSpace(token.Content))
                {
                    isWhitespaceStatement = false;
                    break;
                }
            }

            IDisposable linePragmaScope = null;
            if (node.Source != null)
            {
                if (!isWhitespaceStatement)
                {
                    linePragmaScope = Context.Writer.BuildLinePragma(node.Source.Value);
                }

                var padding = BuildOffsetPadding(0, node.Source.Value, Context);
                Context.Writer.Write(padding);
            }
            else if (isWhitespaceStatement)
            {
                // Don't write whitespace if there is no line mapping for it.
                return;
            }

            for (var i = 0; i < node.Children.Count; i++)
            {
                if (node.Children[i] is RazorIRToken token && token.IsCSharp)
                {
                    Context.AddLineMappingFor(token);
                    Context.Writer.Write(token.Content);
                }
                else
                {
                    // There may be something else inside the statement like an extension node.
                    Visit(node.Children[i]);
                }
            }

            if (linePragmaScope != null)
            {
                linePragmaScope.Dispose();
            }
            else
            {
                Context.Writer.WriteLine();
            }
        }

        public override void VisitDirectiveToken(DirectiveTokenIRNode node)
        {
            const string TypeHelper = "__typeHelper";

            var tokenKind = node.Descriptor.Kind;
            if (!node.Source.HasValue ||
                !string.Equals(
                    Context.SourceDocument.FileName,
                    node.Source.Value.FilePath,
                    StringComparison.OrdinalIgnoreCase))
            {
                // We don't want to handle directives from imports.
                return;
            }

            // Wrap the directive token in a lambda to isolate variable names.
            Context.Writer
                .Write("((")
                .Write(typeof(Action).FullName)
                .Write(")(");
            using (Context.Writer.BuildLambda(endLine: false))
            {
                var originalIndent = Context.Writer.CurrentIndent;
                Context.Writer.ResetIndent();
                switch (tokenKind)
                {
                    case DirectiveTokenKind.Type:

                        Context.AddLineMappingFor(node);
                        Context.Writer
                            .Write(node.Content)
                            .Write(" ")
                            .WriteStartAssignment(TypeHelper)
                            .WriteLine("null;");
                        break;
                    case DirectiveTokenKind.Member:
                        Context.Writer
                            .Write(typeof(object).FullName)
                            .Write(" ");

                        Context.AddLineMappingFor(node);
                        Context.Writer
                            .Write(node.Content)
                            .WriteLine(" = null;");
                        break;
                    case DirectiveTokenKind.String:
                        Context.Writer
                            .Write(typeof(object).FullName)
                            .Write(" ")
                            .WriteStartAssignment(TypeHelper);

                        if (node.Content.StartsWith("\"", StringComparison.Ordinal))
                        {
                            Context.AddLineMappingFor(node);
                            Context.Writer.Write(node.Content);
                        }
                        else
                        {
                            Context.Writer.Write("\"");
                            Context.AddLineMappingFor(node);
                            Context.Writer
                                .Write(node.Content)
                                .Write("\"");
                        }

                        Context.Writer.WriteLine(";");
                        break;
                }
                Context.Writer.SetIndent(originalIndent);
            }
            Context.Writer.WriteLine("))();");
        }

        public override void VisitTagHelper(TagHelperIRNode node)
        {
            var initialTagHelperRenderingContext = Context.TagHelperRenderingContext;
            Context.TagHelperRenderingContext = new TagHelperRenderingContext();
            VisitDefault(node);
            Context.TagHelperRenderingContext = initialTagHelperRenderingContext;
        }

        public override void VisitInitializeTagHelperStructure(InitializeTagHelperStructureIRNode node)
        {
            VisitDefault(node);
        }

        public override void VisitCreateTagHelper(CreateTagHelperIRNode node)
        {
            var tagHelperVariableName = GetTagHelperVariableName(node.TagHelperTypeName);

            Context.Writer
                .WriteStartAssignment(tagHelperVariableName)
                .WriteStartMethodInvocation(
                    "CreateTagHelper" /* ORIGINAL: CreateTagHelperMethodName */,
                    "global::" + node.TagHelperTypeName)
                .WriteEndMethodInvocation();
        }

        public override void VisitSetTagHelperProperty(SetTagHelperPropertyIRNode node)
        {
            var tagHelperVariableName = GetTagHelperVariableName(node.TagHelperTypeName);
            var tagHelperRenderingContext = Context.TagHelperRenderingContext;
            var propertyValueAccessor = GetTagHelperPropertyAccessor(node.IsIndexerNameMatch, tagHelperVariableName, node.AttributeName, node.Descriptor);

            string previousValueAccessor;
            if (tagHelperRenderingContext.RenderedBoundAttributes.TryGetValue(node.AttributeName, out previousValueAccessor))
            {
                Context.Writer
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
                VisitDefault(node);

                Context.Writer.WriteStartAssignment(propertyValueAccessor);
                if (node.Children.Count == 1 && node.Children.First() is HtmlContentIRNode)
                {
                    var htmlNode = node.Children.First() as HtmlContentIRNode;
                    if (htmlNode != null)
                    {
                        Context.Writer.WriteStringLiteral(htmlNode.Content);
                    }
                }
                else
                {
                    Context.Writer.Write("string.Empty");
                }
                Context.Writer.WriteLine(";");
            }
            else
            {
                var firstMappedChild = node.Children.FirstOrDefault(child => child.Source != null) as RazorIRNode;
                var valueStart = firstMappedChild?.Source;

                using (Context.Writer.BuildLinePragma(node.Source.Value))
                {
                    var assignmentPrefixLength = propertyValueAccessor.Length + " = ".Length;
                    if (node.Descriptor.IsEnum &&
                        node.Children.Count == 1 &&
                        node.Children.First() is HtmlContentIRNode)
                    {
                        assignmentPrefixLength += $"global::{node.Descriptor.TypeName}.".Length;

                        if (valueStart != null)
                        {
                            var padding = BuildOffsetPadding(assignmentPrefixLength, node.Source.Value, Context);

                            Context.Writer.Write(padding);
                        }

                        Context.Writer
                            .WriteStartAssignment(propertyValueAccessor)
                            .Write("global::")
                            .Write(node.Descriptor.TypeName)
                            .Write(".");
                    }
                    else
                    {
                        if (valueStart != null)
                        {
                            var padding = BuildOffsetPadding(assignmentPrefixLength, node.Source.Value, Context);

                            Context.Writer.Write(padding);
                        }

                        Context.Writer.WriteStartAssignment(propertyValueAccessor);
                    }

                    RenderTagHelperAttributeInline(node, node.Source.Value);

                    Context.Writer.WriteLine(";");
                }
            }
        }

        public override void VisitDeclareTagHelperFields(DeclareTagHelperFieldsIRNode node)
        {
            foreach (var tagHelperTypeName in node.UsedTagHelperTypeNames)
            {
                var tagHelperVariableName = GetTagHelperVariableName(tagHelperTypeName);
                Context.Writer
                    .Write("private global::")
                    .WriteVariableDeclaration(
                        tagHelperTypeName,
                        tagHelperVariableName,
                        value: null);
            }
        }

        private void RenderTagHelperAttributeInline(
            RazorIRNode node,
            SourceSpan documentLocation)
        {
            if (node is SetTagHelperPropertyIRNode || node is CSharpExpressionIRNode)
            {
                for (var i = 0; i < node.Children.Count; i++)
                {
                    RenderTagHelperAttributeInline(node.Children[i], documentLocation);
                }
            }
            else if (node is HtmlContentIRNode)
            {
                if (node.Source != null)
                {
                    Context.AddLineMappingFor(node);
                }

                Context.Writer.Write(((HtmlContentIRNode)node).Content);
            }
            else if (node is RazorIRToken token && token.IsCSharp)
            {
                if (node.Source != null)
                {
                    Context.AddLineMappingFor(node);
                }

                Context.Writer.Write(token.Content);
            }
            else if (node is CSharpStatementIRNode)
            {
                var error = new RazorError(
                    LegacyResources.TagHelpers_CodeBlocks_NotSupported_InAttributes,
                    new SourceLocation(documentLocation.AbsoluteIndex, documentLocation.CharacterIndex, documentLocation.Length),
                    documentLocation.Length);
                Context.Diagnostics.Add(RazorDiagnostic.Create(error));
            }
            else if (node is TemplateIRNode)
            {
                var attributeValueNode = (SetTagHelperPropertyIRNode)node.Parent;
                var error = new RazorError(
                    LegacyResources.FormatTagHelpers_InlineMarkupBlocks_NotSupported_InAttributes(attributeValueNode.Descriptor.TypeName),
                    new SourceLocation(documentLocation.AbsoluteIndex, documentLocation.CharacterIndex, documentLocation.Length),
                    documentLocation.Length);
                Context.Diagnostics.Add(RazorDiagnostic.Create(error));
            }
        }
    }
}
