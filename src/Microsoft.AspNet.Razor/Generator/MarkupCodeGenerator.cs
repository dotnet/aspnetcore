// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.Parser.SyntaxTree;

namespace Microsoft.AspNet.Razor.Generator
{
    public class MarkupCodeGenerator : SpanCodeGenerator
    {
        public override void GenerateCode(Span target, CodeGeneratorContext context)
        {
            context.CodeTreeBuilder.AddLiteralChunk(target.Content, target);
        }

        public override string ToString()
        {
            return "Markup";
        }
    }
}
