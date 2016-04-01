// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.Parser.SyntaxTree;

namespace Microsoft.AspNet.Razor.Chunks.Generators
{
    public interface ISpanChunkGenerator
    {
        void GenerateChunk(Span target, ChunkGeneratorContext context);
    }
}
