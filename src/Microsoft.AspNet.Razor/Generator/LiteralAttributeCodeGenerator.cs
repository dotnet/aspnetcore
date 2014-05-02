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
    public class LiteralAttributeCodeGenerator : SpanCodeGenerator
    {
        public LiteralAttributeCodeGenerator(LocationTagged<string> prefix, LocationTagged<SpanCodeGenerator> valueGenerator)
        {
            Prefix = prefix;
            ValueGenerator = valueGenerator;
        }

        public LiteralAttributeCodeGenerator(LocationTagged<string> prefix, LocationTagged<string> value)
        {
            Prefix = prefix;
            Value = value;
        }

        public LocationTagged<string> Prefix { get; private set; }
        public LocationTagged<string> Value { get; private set; }
        public LocationTagged<SpanCodeGenerator> ValueGenerator { get; private set; }

        public override void GenerateCode(Span target, CodeGeneratorContext context)
        {
            var chunk = context.CodeTreeBuilder.StartChunkBlock<LiteralCodeAttributeChunk>(target);
            chunk.Prefix = Prefix;
            chunk.Value = Value;

            if (ValueGenerator != null)
            {
                chunk.ValueLocation = ValueGenerator.Location;

                ValueGenerator.Value.GenerateCode(target, context);

                chunk.ValueLocation = ValueGenerator.Location;
            }

            context.CodeTreeBuilder.EndChunkBlock();
        }

        public override string ToString()
        {
            if (ValueGenerator == null)
            {
                return String.Format(CultureInfo.CurrentCulture, "LitAttr:{0:F},{1:F}", Prefix, Value);
            }
            else
            {
                return String.Format(CultureInfo.CurrentCulture, "LitAttr:{0:F},<Sub:{1:F}>", Prefix, ValueGenerator);
            }
        }

        public override bool Equals(object obj)
        {
            LiteralAttributeCodeGenerator other = obj as LiteralAttributeCodeGenerator;
            return other != null &&
                   Equals(other.Prefix, Prefix) &&
                   Equals(other.Value, Value) &&
                   Equals(other.ValueGenerator, ValueGenerator);
        }

        public override int GetHashCode()
        {
            return HashCodeCombiner.Start()
                .Add(Prefix)
                .Add(Value)
                .Add(ValueGenerator)
                .CombinedHash;
        }
    }
}
