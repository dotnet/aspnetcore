// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.Evolution.Legacy
{
    internal class SectionChunkGenerator : ParentChunkGenerator
    {
        public SectionChunkGenerator(string sectionName)
        {
            SectionName = sectionName;
        }

        public string SectionName { get; }

        public override void AcceptStart(ParserVisitor visitor, Block block)
        {
            visitor.VisitStartSectionBlock(this, block);
        }

        public override void AcceptEnd(ParserVisitor visitor, Block block)
        {
            visitor.VisitEndSectionBlock(this, block);
        }

        public override void GenerateStartParentChunk(Block target, ChunkGeneratorContext context)
        {
            //var chunk = context.ChunkTreeBuilder.StartParentChunk<SectionChunk>(target);

            //chunk.Name = SectionName;
        }

        public override void GenerateEndParentChunk(Block target, ChunkGeneratorContext context)
        {
            //context.ChunkTreeBuilder.EndParentChunk();
        }

        public override bool Equals(object obj)
        {
            var other = obj as SectionChunkGenerator;
            return base.Equals(other) &&
                string.Equals(SectionName, other.SectionName, StringComparison.Ordinal);
        }

        public override int GetHashCode()
        {
            return SectionName == null ? 0 : StringComparer.Ordinal.GetHashCode(SectionName);
        }

        public override string ToString()
        {
            return "Section:" + SectionName;
        }
    }
}
