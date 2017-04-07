// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration
{
    public class DesignTimeBasicWriter : BasicWriter
    {
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

            if (node.Children.Count == 0)
            {
                return;
            }

            if (node.Source != null)
            {
                using (context.Writer.BuildLinePragma(node.Source.Value))
                {
                    var offset = RazorDesignTimeIRPass.DesignTimeVariable.Length + " = ".Length;
                    context.Writer.WritePadding(offset, node.Source, context);
                    context.Writer.WriteStartAssignment(RazorDesignTimeIRPass.DesignTimeVariable);

                    for (var i = 0; i < node.Children.Count; i++)
                    {
                        if (node.Children[i] is RazorIRToken token && token.IsCSharp)
                        {
                            context.AddLineMappingFor(token);
                            context.Writer.Write(token.Content);
                        }
                        else
                        {
                            // There may be something else inside the expression like a Template or another extension node.
                            context.RenderNode(node.Children[i]);
                        }
                    }

                    context.Writer.WriteLine(";");
                }
            }
            else
            {
                context.Writer.WriteStartAssignment(RazorDesignTimeIRPass.DesignTimeVariable);
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
                context.Writer.WriteLine(";");
            }
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

            IDisposable linePragmaScope = null;
            if (node.Source != null)
            {
                if (!isWhitespaceStatement)
                {
                    linePragmaScope = context.Writer.BuildLinePragma(node.Source.Value);
                }

                context.Writer.WritePadding(0, node.Source.Value, context);
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
                    context.AddLineMappingFor(token);
                    context.Writer.Write(token.Content);
                }
                else
                {
                    // There may be something else inside the statement like an extension node.
                    context.RenderNode(node.Children[i]);
                }
            }

            if (linePragmaScope != null)
            {
                linePragmaScope.Dispose();
            }
            else
            {
                context.Writer.WriteLine();
            }
        }

        public override void WriteHtmlAttribute(CSharpRenderingContext context, HtmlAttributeIRNode node)
        {
            throw new NotImplementedException();
        }

        public override void WriteHtmlContent(CSharpRenderingContext context, HtmlContentIRNode node)
        {
            // Do nothing
        }
    }
}
