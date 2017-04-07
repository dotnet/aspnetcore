// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration
{
    public class RuntimeBasicWriter : BasicWriter
    {
        public string WriteCSharpExpressionMethod { get; set; } = "Write";

        public string WriteHtmlContentMethod { get; set; } = "WriteLiteral";

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
            throw new NotImplementedException();
        }

        public override void WriteHtmlContent(CSharpRenderingContext context, HtmlContentIRNode node)
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

                context.Writer
                    .WriteStartMethodInvocation(WriteHtmlContentMethod)
                    .WriteStringLiteral(textToRender)
                    .WriteEndMethodInvocation();

                charactersConsumed += textToRender.Length;
            }
        }
    }
}
