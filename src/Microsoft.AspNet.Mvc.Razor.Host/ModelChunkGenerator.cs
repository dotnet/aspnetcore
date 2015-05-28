// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc.Razor;
using Microsoft.AspNet.Razor.Chunks.Generators;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;

namespace Microsoft.AspNet.Razor.Generator
{
    public class ModelChunkGenerator : SpanChunkGenerator
    {
        public ModelChunkGenerator(string baseType, string modelType)
        {
            BaseType = baseType;
            ModelType = modelType;
        }

        public string BaseType { get; private set; }
        public string ModelType { get; private set; }

        public override void GenerateChunk(Span target, ChunkGeneratorContext context)
        {
            var modelChunk = new ModelChunk(BaseType, ModelType);
            context.ChunkTreeBuilder.AddChunk(modelChunk, target, topLevel: true);
        }

        public override string ToString()
        {
            return BaseType + "<" + ModelType + ">";
        }

        public override bool Equals(object obj)
        {
            var other = obj as ModelChunkGenerator;
            return other != null &&
                   string.Equals(ModelType, other.ModelType, StringComparison.Ordinal);
        }

        public override int GetHashCode()
        {
            return ModelType.GetHashCode();
        }
    }
}