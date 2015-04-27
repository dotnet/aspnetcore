// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.Generator.Compiler;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;

namespace Microsoft.AspNet.Razor.Generator
{
    public class ExpressionCodeGenerator : HybridCodeGenerator
    {
        private static readonly int TypeHashCode = typeof(ExpressionCodeGenerator).GetHashCode();

        public override void GenerateStartBlockCode(Block target, CodeGeneratorContext context)
        {
            context.CodeTreeBuilder.StartChunkBlock<ExpressionBlockChunk>(target);
        }

        public override void GenerateCode(Span target, CodeGeneratorContext context)
        {
            context.CodeTreeBuilder.AddExpressionChunk(target.Content, target);
        }

        public override void GenerateEndBlockCode(Block target, CodeGeneratorContext context)
        {
            context.CodeTreeBuilder.EndChunkBlock();
        }

        public override string ToString()
        {
            return "Expr";
        }

        public override bool Equals(object obj)
        {
            return obj != null &&
                GetType() == obj.GetType();
        }

        public override int GetHashCode()
        {
            return TypeHashCode;
        }
    }
}
