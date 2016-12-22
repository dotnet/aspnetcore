// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Razor.Evolution.Intermediate;
using Microsoft.AspNetCore.Razor.Evolution.Legacy;

namespace Microsoft.AspNetCore.Razor.Evolution
{
    internal class DefaultRazorRuntimeCSharpLoweringPhase : RazorCSharpLoweringPhaseBase
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
            };

            codeDocument.SetCSharpDocument(csharpDocument);
        }

        private class CSharpRenderer : PageStructureCSharpRenderer
        {
            public CSharpRenderer(CSharpRenderingContext context) : base(context)
            {
            }

            public override void VisitChecksum(ChecksumIRNode node)
            {
                if (!string.IsNullOrEmpty(node.Bytes))
                {
                    Context.Writer
                    .Write("#pragma checksum \"")
                    .Write(node.Filename)
                    .Write("\" \"")
                    .Write(node.Guid)
                    .Write("\" \"")
                    .Write(node.Bytes)
                    .WriteLine("\"");
                }
            }

            public override void VisitCSharpToken(CSharpTokenIRNode node)
            {
                Context.Writer.Write(node.Content);
            }

            public override void VisitHtml(HtmlContentIRNode node)
            {
                const int MaxStringLiteralLength = 1024;

                var charactersConsumed = 0;

                // Render the string in pieces to avoid Roslyn OOM exceptions at compile time: https://github.com/aspnet/External/issues/54
                while (charactersConsumed < node.Content.Length)
                {
                    string textToRender;
                    if (node.Content.Length <= MaxStringLiteralLength)
                    {
                        textToRender = node.Content;
                    }
                    else
                    {
                        var charactersToSubstring = Math.Min(MaxStringLiteralLength, node.Content.Length - charactersConsumed);
                        textToRender = node.Content.Substring(charactersConsumed, charactersToSubstring);
                    }

                    Context.Writer
                        .Write(Context.RenderingConventions.StartWriteLiteralMethod)
                        .WriteStringLiteral(textToRender)
                        .WriteEndMethodInvocation();

                    charactersConsumed += textToRender.Length;
                }
            }

            public override void VisitCSharpExpression(CSharpExpressionIRNode node)
            {
                IDisposable linePragmaScope = null;
                if (node.SourceRange != null)
                {
                    linePragmaScope = new LinePragmaWriter(Context.Writer, node.SourceRange);
                    var padding = BuildOffsetPadding(Context.RenderingConventions.StartWriteMethod.Length, node.SourceRange, Context);
                    Context.Writer.Write(padding);
                }

                Context.Writer.Write(Context.RenderingConventions.StartWriteMethod);

                VisitDefault(node);

                Context.Writer.WriteEndMethodInvocation();

                linePragmaScope?.Dispose();
            }

            public override void VisitUsingStatement(UsingStatementIRNode node)
            {
                Context.Writer.WriteUsing(node.Content);
            }

            public override void VisitHtmlAttribute(HtmlAttributeIRNode node)
            {
                var valuePieceCount = node
                    .Children
                    .Count(child => child is HtmlAttributeValueIRNode || child is CSharpAttributeValueIRNode);
                var prefixLocation = node.SourceRange.AbsoluteIndex;
                var suffixLocation = node.SourceRange.AbsoluteIndex + node.SourceRange.ContentLength - node.Suffix.Length;
                Context.Writer
                    .Write(Context.RenderingConventions.StartBeginWriteAttributeMethod)
                    .WriteStringLiteral(node.Name)
                    .WriteParameterSeparator()
                    .WriteStringLiteral(node.Prefix)
                    .WriteParameterSeparator()
                    .Write(prefixLocation.ToString(CultureInfo.InvariantCulture))
                    .WriteParameterSeparator()
                    .WriteStringLiteral(node.Suffix)
                    .WriteParameterSeparator()
                    .Write(suffixLocation.ToString(CultureInfo.InvariantCulture))
                    .WriteParameterSeparator()
                    .Write(valuePieceCount.ToString(CultureInfo.InvariantCulture))
                    .WriteEndMethodInvocation();

                VisitDefault(node);

                Context.Writer
                    .Write(Context.RenderingConventions.StartEndWriteAttributeMethod)
                    .WriteEndMethodInvocation();
            }

            public override void VisitHtmlAttributeValue(HtmlAttributeValueIRNode node)
            {
                var prefixLocation = node.SourceRange.AbsoluteIndex;
                var valueLocation = node.SourceRange.AbsoluteIndex + node.Prefix.Length;
                var valueLength = node.SourceRange.ContentLength;
                Context.Writer
                    .Write(Context.RenderingConventions.StartWriteAttributeValueMethod)
                    .WriteStringLiteral(node.Prefix)
                    .WriteParameterSeparator()
                    .Write(prefixLocation.ToString(CultureInfo.InvariantCulture))
                    .WriteParameterSeparator()
                    .WriteStringLiteral(node.Content)
                    .WriteParameterSeparator()
                    .Write(valueLocation.ToString(CultureInfo.InvariantCulture))
                    .WriteParameterSeparator()
                    .Write(valueLength.ToString(CultureInfo.InvariantCulture))
                    .WriteParameterSeparator()
                    .WriteBooleanLiteral(true)
                    .WriteEndMethodInvocation();
            }

            public override void VisitCSharpAttributeValue(CSharpAttributeValueIRNode node)
            {
                const string ValueWriterName = "__razor_attribute_value_writer";

                var expressionValue = node.Children.First() as CSharpExpressionIRNode;
                var linePragma = expressionValue != null ? new LinePragmaWriter(Context.Writer, node.SourceRange) : null;
                var prefixLocation = node.SourceRange.AbsoluteIndex;
                var valueLocation = node.SourceRange.AbsoluteIndex + node.Prefix.Length;
                var valueLength = node.SourceRange.ContentLength - node.Prefix.Length;
                Context.Writer
                    .Write(Context.RenderingConventions.StartWriteAttributeValueMethod)
                    .WriteStringLiteral(node.Prefix)
                    .WriteParameterSeparator()
                    .Write(prefixLocation.ToString(CultureInfo.InvariantCulture))
                    .WriteParameterSeparator();

                if (expressionValue != null)
                {
                    Debug.Assert(node.Children.Count == 1);

                    RenderExpressionInline(expressionValue, Context);
                }
                else
                {
                    // Not an expression; need to buffer the result.
                    Context.Writer.WriteStartNewObject("HelperResult" /* ORIGINAL: TemplateTypeName */);

                    var initialRenderingConventions = Context.RenderingConventions;
                    var redirectConventions = new CSharpRedirectRenderingConventions(ValueWriterName, Context.Writer);
                    Context.RenderingConventions = redirectConventions;
                    using (Context.Writer.BuildAsyncLambda(endLine: false, parameterNames: ValueWriterName))
                    {
                        VisitDefault(node);
                    }
                    Context.RenderingConventions = initialRenderingConventions;

                    Context.Writer.WriteEndMethodInvocation(false);
                }

                Context.Writer
                    .WriteParameterSeparator()
                    .Write(valueLocation.ToString(CultureInfo.InvariantCulture))
                    .WriteParameterSeparator()
                    .Write(valueLength.ToString(CultureInfo.InvariantCulture))
                    .WriteParameterSeparator()
                    .WriteBooleanLiteral(false)
                    .WriteEndMethodInvocation();

                linePragma?.Dispose();
            }

            public override void VisitCSharpStatement(CSharpStatementIRNode node)
            {
                if (string.IsNullOrWhiteSpace(node.Content))
                {
                    return;
                }

                if (node.SourceRange != null)
                {
                    using (new LinePragmaWriter(Context.Writer, node.SourceRange))
                    {
                        var padding = BuildOffsetPadding(0, node.SourceRange, Context);
                        Context.Writer
                            .Write(padding)
                            .WriteLine(node.Content);
                    }
                }
                else
                {
                    Context.Writer.WriteLine(node.Content);
                }
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
        }
    }
}
