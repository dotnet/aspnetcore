// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Globalization;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Text;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Razor.Chunks.Generators
{
    public class LiteralAttributeChunkGenerator : SpanChunkGenerator
    {
        public LiteralAttributeChunkGenerator(
            LocationTagged<string> prefix,
            LocationTagged<SpanChunkGenerator> valueGenerator)
        {
            Prefix = prefix;
            ValueGenerator = valueGenerator;
        }

        public LiteralAttributeChunkGenerator(LocationTagged<string> prefix, LocationTagged<string> value)
        {
            Prefix = prefix;
            Value = value;
        }

        public LocationTagged<string> Prefix { get; }

        public LocationTagged<string> Value { get; }

        public LocationTagged<SpanChunkGenerator> ValueGenerator { get; }

        public override void GenerateChunk(Span target, ChunkGeneratorContext context)
        {
            var chunk = context.ChunkTreeBuilder.StartParentChunk<LiteralCodeAttributeChunk>(target);
            chunk.Prefix = Prefix;
            chunk.Value = Value;

            if (ValueGenerator != null)
            {
                chunk.ValueLocation = ValueGenerator.Location;

                ValueGenerator.Value.GenerateChunk(target, context);

                chunk.ValueLocation = ValueGenerator.Location;
            }

            context.ChunkTreeBuilder.EndParentChunk();
        }

        public override string ToString()
        {
            if (ValueGenerator == null)
            {
                return string.Format(CultureInfo.CurrentCulture, "LitAttr:{0:F},{1:F}", Prefix, Value);
            }
            else
            {
                return string.Format(CultureInfo.CurrentCulture, "LitAttr:{0:F},<Sub:{1:F}>", Prefix, ValueGenerator);
            }
        }

        public override bool Equals(object obj)
        {
            var other = obj as LiteralAttributeChunkGenerator;
            return other != null &&
                Equals(other.Prefix, Prefix) &&
                Equals(other.Value, Value) &&
                Equals(other.ValueGenerator, ValueGenerator);
        }

        public override int GetHashCode()
        {
            var hashCodeCombiner = HashCodeCombiner.Start();

            hashCodeCombiner.Add(Prefix);
            hashCodeCombiner.Add(Value);
            hashCodeCombiner.Add(ValueGenerator);

            return hashCodeCombiner;
        }
    }
}
