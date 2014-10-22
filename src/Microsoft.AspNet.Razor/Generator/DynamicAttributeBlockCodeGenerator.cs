// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using Microsoft.AspNet.Razor.Generator.Compiler;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Text;
using Microsoft.Internal.Web.Utils;

namespace Microsoft.AspNet.Razor.Generator
{
    public class DynamicAttributeBlockCodeGenerator : BlockCodeGenerator
    {
        public DynamicAttributeBlockCodeGenerator(LocationTagged<string> prefix, int offset, int line, int col)
            : this(prefix, new SourceLocation(offset, line, col))
        {
        }

        public DynamicAttributeBlockCodeGenerator(LocationTagged<string> prefix, SourceLocation valueStart)
        {
            Prefix = prefix;
            ValueStart = valueStart;
        }

        public LocationTagged<string> Prefix { get; private set; }
        public SourceLocation ValueStart { get; private set; }

        public override void GenerateStartBlockCode(Block target, CodeGeneratorContext context)
        {
            var chunk = context.CodeTreeBuilder.StartChunkBlock<DynamicCodeAttributeChunk>(target);
            chunk.Start = ValueStart;
            chunk.Prefix = Prefix;
        }

        public override void GenerateEndBlockCode(Block target, CodeGeneratorContext context)
        {
            context.CodeTreeBuilder.EndChunkBlock();
        }

        public override string ToString()
        {
            return String.Format(CultureInfo.CurrentCulture, "DynAttr:{0:F}", Prefix);
        }

        public override bool Equals(object obj)
        {
            var other = obj as DynamicAttributeBlockCodeGenerator;
            return other != null &&
                   Equals(other.Prefix, Prefix);
        }

        public override int GetHashCode()
        {
            return HashCodeCombiner.Start()
                .Add(Prefix)
                .CombinedHash;
        }
    }
}
