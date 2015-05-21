// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.Parser.SyntaxTree;

namespace Microsoft.AspNet.Razor.Chunks.Generators
{
    public class ResolveUrlChunkGenerator : SpanChunkGenerator
    {
        public override void GenerateChunk(Span target, ChunkGeneratorContext context)
        {
            // Check if the host supports it
            if (string.IsNullOrEmpty(context.Host.GeneratedClassContext.ResolveUrlMethodName))
            {
                // Nope, just use the default MarkupChunkGenerator behavior
                new MarkupChunkGenerator().GenerateChunk(target, context);
                return;
            }

            context.ChunkTreeBuilder.AddResolveUrlChunk(target.Content, target);
        }

        public override string ToString()
        {
            return "VirtualPath";
        }
    }
}
