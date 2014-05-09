// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.Text;

namespace Microsoft.AspNet.Razor.Generator.Compiler
{
    public class LiteralCodeAttributeChunk : ChunkBlock
    {
        public string Code { get; set; }
        public LocationTagged<string> Prefix { get; set; }
        public LocationTagged<string> Value { get; set; }
        public SourceLocation ValueLocation { get; set; }
    }
}
