// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Evolution.Intermediate;

namespace Microsoft.AspNetCore.Razor.Evolution.CodeGeneration
{
    internal class RedirectedBasicWriter : BasicWriter
    {
        private readonly BasicWriter _previous;
        private readonly string _textWriter;

        public RedirectedBasicWriter(BasicWriter previous, string textWriter)
        {
            _previous = previous;
            _textWriter = textWriter;
        }

        public string WriteCSharpExpressionMethod { get; set; } = "WriteTo";

        public override void WriteCSharpExpression(CSharpRenderingContext context, CSharpExpressionIRNode node)
        {
            if (context.Options.DesignTimeMode)
            {
                _previous.WriteCSharpExpression(context, node);
                return;
            }

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

        public override void WriteCSharpStatement(CSharpRenderingContext context, CSharpStatementIRNode node)
        {
            _previous.WriteCSharpStatement(context, node);
        }

        public override void WriteHtmlAttribute(CSharpRenderingContext context, HtmlAttributeIRNode node)
        {
            _previous.WriteHtmlAttribute(context, node);
        }

        public override void WriteHtmlContent(CSharpRenderingContext context, HtmlContentIRNode node)
        {
            _previous.WriteHtmlContent(context, node);
        }
    }
}
