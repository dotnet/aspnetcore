// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.Parser.SyntaxTree;

namespace Microsoft.AspNet.Razor.Chunks.Generators
{
    public abstract class HybridChunkGenerator : ISpanChunkGenerator, IParentChunkGenerator
    {
        public virtual void GenerateStartParentChunk(Block target, ChunkGeneratorContext context)
        {
        }

        public virtual void GenerateEndParentChunk(Block target, ChunkGeneratorContext context)
        {
        }

        public virtual void GenerateChunk(Span target, ChunkGeneratorContext context)
        {
        }
    }
}
