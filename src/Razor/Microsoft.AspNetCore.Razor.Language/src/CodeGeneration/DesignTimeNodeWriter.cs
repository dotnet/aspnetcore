// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration;

public class DesignTimeNodeWriter : IntermediateNodeWriter
{
    public override void WriteUsingDirective(CodeRenderingContext context, UsingDirectiveIntermediateNode node)
    {
        if (node.Source.HasValue)
        {
            using (context.CodeWriter.BuildLinePragma(node.Source.Value, context))
            {
                context.AddSourceMappingFor(node);
                context.CodeWriter.WriteUsing(node.Content);
            }
        }
        else
        {
            context.CodeWriter.WriteUsing(node.Content);
        }
    }

    public override void WriteCSharpExpression(CodeRenderingContext context, CSharpExpressionIntermediateNode node)
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
            using (context.CodeWriter.BuildLinePragma(node.Source.Value, context))
            {
                var offset = DesignTimeDirectivePass.DesignTimeVariable.Length + " = ".Length;
                context.CodeWriter.WritePadding(offset, node.Source, context);
                context.CodeWriter.WriteStartAssignment(DesignTimeDirectivePass.DesignTimeVariable);

                for (var i = 0; i < node.Children.Count; i++)
                {
                    if (node.Children[i] is IntermediateToken token && token.IsCSharp)
                    {
                        context.AddSourceMappingFor(token);
                        context.CodeWriter.Write(token.Content);
                    }
                    else
                    {
                        // There may be something else inside the expression like a Template or another extension node.
                        context.RenderNode(node.Children[i]);
                    }
                }

                context.CodeWriter.WriteLine(";");
            }
        }
        else
        {
            context.CodeWriter.WriteStartAssignment(DesignTimeDirectivePass.DesignTimeVariable);
            for (var i = 0; i < node.Children.Count; i++)
            {
                if (node.Children[i] is IntermediateToken token && token.IsCSharp)
                {
                    context.CodeWriter.Write(token.Content);
                }
                else
                {
                    // There may be something else inside the expression like a Template or another extension node.
                    context.RenderNode(node.Children[i]);
                }
            }
            context.CodeWriter.WriteLine(";");
        }
    }

    public override void WriteCSharpCode(CodeRenderingContext context, CSharpCodeIntermediateNode node)
    {
        IDisposable linePragmaScope = null;
        if (node.Source != null)
        {
            linePragmaScope = context.CodeWriter.BuildLinePragma(node.Source.Value, context);

            context.CodeWriter.WritePadding(0, node.Source.Value, context);
        }

        for (var i = 0; i < node.Children.Count; i++)
        {
            if (node.Children[i] is IntermediateToken token && token.IsCSharp)
            {
                context.AddSourceMappingFor(token);
                context.CodeWriter.Write(token.Content);
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
            context.CodeWriter.WriteLine();
        }
    }

    public override void WriteHtmlAttribute(CodeRenderingContext context, HtmlAttributeIntermediateNode node)
    {
        context.RenderChildren(node);
    }

    public override void WriteHtmlAttributeValue(CodeRenderingContext context, HtmlAttributeValueIntermediateNode node)
    {
        context.RenderChildren(node);
    }

    public override void WriteCSharpExpressionAttributeValue(CodeRenderingContext context, CSharpExpressionAttributeValueIntermediateNode node)
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
            using (context.CodeWriter.BuildLinePragma(firstChild.Source.Value, context))
            {
                var offset = DesignTimeDirectivePass.DesignTimeVariable.Length + " = ".Length;
                context.CodeWriter.WritePadding(offset, firstChild.Source, context);
                context.CodeWriter.WriteStartAssignment(DesignTimeDirectivePass.DesignTimeVariable);

                for (var i = 0; i < node.Children.Count; i++)
                {
                    if (node.Children[i] is IntermediateToken token && token.IsCSharp)
                    {
                        context.AddSourceMappingFor(token);
                        context.CodeWriter.Write(token.Content);
                    }
                    else
                    {
                        // There may be something else inside the expression like a Template or another extension node.
                        context.RenderNode(node.Children[i]);
                    }
                }

                context.CodeWriter.WriteLine(";");
            }
        }
        else
        {
            context.CodeWriter.WriteStartAssignment(DesignTimeDirectivePass.DesignTimeVariable);
            for (var i = 0; i < node.Children.Count; i++)
            {
                if (node.Children[i] is IntermediateToken token && token.IsCSharp)
                {
                    if (token.Source != null)
                    {
                        context.AddSourceMappingFor(token);
                    }

                    context.CodeWriter.Write(token.Content);
                }
                else
                {
                    // There may be something else inside the expression like a Template or another extension node.
                    context.RenderNode(node.Children[i]);
                }
            }
            context.CodeWriter.WriteLine(";");
        }
    }

    public override void WriteCSharpCodeAttributeValue(CodeRenderingContext context, CSharpCodeAttributeValueIntermediateNode node)
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
                        linePragmaScope = context.CodeWriter.BuildLinePragma(token.Source.Value, context);
                    }

                    context.CodeWriter.WritePadding(0, token.Source.Value, context);
                }
                else if (isWhitespaceStatement)
                {
                    // Don't write whitespace if there is no line mapping for it.
                    continue;
                }

                context.AddSourceMappingFor(token);
                context.CodeWriter.Write(token.Content);

                if (linePragmaScope != null)
                {
                    linePragmaScope.Dispose();
                }
                else
                {
                    context.CodeWriter.WriteLine();
                }
            }
            else
            {
                // There may be something else inside the statement like an extension node.
                context.RenderNode(node.Children[i]);
            }
        }
    }

    public override void WriteHtmlContent(CodeRenderingContext context, HtmlContentIntermediateNode node)
    {
        // Do nothing
    }

    public override void BeginWriterScope(CodeRenderingContext context, string writer)
    {
        // Do nothing
    }

    public override void EndWriterScope(CodeRenderingContext context)
    {
        // Do nothing
    }
}
