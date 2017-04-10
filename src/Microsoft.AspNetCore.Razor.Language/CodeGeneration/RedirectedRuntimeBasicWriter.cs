// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration
{
    internal class RedirectedRuntimeBasicWriter : RuntimeBasicWriter
    {
        private readonly string _textWriter;

        public RedirectedRuntimeBasicWriter(string textWriter)
        {
            _textWriter = textWriter;
        }

        public new string WriteCSharpExpressionMethod { get; set; } = "WriteTo";

        public new string WriteHtmlContentMethod { get; set; } = "WriteLiteralTo";

        public override void WriteCSharpExpression(CSharpRenderingContext context, CSharpExpressionIRNode node)
        {
            IDisposable linePragmaScope = null;
            if (node.Source != null)
            {
                linePragmaScope = context.Writer.BuildLinePragma(node.Source.Value);

                var offset = WriteCSharpExpressionMethod.Length + "(".Length + _textWriter.Length + ", ".Length;
                context.Writer.WritePadding(offset, node.Source, context);
            }

            context.Writer.WriteStartMethodInvocation(WriteCSharpExpressionMethod);
            context.Writer.Write(_textWriter);
            context.Writer.WriteParameterSeparator();

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
                    .Write(_textWriter)
                    .WriteParameterSeparator()
                    .WriteStringLiteral(textToRender)
                    .WriteEndMethodInvocation();

                    charactersConsumed += textToRender.Length;
                }
            }
    }
}
