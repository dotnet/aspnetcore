// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Razor.Evolution.Legacy
{
    internal class AttributeBlockChunkGenerator : ParentChunkGenerator
    {
        public AttributeBlockChunkGenerator(string name, LocationTagged<string> prefix, LocationTagged<string> suffix)
        {
            Name = name;
            Prefix = prefix;
            Suffix = suffix;
        }

        public string Name { get; }

        public LocationTagged<string> Prefix { get; }

        public LocationTagged<string> Suffix { get; }

        public override void GenerateStartParentChunk(Block target, ChunkGeneratorContext context)
        {
            //var chunk = context.ChunkTreeBuilder.StartParentChunk<CodeAttributeChunk>(target);

            //chunk.Attribute = Name;
            //chunk.Prefix = Prefix;
            //chunk.Suffix = Suffix;
        }

        public override void GenerateEndParentChunk(Block target, ChunkGeneratorContext context)
        {
            //context.ChunkTreeBuilder.EndParentChunk();
        }

        public override void AcceptStart(ParserVisitor visitor, Block block)
        {
            visitor.VisitStartAttributeBlock(this, block);
        }

        public override void AcceptEnd(ParserVisitor visitor, Block block)
        {
            visitor.VisitEndAttributeBlock(this, block);
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.CurrentCulture, "Attr:{0},{1:F},{2:F}", Name, Prefix, Suffix);
        }

        public override bool Equals(object obj)
        {
            var other = obj as AttributeBlockChunkGenerator;
            return other != null &&
                string.Equals(other.Name, Name, StringComparison.Ordinal) &&
                Equals(other.Prefix, Prefix) &&
                Equals(other.Suffix, Suffix);
        }

        public override int GetHashCode()
        {
            var hashCodeCombiner = HashCodeCombiner.Start();
            hashCodeCombiner.Add(Name, StringComparer.Ordinal);
            hashCodeCombiner.Add(Prefix);
            hashCodeCombiner.Add(Suffix);

            return hashCodeCombiner;
        }
    }
}
