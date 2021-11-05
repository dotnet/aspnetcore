// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

internal class SpanContext
{
    public SpanContext(ISpanChunkGenerator chunkGenerator, SpanEditHandler editHandler)
    {
        ChunkGenerator = chunkGenerator;
        EditHandler = editHandler;
    }

    public ISpanChunkGenerator ChunkGenerator { get; }

    public SpanEditHandler EditHandler { get; }
}

internal class SpanContextBuilder
{
    public SpanContextBuilder()
    {
        Reset();
    }

    public SpanContextBuilder(SpanContext context)
    {
        EditHandler = context.EditHandler;
        ChunkGenerator = context.ChunkGenerator;
    }

    public ISpanChunkGenerator ChunkGenerator { get; set; }

    public SpanEditHandler EditHandler { get; set; }

    public SpanContext Build()
    {
        var result = new SpanContext(ChunkGenerator, EditHandler);
        Reset();
        return result;
    }

    public void Reset()
    {
        EditHandler = SpanEditHandler.CreateDefault((content) => Enumerable.Empty<SyntaxToken>());
        ChunkGenerator = SpanChunkGenerator.Null;
    }
}
