// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Parser.SyntaxTree;

namespace Microsoft.AspNetCore.Razor.Chunks
{
    public class Chunk
    {
        public SourceLocation Start { get; set; }
        public SyntaxTreeNode Association { get; set; }
    }
}
