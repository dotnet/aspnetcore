// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

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

        public string Name { get; private set; }
        public LocationTagged<string> Prefix { get; private set; }
        public LocationTagged<string> Suffix { get; private set; }

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
            return String.Format(CultureInfo.CurrentCulture, "Attr:{0},{1:F},{2:F}", Name, Prefix, Suffix);
        }

        public override bool Equals(object obj)
        {
            AttributeBlockCodeGenerator other = obj as AttributeBlockCodeGenerator;
            return other != null &&
                   String.Equals(other.Name, Name, StringComparison.Ordinal) &&
                   Equals(other.Prefix, Prefix) &&
                   Equals(other.Suffix, Suffix);
        }

        public override int GetHashCode()
        {
            return HashCodeCombiner.Start()
                .Add(Name)
                .Add(Prefix)
                .Add(Suffix)
                .CombinedHash;
        }
    }
}
