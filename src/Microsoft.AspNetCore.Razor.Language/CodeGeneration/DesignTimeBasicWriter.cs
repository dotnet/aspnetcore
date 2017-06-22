// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration
{
    public class DesignTimeBasicWriter : BasicWriter
    {
        public override void WriteUsingDirective(CSharpRenderingContext context, UsingDirectiveIntermediateNode node)
        {
            if (node.Source.HasValue)
            {
                using (context.Writer.BuildLinePragma(node.Source.Value))
                {
                    context.AddLineMappingFor(node);
                    context.Writer.WriteUsing(node.Content);
                }
            }
            else
            {
                context.Writer.WriteUsing(node.Content);
            }
        }

        public override void WriteCSharpExpression(CSharpRenderingContext context, CSharpExpressionIntermediateNode node)
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
                    var offset = DesignTimeDirectivePass.DesignTimeVariable.Length + " = ".Length;
                    context.Writer.WritePadding(offset, node.Source, context);
                    context.Writer.WriteStartAssignment(DesignTimeDirectivePass.DesignTimeVariable);

                    for (var i = 0; i < node.Children.Count; i++)
                    {
                        if (node.Children[i] is IntermediateToken token && token.IsCSharp)
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
                context.Writer.WriteStartAssignment(DesignTimeDirectivePass.DesignTimeVariable);
                for (var i = 0; i < node.Children.Count; i++)
                {
                    if (node.Children[i] is IntermediateToken token && token.IsCSharp)
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

        public override void WriteCSharpCode(CSharpRenderingContext context, CSharpCodeIntermediateNode node)
        {
            var isWhitespaceStatement = true;
            for (var i = 0; i < node.Children.Count; i++)
            {
                var token = node.Children[i] as IntermediateToken;
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
                if (node.Children[i] is IntermediateToken token && token.IsCSharp)
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

        public override void WriteHtmlAttribute(CSharpRenderingContext context, HtmlAttributeIntermediateNode node)
        {
            context.RenderChildren(node);
        }

        public override void WriteHtmlAttributeValue(CSharpRenderingContext context, HtmlAttributeValueIntermediateNode node)
        {
            context.RenderChildren(node);
        }

        public override void WriteCSharpExpressionAttributeValue(CSharpRenderingContext context, CSharpExpressionAttributeValueIntermediateNode node)
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

            var firstChild = node.Children[0];
            if (firstChild.Source != null)
            {
                using (context.Writer.BuildLinePragma(firstChild.Source.Value))
                {
                    var offset = DesignTimeDirectivePass.DesignTimeVariable.Length + " = ".Length;
                    context.Writer.WritePadding(offset, firstChild.Source, context);
                    context.Writer.WriteStartAssignment(DesignTimeDirectivePass.DesignTimeVariable);

                    for (var i = 0; i < node.Children.Count; i++)
                    {
                        if (node.Children[i] is IntermediateToken token && token.IsCSharp)
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
                context.Writer.WriteStartAssignment(DesignTimeDirectivePass.DesignTimeVariable);
                for (var i = 0; i < node.Children.Count; i++)
                {
                    if (node.Children[i] is IntermediateToken token && token.IsCSharp)
                    {
                        if (token.Source != null)
                        {
                            context.AddLineMappingFor(token);
                        }

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

        public override void WriteCSharpCodeAttributeValue(CSharpRenderingContext context, CSharpCodeAttributeValueIntermediateNode node)
        {
            for (var i = 0; i < node.Children.Count; i++)
            {
                if (node.Children[i] is IntermediateToken token && token.IsCSharp)
                {
                    IDisposable linePragmaScope = null;
                    var isWhitespaceStatement = string.IsNullOrWhiteSpace(token.Content);

                    if (token.Source != null)
                    {
                        if (!isWhitespaceStatement)
                        {
                            linePragmaScope = context.Writer.BuildLinePragma(token.Source.Value);
                        }

                        context.Writer.WritePadding(0, token.Source.Value, context);
                    }
                    else if (isWhitespaceStatement)
                    {
                        // Don't write whitespace if there is no line mapping for it.
                        continue;
                    }

                    context.AddLineMappingFor(token);
                    context.Writer.Write(token.Content);

                    if (linePragmaScope != null)
                    {
                        linePragmaScope.Dispose();
                    }
                    else
                    {
                        context.Writer.WriteLine();
                    }
                }
                else
                {
                    // There may be something else inside the statement like an extension node.
                    context.RenderNode(node.Children[i]);
                }
            }
        }

        public override void WriteHtmlContent(CSharpRenderingContext context, HtmlContentIntermediateNode node)
        {
            // Do nothing
        }

        public override void BeginWriterScope(CSharpRenderingContext context, string writer)
        {
            // Do nothing
        }

        public override void EndWriterScope(CSharpRenderingContext context)
        {
            // Do nothing
        }
    }
}
