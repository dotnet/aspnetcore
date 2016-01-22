// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Razor.Chunks.Generators;
using Microsoft.AspNetCore.Razor.Parser.SyntaxTree;

namespace Microsoft.AspNetCore.Razor.Generator
{
    public class ModelChunkGenerator : SpanChunkGenerator
    {
        public ModelChunkGenerator(string modelType)
        {
            ModelType = modelType;
        }

        public string ModelType { get; }

        public override void GenerateChunk(Span target, ChunkGeneratorContext context)
        {
            var modelChunk = new ModelChunk(ModelType);
            context.ChunkTreeBuilder.AddChunk(modelChunk, target, topLevel: true);
        }

        public override string ToString() => ModelType;

        public override bool Equals(object obj)
        {
            var other = obj as ModelChunkGenerator;
            return other != null &&
                string.Equals(ModelType, other.ModelType, StringComparison.Ordinal);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(ModelType);
        }
    }
}