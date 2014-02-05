// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNet.Razor.Generator.Compiler;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;

namespace Microsoft.AspNet.Razor.Generator
{
    public class ExpressionCodeGenerator : HybridCodeGenerator
    {
        public void GenerateStartBlockCode(SyntaxTreeNode target, CodeTreeBuilder codeTreeBuilder, CodeGeneratorContext context)
        {
            ExpressionBlockChunk chunk = codeTreeBuilder.StartChunkBlock<ExpressionBlockChunk>(target);
        }

        public override void GenerateStartBlockCode(Block target, CodeGeneratorContext context)
        {
#if NET45
            // No CodeDOM + This code will not be needed once we transition to the CodeTree

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
#endif

            // TODO: Make this generate the primary generator
            GenerateStartBlockCode(target, context.CodeTreeBuilder, context);
        }

        public void GenerateEndBlockCode(SyntaxTreeNode target, CodeTreeBuilder codeTreeBuilder, CodeGeneratorContext context)
        {
            codeTreeBuilder.EndChunkBlock();
        }

        public override void GenerateEndBlockCode(Block target, CodeGeneratorContext context)
        {
#if NET45
            // No CodeDOM + This code will not be needed once we transition to the CodeTree

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
#endif

            // TODO: Make this generate the primary generator
            GenerateEndBlockCode(target, context.CodeTreeBuilder, context);
        }

        public void GenerateCode(Span target, CodeTreeBuilder codeTreeBuilder, CodeGeneratorContext context)
        {
            codeTreeBuilder.AddExpressionChunk(target.Content, target);
        }

        public override void GenerateCode(Span target, CodeGeneratorContext context)
        {
#if NET45
            // No CodeDOM + This code will not be needed once we transition to the CodeTree

            Span sourceSpan = null;
            if (context.CreateCodeWriter().SupportsMidStatementLinePragmas || context.ExpressionRenderingMode == ExpressionRenderingMode.WriteToOutput)
            {
                sourceSpan = target;
            }
            context.BufferStatementFragment(target.Content, sourceSpan);
#endif

            // TODO: Make this generate the primary generator
            GenerateCode(target, context.CodeTreeBuilder, context);
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
