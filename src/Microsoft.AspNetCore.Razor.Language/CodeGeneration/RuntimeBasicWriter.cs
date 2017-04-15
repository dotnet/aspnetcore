// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration
{
    public class RuntimeBasicWriter : BasicWriter
    {
        public virtual string WriteCSharpExpressionMethod { get; set; } = "Write";

        public virtual string WriteHtmlContentMethod { get; set; } = "WriteLiteral";

        public virtual string BeginWriteAttributeMethod { get; set; } = "BeginWriteAttribute";

        public virtual string EndWriteAttributeMethod { get; set; } = "EndWriteAttribute";

        public virtual string WriteAttributeValueMethod { get; set; } = "WriteAttributeValue";

        public string TemplateTypeName { get; set; } = "Microsoft.AspNetCore.Mvc.Razor.HelperResult";

        public override void WriteChecksum(CSharpRenderingContext context, ChecksumIRNode node)
        {
            if (!string.IsNullOrEmpty(node.Bytes))
            {
                context.Writer
                .Write("#pragma checksum \"")
                .Write(node.FileName)
                .Write("\" \"")
                .Write(node.Guid)
                .Write("\" \"")
                .Write(node.Bytes)
                .WriteLine("\"");
            }
        }

        public override void WriteUsingStatement(CSharpRenderingContext context, UsingStatementIRNode node)
        {
            if (node.Source.HasValue)
            {
                using (context.Writer.BuildLinePragma(node.Source.Value))
                {
                    context.Writer.WriteUsing(node.Content);
                }
            }
            else
            {
                context.Writer.WriteUsing(node.Content);
            }
        }

        public override void WriteCSharpExpression(CSharpRenderingContext context, CSharpExpressionIRNode node)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            IDisposable linePragmaScope = null;
            if (node.Source != null)
            {
                linePragmaScope = context.Writer.BuildLinePragma(node.Source.Value);
                context.Writer.WritePadding(WriteCSharpExpressionMethod.Length + 1, node.Source, context);
            }

            context.Writer.WriteStartMethodInvocation(WriteCSharpExpressionMethod);

            for (var i = 0; i < node.Children.Count; i++)
            {
                if (node.Children[i] is RazorIRToken token && token.IsCSharp)
                {
                    context.Writer.Write(token.Content);
                }
                else
                {
                    // There may be something else inside the expression like a Template or another extension node.
                    context.RenderNode(node.Children[i]);
                }
            }

            context.Writer.WriteEndMethodInvocation();

            linePragmaScope?.Dispose();
        }

        public override void WriteCSharpStatement(CSharpRenderingContext context, CSharpStatementIRNode node)
        {
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

            if (isWhitespaceStatement)
            {
                return;
            }

            IDisposable linePragmaScope = null;
            if (node.Source != null)
            {
                linePragmaScope = context.Writer.BuildLinePragma(node.Source.Value);
                context.Writer.WritePadding(0, node.Source.Value, context);
            }

            for (var i = 0; i < node.Children.Count; i++)
            {
                if (node.Children[i] is RazorIRToken token && token.IsCSharp)
                {
                    context.Writer.Write(token.Content);
                }
                else
                {
                    // There may be something else inside the statement like an extension node.
                    context.RenderNode(node.Children[i]);
                }
            }

            if (linePragmaScope == null)
            {
                context.Writer.WriteLine();
            }

            linePragmaScope?.Dispose();
        }

        public override void WriteHtmlAttribute(CSharpRenderingContext context, HtmlAttributeIRNode node)
        {
            var valuePieceCount = node
                .Children
                .Count(child => child is HtmlAttributeValueIRNode || child is CSharpAttributeValueIRNode);
            var prefixLocation = node.Source.Value.AbsoluteIndex;
            var suffixLocation = node.Source.Value.AbsoluteIndex + node.Source.Value.Length - node.Suffix.Length;
            context.Writer
                .WriteStartMethodInvocation(BeginWriteAttributeMethod)
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

            context.RenderChildren(node);

            context.Writer
                .WriteStartMethodInvocation(EndWriteAttributeMethod)
                .WriteEndMethodInvocation();
        }

        public override void WriteHtmlAttributeValue(CSharpRenderingContext context, HtmlAttributeValueIRNode node)
        {
            var prefixLocation = node.Source.Value.AbsoluteIndex;
            var valueLocation = node.Source.Value.AbsoluteIndex + node.Prefix.Length;
            var valueLength = node.Source.Value.Length;
            context.Writer
                .WriteStartMethodInvocation(WriteAttributeValueMethod)
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

        public override void WriteCSharpAttributeValue(CSharpRenderingContext context, CSharpAttributeValueIRNode node)
        {
            const string ValueWriterName = "__razor_attribute_value_writer";

            var expressionValue = node.Children.FirstOrDefault() as CSharpExpressionIRNode;
            var linePragma = expressionValue != null ? context.Writer.BuildLinePragma(node.Source.Value) : null;
            var prefixLocation = node.Source.Value.AbsoluteIndex;
            var valueLocation = node.Source.Value.AbsoluteIndex + node.Prefix.Length;
            var valueLength = node.Source.Value.Length - node.Prefix.Length;
            context.Writer
                .WriteStartMethodInvocation(WriteAttributeValueMethod)
                .WriteStringLiteral(node.Prefix)
                .WriteParameterSeparator()
                .Write(prefixLocation.ToString(CultureInfo.InvariantCulture))
                .WriteParameterSeparator();

            if (expressionValue != null)
            {
                Debug.Assert(node.Children.Count == 1);

                RenderExpressionInline(context, expressionValue);
            }
            else
            {
                // Not an expression; need to buffer the result.
                context.Writer.WriteStartNewObject(TemplateTypeName);

                using (context.Push(new RedirectedRuntimeBasicWriter(ValueWriterName)))
                using (context.Writer.BuildAsyncLambda(endLine: false, parameterNames: ValueWriterName))
                {
                    context.RenderChildren(node);
                }

                context.Writer.WriteEndMethodInvocation(false);
            }

            context.Writer
                .WriteParameterSeparator()
                .Write(valueLocation.ToString(CultureInfo.InvariantCulture))
                .WriteParameterSeparator()
                .Write(valueLength.ToString(CultureInfo.InvariantCulture))
                .WriteParameterSeparator()
                .WriteBooleanLiteral(false)
                .WriteEndMethodInvocation();

            linePragma?.Dispose();
        }

        public override void WriteHtmlContent(CSharpRenderingContext context, HtmlContentIRNode node)
        {
            const int MaxStringLiteralLength = 1024;

            var builder = new StringBuilder();
            for (var i = 0; i < node.Children.Count; i++)
            {
                if (node.Children[i] is RazorIRToken token && token.IsHtml)
                {
                    builder.Append(token.Content);
                }
            }

            var content = builder.ToString();

            var charactersConsumed = 0;

            // Render the string in pieces to avoid Roslyn OOM exceptions at compile time: https://github.com/aspnet/External/issues/54
            while (charactersConsumed < content.Length)
            {
                string textToRender;
                if (content.Length <= MaxStringLiteralLength)
                {
                    textToRender = content;
                }
                else
                {
                    var charactersToSubstring = Math.Min(MaxStringLiteralLength, content.Length - charactersConsumed);
                    textToRender = content.Substring(charactersConsumed, charactersToSubstring);
                }

                context.Writer
                    .WriteStartMethodInvocation(WriteHtmlContentMethod)
                    .WriteStringLiteral(textToRender)
                    .WriteEndMethodInvocation();

                charactersConsumed += textToRender.Length;
            }
        }

        protected static void RenderExpressionInline(CSharpRenderingContext context, RazorIRNode node)
        {
            if (node is RazorIRToken token && token.IsCSharp)
            {
                context.Writer.Write(token.Content);
            }
            else
            {
                for (var i = 0; i < node.Children.Count; i++)
                {
                    RenderExpressionInline(context, node.Children[i]);
                }
            }
        }
    }
}
