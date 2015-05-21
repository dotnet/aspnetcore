// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.Parser.SyntaxTree;

namespace Microsoft.AspNet.Razor.Chunks.Generators
{
    public class ExpressionChunkGenerator : HybridChunkGenerator
    {
        private static readonly int TypeHashCode = typeof(ExpressionChunkGenerator).GetHashCode();

        public override void GenerateStartParentChunk(Block target, ChunkGeneratorContext context)
        {
            context.ChunkTreeBuilder.StartParentChunk<ExpressionBlockChunk>(target);
        }

        public override void GenerateChunk(Span target, ChunkGeneratorContext context)
        {
            context.ChunkTreeBuilder.AddExpressionChunk(target.Content, target);
        }

        public override void GenerateEndParentChunk(Block target, ChunkGeneratorContext context)
        {
            context.ChunkTreeBuilder.EndParentChunk();
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
