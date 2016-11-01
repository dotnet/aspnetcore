// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Evolution.Legacy
{
    internal interface ISpanChunkGenerator
    {
        void GenerateChunk(Span target, ChunkGeneratorContext context);

        void Accept(ParserVisitor visitor, Span span);
    }
}
