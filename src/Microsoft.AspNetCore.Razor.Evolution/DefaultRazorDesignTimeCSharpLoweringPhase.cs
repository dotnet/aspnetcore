// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Razor.Evolution.Intermediate;
using Microsoft.AspNetCore.Razor.Evolution.Legacy;

namespace Microsoft.AspNetCore.Razor.Evolution
{
    internal class DefaultRazorDesignTimeCSharpLoweringPhase : RazorCSharpLoweringPhaseBase
    {
        protected override void ExecuteCore(RazorCodeDocument codeDocument)
        {
            var irDocument = codeDocument.GetIRDocument();
            ThrowForMissingDependency(irDocument);

            var syntaxTree = codeDocument.GetSyntaxTree();
            ThrowForMissingDependency(syntaxTree);

            var renderingContext = new CSharpRenderingContext()
            {
                Writer = new CSharpCodeWriter(),
                SourceDocument = codeDocument.Source,
                Options = syntaxTree.Options,
            };
            var visitor = new CSharpRenderer(renderingContext);
            visitor.VisitDocument(irDocument);
            var csharpDocument = new RazorCSharpDocument()
            {
                GeneratedCode = renderingContext.Writer.GenerateCode(),
                LineMappings = renderingContext.LineMappings,
                Diagnostics = renderingContext.ErrorSink.Errors
            };

            codeDocument.SetCSharpDocument(csharpDocument);
        }

        private class CSharpRenderer : PageStructureCSharpRenderer
        {
            public CSharpRenderer(CSharpRenderingContext context) : base(context)
            {
            }

            public override void VisitCSharpToken(CSharpTokenIRNode node)
            {
                Context.Writer.Write(node.Content);
            }

            public override void VisitCSharpExpression(CSharpExpressionIRNode node)
            {
                if (node.Children.Count == 0)
                {
                    return;
                }

                if (node.Source != null)
                {
                    using (new LinePragmaWriter(Context.Writer, node.Source.Value))
                    {
                        var padding = BuildOffsetPadding(RazorDesignTimeIRPass.DesignTimeVariable.Length, node.Source.Value, Context);

                        Context.Writer
                            .Write(padding)
                            .WriteStartAssignment(RazorDesignTimeIRPass.DesignTimeVariable);

                        for (var i = 0; i < node.Children.Count; i++)
                        {
                            var childNode = node.Children[i];

                            if (childNode is CSharpTokenIRNode)
                            {
                                AddLineMappingFor(childNode);
                            }

                            childNode.Accept(this);
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
                Context.Writer.WriteUsing(node.Content);
            }

            public override void VisitCSharpStatement(CSharpStatementIRNode node)
            {
                if (node.Source != null)
                {
                    using (new LinePragmaWriter(Context.Writer, node.Source.Value))
                    {
                        var padding = BuildOffsetPadding(0, node.Source.Value, Context);
                        Context.Writer.Write(padding);

                        AddLineMappingFor(node);
                        Context.Writer.Write(node.Content);
                    }
                }
                else
                {
                    Context.Writer.WriteLine(node.Content);
                }
            }

            public override void VisitDirectiveToken(DirectiveTokenIRNode node)
            {
                const string TypeHelper = "__typeHelper";

                var tokenKind = node.Descriptor.Kind;
                if (node.Source == null || node.Descriptor.Kind == DirectiveTokenKind.Literal)
                {
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

                            AddLineMappingFor(node);
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

                            AddLineMappingFor(node);
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
                                AddLineMappingFor(node);
                                Context.Writer.Write(node.Content);
                            }
                            else
                            {
                                Context.Writer.Write("\"");
                                AddLineMappingFor(node);
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

            public override void VisitTemplate(TemplateIRNode node)
            {
                const string ItemParameterName = "item";
                const string TemplateWriterName = "__razor_template_writer";

                Context.Writer
                    .Write(ItemParameterName).Write(" => ")
                    .WriteStartNewObject("HelperResult" /* ORIGINAL: TemplateTypeName */);

                var initialRenderingConventions = Context.RenderingConventions;
                var redirectConventions = new CSharpRedirectRenderingConventions(TemplateWriterName, Context.Writer);
                Context.RenderingConventions = redirectConventions;
                using (Context.Writer.BuildAsyncLambda(endLine: false, parameterNames: TemplateWriterName))
                {
                    VisitDefault(node);
                }
                Context.RenderingConventions = initialRenderingConventions;

                Context.Writer.WriteEndMethodInvocation(endLine: false);
            }

            internal override void VisitTagHelper(TagHelperIRNode node)
            {
                var initialTagHelperRenderingContext = Context.TagHelperRenderingContext;
                Context.TagHelperRenderingContext = new TagHelperRenderingContext();
                VisitDefault(node);
                Context.TagHelperRenderingContext = initialTagHelperRenderingContext;
            }

            internal override void VisitInitializeTagHelperStructure(InitializeTagHelperStructureIRNode node)
            {
                VisitDefault(node);
            }

            internal override void VisitCreateTagHelper(CreateTagHelperIRNode node)
            {
                var tagHelperVariableName = GetTagHelperVariableName(node.TagHelperTypeName);

                Context.Writer
                    .WriteStartAssignment(tagHelperVariableName)
                    .WriteStartMethodInvocation(
                        "CreateTagHelper" /* ORIGINAL: CreateTagHelperMethodName */,
                        "global::" + node.TagHelperTypeName)
                    .WriteEndMethodInvocation();
            }

            internal override void VisitSetTagHelperProperty(SetTagHelperPropertyIRNode node)
            {
                var tagHelperVariableName = GetTagHelperVariableName(node.TagHelperTypeName);
                var tagHelperRenderingContext = Context.TagHelperRenderingContext;
                var propertyValueAccessor = GetTagHelperPropertyAccessor(tagHelperVariableName, node.AttributeName, node.Descriptor);

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

                if (node.Descriptor.IsStringProperty)
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

                    using (new LinePragmaWriter(Context.Writer, node.Source.Value))
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

            internal override void VisitDeclareTagHelperFields(DeclareTagHelperFieldsIRNode node)
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

            private void AddLineMappingFor(RazorIRNode node)
            {
                var sourceLocation = node.Source.Value;
                var generatedLocation = new SourceSpan(Context.Writer.GetCurrentSourceLocation(), sourceLocation.Length);
                var lineMapping = new LineMapping(sourceLocation, generatedLocation);

                Context.LineMappings.Add(lineMapping);
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
                        AddLineMappingFor(node);
                    }

                    Context.Writer.Write(((HtmlContentIRNode)node).Content);
                }
                else if (node is CSharpTokenIRNode)
                {
                    if (node.Source != null)
                    {
                        AddLineMappingFor(node);
                    }

                    Context.Writer.Write(((CSharpTokenIRNode)node).Content);
                }
                else if (node is CSharpStatementIRNode)
                {
                    Context.ErrorSink.OnError(
                        new SourceLocation(documentLocation.AbsoluteIndex, documentLocation.CharacterIndex, documentLocation.Length),
                        LegacyResources.TagHelpers_CodeBlocks_NotSupported_InAttributes,
                        documentLocation.Length);
                }
                else if (node is TemplateIRNode)
                {
                    var attributeValueNode = (SetTagHelperPropertyIRNode)node.Parent;
                    Context.ErrorSink.OnError(
                        new SourceLocation(documentLocation.AbsoluteIndex, documentLocation.CharacterIndex, documentLocation.Length),
                        LegacyResources.FormatTagHelpers_InlineMarkupBlocks_NotSupported_InAttributes(attributeValueNode.Descriptor.TypeName),
                        documentLocation.Length);
                }
            }
        }
    }
}
