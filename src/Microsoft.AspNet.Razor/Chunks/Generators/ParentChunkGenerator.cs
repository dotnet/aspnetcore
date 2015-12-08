// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.Parser.SyntaxTree;

namespace Microsoft.AspNet.Razor.Chunks.Generators
{
    public abstract class ParentChunkGenerator : IParentChunkGenerator
    {
        private static readonly int TypeHashCode = typeof(ParentChunkGenerator).GetHashCode();

        public static readonly IParentChunkGenerator Null = new NullParentChunkGenerator();

        public virtual void GenerateStartParentChunk(Block target, ChunkGeneratorContext context)
        {
        }

        public virtual void GenerateEndParentChunk(Block target, ChunkGeneratorContext context)
        {
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

        private class NullParentChunkGenerator : IParentChunkGenerator
        {
            public void GenerateStartParentChunk(Block target, ChunkGeneratorContext context)
            {
            }

            public void GenerateEndParentChunk(Block target, ChunkGeneratorContext context)
            {
            }

            public override string ToString()
            {
                return "None";
            }
        }
    }
}
