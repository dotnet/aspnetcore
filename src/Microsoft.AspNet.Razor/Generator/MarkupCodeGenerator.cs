// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Razor.Generator.Compiler;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;

namespace Microsoft.AspNet.Razor.Generator
{
    public class MarkupCodeGenerator : SpanCodeGenerator
    {
        public void GenerateCode(Span target, CodeTreeBuilder codeTreeBuilder, CodeGeneratorContext context)
        {
            codeTreeBuilder.AddLiteralChunk(target.Content, target);
        }

        public override void GenerateCode(Span target, CodeGeneratorContext context)
        {
            if (!context.Host.DesignTimeMode && String.IsNullOrEmpty(target.Content))
            {
                return;
            }
#if NET45
            // No CodeDOM + This code will not be needed once we transition to the CodeTree

            if (context.Host.EnableInstrumentation)
            {
                context.AddContextCall(target, context.Host.GeneratedClassContext.BeginContextMethodName, isLiteral: true);
            }

            if (!String.IsNullOrEmpty(target.Content) && !context.Host.DesignTimeMode)
            {
                string code = context.BuildCodeString(cw =>
                {
                    if (!String.IsNullOrEmpty(context.TargetWriterName))
                    {
                        cw.WriteStartMethodInvoke(context.Host.GeneratedClassContext.WriteLiteralToMethodName);
                        cw.WriteSnippet(context.TargetWriterName);
                        cw.WriteParameterSeparator();
                    }
                    else
                    {
                        cw.WriteStartMethodInvoke(context.Host.GeneratedClassContext.WriteLiteralMethodName);
                    }
                    cw.WriteStringLiteral(target.Content);
                    cw.WriteEndMethodInvoke();
                    cw.WriteEndStatement();
                });
                context.AddStatement(code);
            }

            if (context.Host.EnableInstrumentation)
            {
                context.AddContextCall(target, context.Host.GeneratedClassContext.EndContextMethodName, isLiteral: true);
            }
#endif

            // TODO: Make this generate the primary generator
            GenerateCode(target, context.CodeTreeBuilder, context);
        }

        public override string ToString()
        {
            return "Markup";
        }

        public override bool Equals(object obj)
        {
            return obj is MarkupCodeGenerator;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
