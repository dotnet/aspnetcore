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
    public class AttributeBlockCodeGenerator : BlockCodeGenerator
    {
        public AttributeBlockCodeGenerator(string name, LocationTagged<string> prefix, LocationTagged<string> suffix)
        {
            Name = name;
            Prefix = prefix;
            Suffix = suffix;
        }

        public string Name { get; }

        public LocationTagged<string> Prefix { get; }

        public LocationTagged<string> Suffix { get; }

        public override void GenerateStartBlockCode(Block target, CodeGeneratorContext context)
        {
            var chunk = context.CodeTreeBuilder.StartChunkBlock<CodeAttributeChunk>(target);

            chunk.Attribute = Name;
            chunk.Prefix = Prefix;
            chunk.Suffix = Suffix;
        }

        public override void GenerateEndBlockCode(Block target, CodeGeneratorContext context)
        {
            context.CodeTreeBuilder.EndChunkBlock();
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.CurrentCulture, "Attr:{0},{1:F},{2:F}", Name, Prefix, Suffix);
        }

        public override bool Equals(object obj)
        {
            var other = obj as AttributeBlockCodeGenerator;
            return other != null &&
                string.Equals(other.Name, Name, StringComparison.Ordinal) &&
                Equals(other.Prefix, Prefix) &&
                Equals(other.Suffix, Suffix);
        }

        public override int GetHashCode()
        {
            return HashCodeCombiner.Start()
                .Add(Name, StringComparer.Ordinal)
                .Add(Prefix)
                .Add(Suffix)
                .CombinedHash;
        }
    }
}
