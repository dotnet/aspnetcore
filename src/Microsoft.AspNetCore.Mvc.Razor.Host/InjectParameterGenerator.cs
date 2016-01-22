// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using Microsoft.AspNetCore.Razor.Chunks.Generators;
using Microsoft.AspNetCore.Razor.Parser.SyntaxTree;

namespace Microsoft.AspNetCore.Mvc.Razor
{
    public class InjectParameterGenerator : SpanChunkGenerator
    {
        public InjectParameterGenerator(string typeName, string propertyName)
        {
            TypeName = typeName;
            PropertyName = propertyName;
        }

        public string TypeName { get; private set; }

        public string PropertyName { get; private set; }

        public override void GenerateChunk(Span target, ChunkGeneratorContext context)
        {
            var injectChunk = new InjectChunk(TypeName, PropertyName);
            context.ChunkTreeBuilder.AddChunk(injectChunk, target);
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "@inject {0} {1}", TypeName, PropertyName);
        }

        public override bool Equals(object obj)
        {
            var other = obj as InjectParameterGenerator;
            return other != null &&
                   string.Equals(TypeName, other.TypeName, StringComparison.Ordinal) &&
                   string.Equals(PropertyName, other.PropertyName, StringComparison.Ordinal);
        }

        public override int GetHashCode()
        {
            return TypeName.GetHashCode() +
                   (PropertyName.GetHashCode() * 13);
        }
    }
}