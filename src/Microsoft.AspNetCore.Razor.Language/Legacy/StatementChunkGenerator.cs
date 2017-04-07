// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    internal class StatementChunkGenerator : SpanChunkGenerator
    {
        public override void Accept(ParserVisitor visitor, Span span)
        {
            visitor.VisitStatementSpan(this, span);
        }

        public override void GenerateChunk(Span target, ChunkGeneratorContext context)
        {
            //context.ChunkTreeBuilder.AddStatementChunk(target.Content, target);
        }

        public override string ToString()
        {
            return "Stmt";
        }
    }
}
