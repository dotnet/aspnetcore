// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;

namespace Microsoft.AspNet.Razor.Generator
{
    public class SetLayoutCodeGenerator : SpanCodeGenerator
    {
        public SetLayoutCodeGenerator(string layoutPath)
        {
            LayoutPath = layoutPath;
        }

        public string LayoutPath { get; set; }

        public override void GenerateCode(Span target, CodeGeneratorContext context)
        {
            context.CodeTreeBuilder.AddSetLayoutChunk(LayoutPath, target);
        }

        public override string ToString()
        {
            return "Layout: " + LayoutPath;
        }

        public override bool Equals(object obj)
        {
            SetLayoutCodeGenerator other = obj as SetLayoutCodeGenerator;
            return other != null && String.Equals(other.LayoutPath, LayoutPath, StringComparison.Ordinal);
        }

        public override int GetHashCode()
        {
            return LayoutPath.GetHashCode();
        }
    }
}
