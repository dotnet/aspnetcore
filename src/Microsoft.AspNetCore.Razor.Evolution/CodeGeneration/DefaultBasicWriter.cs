// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Evolution.Intermediate;

namespace Microsoft.AspNetCore.Razor.Evolution.CodeGeneration
{
    public class DefaultBasicWriter : BasicWriter
    {
        public string WriteCSharpExpressionMethod { get; set; } = "Write";

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

            if (context.Options.DesignTimeMode)
            {
                WriteCSharpExpressionDesignTime(context, node);
            }
            else
            {
                WriteCSharpExpressionRuntime(context, node);
            }
        }

        public override void WriteCSharpStatement(CSharpRenderingContext context, CSharpStatementIRNode node)
        {
            throw new NotImplementedException();
        }

        public override void WriteHtmlAttribute(CSharpRenderingContext context, HtmlAttributeIRNode node)
        {
            throw new NotImplementedException();
        }

        public override void WriteHtmlContent(CSharpRenderingContext context, HtmlContentIRNode node)
        {
            throw new NotImplementedException();
        }

        protected void WriteCSharpExpressionRuntime(CSharpRenderingContext context, CSharpExpressionIRNode node)
        {
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

        protected void WriteCSharpExpressionDesignTime(CSharpRenderingContext context, CSharpExpressionIRNode node)
        {
            if (node.Children.Count == 0)
            {
                return;
            }

            if (node.Source != null)
            {
                using (context.Writer.BuildLinePragma(node.Source.Value))
                {
                    context.Writer.WritePadding(RazorDesignTimeIRPass.DesignTimeVariable.Length, node.Source, context);
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
    }
}
