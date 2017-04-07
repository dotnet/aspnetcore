// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    internal class ExpressionChunkGenerator : ISpanChunkGenerator, IParentChunkGenerator
    {
        private static readonly int TypeHashCode = typeof(ExpressionChunkGenerator).GetHashCode();

        public void GenerateStartParentChunk(Block target, ChunkGeneratorContext context)
        {
            //context.ChunkTreeBuilder.StartParentChunk<ExpressionBlockChunk>(target);
        }

        public void GenerateChunk(Span target, ChunkGeneratorContext context)
        {
            //context.ChunkTreeBuilder.AddExpressionChunk(target.Content, target);
        }

        public void GenerateEndParentChunk(Block target, ChunkGeneratorContext context)
        {
            //context.ChunkTreeBuilder.EndParentChunk();
        }

        public void Accept(ParserVisitor visitor, Span span)
        {
            visitor.VisitExpressionSpan(this, span);
        }

        public void Accept(ParserVisitor visitor, Block block)
        {
            visitor.VisitExpressionBlock(this, block);
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
