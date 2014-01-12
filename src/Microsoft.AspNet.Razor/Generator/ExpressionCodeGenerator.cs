// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;

namespace Microsoft.AspNet.Razor.Generator
{
    public class ExpressionCodeGenerator : HybridCodeGenerator
    {
        public override void GenerateStartBlockCode(Block target, CodeGeneratorContext context)
        {
            if (context.Host.EnableInstrumentation && context.ExpressionRenderingMode == ExpressionRenderingMode.WriteToOutput)
            {
                Span contentSpan = target.Children
                    .OfType<Span>()
                    .Where(s => s.Kind == SpanKind.Code || s.Kind == SpanKind.Markup)
                    .FirstOrDefault();

                if (contentSpan != null)
                {
                    context.AddContextCall(contentSpan, context.Host.GeneratedClassContext.BeginContextMethodName, false);
                }
            }

            string writeInvocation = context.BuildCodeString(cw =>
            {
                if (context.Host.DesignTimeMode)
                {
                    context.EnsureExpressionHelperVariable();
                    cw.WriteStartAssignment("__o");
                }
                else if (context.ExpressionRenderingMode == ExpressionRenderingMode.WriteToOutput)
                {
                    if (!String.IsNullOrEmpty(context.TargetWriterName))
                    {
                        cw.WriteStartMethodInvoke(context.Host.GeneratedClassContext.WriteToMethodName);
                        cw.WriteSnippet(context.TargetWriterName);
                        cw.WriteParameterSeparator();
                    }
                    else
                    {
                        cw.WriteStartMethodInvoke(context.Host.GeneratedClassContext.WriteMethodName);
                    }
                }
            });

            context.BufferStatementFragment(writeInvocation);
            context.MarkStartOfGeneratedCode();
        }

        public override void GenerateEndBlockCode(Block target, CodeGeneratorContext context)
        {
            string endBlock = context.BuildCodeString(cw =>
            {
                if (context.ExpressionRenderingMode == ExpressionRenderingMode.WriteToOutput)
                {
                    if (!context.Host.DesignTimeMode)
                    {
                        cw.WriteEndMethodInvoke();
                    }
                    cw.WriteEndStatement();
                }
                else
                {
                    cw.WriteLineContinuation();
                }
            });

            context.MarkEndOfGeneratedCode();
            context.BufferStatementFragment(endBlock);
            context.FlushBufferedStatement();

            if (context.Host.EnableInstrumentation && context.ExpressionRenderingMode == ExpressionRenderingMode.WriteToOutput)
            {
                Span contentSpan = target.Children
                    .OfType<Span>()
                    .Where(s => s.Kind == SpanKind.Code || s.Kind == SpanKind.Markup)
                    .FirstOrDefault();

                if (contentSpan != null)
                {
                    context.AddContextCall(contentSpan, context.Host.GeneratedClassContext.EndContextMethodName, false);
                }
            }
        }

        public override void GenerateCode(Span target, CodeGeneratorContext context)
        {
            Span sourceSpan = null;
            if (context.CreateCodeWriter().SupportsMidStatementLinePragmas || context.ExpressionRenderingMode == ExpressionRenderingMode.WriteToOutput)
            {
                sourceSpan = target;
            }
            context.BufferStatementFragment(target.Content, sourceSpan);
        }

        public override string ToString()
        {
            return "Expr";
        }

        public override bool Equals(object obj)
        {
            return obj is ExpressionCodeGenerator;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
