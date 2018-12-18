// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Globalization;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    internal class LiteralAttributeChunkGenerator : SpanChunkGenerator
    {
        public LiteralAttributeChunkGenerator(LocationTagged<string> prefix, LocationTagged<string> value)
        {
            Prefix = prefix;
            Value = value;
        }

        public LocationTagged<string> Prefix { get; }

        public LocationTagged<string> Value { get; }

        public override void Accept(ParserVisitor visitor, Span span)
        {
            visitor.VisitLiteralAttributeSpan(this, span);
        }

        public override void GenerateChunk(Span target, ChunkGeneratorContext context)
        {
            //var chunk = context.ChunkTreeBuilder.StartParentChunk<LiteralCodeAttributeChunk>(target);
            //chunk.Prefix = Prefix;
            //chunk.Value = Value;

            //if (ValueGenerator != null)
            //{
            //    chunk.ValueLocation = ValueGenerator.Location;

            //    ValueGenerator.Value.GenerateChunk(target, context);

            //    chunk.ValueLocation = ValueGenerator.Location;
            //}

            //context.ChunkTreeBuilder.EndParentChunk();
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.CurrentCulture, "LitAttr:{0:F}", Prefix);
        }

        public override bool Equals(object obj)
        {
            var other = obj as LiteralAttributeChunkGenerator;
            return other != null &&
                Equals(other.Prefix, Prefix) &&
                Equals(other.Value, Value);
        }

        public override int GetHashCode()
        {
            var hashCodeCombiner = HashCodeCombiner.Start();

            hashCodeCombiner.Add(Prefix);
            hashCodeCombiner.Add(Value);

            return hashCodeCombiner;
        }
    }
}
