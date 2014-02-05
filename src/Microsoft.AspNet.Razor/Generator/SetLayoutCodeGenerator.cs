// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.CodeDom;
using Microsoft.AspNet.Razor.Generator.Compiler;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;

namespace Microsoft.AspNet.Razor.Generator
{
    public class SetLayoutCodeGenerator : SpanCodeGenerator
    {
        public SetLayoutCodeGenerator(string layoutPath)
        {
            LayoutPath = layoutPath;
        }

        public string LayoutPath { get; set; }

        public void GenerateCode(SyntaxTreeNode target, CodeTreeBuilder codeTreeBuilder, CodeGeneratorContext context)
        {
            codeTreeBuilder.AddSetLayoutChunk(LayoutPath, target);
        }

        public override void GenerateCode(Span target, CodeGeneratorContext context)
        {
#if NET45
            // No CodeDOM

            if (!context.Host.DesignTimeMode && !String.IsNullOrEmpty(context.Host.GeneratedClassContext.LayoutPropertyName))
            {
                context.TargetMethod.Statements.Add(
                    new CodeAssignStatement(
                        new CodePropertyReferenceExpression(null, context.Host.GeneratedClassContext.LayoutPropertyName),
                        new CodePrimitiveExpression(LayoutPath)));
            }
#endif

            // TODO: Make this generate the primary generator
            GenerateCode(target, context.CodeTreeBuilder, context);
        }

        public override string ToString()
        {
            return "Layout: " + LayoutPath;
        }

        public override bool Equals(object obj)
        {
            SetLayoutCodeGenerator other = obj as SetLayoutCodeGenerator;
            return other != null && String.Equals(other.LayoutPath, LayoutPath, StringComparison.Ordinal);
        }

        public override int GetHashCode()
        {
            return LayoutPath.GetHashCode();
        }
    }
}
