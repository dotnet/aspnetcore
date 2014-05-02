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
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;

namespace Microsoft.AspNet.Razor.Generator
{
    public class RazorDirectiveAttributeCodeGenerator : SpanCodeGenerator
    {
        public RazorDirectiveAttributeCodeGenerator(string name, string value)
        {
            if (String.IsNullOrEmpty(name))
            {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "name");
            }
            Name = name;
            Value = value ?? String.Empty; // Coerce to empty string if it was null.
        }

        public string Name { get; private set; }

        public string Value { get; private set; }

        public override void GenerateCode(Span target, CodeGeneratorContext context)
        {
            if (Name == SyntaxConstants.CSharp.SessionStateKeyword)
            {
                context.CodeTreeBuilder.AddSessionStateChunk(Value, target);
            }
        }

        public override string ToString()
        {
            return "Directive: " + Name + ", Value: " + Value;
        }

        public override bool Equals(object obj)
        {
            RazorDirectiveAttributeCodeGenerator other = obj as RazorDirectiveAttributeCodeGenerator;
            return other != null &&
                   Name.Equals(other.Name, StringComparison.OrdinalIgnoreCase) &&
                   Value.Equals(other.Value, StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode()
        {
            return Tuple.Create(Name.ToUpperInvariant(), Value.ToUpperInvariant())
                .GetHashCode();
        }
    }
}
